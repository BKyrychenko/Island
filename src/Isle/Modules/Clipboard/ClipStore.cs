using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Isle.Core;

namespace Isle.Modules.Clipboard;

public enum ClipKind { Text, Image }

/// <summary>One history entry. Text lives only DPAPI-encrypted on disk (<see cref="Cipher"/>) and decrypted in memory.</summary>
public sealed class ClipEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public ClipKind Kind { get; set; }
    public DateTimeOffset Time { get; set; }
    public bool Pinned { get; set; }
    /// <summary>DPAPI(base64) of the UTF-8 text (text entries).</summary>
    public string? Cipher { get; set; }
    /// <summary>PNG file name inside the clip folder (image entries).</summary>
    public string? Image { get; set; }
    /// <summary>SHA-256 of the image pixels, for dedupe (images only; text is compared in memory).</summary>
    public string? ImageHash { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }

    [JsonIgnore] public string Text { get; set; } = "";
}

public sealed class ClipFile
{
    public int Version { get; set; } = 1;
    public List<ClipEntry> Entries { get; set; } = [];
}

/// <summary>
/// Clipboard history: newest first, dedupe (a repeat moves the entry to the top), pinned entries never expire,
/// the oldest unpinned entries beyond <see cref="Limit"/> are dropped (with their PNG files). Not thread-safe: UI thread.
/// </summary>
public sealed class ClipStore
{
    readonly string _file, _dir;
    readonly Func<DateTimeOffset> _now;
    readonly List<ClipEntry> _entries = [];

    public int Limit { get; set; } = 200;
    public event Action? Changed;

    public ClipStore(string file, string imageDir, Func<DateTimeOffset>? now = null)
    {
        _file = file;
        _dir = imageDir;
        _now = now ?? (() => DateTimeOffset.Now);
    }

    public static ClipStore OpenDefault() => new(Paths.File("clipboard.json"), Paths.Dir("clip"));

    public IReadOnlyList<ClipEntry> Entries => _entries;
    public string ImagePath(ClipEntry e) => Path.Combine(_dir, e.Image ?? "");

    public void Load()
    {
        _entries.Clear();
        var f = JsonStore.Load<ClipFile>(_file);
        foreach (var e in f.Entries)
        {
            if (e.Kind == ClipKind.Text)
            {
                var text = Decrypt(e.Cipher);
                if (text is null) continue; // another user/machine, or damaged: drop
                e.Text = text;
            }
            else if (e.Image is null || !File.Exists(ImagePath(e))) continue;
            _entries.Add(e);
        }
        Sort();
    }

    public void Save()
    {
        foreach (var e in _entries)
            if (e.Kind == ClipKind.Text && e.Cipher is null) e.Cipher = Encrypt(e.Text);
        JsonStore.Save(_file, new ClipFile { Entries = _entries });
    }

    /// <summary>Adds text (empty/whitespace ignored). Returns the entry (existing one when it was a repeat).</summary>
    public ClipEntry? AddText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var existing = _entries.FirstOrDefault(e => e.Kind == ClipKind.Text && e.Text == text);
        if (existing is not null) return Touch(existing);
        var entry = new ClipEntry { Kind = ClipKind.Text, Text = text, Time = _now() };
        _entries.Add(entry);
        Trim();
        Sort();
        Changed?.Invoke();
        return entry;
    }

    /// <summary>Adds an image from PNG bytes; <paramref name="pixelHash"/> identifies repeats.</summary>
    public ClipEntry AddImage(byte[] png, string pixelHash, int width, int height)
    {
        var existing = _entries.FirstOrDefault(e => e.Kind == ClipKind.Image && e.ImageHash == pixelHash);
        if (existing is not null) return Touch(existing);
        var entry = new ClipEntry { Kind = ClipKind.Image, Time = _now(), ImageHash = pixelHash, ImageWidth = width, ImageHeight = height };
        entry.Image = entry.Id + ".png";
        Directory.CreateDirectory(_dir);
        File.WriteAllBytes(ImagePath(entry), png);
        _entries.Add(entry);
        Trim();
        Sort();
        Changed?.Invoke();
        return entry;
    }

    ClipEntry Touch(ClipEntry e)
    {
        e.Time = _now();
        Sort();
        Changed?.Invoke();
        return e;
    }

    public void SetPinned(string id, bool pinned)
    {
        var e = Find(id);
        if (e is null || e.Pinned == pinned) return;
        e.Pinned = pinned;
        Trim();
        Sort();
        Changed?.Invoke();
    }

    public void Delete(string id)
    {
        var e = Find(id);
        if (e is null) return;
        Remove(e);
        Changed?.Invoke();
    }

    /// <summary>Removes every unpinned entry (pinned ones are kept — they "never expire").</summary>
    public void ClearUnpinned()
    {
        foreach (var e in _entries.Where(e => !e.Pinned).ToList()) Remove(e);
        Changed?.Invoke();
    }

    public ClipEntry? Find(string id) => _entries.FirstOrDefault(e => e.Id == id);

    /// <summary>Pinned first, then newest; text entries filtered by every word of the query (case-insensitive).</summary>
    public IEnumerable<ClipEntry> Search(string? query)
    {
        var words = (query ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return _entries;
        return _entries.Where(e => e.Kind == ClipKind.Text && words.All(w => e.Text.Contains(w, StringComparison.CurrentCultureIgnoreCase)));
    }

    public static string Preview(string text, int max = 160)
    {
        var sb = new StringBuilder(Math.Min(text.Length, max));
        var space = false;
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c)) { space = sb.Length > 0; continue; }
            if (space) { sb.Append(' '); space = false; }
            sb.Append(c);
            if (sb.Length >= max) { sb.Append('…'); break; }
        }
        return sb.ToString();
    }

    public static string HashPixels(byte[] pixels) => Convert.ToHexString(SHA256.HashData(pixels));

    void Trim()
    {
        var unpinned = _entries.Where(e => !e.Pinned).OrderByDescending(e => e.Time).ToList();
        foreach (var e in unpinned.Skip(Math.Max(0, Limit))) Remove(e);
    }

    void Remove(ClipEntry e)
    {
        _entries.Remove(e);
        if (e.Kind == ClipKind.Image)
        {
            try { File.Delete(ImagePath(e)); } catch (Exception ex) { Logger.Warn($"clip image delete: {ex.GetType().Name}"); }
        }
    }

    void Sort() => _entries.Sort((a, b) => a.Pinned != b.Pinned ? (a.Pinned ? -1 : 1) : b.Time.CompareTo(a.Time));

    static string Encrypt(string text) => Convert.ToBase64String(Secret.ProtectBytes(Encoding.UTF8.GetBytes(text)));

    static string? Decrypt(string? cipher)
    {
        if (string.IsNullOrEmpty(cipher)) return null;
        try { return Encoding.UTF8.GetString(Secret.UnprotectBytes(Convert.FromBase64String(cipher))); }
        catch (Exception ex) when (ex is CryptographicException or FormatException) { return null; }
    }
}
