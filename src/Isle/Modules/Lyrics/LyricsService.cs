using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Isle.Core;

namespace Isle.Modules.Lyrics;

public enum LyricsStatus { Synced, Plain, Instrumental, NotFound, Error }

public sealed record LyricsResult(LyricsStatus Status, IReadOnlyList<LrcLine> Lines, string Plain, DateTimeOffset Fetched)
{
    public static LyricsResult Error() => new(LyricsStatus.Error, [], "", DateTimeOffset.Now);
    public static LyricsResult NotFound(DateTimeOffset now) => new(LyricsStatus.NotFound, [], "", now);
}

public sealed record TrackQuery(string Artist, string Title, string Album, TimeSpan Duration)
{
    public string CacheKey
    {
        get
        {
            var raw = $"{Artist.Trim().ToLowerInvariant()}|{Title.Trim().ToLowerInvariant()}|{Album.Trim().ToLowerInvariant()}|{(int)Math.Round(Duration.TotalSeconds)}";
            return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(raw)))[..20].ToLowerInvariant();
        }
    }
}

/// <summary>Minimal HTTP seam so the LRCLIB logic is testable: returns (status code, body); status 0 = network failure.</summary>
public interface ILyricsHttp
{
    Task<(int Status, string Body)> GetAsync(string url, CancellationToken ct);
}

/// <summary>
/// Synced lyrics from LRCLIB (https://lrclib.net, free, no key): /api/get with full metadata, then /api/search fallback.
/// Results (including "not found") are cached in memory and on disk (%AppData%\Isle\lyrics). The HttpClient is
/// created on first use and disposed after 2 minutes without requests.
/// </summary>
public sealed partial class LyricsService : IDisposable
{
    const string BaseUrl = "https://lrclib.net/api/";
    static readonly TimeSpan NotFoundRetry = TimeSpan.FromDays(7);

    readonly ILyricsHttp _http;
    readonly Func<DateTimeOffset> _now;
    readonly string _cacheDir;
    readonly Dictionary<string, LyricsResult> _memory = [];
    readonly Queue<string> _memoryOrder = new();

    public LyricsService(ILyricsHttp? http = null, string? cacheDir = null, Func<DateTimeOffset>? now = null)
    {
        _http = http ?? new LrclibHttp();
        _cacheDir = cacheDir ?? Path.Combine(Paths.Root, "lyrics");
        _now = now ?? (() => DateTimeOffset.Now);
    }

    public async Task<LyricsResult> GetAsync(TrackQuery q, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q.Title)) return LyricsResult.NotFound(_now());
        var key = q.CacheKey;
        if (_memory.TryGetValue(key, out var mem) && !Expired(mem)) return mem;
        var disk = await Task.Run(() => ReadDisk(key), ct).ConfigureAwait(false);
        if (disk is not null && !Expired(disk))
        {
            Remember(key, disk);
            return disk;
        }

        var result = await FetchAsync(q, ct).ConfigureAwait(false);
        if (result.Status != LyricsStatus.Error)
        {
            Remember(key, result);
            await Task.Run(() => WriteDisk(key, result), CancellationToken.None).ConfigureAwait(false);
        }
        return result;
    }

    bool Expired(LyricsResult result) => result.Status == LyricsStatus.NotFound && _now() - result.Fetched >= NotFoundRetry;

    async Task<LyricsResult> FetchAsync(TrackQuery q, CancellationToken ct)
    {
        var now = _now();
        var url = BaseUrl + "get?" + Query(("artist_name", q.Artist), ("track_name", q.Title), ("album_name", q.Album),
            ("duration", ((int)Math.Round(q.Duration.TotalSeconds)).ToString(CultureInfo.InvariantCulture)));
        if (q.Duration > TimeSpan.Zero && q.Artist.Length > 0)
        {
            var (status, body) = await _http.GetAsync(url, ct).ConfigureAwait(false);
            if (status == 200 && FromRecord(TryParse(body), now) is { } hit && hit.Status != LyricsStatus.NotFound) return hit;
            if (status is 0 or >= 500 or 429) return LyricsResult.Error();
        }

        var title = CleanTitle(q.Title);
        var searchUrl = BaseUrl + "search?" + Query(("track_name", title), ("artist_name", CleanArtist(q.Artist)));
        var (s2, b2) = await _http.GetAsync(searchUrl, ct).ConfigureAwait(false);
        if (s2 == 200) return PickFromSearch(TryParse(b2) as JsonArray, q.Duration, now);
        return s2 is 0 or >= 500 or 429 ? LyricsResult.Error() : LyricsResult.NotFound(now);
    }

    static JsonNode? TryParse(string body)
    {
        try { return JsonNode.Parse(body); }
        catch (JsonException) { return null; }
    }

    /// <summary>Maps one LRCLIB record ({instrumental, syncedLyrics, plainLyrics}) to a result.</summary>
    public static LyricsResult? FromRecord(JsonNode? node, DateTimeOffset now)
    {
        if (node is not JsonObject o) return null;
        if (o["instrumental"]?.GetValueKind() == JsonValueKind.True) return new LyricsResult(LyricsStatus.Instrumental, [], "", now);
        var synced = o["syncedLyrics"]?.GetValueKind() == JsonValueKind.String ? o["syncedLyrics"]!.GetValue<string>() : null;
        var plain = o["plainLyrics"]?.GetValueKind() == JsonValueKind.String ? o["plainLyrics"]!.GetValue<string>() : null;
        var lines = Lrc.Parse(synced);
        if (lines.Count > 0) return new LyricsResult(LyricsStatus.Synced, lines, plain ?? "", now);
        if (!string.IsNullOrWhiteSpace(plain)) return new LyricsResult(LyricsStatus.Plain, [], plain.Trim(), now);
        return LyricsResult.NotFound(now);
    }

    /// <summary>Best search hit: synced first, then closest duration (within 5 s when the duration is known).</summary>
    public static LyricsResult PickFromSearch(JsonArray? items, TimeSpan duration, DateTimeOffset now)
    {
        if (items is null || items.Count == 0) return LyricsResult.NotFound(now);
        JsonObject? best = null;
        var bestScore = double.MinValue;
        foreach (var item in items)
        {
            if (item is not JsonObject o) continue;
            var d = o["duration"]?.GetValueKind() == JsonValueKind.Number ? o["duration"]!.GetValue<double>() : 0;
            var diff = duration > TimeSpan.Zero && d > 0 ? Math.Abs(d - duration.TotalSeconds) : 0;
            if (duration > TimeSpan.Zero && d > 0 && diff > 5) continue;
            var hasSynced = o["syncedLyrics"]?.GetValueKind() == JsonValueKind.String;
            var instrumental = o["instrumental"]?.GetValueKind() == JsonValueKind.True;
            var score = (hasSynced ? 100 : 0) + (instrumental ? 10 : 0) - diff;
            if (score > bestScore) { bestScore = score; best = o; }
        }
        return FromRecord(best, now) ?? LyricsResult.NotFound(now);
    }

    [GeneratedRegex(@"\s*[\(\[](?:feat\.?|ft\.?|with|official|lyric|lyrics|audio|video|visuali[sz]er|remaster(?:ed)?|live|hd|hq|mv|explicit)[^\)\]]*[\)\]]", RegexOptions.IgnoreCase)]
    private static partial Regex Decorations();

    [GeneratedRegex(@"\s+-\s+(?:\d{4}\s+)?remaster(?:ed)?(?:\s+\d{4})?.*$", RegexOptions.IgnoreCase)]
    private static partial Regex RemasterSuffix();

    /// <summary>"Song (feat. X) [Official Video] - Remastered 2011" → "Song".</summary>
    public static string CleanTitle(string title)
    {
        var t = Decorations().Replace(title ?? "", "");
        t = RemasterSuffix().Replace(t, "");
        return t.Trim();
    }

    /// <summary>"ArtistVEVO" / "Artist - Topic" (YouTube channel names) → "Artist".</summary>
    public static string CleanArtist(string artist)
    {
        var a = (artist ?? "").Trim();
        if (a.EndsWith(" - Topic", StringComparison.OrdinalIgnoreCase)) a = a[..^8];
        if (a.EndsWith("VEVO", StringComparison.Ordinal)) a = a[..^4];
        return a.Trim();
    }

    static string Query(params (string Key, string Value)[] pairs) =>
        string.Join("&", pairs.Where(p => !string.IsNullOrWhiteSpace(p.Value)).Select(p => p.Key + "=" + Uri.EscapeDataString(p.Value.Trim())));

    void Remember(string key, LyricsResult r)
    {
        if (!_memory.ContainsKey(key)) _memoryOrder.Enqueue(key);
        _memory[key] = r;
        while (_memoryOrder.Count > 30) _memory.Remove(_memoryOrder.Dequeue());
    }

    sealed record DiskEntry(string Status, string? Synced, string? Plain, DateTimeOffset Fetched);

    LyricsResult? ReadDisk(string key)
    {
        var path = Path.Combine(_cacheDir, key + ".json");
        try
        {
            if (!File.Exists(path)) return null;
            var e = JsonSerializer.Deserialize<DiskEntry>(File.ReadAllText(path), JsonStore.Options);
            if (e is null || !Enum.TryParse<LyricsStatus>(e.Status, out var st)) return null;
            return new LyricsResult(st, st == LyricsStatus.Synced ? Lrc.Parse(e.Synced) : [], e.Plain ?? "", e.Fetched);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            try { File.Delete(path); } catch { /* cache only */ }
            return null;
        }
    }

    void WriteDisk(string key, LyricsResult r)
    {
        try
        {
            var synced = r.Status == LyricsStatus.Synced
                ? string.Join("\n", r.Lines.Select(l => $"[{(int)l.Time.TotalMinutes:00}:{l.Time.Seconds:00}.{l.Time.Milliseconds / 10:00}]{l.Text}"))
                : null;
            var e = new DiskEntry(r.Status.ToString(), synced, r.Plain.Length > 0 ? r.Plain : null, r.Fetched);
            JsonStore.WriteAtomic(Path.Combine(_cacheDir, key + ".json"), JsonSerializer.Serialize(e, JsonStore.Options));
        }
        catch (Exception ex) { Logger.Error("lyrics cache write", ex); }
    }

    public void Dispose() => (_http as IDisposable)?.Dispose();
}

/// <summary>Real LRCLIB HTTP client: created lazily, disposed after 2 minutes idle. Sends a descriptive User-Agent as LRCLIB asks.</summary>
sealed class LrclibHttp : ILyricsHttp, IDisposable
{
    HttpClient? _client;
    Timer? _idle;
    readonly object _gate = new();

    public async Task<(int Status, string Body)> GetAsync(string url, CancellationToken ct)
    {
        HttpClient client;
        lock (_gate)
        {
            _client ??= CreateClient();
            client = _client;
            _idle ??= new Timer(_ => ReleaseClient());
            _idle.Change(TimeSpan.FromMinutes(2), Timeout.InfiniteTimeSpan);
        }
        try
        {
            using var resp = await client.GetAsync(url, ct).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return ((int)resp.StatusCode, body);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            Logger.Warn($"lyrics request failed: {ex.GetType().Name}");
            return (0, "");
        }
    }

    static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("Isle/1.0 (Windows desktop overlay; personal use)");
        return c;
    }

    void ReleaseClient()
    {
        lock (_gate)
        {
            _client?.Dispose();
            _client = null;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _idle?.Dispose();
            _idle = null;
            _client?.Dispose();
            _client = null;
        }
    }
}
