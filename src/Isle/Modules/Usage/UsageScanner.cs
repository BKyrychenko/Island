using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Isle.Core;

namespace Isle.Modules.Usage;

/// <summary>What is kept about one transcript file: where reading stopped, which messages were counted, and the sums.</summary>
public sealed class FileState
{
    public long Offset { get; set; }
    /// <summary>Creation time (ticks): a different value means the path now holds another file.</summary>
    public long Created { get; set; }
    public string Session { get; set; } = "";
    public string Slug { get; set; } = "";
    /// <summary>Last folder name of the working directory (never the full path).</summary>
    public string Cwd { get; set; } = "";
    /// <summary>First / last usage timestamp, unix ms.</summary>
    public long First { get; set; }
    public long Last { get; set; }
    /// <summary>Local day (yyyy-MM-dd) → model id → tokens.</summary>
    public Dictionary<string, Dictionary<string, Tokens>> Days { get; set; } = [];
    /// <summary>64-bit hashes of the (message.id, requestId) pairs already counted, packed as ulongs.</summary>
    public byte[] Seen { get; set; } = [];
}

public sealed class UsageData
{
    public const int CurrentVersion = 1;
    public int Version { get; set; } = CurrentVersion;
    public Dictionary<string, FileState> Files { get; set; } = [];

    public static UsageData Load(string path)
    {
        var d = JsonStore.Load<UsageData>(path);
        return d.Version == CurrentVersion ? d : new UsageData();
    }
}

/// <summary>One assistant usage line. Only counters, model, time and the leaf folder of cwd: never message content.</summary>
internal readonly record struct UsageLine(string Model, long Time, string Cwd, ulong Key, Tokens Tokens);

internal readonly record struct ScanStats(int Files, int FilesRead, long Messages, long Bytes);

internal static class UsageScanner
{
    const int ChunkSize = 1 << 20;

    public static string DefaultProjectsDir()
    {
        var env = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");
        var root = string.IsNullOrWhiteSpace(env) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude") : env;
        return Path.Combine(root, "projects");
    }

    // ---- line parsing ----

    /// <summary>The usage of one transcript line, or null when it is not an assistant usage line (or is malformed).</summary>
    public static UsageLine? Parse(ReadOnlyMemory<byte> line)
    {
        var span = line.Span;
        // Cheap prefilter: most lines are user/tool lines and are never parsed.
        if (span.IndexOf("\"usage\""u8) < 0 || span.IndexOf("\"assistant\""u8) < 0) return null;
        try
        {
            using var doc = JsonDocument.Parse(line, new JsonDocumentOptions { MaxDepth = 256 });
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || Str(root, "type") != "assistant") return null;
            if (!root.TryGetProperty("message", out var msg) || msg.ValueKind != JsonValueKind.Object) return null;
            if (!msg.TryGetProperty("usage", out var u) || u.ValueKind != JsonValueKind.Object) return null;
            if (!root.TryGetProperty("timestamp", out var ts) || ts.ValueKind != JsonValueKind.String || !ts.TryGetDateTimeOffset(out var time)) return null;

            var t = new Tokens { In = Num(u, "input_tokens"), Out = Num(u, "output_tokens"), CacheRead = Num(u, "cache_read_input_tokens") };
            var write = Num(u, "cache_creation_input_tokens");
            if (u.TryGetProperty("cache_creation", out var cc) && cc.ValueKind == JsonValueKind.Object)
            {
                t.Write5m = Num(cc, "ephemeral_5m_input_tokens");
                t.Write1h = Num(cc, "ephemeral_1h_input_tokens");
            }
            if (write > t.CacheWrite) t.Write5m += write - t.CacheWrite; // no TTL split reported: treat the rest as 5-minute writes
            if (t.Total == 0) return null; // "<synthetic>" error messages and the like

            var id = Str(msg, "id");
            var request = Str(root, "requestId");
            var key = string.IsNullOrEmpty(id) && string.IsNullOrEmpty(request) ? 0 : Hash(id + "|" + request);
            return new UsageLine(Str(msg, "model") ?? "", time.ToUnixTimeMilliseconds(), Leaf(Str(root, "cwd")), key, t);
        }
        catch (JsonException) { return null; }
    }

    static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    static long Num(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n) && n > 0 ? n : 0;

    static string Leaf(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        var p = path.TrimEnd('\\', '/');
        return p[(p.LastIndexOfAny(['\\', '/']) + 1)..];
    }

    /// <summary>FNV-1a, never 0 (0 means "no key").</summary>
    static ulong Hash(string s)
    {
        var h = 14695981039346656037UL;
        foreach (var c in s) h = (h ^ c) * 1099511628211UL;
        return h == 0 ? 1 : h;
    }

    // ---- scanning ----

    /// <summary>Reads what was appended to every transcript since the last scan into <paramref name="data"/>.</summary>
    public static ScanStats Scan(string projectsDir, UsageData data, CancellationToken ct = default)
    {
        int files = 0, read = 0;
        long messages = 0, bytes = 0;
        var present = new HashSet<string>(StringComparer.Ordinal);
        if (Directory.Exists(projectsDir))
        {
            // ponytail: one enumeration of the whole tree; fine for a few hundred files.
            foreach (var path in Directory.EnumerateFiles(projectsDir, "*.jsonl", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                var parts = Path.GetRelativePath(projectsDir, path).Split(Path.DirectorySeparatorChar);
                // <slug>/<session>.jsonl, or <slug>/<session>/subagents/agent-*.jsonl (counted for the parent session)
                var session = parts switch
                {
                    [_, var f] => Path.GetFileNameWithoutExtension(f),
                    [_, var s, "subagents", _] => s,
                    _ => null,
                };
                if (session is null) continue;
                var key = string.Join('/', parts);
                present.Add(key);
                files++;
                try
                {
                    var (n, b) = ScanFile(path, key, session, parts[0], data, ct);
                    if (b > 0) read++;
                    messages += n;
                    bytes += b;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Logger.Warn($"usage: could not read a transcript ({ex.GetType().Name})"); }
            }
        }
        foreach (var gone in data.Files.Keys.Where(k => !present.Contains(k)).ToList()) { data.Files.Remove(gone); read++; }
        return new ScanStats(files, read, messages, bytes);
    }

    static (long Messages, long Bytes) ScanFile(string path, string key, string session, string slug, UsageData data, CancellationToken ct)
    {
        var info = new FileInfo(path);
        var created = info.CreationTimeUtc.Ticks;
        if (!data.Files.TryGetValue(key, out var st) || st.Created != created || info.Length < st.Offset)
            data.Files[key] = st = new FileState { Created = created, Session = session, Slug = slug };
        if (info.Length <= st.Offset) return (0, 0);

        HashSet<ulong>? seen = null;
        long messages = 0, consumed = 0;
        var buf = new byte[ChunkSize];
        var have = 0;
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1, FileOptions.SequentialScan);
        try
        {
            fs.Seek(st.Offset, SeekOrigin.Begin);
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                if (have == buf.Length) Array.Resize(ref buf, buf.Length * 2); // one line longer than the buffer
                var n = fs.Read(buf, have, buf.Length - have);
                if (n == 0) break;
                have += n;
                var start = 0;
                int nl;
                // Only complete lines are consumed: the last one may still be being written.
                while ((nl = Array.IndexOf(buf, (byte)'\n', start, have - start)) >= 0)
                {
                    if (Parse(buf.AsMemory(start, nl - start)) is { } line)
                    {
                        seen ??= Unpack(st.Seen);
                        if (Apply(st, seen, line)) messages++;
                    }
                    consumed += nl + 1 - start;
                    start = nl + 1;
                }
                Buffer.BlockCopy(buf, start, buf, 0, have - start);
                have -= start;
            }
        }
        finally
        {
            // also on cancellation or an I/O error: offset and the seen set must match what was counted
            st.Offset += consumed;
            if (seen is not null && messages > 0) st.Seen = MemoryMarshal.AsBytes(seen.ToArray().AsSpan()).ToArray();
        }
        return (messages, consumed);
    }

    static HashSet<ulong> Unpack(byte[] seen) => [.. MemoryMarshal.Cast<byte, ulong>(seen)];

    /// <summary>Counts the line unless its (message.id, requestId) was seen before. False when it was a duplicate.</summary>
    static bool Apply(FileState st, HashSet<ulong> seen, UsageLine l)
    {
        if (l.Key != 0 && !seen.Add(l.Key)) return false;
        var day = DateTimeOffset.FromUnixTimeMilliseconds(l.Time).ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (!st.Days.TryGetValue(day, out var models)) st.Days[day] = models = [];
        if (!models.TryGetValue(l.Model, out var t)) models[l.Model] = t = new Tokens();
        t.Add(l.Tokens);
        if (st.First == 0 || l.Time < st.First) st.First = l.Time;
        if (l.Time > st.Last) st.Last = l.Time;
        if (st.Cwd.Length == 0) st.Cwd = l.Cwd;
        return true;
    }
}

/// <summary>Holds the persisted scan state; one refresh at a time (the view guards that).</summary>
public sealed class UsageService(string projectsDir, string storePath)
{
    UsageData? _data;

    /// <summary>Last finished report (null before the first scan).</summary>
    public UsageReport? Report { get; private set; }

    /// <summary>Scans on a worker thread: reads only appended bytes, saves the state when something changed.</summary>
    public Task<UsageReport> RefreshAsync(CancellationToken ct) => Task.Run(() =>
    {
        var sw = Stopwatch.StartNew();
        _data ??= UsageData.Load(storePath);
        var stats = UsageScanner.Scan(projectsDir, _data, ct);
        if (stats.FilesRead > 0) JsonStore.Save(storePath, _data);
        Report = UsageReport.Build(_data, DateTimeOffset.Now);
        Logger.Info($"usage scan: {stats.Files} files ({stats.FilesRead} changed), {stats.Messages} new messages, {sw.ElapsedMilliseconds} ms");
        return Report;
    }, ct);
}
