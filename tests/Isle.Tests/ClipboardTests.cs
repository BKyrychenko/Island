using Isle.Modules.Clipboard;

namespace Isle.Tests;

public class ClipStoreTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "isle-clip-" + Guid.NewGuid().ToString("N"));
    DateTimeOffset _now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    ClipStore NewStore(int limit = 200) =>
        new(Path.Combine(_dir, "clipboard.json"), Path.Combine(_dir, "clip"), () => _now) { Limit = limit };

    void Tick() => _now = _now.AddSeconds(1);

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Newest_first_and_dedupe_moves_to_top()
    {
        var s = NewStore();
        s.AddText("one"); Tick();
        s.AddText("two"); Tick();
        s.AddText("one");
        Assert.Equal(["one", "two"], s.Entries.Select(e => e.Text));
        Assert.Equal(2, s.Entries.Count);
    }

    [Fact]
    public void Empty_and_whitespace_are_ignored()
    {
        var s = NewStore();
        Assert.Null(s.AddText(""));
        Assert.Null(s.AddText("  \r\n "));
        Assert.Empty(s.Entries);
    }

    [Fact]
    public void Limit_drops_oldest_unpinned_but_never_pinned()
    {
        var s = NewStore(limit: 3);
        var first = s.AddText("a")!; Tick();
        s.SetPinned(first.Id, true);
        for (var i = 0; i < 5; i++) { s.AddText($"t{i}"); Tick(); }
        Assert.Equal(["a", "t4", "t3", "t2"], s.Entries.Select(e => e.Text));
        Assert.True(s.Entries[0].Pinned);
    }

    [Fact]
    public void Pinned_first_then_newest()
    {
        var s = NewStore();
        var old = s.AddText("old")!; Tick();
        s.AddText("new"); Tick();
        s.SetPinned(old.Id, true);
        Assert.Equal(["old", "new"], s.Entries.Select(e => e.Text));
        s.SetPinned(old.Id, false);
        Assert.Equal(["new", "old"], s.Entries.Select(e => e.Text));
    }

    [Fact]
    public void Clear_keeps_pinned_and_delete_removes()
    {
        var s = NewStore();
        var keep = s.AddText("keep")!; Tick();
        var gone = s.AddText("gone")!; Tick();
        s.AddText("also gone");
        s.SetPinned(keep.Id, true);
        s.Delete(gone.Id);
        Assert.Equal(2, s.Entries.Count);
        s.ClearUnpinned();
        Assert.Equal(["keep"], s.Entries.Select(e => e.Text));
    }

    [Fact]
    public void Search_all_words_case_insensitive_text_only()
    {
        var s = NewStore();
        s.AddText("Meeting link https://example.com/abc"); Tick();
        s.AddText("grocery list: milk, eggs"); Tick();
        s.AddImage([1, 2, 3], "HASH", 10, 10);
        Assert.Single(s.Search("MEETING example"));
        Assert.Single(s.Search("milk"));
        Assert.Empty(s.Search("milk meeting"));
        Assert.Equal(3, s.Search("").Count());
    }

    [Fact]
    public void Text_is_encrypted_on_disk_and_roundtrips()
    {
        var s = NewStore();
        s.AddText("my secret note 12345"); Tick();
        var pin = s.AddText("pinned thing")!;
        s.SetPinned(pin.Id, true);
        s.Save();
        var raw = File.ReadAllText(Path.Combine(_dir, "clipboard.json"));
        Assert.DoesNotContain("secret note", raw);
        Assert.DoesNotContain("pinned thing", raw);

        var again = NewStore();
        again.Load();
        Assert.Equal(["pinned thing", "my secret note 12345"], again.Entries.Select(e => e.Text));
        Assert.True(again.Entries[0].Pinned);
    }

    [Fact]
    public void Undecryptable_entries_are_dropped_on_load()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "clipboard.json"), """{"Entries":[{"Id":"x","Kind":0,"Cipher":"AAAA"},{"Id":"y","Kind":0}]}""");
        var s = NewStore();
        s.Load();
        Assert.Empty(s.Entries);
    }

    [Fact]
    public void Images_dedupe_by_pixels_and_files_are_removed()
    {
        var s = NewStore(limit: 1);
        var a = s.AddImage([9, 9, 9], "H1", 2, 2); Tick();
        var again = s.AddImage([9, 9, 9], "H1", 2, 2);
        Assert.Same(a, again);
        Assert.True(File.Exists(s.ImagePath(a)));
        Tick();
        s.AddImage([7, 7], "H2", 1, 1); // limit 1 → the first image goes, with its file
        Assert.False(File.Exists(s.ImagePath(a)));
        s.Save();
        var reloaded = NewStore();
        reloaded.Load();
        Assert.Single(reloaded.Entries);
        Assert.Equal("H2", reloaded.Entries[0].ImageHash);
    }

    [Fact]
    public void Missing_image_files_are_dropped_on_load()
    {
        var s = NewStore();
        var e = s.AddImage([1], "H", 1, 1);
        s.Save();
        File.Delete(s.ImagePath(e));
        var r = NewStore();
        r.Load();
        Assert.Empty(r.Entries);
    }

    [Fact]
    public void Corrupt_file_starts_empty_with_backup()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "clipboard.json"), "{ not json");
        var s = NewStore();
        s.Load();
        Assert.Empty(s.Entries);
        Assert.NotEmpty(Directory.GetFiles(_dir, "clipboard.json.bak*"));
    }

    [Theory]
    [InlineData("hello   world\r\n\tnext", "hello world next")]
    [InlineData("  lead", "lead")]
    public void Preview_collapses_whitespace(string text, string preview) => Assert.Equal(preview, ClipStore.Preview(text));

    [Fact]
    public void Preview_truncates()
    {
        var p = ClipStore.Preview(new string('x', 500), 20);
        Assert.Equal(21, p.Length);
        Assert.EndsWith("…", p);
    }

    [Theory]
    [InlineData("KeePassXC", "keepassxc", true)]
    [InlineData("1Password", "1password.exe", true)]
    [InlineData("chrome", "keepass", false)]
    [InlineData("", "", false)]
    public void Excluded_apps(string process, string excluded, bool result) =>
        Assert.Equal(result, ClipPrivacy.IsExcludedApp(process, [excluded, " "]));
}
