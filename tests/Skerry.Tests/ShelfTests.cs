using Skerry.Modules.Shelf;

namespace Skerry.Tests;

public class ShelfStoreTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "skerry-shelf-" + Guid.NewGuid().ToString("N"));
    readonly HashSet<string> _existing = new(StringComparer.OrdinalIgnoreCase);

    string File => Path.Combine(_dir, "shelf.json");
    ShelfStore NewStore() => new(File, null, _existing.Contains);

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Add_newest_first_without_duplicates()
    {
        var s = NewStore();
        Assert.Equal(2, s.Add([@"C:\a\one.txt", @"C:\a\two.txt"]));
        Assert.Equal(0, s.Add([@"c:\A\ONE.txt"]));
        Assert.Equal([@"c:\A\ONE.txt", @"C:\a\two.txt"], s.Items.Select(i => i.Path));
    }

    [Fact]
    public void Paths_are_normalised()
    {
        var s = NewStore();
        s.Add([@"C:\a\folder\", @"C:\a\.\b\..\c.txt", "D:", " "]);
        Assert.Equal([@"D:\", @"C:\a\c.txt", @"C:\a\folder"], s.Items.Select(i => i.Path));
    }

    [Fact]
    public void Persists_and_reloads()
    {
        var s = NewStore();
        s.Add([@"C:\x.txt", @"C:\y.txt"]);
        var r = NewStore();
        r.Load();
        Assert.Equal(s.Items.Select(i => (i.Id, i.Path)), r.Items.Select(i => (i.Id, i.Path)));
    }

    [Fact]
    public void Missing_files_are_marked_and_can_be_removed()
    {
        _existing.Add(@"C:\here.txt");
        var s = NewStore();
        s.Add([@"C:\here.txt", @"C:\gone.txt"]);
        Assert.False(s.IsMissing(s.Items.Single(i => i.Path.EndsWith("here.txt"))));
        Assert.True(s.IsMissing(s.Items.Single(i => i.Path.EndsWith("gone.txt"))));
        s.RemoveMissing();
        Assert.Equal([@"C:\here.txt"], s.Items.Select(i => i.Path));
    }

    [Fact]
    public void Remove_clear_and_changed_event()
    {
        var s = NewStore();
        var changes = 0;
        s.Changed += () => changes++;
        s.Add([@"C:\1", @"C:\2"]);
        s.Remove(s.Items[0].Id);
        Assert.Single(s.Items);
        s.Clear();
        Assert.Empty(s.Items);
        Assert.Equal(3, changes);
        var r = NewStore();
        r.Load();
        Assert.Empty(r.Items);
    }

    [Fact]
    public void Corrupt_file_starts_empty()
    {
        Directory.CreateDirectory(_dir);
        System.IO.File.WriteAllText(File, "[[[");
        var s = NewStore();
        s.Load();
        Assert.Empty(s.Items);
    }
}
