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
    public IReadOnlyList<ShelfItem> Items => _items;

    public static ShelfStore OpenDefault() => new(Paths.File("shelf.json"));

    public void Load()
    {
        _items.Clear();
        _items.AddRange(JsonStore.Load<ShelfFile>(file).Items.Where(i => !string.IsNullOrWhiteSpace(i.Path)));
    }

    public bool IsMissing(ShelfItem item) => !_exists(item.Path);

    /// <summary>Adds paths (full paths; duplicates move to the front). Returns how many were new.</summary>
    public int Add(IEnumerable<string> paths)
    {
        var added = 0;
        foreach (var raw in paths)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var path = System.IO.Path.GetFullPath(raw).TrimEnd('\\');
            if (path.Length == 2 && path[1] == ':') path += "\\"; // drive root
            var old = _items.FindIndex(i => string.Equals(i.Path, path, StringComparison.OrdinalIgnoreCase));
            if (old >= 0) _items.RemoveAt(old);
            else added++;
            _items.Insert(0, new ShelfItem { Path = path, Added = _now() });
        }
        Save();
        return added;
    }

    public void Remove(string id)
    {
        if (_items.RemoveAll(i => i.Id == id) > 0) Save();
    }

    public void RemoveMissing()
    {
        if (_items.RemoveAll(IsMissing) > 0) Save();
    }

    public void Clear()
    {
        if (_items.Count == 0) return;
        _items.Clear();
        Save();
    }

    public ShelfItem? Find(string id) => _items.FirstOrDefault(i => i.Id == id);

    void Save()
    {
        JsonStore.Save(file, new ShelfFile { Items = _items });
        Changed?.Invoke();
    }
}
