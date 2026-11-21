using System.Security.Cryptography;
using System.Text;

namespace Isle.Modules.LiveEvents;

/// <summary>Browser partial-download files: Chrome/Edge "*.crdownload", Firefox "*.part", old Edge "*.partial", Safari "*.download".</summary>
public static class DownloadDetector
{
    static readonly string[] Extensions = [".crdownload", ".part", ".partial", ".download"];

    public static bool IsPartial(string path)
    {
        var name = Path.GetFileName(path);
        if (name.StartsWith('.')) return false;
        return Extensions.Any(e => name.EndsWith(e, StringComparison.OrdinalIgnoreCase) && name.Length > e.Length);
    }

    /// <summary>Name the file will have when finished, or null when the browser uses a placeholder ("Unconfirmed 123.crdownload").</summary>
    public static string? FinalName(string path)
    {
        var name = Path.GetFileName(path);
        foreach (var e in Extensions)
        {
            if (!name.EndsWith(e, StringComparison.OrdinalIgnoreCase)) continue;
            var final = name[..^e.Length];
            if (final.StartsWith("Unconfirmed ", StringComparison.OrdinalIgnoreCase) && final[12..].All(char.IsDigit)) return null;
            // Firefox: "file.zip.part" or "abcd1234.part" (random) — keep it if it still has an extension
            if (e == ".part" && !Path.HasExtension(final)) return null;
            return final;
        }
        return null;
    }

    public static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0} KB",
        < 1024L * 1024 * 1024 => $"{bytes / 1024.0 / 1024:0.0} MB",
        _ => $"{bytes / 1024.0 / 1024 / 1024:0.00} GB",
    };
}

/// <summary>Bytes-per-second over a short sliding window of (time, total bytes) samples.</summary>
public sealed class SpeedMeter
{
    readonly Queue<(DateTimeOffset T, long Bytes)> _samples = new();
    readonly TimeSpan _window;

    public SpeedMeter(TimeSpan? window = null) => _window = window ?? TimeSpan.FromSeconds(4);

    public void Add(DateTimeOffset t, long bytes)
    {
        _samples.Enqueue((t, bytes));
        while (_samples.Count > 2 && t - _samples.Peek().T > _window) _samples.Dequeue();
    }

    public double BytesPerSecond
    {
        get
        {
            if (_samples.Count < 2) return 0;
            var first = _samples.Peek();
            var last = _samples.Last();
            var secs = (last.T - first.T).TotalSeconds;
            return secs <= 0.2 ? 0 : Math.Max(0, (last.Bytes - first.Bytes) / secs);
        }
    }
}

/// <summary>Valve KeyValues text ("VDF"/"ACF"): nested "key" "value" / "key" { ... } blocks.</summary>
public static class Vdf
{
    public sealed class Node : Dictionary<string, object>
    {
        public Node() : base(StringComparer.OrdinalIgnoreCase) { }
        public string? Str(string key) => TryGetValue(key, out var v) ? v as string : null;
        public Node? Child(string key) => TryGetValue(key, out var v) ? v as Node : null;
        public long Long(string key) => long.TryParse(Str(key), out var n) ? n : 0;
    }

    public static Node Parse(string text)
    {
        var pos = 0;
        var root = new Node();
        ParseInto(text, ref pos, root);
        return root;
    }

    static void ParseInto(string s, ref int pos, Node node)
    {
        while (true)
        {
            var key = Token(s, ref pos);
            if (key is null || key == "}") return;
            var save = pos;
            var next = Token(s, ref pos);
            if (next is null) return;
            if (next == "{")
            {
                var child = new Node();
                ParseInto(s, ref pos, child);
                node[key] = child;
            }
            else node[key] = next;
            _ = save;
        }
    }

    /// <summary>Next token: a quoted string (with \" and \\ escapes), "{" or "}", or null at the end. Skips // comments.</summary>
    static string? Token(string s, ref int pos)
    {
        while (pos < s.Length)
        {
            var c = s[pos];
            if (char.IsWhiteSpace(c)) { pos++; continue; }
            if (c == '/' && pos + 1 < s.Length && s[pos + 1] == '/')
            {
                while (pos < s.Length && s[pos] != '\n') pos++;
                continue;
            }
            if (c is '{' or '}') { pos++; return c.ToString(); }
            if (c == '"')
            {
                var sb = new StringBuilder();
                pos++;
                while (pos < s.Length && s[pos] != '"')
                {
                    if (s[pos] == '\\' && pos + 1 < s.Length) { pos++; sb.Append(s[pos] == 'n' ? '\n' : s[pos]); }
                    else sb.Append(s[pos]);
                    pos++;
                }
                pos++;
                return sb.ToString();
            }
            // unquoted token
            var start = pos;
            while (pos < s.Length && !char.IsWhiteSpace(s[pos]) && s[pos] is not ('{' or '}' or '"')) pos++;
            return s[start..pos];
        }
        return null;
    }
}

public sealed record SteamDownload(string AppId, string Name, long Downloaded, long Total)
{
    public double Progress => Total > 0 ? Math.Clamp((double)Downloaded / Total, 0, 1) : 0;
}

public static class Steam
{
    /// <summary>Library "steamapps" folders from libraryfolders.vdf (new format: "path" keys; old: numbered values).</summary>
    public static List<string> Libraries(string steamPath, string? libraryFoldersVdf)
    {
        var list = new List<string> { Path.Combine(steamPath, "steamapps") };
        if (libraryFoldersVdf is null) return list;
        var root = Vdf.Parse(libraryFoldersVdf);
        var lf = root.Child("libraryfolders") ?? root.Child("LibraryFolders");
        if (lf is null) return list;
        foreach (var (key, value) in lf)
        {
            var path = value is Vdf.Node n ? n.Str("path") : int.TryParse(key, out _) ? value as string : null;
            if (string.IsNullOrWhiteSpace(path)) continue;
            var apps = Path.Combine(path.Replace(@"\\", @"\"), "steamapps");
            if (!list.Contains(apps, StringComparer.OrdinalIgnoreCase)) list.Add(apps);
        }
        return list;
    }

    /// <summary>A download in progress from an appmanifest_*.acf, or null.</summary>
    public static SteamDownload? FromManifest(string acf)
    {
        var app = Vdf.Parse(acf).Child("AppState");
        if (app is null) return null;
        var flags = app.Long("StateFlags");
        long done = app.Long("BytesDownloaded"), total = app.Long("BytesToDownload");
        const long UpdateRunning = 1024, UpdateStarted = 2048, Downloading = 1048576;
        var active = (flags & (UpdateRunning | Downloading)) != 0 || ((flags & UpdateStarted) != 0 && total > 0 && done < total);
        if (!active || total <= 0 || done >= total) return null;
        return new SteamDownload(app.Str("appid") ?? "", app.Str("name") ?? "Steam", done, total);
    }
}

/// <summary>Who uses the microphone/camera, from the CapabilityAccessManager consent store (LastUsedTimeStart/Stop).</summary>
public static class ConsentStore
{
    /// <summary>In use when the last start is after the last stop (a stop of 0 means still running).</summary>
    public static bool InUse(long start, long stop) => start > 0 && (stop == 0 || start > stop);

    /// <summary>"C:#Program Files#Zoom#bin#Zoom.exe" → "Zoom"; "Microsoft.WindowsCamera_8wekyb3d8bbwe" → "Camera".</summary>
    public static string AppName(string key)
    {
        if (key.Contains('#'))
        {
            var file = key.Split('#')[^1];
            return Path.GetFileNameWithoutExtension(file);
        }
        var name = key.Split('_')[0];
        var dot = name.LastIndexOf('.');
        if (dot >= 0 && dot < name.Length - 1) name = name[(dot + 1)..];
        if (name.StartsWith("Windows", StringComparison.Ordinal) && name.Length > 7) name = name[7..];
        return name;
    }
}

/// <summary>OBS WebSocket v5 authentication: base64(sha256(base64(sha256(password + salt)) + challenge)).</summary>
public static class ObsAuth
{
    public static string Response(string password, string salt, string challenge)
    {
        var secret = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret + challenge)));
    }
}
