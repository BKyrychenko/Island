using System.Windows;
using System.Windows.Controls;
using Isle.Core;
using Isle.Modules.Clipboard;
using Isle.Modules.Shelf;
using Isle.Views.Clipboard;
using Isle.Views.Shelf;

namespace Isle.Tests;

public class ClipboardShelfPersistenceTests
{
    [Fact]
    public void Locked_clipboard_rejects_deletes_and_keeps_the_row_available_for_retry()
    {
        using var dir = new TempDir();
        FakeSink.Run(_ =>
        {
            var index = dir.File("clipboard.json");
            ClipStore Open() => new(index, dir.File("images"));
            var store = Open();
            var entry = store.AddText("synthetic clipboard fixture")!;
            Assert.True(store.Save());
            var view = new ClipboardView(store, _ => throw new InvalidOperationException(), _ => { });
            view.OnShown();
            var list = Assert.IsType<StackPanel>(view.Children.OfType<ScrollViewer>().Single().Content);
            var row = Assert.IsType<Button>(Assert.Single(list.Children.Cast<UIElement>()));
            var buttons = Assert.IsType<DockPanel>(row.Content).Children.OfType<StackPanel>().Single();
            var delete = buttons.Children.OfType<Button>().Single(b => Equals(b.ToolTip, Strings.T("clip.delete")));
            var before = File.ReadAllBytes(index);
            using (File.Open(index, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                delete.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Same(entry, store.Find(entry.Id));
                Assert.Same(row, list.Children[0]);
                Assert.Equal(before, File.ReadAllBytes(index));
                view.OnHidden();
                view.OnShown();
                Assert.Contains(view.Children.OfType<TextBlock>(), t => t.Visibility == Visibility.Visible && t.Text == Strings.T("clip.editFailed"));
                store.SetPinned(entry.Id, true);
                Assert.False(entry.Pinned);
                store.ClearUnpinned();
                Assert.Single(store.Entries);
            }
            delete.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Empty(store.Entries);
            Assert.Null(store.SaveErrorKey);
            var restarted = Open();
            restarted.Load();
            Assert.Empty(restarted.Entries);
        });
    }

    [Fact]
    public void Failed_capture_flush_preserves_the_previous_png_and_retries_the_pending_capture()
    {
        using var dir = new TempDir();
        var index = dir.File("clipboard.json");
        var now = DateTimeOffset.UtcNow;
        ClipStore Open() => new(index, dir.File("images"), () => now) { Limit = 1 };
        var store = Open();
        var old = store.AddImage([1, 2, 3], "fixture hash", 1, 1);
        Assert.True(store.Save());
        now = now.AddMinutes(1);
        using (File.Open(index, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            store.AddText("next capture");
            Assert.False(store.Save());
            Assert.True(store.HasPendingChanges);
            Assert.Equal("clip.captureSaveFailed", store.SaveErrorKey);
            Assert.True(File.Exists(store.ImagePath(old)));
            var reader = Open();
            reader.Load();
            Assert.Equal(old.Id, Assert.Single(reader.Entries).Id);
        }
        Assert.True(store.Save());
        Assert.False(store.HasPendingChanges);
        Assert.Null(store.SaveErrorKey);
        Assert.False(File.Exists(store.ImagePath(old)));
        var restarted = Open();
        restarted.Load();
        Assert.Equal("next capture", Assert.Single(restarted.Entries).Text);
    }

    [Fact]
    public void Autosave_retries_without_another_capture_and_flushes_on_stop()
    {
        using var dir = new TempDir();
        FakeSink.Run(sink =>
        {
            var index = dir.File("clipboard.json");
            var store = new ClipStore(index, dir.File("images"));
            Assert.True(store.Save());
            var autosave = new ClipboardAutosave(store, sink.Ui);
            using (File.Open(index, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                store.AddText("first delayed capture");
                Assert.True(sink.PumpUntil(() => store.SaveErrorKey == "clip.captureSaveFailed"));
            }
            Assert.True(sink.PumpUntil(() => !store.HasPendingChanges, 15000));
            Assert.Null(store.SaveErrorKey);
            store.AddText("captured just before stopping");
            Assert.True(autosave.Stop());
            var restarted = new ClipStore(index, dir.File("images"));
            restarted.Load();
            Assert.Equal(2, restarted.Entries.Count);
        });
    }

    [Fact]
    public void Shelf_drop_and_removal_publish_only_after_a_successful_save()
    {
        using var dir = new TempDir();
        FakeSink.Run(_ =>
        {
            var index = dir.File("shelf.json");
            var first = dir.File("first.txt");
            var second = dir.File("second.txt");
            var store = new ShelfStore(index, exists: _ => false);
            Assert.Equal(1, store.Add([first]));
            var original = Assert.Single(store.Items);
            var view = new ShelfView(store, null);
            var changes = 0;
            store.Changed += () => changes++;
            var before = File.ReadAllBytes(index);
            using (File.Open(index, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert.False(store.TryAdd([second], out var added));
                Assert.Equal(0, added);
                view.OnShown();
                Assert.Contains(view.Children.OfType<TextBlock>(), t => t.Visibility == Visibility.Visible && t.Text == Strings.T("shelf.saveFailed"));
                store.Clear();
                store.Remove(original.Id);
                store.RemoveMissing();
                Assert.Same(original, Assert.Single(store.Items));
                Assert.Equal(0, changes);
                Assert.Equal(before, File.ReadAllBytes(index));
            }
            Assert.True(store.TryAdd([second], out var retried));
            Assert.Equal(1, retried);
            Assert.False(store.SaveFailed);
            Assert.Equal(1, changes);
            var restarted = new ShelfStore(index);
            restarted.Load();
            Assert.Equal([second, first], restarted.Items.Select(i => i.Path));
            Assert.True(store.TryAdd([second], out var duplicates));
            Assert.Equal(0, duplicates);
        });
    }
}
