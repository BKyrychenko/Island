using Skerry.Modules.Clipboard;

namespace Skerry.Tests;

public class ClipStoreTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "skerry-clip-" + Guid.NewGuid().ToString("N"));
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
    public void Image_identity_includes_dimensions_and_keeps_legacy_hashes_after_reload()
    {
        var s = NewStore();
        var old = s.AddImage([1, 2, 3, 4], "LEGACY-PIXEL-HASH", 2, 6);
        s.Save();
        var loaded = NewStore();
        loaded.Load();
        Assert.Equal(old.Id, loaded.AddImage([1, 2, 3, 4], "LEGACY-PIXEL-HASH", 2, 6).Id);
        Assert.NotEqual(old.Id, loaded.AddImage([1, 2, 3, 4], "LEGACY-PIXEL-HASH", 3, 4).Id);
        Assert.Equal(2, loaded.Entries.Count);
    }

    [Fact]
    public void Image_search_matches_label_and_dimensions()
    {
        var s = NewStore();
        var image = s.AddImage([1], "H", 320, 180);
        s.AddText("320 notes");
        Assert.Same(image, Assert.Single(s.Search("image 320")));
        Assert.Same(image, Assert.Single(s.Search("зображення 180", "Зображення")));
        Assert.Same(image, Assert.Single(s.Search("320x180")));
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

public class ClipboardBorrowTests
{
    [Fact]
    public void PreservesExactRichFormatsAndStreamsWithoutUsingTheSystemClipboard()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var source = new System.Windows.DataObject();
                source.SetData(System.Windows.DataFormats.Html, "<b>formatted</b>", false);
                source.SetData(System.Windows.DataFormats.Rtf, "{\\rtf1 sample}", false);
                using var stream = new MemoryStream([1, 2, 3, 4]);
                stream.Position = 2;
                source.SetData("Skerry.Custom.Binary", stream, false);
                var copy = Skerry.Core.ClipboardOps.CopyFormats(source);
                Assert.Equal(source.GetFormats(false).Order(), copy.GetFormats(false).Order());
                Assert.Equal("<b>formatted</b>", copy.GetData(System.Windows.DataFormats.Html, false));
                var preserved = Assert.IsType<MemoryStream>(copy.GetData("Skerry.Custom.Binary", false));
                Assert.Equal(new byte[] { 1, 2, 3, 4 }, preserved.ToArray());
                Assert.Equal(2, stream.Position);
                Assert.NotSame(stream, preserved);
                Skerry.Core.ClipboardOps.ExcludeFromHistory(copy);
                foreach (var name in new[] { "ExcludeClipboardContentFromMonitorProcessing", "CanIncludeInClipboardHistory", "CanUploadToCloudClipboard" })
                    Assert.Equal(new byte[4], Assert.IsType<MemoryStream>(copy.GetData(name, false)).ToArray());
            }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(5)));
        if (error is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}

public class ClipboardCaptureTests
{
    [Fact]
    public void Only_own_writes_and_the_requested_selection_are_ignored()
    {
        var guard = new Skerry.Core.ClipboardOps.CaptureGuard();
        guard.OwnWrite(10);
        Assert.True(guard.Ignore(10, 1));
        Assert.False(guard.Ignore(11, 1));
        guard.BeginSelection(11, 42);
        Assert.False(guard.Ignore(12, 99)); // unrelated external copy during Ctrl+C
        Assert.True(guard.Ignore(13, 42));
        Assert.Equal(13u, guard.SelectionSequence);
        Assert.True(guard.Ignore(13, 42)); // duplicate notification / retry
        Assert.False(guard.Ignore(14, 42)); // next copy, even from the same app, is captured
        guard.Writing = true;
        Assert.True(guard.Ignore(15, 1)); // a reentrant notification inside our write
        guard.Writing = false;
        Assert.True(guard.Ignore(15, 1));
        Assert.False(guard.Ignore(16, 99));
        guard.BeginSelection(16, 42);
        guard.EndSelection(); // failed or timed out Ctrl+C
        Assert.False(guard.Ignore(17, 42));
    }

    [Fact]
    public void Full_history_is_reachable_and_row_paste_is_explained()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            var dir = Path.Combine(Path.GetTempPath(), "skerry-clip-view-" + Guid.NewGuid().ToString("N"));
            try
            {
                var now = DateTimeOffset.Now;
                var store = new ClipStore(Path.Combine(dir, "c.json"), Path.Combine(dir, "images"), () => now);
                var oldest = store.AddText("oldest history item")!;
                for (var i = 0; i < 125; i++) { now = now.AddSeconds(1); store.AddText($"entry {i}"); }
                ClipEntry? pasted = null;
                var view = new Skerry.Views.Clipboard.ClipboardView(store, e => pasted = e, _ => { });
                Assert.Contains(view.Children.OfType<System.Windows.Controls.TextBlock>(), b => b.Text == Skerry.Core.Strings.T("clip.clickToPaste"));
                var scroll = view.Children.OfType<System.Windows.Controls.ScrollViewer>().Single();
                var rows = (System.Windows.Controls.StackPanel)scroll.Content;
                Assert.Equal(61, rows.Children.Count);
                ((System.Windows.Controls.Button)rows.Children[^1]).RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                Assert.Equal(121, rows.Children.Count);
                ((System.Windows.Controls.Button)rows.Children[^1]).RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                Assert.Equal(126, rows.Children.Count);
                ((System.Windows.Controls.Button)rows.Children[^1]).RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                Assert.Same(oldest, pasted);
            }
            catch (Exception ex) { error = ex; }
            finally { try { Directory.Delete(dir, true); } catch { } }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
        if (error is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
