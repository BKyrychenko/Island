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
    HashSet<string> _savedImages = new(StringComparer.OrdinalIgnoreCase);

    public int Limit { get; set; } = 200;
    public bool HasPendingChanges { get; private set; }
    public string? SaveErrorKey { get; private set; }
    public event Action? Changed;
    public event Action? SaveStatusChanged;

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
        if (!JsonStore.TryLoad<ClipFile>(_file, out var f))
            throw new IOException("Could not read the clipboard index.");
        _entries.Clear();
        foreach (var e in f.Entries)
        {
            if (e.Kind == ClipKind.Text)
            {
                var text = Decrypt(e.Cipher);
                if (text is null) continue; // another user/machine, or damaged: drop
                e.Text = text;
            }
            else if (e.Image is null || Path.GetFileName(e.Image) != e.Image || !File.Exists(ImagePath(e))) continue;
            _entries.Add(e);
        }
        Sort(_entries);
        _savedImages = ImageNames(_entries);
        HasPendingChanges = false;
    }

    public bool Save()
    {
        var saved = TryWrite(_entries);
        if (!saved) SetSaveError("clip.captureSaveFailed");
        else if (SaveErrorKey == "clip.captureSaveFailed") SetSaveError(null);
        return saved;
    }

    bool TryWrite(List<ClipEntry> next)
    {
        try
        {
            foreach (var e in next)
                if (e.Kind == ClipKind.Text && e.Cipher is null) e.Cipher = Encrypt(e.Text);
        }
        catch (CryptographicException ex)
        {
            Logger.Error("encrypt clipboard history", ex);
            return false;
        }
        if (!JsonStore.Save(_file, new ClipFile { Entries = next })) return false;

        // Old PNGs stay usable by the on-disk index until its atomic replacement succeeds.
        var keep = ImageNames(next);
        _savedImages.UnionWith(ImageNames(_entries));
        _savedImages.ExceptWith(keep);
        foreach (var name in _savedImages) DeleteImage(name);
        _savedImages = keep;
        HasPendingChanges = false;
        return true;
    }

    static HashSet<string> ImageNames(IEnumerable<ClipEntry> entries) =>
        entries.Where(e => e.Kind == ClipKind.Image && e.Image is not null)
               .Select(e => e.Image!).ToHashSet(StringComparer.OrdinalIgnoreCase);

    void SetSaveError(string? key)
    {
        if (SaveErrorKey == key) return;
        SaveErrorKey = key;
        SaveStatusChanged?.Invoke();
    }

    /// <summary>Adds text (empty/whitespace ignored). Returns the entry (existing one when it was a repeat).</summary>
    public ClipEntry? AddText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var existing = _entries.FirstOrDefault(e => e.Kind == ClipKind.Text && e.Text == text);
        if (existing is not null) return Touch(existing);
        var entry = new ClipEntry { Kind = ClipKind.Text, Text = text, Time = _now() };
        return AddCaptured(entry);
    }

    /// <summary>Adds PNG bytes; the pixel hash and dimensions together identify repeats, including legacy hashes.</summary>
    public ClipEntry AddImage(byte[] png, string pixelHash, int width, int height)
    {
        var existing = _entries.FirstOrDefault(e => e.Kind == ClipKind.Image && e.ImageHash == pixelHash && e.ImageWidth == width && e.ImageHeight == height);
        if (existing is not null) return Touch(existing);
        var entry = new ClipEntry { Kind = ClipKind.Image, Time = _now(), ImageHash = pixelHash, ImageWidth = width, ImageHeight = height };
        entry.Image = entry.Id + ".png";
        try { JsonStore.WriteAtomic(ImagePath(entry), png); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetSaveError("clip.editFailed");
            throw;
        }
        return AddCaptured(entry);
    }

    ClipEntry AddCaptured(ClipEntry entry)
    {
        _entries.Add(entry);
        foreach (var e in Trim(_entries))
            if (e.Image is { } name && !_savedImages.Contains(name)) DeleteImage(name);
        return CaptureChanged(entry);
    }

    ClipEntry Touch(ClipEntry e)
    {
        e.Time = _now();
        return CaptureChanged(e);
    }

    ClipEntry CaptureChanged(ClipEntry e)
    {
        Sort(_entries);
        HasPendingChanges = true;
        Changed?.Invoke();
        return e;
    }

    public void SetPinned(string id, bool pinned)
    {
        var e = Find(id);
        if (e is null || e.Pinned == pinned) return;
        var next = new List<ClipEntry>(_entries);
        e.Pinned = pinned;
        Trim(next);
        Sort(next);
        CommitEdit(next, () => e.Pinned = !pinned);
    }

    public void Delete(string id)
    {
        if (Find(id) is not null) CommitEdit(_entries.Where(e => e.Id != id).ToList());
    }

    /// <summary>Removes every unpinned entry (pinned ones are kept — they "never expire").</summary>
    public void ClearUnpinned()
    {
        if (_entries.Any(e => !e.Pinned)) CommitEdit(_entries.Where(e => e.Pinned).ToList());
    }

    void CommitEdit(List<ClipEntry> next, Action? rollback = null)
    {
        if (!TryWrite(next))
        {
            rollback?.Invoke();
            SetSaveError("clip.editFailed");
            return;
        }
        _entries.Clear();
        _entries.AddRange(next);
        SetSaveError(null);
        Changed?.Invoke();
    }

    public ClipEntry? Find(string id) => _entries.FirstOrDefault(e => e.Id == id);

    /// <summary>Pinned first, then newest; filter text or image label/dimensions by every query word (case-insensitive).</summary>
    public IEnumerable<ClipEntry> Search(string? query, string imageLabel = "Image")
    {
        var words = (query ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return _entries;
        return _entries.Where(e => words.All(w => (e.Kind == ClipKind.Text ? e.Text : $"{imageLabel} {e.ImageWidth} × {e.ImageHeight} {e.ImageWidth}x{e.ImageHeight}").Contains(w, StringComparison.CurrentCultureIgnoreCase)));
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

    List<ClipEntry> Trim(List<ClipEntry> entries)
    {
        var removed = entries.Where(e => !e.Pinned).OrderByDescending(e => e.Time).Skip(Math.Max(0, Limit)).ToList();
        foreach (var e in removed) entries.Remove(e);
        return removed;
    }

    void DeleteImage(string name)
    {
        try { File.Delete(Path.Combine(_dir, name)); }
        catch (Exception ex) { Logger.Warn($"clip image delete: {ex.GetType().Name}"); }
    }

    static void Sort(List<ClipEntry> entries) => entries.Sort((a, b) => a.Pinned != b.Pinned ? (a.Pinned ? -1 : 1) : b.Time.CompareTo(a.Time));

    static string Encrypt(string text) => Convert.ToBase64String(Secret.ProtectBytes(Encoding.UTF8.GetBytes(text)));

    static string? Decrypt(string? cipher)
    {
        if (string.IsNullOrEmpty(cipher)) return null;
        try { return Encoding.UTF8.GetString(Secret.UnprotectBytes(Convert.FromBase64String(cipher))); }
        catch (Exception ex) when (ex is CryptographicException or FormatException) { return null; }
    }
}
