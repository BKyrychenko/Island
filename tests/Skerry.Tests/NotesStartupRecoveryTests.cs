using Skerry.Core;
using Skerry.Modules.Notes;
using Skerry.Views.Notes;

namespace Skerry.Tests;

[Collection(nameof(GlobalSettingsTests))]
public class NotesStartupRecoveryTests
{
    [Fact]
    public void Opening_the_notes_tab_retries_a_locked_startup_read_without_an_edit()
    {
        FakeSink.Run(_ =>
        {
            using var dir = new TempDir();
            var path = dir.File("notes.json");
            const string json = """{"Notes":[{"Id":"old","Plain":"Saved note"}]}""";
            File.WriteAllText(path, json);
            var previous = Settings.Current.Opt("notes.last");
            try
            {
                using var locked = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                var store = new NotesStore(path);
                using var view = new NotesView(store, null);
                view.OnShown();
                Assert.Empty(store.Notes);
                locked.Dispose();
                view.OnShown();
                Assert.Equal("Saved note", Assert.Single(store.Notes).Plain);
                Assert.Equal(json, File.ReadAllText(path));
            }
            finally { Settings.Current.SetOpt("notes.last", previous); }
        });
    }

    [Fact]
    public void Retry_merges_drafts_with_the_latest_readable_notes_only_after_replacement_succeeds()
    {
        using var dir = new TempDir();
        var path = dir.File("notes.json");
        File.WriteAllText(path, """{"Notes":[{"Id":"old","Plain":"first version","Folder":"Work"}]}""");
        NotesStore store;
        Note draft;
        using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            store = new NotesStore(path);
            draft = store.Create("work", "draft");
            Assert.False(store.Update(draft.Id, "<draft/>", "edited draft"));
            Assert.True(store.HasUnsavedChanges);
        }

        using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Assert.False(store.Flush());
            Assert.Equal("first version", new NotesStore(path).Find("old")!.Plain);
        }
        File.WriteAllText(path, """{"Notes":[null,{"Id":"old","Plain":"latest","Folder":"Work"},{"Id":"second","Plain":null}]}""");
        Assert.True(store.Flush());
        Assert.False(store.HasUnsavedChanges);
        Assert.Same(draft, store.Find(draft.Id));
        Assert.Equal("latest", store.Find("old")!.Plain);
        var reopened = new NotesStore(path);
        Assert.Equal(3, reopened.Notes.Count);
        Assert.Equal(["Work"], reopened.Folders);
        Assert.Equal("", reopened.Find("second")!.Plain);
        Assert.Equal("edited draft", reopened.Find(draft.Id)!.Plain);
        Assert.Equal("<draft/>", reopened.Find(draft.Id)!.Xaml);
    }

    [Fact]
    public void Clean_flush_restores_originals_and_notifies_the_view_without_writing()
    {
        using var dir = new TempDir();
        var path = dir.File("notes.json");
        const string json = """{"Notes":[{"Id":"old","Plain":"old note"}]}""";
        File.WriteAllText(path, json);
        NotesStore store;
        using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
            store = new NotesStore(path);
        var changes = 0;
        store.Changed += () => changes++;
        Assert.True(store.Flush());
        Assert.True(store.Flush());
        Assert.Equal(1, changes);
        Assert.Equal("old note", Assert.Single(store.Notes).Plain);
        Assert.Equal(json, File.ReadAllText(path));
    }

    [Fact]
    public void Retry_refuses_to_replace_an_absent_corrupt_or_conflicting_original()
    {
        using var dir = new TempDir();
        var path = dir.File("notes.json");
        File.WriteAllText(path, """{"Notes":[{"Id":"old","Plain":"old note"}]}""");
        NotesStore store;
        using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
            store = new NotesStore(path);
        File.Delete(path);
        var draft = store.Create(plain: "pending");
        Assert.True(store.HasUnsavedChanges);
        Assert.False(File.Exists(path));
        File.WriteAllText(path, "{corrupt");
        Assert.False(store.Flush());
        Assert.Equal("{corrupt", File.ReadAllText(path));
        var conflicting = $$"""{"Notes":[{"Id":"{{draft.Id}}","Plain":"another note"}]}""";
        File.WriteAllText(path, conflicting);
        Assert.False(store.Flush());
        Assert.Equal(conflicting, File.ReadAllText(path));
        Assert.Equal("pending", Assert.Single(store.Notes).Plain);
    }
}
