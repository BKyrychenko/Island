using System.Windows;
using System.Windows.Controls;
using Isle.Core;
using Isle.Views.Characters;

namespace Isle.Modules.Characters;

/// <summary>--snapshot states for the characters (still frames, no clock).</summary>
static class CharacterSnapshots
{
    static readonly GalleryActions NoOp = new(_ => { }, _ => { }, _ => { }, () => { });

    public static void Register(Dictionary<string, Action<IslandWindow>> states)
    {
        states["chars-gallery"] = w =>
        {
            var g = new CharacterGallery(NoOp, null);
            g.Show("sprig", true, 5);
            w.ShowOverlay(g);
        };
        states["chars-sheet"] = w => w.ShowOverlay(Sheet());
        states["chars-panel"] = w =>
        {
            w.Tabs.Mascot = Still(CreatureDesign.Pebb, Mood.Awake, CharactersModule.PanelSize, new Vector(1.2, 0.6));
            w.AddTab(new TabInfo("demo", "tab.timers", "Icon.Timer", () => new TextBlock(), 10));
            w.AddTab(new TabInfo("demo2", "tab.notes", "Icon.Notes", () => new TextBlock(), 20));
            w.Expand("demo");
        };
        foreach (var d in CreatureDesign.All)
            states[$"chars-pill-{d.Id}"] = w => w.IdleView.LeftSlot = Still(d, Mood.Awake, CharactersModule.PillSize, new Vector(1, 0.4));
        states["chars-pill-sleep"] = w => w.IdleView.LeftSlot = Still(CreatureDesign.Pebb, Mood.Sleeping, CharactersModule.PillSize);
        states["chars-pill-dance"] = w => w.IdleView.LeftSlot = Still(CreatureDesign.Kubo, Mood.Dancing, CharactersModule.PillSize);
        states["chars-pill-happy"] = w => w.IdleView.LeftSlot = Still(CreatureDesign.Sprig, Mood.Happy, CharactersModule.PillSize);
    }

    static CreatureView Still(CreatureDesign d, Mood mood, double size, Vector gaze = default, double t = 0.4)
    {
        var v = new CreatureView(d, null) { Width = size, Height = size };
        v.Still(mood, t, gaze);
        return v;
    }

    /// <summary>Every character in every mood, large, for close inspection.</summary>
    static FrameworkElement Sheet()
    {
        var grid = new Grid { Margin = new Thickness(16, 8, 16, 8) };
        grid.SetResourceReference(Panel.BackgroundProperty, "BgBrush");
        var moods = new[] { Mood.Awake, Mood.Dancing, Mood.Happy, Mood.Sleeping };
        for (var c = 0; c < moods.Length + 1; c++) grid.ColumnDefinitions.Add(new ColumnDefinition());
        for (var r = 0; r < CreatureDesign.All.Count; r++) grid.RowDefinitions.Add(new RowDefinition());
        for (var r = 0; r < CreatureDesign.All.Count; r++)
        {
            var d = CreatureDesign.All[r];
            var blink = Still(d, Mood.Awake, 56, default);
            blink.Still(Mood.Awake, 1, default, 0.09); // mid-blink
            Place(grid, blink, r, 0);
            for (var c = 0; c < moods.Length; c++)
                Place(grid, Still(d, moods[c], 56, moods[c] == Mood.Awake ? new Vector(-1.3, 0.5) : default, moods[c] == Mood.Dancing ? 0.2 : 0.4), r, c + 1);
        }
        var bg = new Grid();
        bg.SetResourceReference(Panel.BackgroundProperty, "BgBrush");
        bg.Children.Add(grid);
        return bg;
    }

    static void Place(Grid g, UIElement e, int r, int c)
    {
        Grid.SetRow(e, r);
        Grid.SetColumn(e, c);
        g.Children.Add(e);
    }
}
