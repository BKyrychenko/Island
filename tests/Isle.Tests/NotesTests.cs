using Isle.Modules.Notes;

namespace Isle.Tests;

public class NotesStoreTests
{
    static Func<DateTimeOffset> Clock()
    {
        var t = new DateTimeOffset(2026, 3, 11, 10, 0, 0, TimeSpan.Zero);
        return () => t = t.AddMinutes(1);
    }

    [Fact]
    public void Crud_and_persistence()
    {
        using var dir = new TempDir();
        var path = dir.File("notes.json");
        var s = new NotesStore(path, Clock());
        var a = s.Create(plain: "Shopping\nmilk");
        var b = s.Create("Work", "Plan\nrelease");
        Assert.True(s.Update(a.Id, "<Section/>", "Shopping list\nmilk, bread"));
        Assert.Equal("Shopping list", s.Find(a.Id)!.Title);
        Assert.Equal("milk, bread", s.Find(a.Id)!.Preview);

        var again = new NotesStore(path, Clock());
        Assert.Equal(2, again.Notes.Count);
        Assert.Equal("<Section/>", again.Find(a.Id)!.Xaml);
        Assert.Equal(["Work"], again.Folders);

        var removed = again.Delete(b.Id);
        Assert.NotNull(removed);
        Assert.Single(again.Notes);
        again.Restore(removed.Value.Note, removed.Value.Index); // undo
        Assert.Equal(2, new NotesStore(path, Clock()).Notes.Count);
    }

    [Fact]
    public void Search_folders_and_pins()
    {
        var s = new NotesStore(null, Clock());
        var a = s.Create(plain: "Groceries milk bread");
        var b = s.Create("Work", "Meeting about the Release");
        var c = s.Create("Work", "Release checklist");
        Assert.Equal([c.Id, b.Id], s.Search("release").Select(n => n.Id));          // newest first, case-insensitive
        Assert.Equal([b.Id], s.Search("release meeting").Select(n => n.Id));        // every word must match
        s.SetPinned(b.Id, true);
        Assert.Equal([b.Id, c.Id], s.Search("release").Select(n => n.Id));          // pinned first
        Assert.Equal([a.Id], s.Search(null, "").Select(n => n.Id));                 // "no folder"
        Assert.Equal(2, s.Search("", "Work").Count);
        s.Move(a.Id, "Home");
        Assert.Contains("Home", s.Folders);
        s.DeleteFolder("Work");
        Assert.All(s.Notes.Where(n => n.Id != a.Id), n => Assert.Equal("", n.Folder));
        Assert.False(s.AddFolder("home")); // case-insensitive duplicate
    }

    [Fact]
    public void Corrupt_file_is_backed_up_and_replaced()
    {
        using var dir = new TempDir();
        var path = dir.File("notes.json");
        File.WriteAllText(path, "{ this is not json");
        var s = new NotesStore(path, Clock());
        Assert.Empty(s.Notes);
        Assert.Single(Directory.GetFiles(dir.Path, "notes.json.bak-*"));
        s.Create(plain: "fresh start");
        Assert.Single(new NotesStore(path, Clock()).Notes);
    }

    [Fact]
    public void Save_is_atomic_no_temp_left()
    {
        using var dir = new TempDir();
        var path = dir.File("notes.json");
        var s = new NotesStore(path, Clock());
        for (var i = 0; i < 5; i++) s.Create(plain: "n" + i);
        Assert.Empty(Directory.GetFiles(dir.Path, "*.tmp"));
        Assert.Equal(5, new NotesStore(path, Clock()).Notes.Count);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("\n\n  Hello  \nworld", "Hello")]
    [InlineData("☐ Buy milk\n☑ Bread", "Buy milk")]
    public void Titles(string plain, string title) => Assert.Equal(title, new Note { Plain = plain }.Title);
}

public class NotesFormattingTests
{
    static void Sta(Action a)
    {
        Exception? error = null;
        var t = new Thread(() => { try { a(); } catch (Exception ex) { error = ex; } });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
        if (error is not null) throw new Exception("STA test failed", error);
    }

    [Fact]
    public void Formatting_survives_save_and_load()
    {
        Sta(() =>
        {
            var doc = Isle.Views.Notes.NotesView.NewDocument();
            var p = new System.Windows.Documents.Paragraph();
            var bold = new System.Windows.Documents.Run("bold");
            var under = new System.Windows.Documents.Run("under");
            p.Inlines.Add(bold);
            p.Inlines.Add(new System.Windows.Documents.Run(" "));
            p.Inlines.Add(under);
            doc.Blocks.Add(p);
            var heading = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Title")) { FontSize = 18, FontWeight = System.Windows.FontWeights.Bold };
            doc.Blocks.InsertBefore(p, heading);
            new System.Windows.Documents.TextRange(bold.ContentStart, bold.ContentEnd).ApplyPropertyValue(System.Windows.Documents.TextElement.FontWeightProperty, System.Windows.FontWeights.Bold);
            new System.Windows.Documents.TextRange(under.ContentStart, under.ContentEnd).ApplyPropertyValue(System.Windows.Documents.Inline.TextDecorationsProperty, System.Windows.TextDecorations.Underline);
            var list = new System.Windows.Documents.List { MarkerStyle = System.Windows.TextMarkerStyle.Decimal };
            list.ListItems.Add(new System.Windows.Documents.ListItem(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("one"))));
            doc.Blocks.Add(list);

            var (xaml, plain) = Isle.Views.Notes.NotesView.Serialize(doc);
            Assert.Contains("Title", plain);
            Assert.Contains("one", plain);

            var back = Isle.Views.Notes.NotesView.NewDocument();
            Isle.Views.Notes.NotesView.Load(back, new Note { Xaml = xaml, Plain = plain });
            var text = new System.Windows.Documents.TextRange(back.ContentStart, back.ContentEnd);
            Assert.Contains("bold under", text.Text);
            var first = (System.Windows.Documents.Paragraph)back.Blocks.FirstBlock;
            Assert.Equal(18, first.FontSize);
            Assert.Contains(back.Blocks, b => b is System.Windows.Documents.List { MarkerStyle: System.Windows.TextMarkerStyle.Decimal });
            var runs = back.Blocks.OfType<System.Windows.Documents.Paragraph>().SelectMany(x => x.Inlines).OfType<System.Windows.Documents.Run>().ToList();
            Assert.Contains(runs, r => r.Text == "bold" && r.FontWeight == System.Windows.FontWeights.Bold);
            Assert.Contains(runs, r => r.Text == "under" && r.TextDecorations.Count > 0);
        });
    }
}
