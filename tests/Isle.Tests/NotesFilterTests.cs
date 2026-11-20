using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Isle.Core;
using Isle.Modules.Notes;
using Isle.Views.Notes;

namespace Isle.Tests;

[Collection(nameof(GlobalSettingsTests))]
public class NotesFilterTests
{
    [Fact]
    public void Folder_and_search_keep_edits_and_actions_on_visible_notes()
    {
        FakeSink.Run(sink =>
        {
            var store = new NotesStore(null);
            var alpha = store.Create("Alpha", "First draft");
            var beta = store.Create("Beta", "Second draft");
            var previous = Settings.Current.Opt("notes.last");
            try
            {
                using var view = new NotesView(store, null);
                var editor = Elements(view).OfType<RichTextBox>().Single();
                var search = Elements(view).OfType<TextBox>().Single();
                Button Tool(string key) => Elements(view).OfType<Button>().Single(b => Equals(b.ToolTip, Strings.T(key)));
                TextRange Body() => new(editor.Document.ContentStart, editor.Document.ContentEnd);
                void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                void ChooseFolder(string folder, string tool = "notes.folders")
                {
                    Click(Tool(tool));
                    Click(Elements(view).OfType<Button>().Single(b => b.Content is TextBlock t && t.Text == folder));
                }

                ChooseFolder("Alpha");
                Body().Text = "Edited Alpha";
                ChooseFolder("Beta");
                Assert.Equal("Edited Alpha", alpha.Plain);
                Assert.Equal("Second draft", Body().Text.TrimEnd());
                Assert.Equal(beta.Id, Elements(view).OfType<Button>().Single(b => b.Tag is string).Tag);

                search.Text = "missing";
                Assert.Equal(Visibility.Collapsed, editor.Visibility);
                Assert.All(new[] { "notes.delete", "notes.pin", "notes.move", "notes.bold" },
                    key => Assert.False(Tool(key).IsEnabled));
                search.Text = "second";
                Assert.True(Tool("notes.delete").IsEnabled);
                Body().Text = "Revised Beta";
                Assert.True(sink.PumpUntil(() => editor.Visibility == Visibility.Collapsed));
                Assert.Equal("Revised Beta", beta.Plain);
                search.Clear();
                Assert.Equal("Revised Beta", Body().Text.TrimEnd());
                Click(Tool("notes.delete"));
                Assert.Null(store.Find(beta.Id));
                Assert.Same(alpha, Assert.Single(store.Notes));
                Assert.Equal(Visibility.Collapsed, editor.Visibility);
                var moved = store.Create("Beta", "Move this note");
                ChooseFolder("Alpha", "notes.move");
                Assert.Equal("Alpha", moved.Folder);
                Assert.Equal(Visibility.Collapsed, editor.Visibility);
            }
            finally { Settings.Current.SetOpt("notes.last", previous); }
        });
    }

    static IEnumerable<DependencyObject> Elements(DependencyObject parent)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var descendant in Elements(child)) yield return descendant;
        }
    }
}
