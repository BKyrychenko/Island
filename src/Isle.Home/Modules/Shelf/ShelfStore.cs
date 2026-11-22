using Isle.Core;

namespace Isle.Modules.Shelf;

public sealed class ShelfItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Path { get; set; } = "";
    public DateTimeOffset Added { get; set; }
}

public sealed class ShelfFile
{
    public int Version { get; set; } = 1;
    public List<ShelfItem> Items { get; set; } = [];
}

/// <summary>Parked file/folder references (never copies). Newest first, no duplicates, saved to shelf.json on every change.</summary>
public sealed class ShelfStore(string file, Func<DateTimeOffset>? now = null, Func<string, bool>? exists = null)
{
    readonly List<ShelfItem> _items = [];
    readonly Func<DateTimeOffset> _now = now ?? (() => DateTimeOffset.Now);
    readonly Func<string, bool> _exists = exists ?? (p => File.Exists(p) || Directory.Exists(p));

    public event Action? Changed;
    public event Action? SaveStatusChanged;
    public bool SaveFailed { get; private set; }
    public IReadOnlyList<ShelfItem> Items => _items;

    public static ShelfStore OpenDefault() => new(Paths.File("shelf.json"));

    public void Load()
    {
        if (!JsonStore.TryLoad<ShelfFile>(file, out var saved))
            throw new IOException("Could not read the shelf index.");
        _items.Clear();
        _items.AddRange(saved.Items.Where(i => !string.IsNullOrWhiteSpace(i.Path)));
    }

    public bool IsMissing(ShelfItem item) => !_exists(item.Path);

    /// <summary>Adds paths (full paths; duplicates move to the front). Returns how many were new.</summary>
    public int Add(IEnumerable<string> paths)
    {
        TryAdd(paths, out var added);
        return added;
    }

    public bool TryAdd(IEnumerable<string> paths, out int added)
    {
        var next = new List<ShelfItem>(_items);
        added = 0;
        foreach (var raw in paths)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var path = System.IO.Path.GetFullPath(raw).TrimEnd('\\');
            if (path.Length == 2 && path[1] == ':') path += "\\"; // drive root
            var old = next.FindIndex(i => string.Equals(i.Path, path, StringComparison.OrdinalIgnoreCase));
            if (old >= 0) next.RemoveAt(old);
            else added++;
            next.Insert(0, new ShelfItem { Path = path, Added = _now() });
        }
        if (Commit(next)) return true;
        added = 0;
        return false;
    }

    public void Remove(string id)
    {
        if (Find(id) is not null) Commit(_items.Where(i => i.Id != id).ToList());
    }

    public void RemoveMissing()
    {
        var next = _items.Where(i => !IsMissing(i)).ToList();
        if (next.Count != _items.Count) Commit(next);
    }

    public void Clear()
    {
        if (_items.Count == 0) return;
        Commit([]);
    }

    public ShelfItem? Find(string id) => _items.FirstOrDefault(i => i.Id == id);

    bool Commit(List<ShelfItem> next)
    {
        var failed = !JsonStore.Save(file, new ShelfFile { Items = next });
        if (SaveFailed != failed)
        {
            SaveFailed = failed;
            SaveStatusChanged?.Invoke();
        }
        if (failed) return false;
        _items.Clear();
        _items.AddRange(next);
        Changed?.Invoke();
        return true;
    }
}
