using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using Isle.Core;
using Isle.Modules.Notes;
using Isle.Modules.Shelf;
using Isle.Views.Notes;
using Isle.Views.Shelf;

namespace Isle.Tests;

public class NotesShelfViewTests
{
    [Fact]
    public void Compact_note_toolbar_keeps_every_button_on_one_row_at_full_hit_size()
    {
        FakeSink.Run(_ =>
        {
            var store = new NotesStore(null);
            store.Create(plain: "Toolbar fixture");
            var previous = Settings.Current.Opt("notes.last");
            using var view = new NotesView(store, null);
            Settings.Current.SetOpt("notes.last", previous);
            view.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/Isle;component/Styles/Theme.xaml", UriKind.Relative)
            });
            view.Resources["CompactTargetMin"] = 30d;
            var parameters = new HwndSourceParameters("IsleCompactNotes") { Width = 496, Height = 160 };
            parameters.SetPosition(-32000, -32000);
            using var source = new HwndSource(parameters) { RootVisual = view };
            view.Measure(new Size(496, 160));
            view.Arrange(new Rect(0, 0, 496, 160));
            view.UpdateLayout();

            var toolbar = (DockPanel)((Grid)view.Children[1]).Children[0];
            var buttons = toolbar.Children.OfType<Button>().Concat(
                toolbar.Children.OfType<Panel>().Single().Children.OfType<Button>()).ToList();
            Assert.Equal(11, buttons.Count);
            Assert.Single(buttons.Select(button => button.TranslatePoint(new Point(), toolbar).Y).Distinct());
            foreach (var button in buttons)
            {
                Assert.True(button.ActualWidth * .8 >= 24 && button.ActualHeight * .8 >= 24);
                foreach (var x in new[] { 1d, button.ActualWidth - 1 })
                foreach (var y in new[] { 1d, button.ActualHeight - 1 })
                {
                    var hit = (DependencyObject?)view.InputHitTest(button.TranslatePoint(new Point(x, y), view));
                    Assert.True(hit is not null && (hit == button || button.IsAncestorOf(hit)), button.ToolTip?.ToString());
                }
            }
        });
    }

    [Fact]
    public void Notes_reveal_active_row_on_reentry_without_moving_keyboard_focus()
    {
        FakeSink.Run(sink =>
        {
            var previous = Settings.Current.Opt("notes.last");
            try
            {
                var store = new NotesStore(null);
                var active = store.Create(plain: "Old active note");
                for (var i = 0; i < 30; i++) store.Create(plain: "New note " + i);
                Settings.Current.SetOpt("notes.last", active.Id);
                using var view = new NotesView(store, null);
                using var source = new HwndSource(new HwndSourceParameters("IsleNotesRevealTest") { Width = 700, Height = 180 });
                source.RootVisual = view;
                view.Measure(new Size(700, 180));
                view.Arrange(new Rect(0, 0, 700, 180));
                var left = (Grid)view.Children[0];
                var scroll = (ScrollViewer)left.Children[2];
                var rows = (StackPanel)scroll.Content;
                var search = ((Grid)((DockPanel)left.Children[0]).Children[2]).Children.OfType<TextBox>().Single();
                Keyboard.Focus(search);
                var focused = Keyboard.FocusedElement;
                Assert.Same(search, focused);
                view.OnShown();
                Assert.True(sink.PumpUntil(() => scroll.VerticalOffset > 0));
                Assert.Same(focused, Keyboard.FocusedElement);
                var row = rows.Children.OfType<Button>().Single(b => Equals(b.Tag, active.Id));
                Assert.InRange(row.TranslatePoint(new Point(), scroll).Y, 0, scroll.ActualHeight);

                row.Focus();
                Assert.Same(row, Keyboard.FocusedElement);
                store.SetPinned(active.Id, true);
                Assert.NotSame(row, rows.Children.OfType<Button>().Single(b => Equals(b.Tag, active.Id)));
                Assert.Equal(active.Id, Assert.IsType<Button>(Keyboard.FocusedElement).Tag);
            }
            finally { Settings.Current.SetOpt("notes.last", previous); }
        });
    }

    [Fact]
    public void Shelf_reentry_refreshes_missing_state_and_toggle_labels_follow_state()
    {
        using var dir = new TempDir();
        FakeSink.Run(_ =>
        {
            var exists = false;
            var store = new ShelfStore(dir.File("shelf.json"), exists: _ => exists);
            store.Add([dir.File("sample.txt")]);
            var view = new ShelfView(store, null);
            var tiles = (WrapPanel)((ScrollViewer)view.Children[1]).Content;
            string Name() => System.Windows.Automation.AutomationProperties.GetName((Button)tiles.Children[0]);
            view.OnShown();
            Assert.Contains(Strings.T("shelf.missing"), Name());
            view.OnHidden();
            exists = true;
            view.OnShown();
            Assert.DoesNotContain(Strings.T("shelf.missing"), Name());
            view.OnHidden();
            exists = false;
            view.OnShown();
            Assert.Contains(Strings.T("shelf.missing"), Name());
            var toggle = ((DockPanel)view.Children[0]).Children.OfType<ToggleButton>().Single();
            var label = Assert.IsType<TextBlock>(toggle.Content);
            toggle.IsChecked = false;
            Assert.Equal(Strings.T("shelf.removeAfterDropOff"), label.Text);
            toggle.IsChecked = true;
            Assert.Equal(Strings.T("shelf.removeAfterDropOn"), label.Text);
        });
    }
}
