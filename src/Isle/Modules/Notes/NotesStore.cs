using Isle.Core;

namespace Isle.Modules.Notes;

/// <summary>One note: formatted content as FlowDocument XAML plus a plain-text shadow for search and titles.</summary>
public sealed class Note
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string Folder { get; set; } = "";
    public bool Pinned { get; set; }
    /// <summary>FlowDocument content in WPF's XAML clipboard format (TextRange.Save, DataFormats.Xaml).</summary>
    public string Xaml { get; set; } = "";
    public string Plain { get; set; } = "";
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Updated { get; set; }

    /// <summary>First non-empty line (checkbox glyphs removed), at most 60 characters.</summary>
    public string Title
    {
        get
        {
            foreach (var raw in Plain.Split('\n'))
            {
                var line = raw.Trim().TrimStart('☐', '☑', '☒', '•', ' ').Trim();
                if (line.Length > 0) return line.Length > 60 ? line[..60].TrimEnd() + "…" : line;
            }
            return "";
        }
    }

    /// <summary>Second line onwards, for the list preview.</summary>
    public string Preview
    {
        get
        {
            var title = Title;
            var lines = Plain.Split('\n').Select(l => l.Trim().TrimStart('☐', '☑', '☒', '•', ' ').Trim()).Where(l => l.Length > 0).ToList();
            var rest = lines.Count > 1 ? string.Join(" ", lines.Skip(1)) : "";
            return rest.Length > 120 ? rest[..120] : rest;
        }
    }

    public Note Clone() => (Note)MemberwiseClone();
}

public sealed class NotesDoc
{
    public List<Note> Notes { get; set; } = [];
    public List<string> Folders { get; set; } = [];
}

/// <summary>
/// notes.json: CRUD, folders (one level), pins, search. Saves atomically (JsonStore); a corrupt file is backed up and
/// replaced by an empty document. Pure apart from the file; clock injectable.
/// </summary>
public sealed class NotesStore
{
    readonly string? _path;
    readonly Func<DateTimeOffset> _now;

    public NotesDoc Doc { get; }
    public event Action? Changed;

    /// <param name="path">File to load/save; null = memory only (snapshots).</param>
    public NotesStore(string? path, Func<DateTimeOffset>? now = null)
    {
        _path = path;
        _now = now ?? (() => DateTimeOffset.Now);
        Doc = path is null ? new NotesDoc() : JsonStore.Load<NotesDoc>(path);
        Doc.Notes ??= [];
        Doc.Folders ??= [];
        Doc.Notes.RemoveAll(n => n is null);
        foreach (var n in Doc.Notes) { n.Folder ??= ""; n.Xaml ??= ""; n.Plain ??= ""; }
    }

    public IReadOnlyList<Note> Notes => Doc.Notes;
    public IReadOnlyList<string> Folders => Doc.Folders;

    public Note? Find(string id) => Doc.Notes.FirstOrDefault(n => n.Id == id);

    public Note Create(string folder = "", string plain = "", string xaml = "")
    {
        var now = _now();
        var n = new Note { Folder = folder, Plain = plain, Xaml = xaml, Created = now, Updated = now };
        Doc.Notes.Add(n);
        if (folder.Length > 0) AddFolderQuiet(folder);
        Save();
        return n;
    }

    public bool Update(string id, string xaml, string plain)
    {
        if (Find(id) is not { } n) return false;
        if (n.Xaml == xaml && n.Plain == plain) return true;
        n.Xaml = xaml;
        n.Plain = plain;
        n.Updated = _now();
        Save();
        return true;
    }

    /// <summary>Removes a note and returns it (with its position) so it can be restored by Undo.</summary>
    public (Note Note, int Index)? Delete(string id)
    {
        var i = Doc.Notes.FindIndex(n => n.Id == id);
        if (i < 0) return null;
        var n = Doc.Notes[i];
        Doc.Notes.RemoveAt(i);
        Save();
        return (n, i);
    }

    public void Restore(Note note, int index)
    {
        if (Find(note.Id) is not null) return;
        Doc.Notes.Insert(Math.Clamp(index, 0, Doc.Notes.Count), note);
        if (note.Folder.Length > 0) AddFolderQuiet(note.Folder);
        Save();
    }

    public void SetPinned(string id, bool pinned)
    {
        if (Find(id) is not { } n || n.Pinned == pinned) return;
        n.Pinned = pinned;
        Save();
    }

    public void Move(string id, string folder)
    {
        if (Find(id) is not { } n || n.Folder == folder) return;
        n.Folder = folder;
        if (folder.Length > 0) AddFolderQuiet(folder);
        Save();
    }

    public bool AddFolder(string name)
    {
        name = name.Trim();
        if (name.Length == 0 || Doc.Folders.Contains(name, StringComparer.OrdinalIgnoreCase)) return false;
        Doc.Folders.Add(name);
        Save();
        return true;
    }

    void AddFolderQuiet(string name)
    {
        if (!Doc.Folders.Contains(name, StringComparer.OrdinalIgnoreCase)) Doc.Folders.Add(name);
    }

    /// <summary>Deletes a folder; its notes move to "no folder".</summary>
    public void DeleteFolder(string name)
    {
        if (Doc.Folders.RemoveAll(f => f == name) == 0) return;
        foreach (var n in Doc.Notes.Where(n => n.Folder == name)) n.Folder = "";
        Save();
    }

    /// <summary>Notes matching every word of <paramref name="query"/> (title or text, case-insensitive) in
    /// <paramref name="folder"/> (null = all). Pinned first, then most recently edited.</summary>
    public List<Note> Search(string? query, string? folder = null)
    {
        var words = (query ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return [.. Doc.Notes
            .Where(n => folder is null || n.Folder == folder)
            .Where(n => words.All(w => n.Plain.Contains(w, StringComparison.CurrentCultureIgnoreCase)))
            .OrderByDescending(n => n.Pinned)
            .ThenByDescending(n => n.Updated)];
    }

    void Save()
    {
        if (_path is not null) JsonStore.Save(_path, Doc);
        try { Changed?.Invoke(); }
        catch (Exception ex) { Logger.Error("notes changed handler", ex); }
    }
}
