using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Isle.Core;
using Isle.Modules.Characters;

namespace Isle.Views.Characters;

/// <summary>What the gallery can change (the module applies and saves it).</summary>
public sealed record GalleryActions(Action<string> Pick, Action<bool> InPill, Action<int> SleepMinutes, Action Close);

/// <summary>
/// Full-panel overlay: the three characters as live cards (pick one), "show in idle pill" and the nap delay.
/// </summary>
public sealed class CharacterGallery : Grid
{
    public static readonly int[] SleepChoices = [2, 5, 15, 0];

    readonly GalleryActions _actions;
    readonly CreatureBrain? _brain;
    readonly StackPanel _cards = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
    readonly WrapPanel _options = new() { Margin = new Thickness(0, 10, 0, 0) };

    public CharacterGallery(GalleryActions actions, CreatureBrain? brain)
    {
        _actions = actions;
        _brain = brain;
        SetResourceReference(BackgroundProperty, "BgBrush");
        Margin = new Thickness(0);
        Children.Add(_root);
    }

    readonly StackPanel _root = new() { Margin = new Thickness(16, 12, 16, 8) };

    /// <summary>Rebuilds the content for the current choice (cheap: three cards and a few chips).</summary>
    public void Show(string selected, bool inPill, int sleepMinutes)
    {
        _root.Children.Clear();

        var head = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 0, 0, 8) };
        var close = new Button { ToolTip = Strings.T("chars.close"), Width = 28, Height = 28 };
        close.SetResourceReference(StyleProperty, "IconButton");
        var x = new Icon { Width = 14, Height = 14 };
        x.SetResourceReference(Icon.DataProperty, "Icon.Close");
        x.SetResourceReference(Icon.ForegroundProperty, "FgDimBrush");
        close.Content = x;
        close.Click += (_, _) => _actions.Close();
        DockPanel.SetDock(close, Dock.Right);
        head.Children.Add(close);
        var title = new TextBlock { Text = Strings.T("chars.title"), VerticalAlignment = VerticalAlignment.Center };
        title.SetResourceReference(StyleProperty, "TextTitle");
        head.Children.Add(title);
        _root.Children.Add(head);

        _cards.Children.Clear();
        foreach (var d in CreatureDesign.All) _cards.Children.Add(Card(d, d.Id == selected));
        _root.Children.Add(_cards);

        _options.Children.Clear();
        var pill = Chip(Strings.T("chars.inPill"), inPill);
        pill.Click += (_, _) => _actions.InPill(pill.IsChecked == true);
        pill.Margin = new Thickness(0, 0, 16, 4);
        _options.Children.Add(pill);
        var napLabel = new TextBlock { Text = Strings.T("chars.sleep"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 4), FontSize = 12 };
        napLabel.SetResourceReference(StyleProperty, "TextDim");
        _options.Children.Add(napLabel);
        foreach (var m in SleepChoices)
        {
            var chip = Chip(m == 0 ? Strings.T("chars.never") : Strings.T("chars.min", m), m == sleepMinutes);
            var minutes = m;
            chip.Click += (_, _) => _actions.SleepMinutes(minutes);
            chip.Margin = new Thickness(0, 0, 4, 4);
            _options.Children.Add(chip);
        }
        _root.Children.Add(_options);
    }

    FrameworkElement Card(CreatureDesign d, bool selected)
    {
        var creature = new CreatureView(d, _brain) { Width = 56, Height = 56, HorizontalAlignment = HorizontalAlignment.Center };
        if (_brain is null) creature.Still(Mood.Awake);
        var name = new TextBlock { Text = Strings.T(d.NameKey), HorizontalAlignment = HorizontalAlignment.Center, FontWeight = FontWeights.SemiBold, FontSize = 13, Margin = new Thickness(0, 4, 0, 0) };
        name.SetResourceReference(StyleProperty, "Text");
        name.FontWeight = FontWeights.SemiBold;
        var trait = new TextBlock
        {
            Text = Strings.T(d.TraitKey), HorizontalAlignment = HorizontalAlignment.Center, FontSize = 11.5,
            TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap,
        };
        trait.SetResourceReference(StyleProperty, "TextDim");
        trait.FontSize = 11.5;
        var stack = new StackPanel();
        stack.Children.Add(creature);
        stack.Children.Add(name);
        stack.Children.Add(trait);
        var card = new Border { Child = stack, Width = 140, Margin = new Thickness(4, 0, 4, 0), Padding = new Thickness(8, 8, 8, 8), BorderThickness = new Thickness(2), Cursor = Cursors.Hand };
        card.SetResourceReference(StyleProperty, "Card");
        card.Padding = new Thickness(8);
        if (selected) card.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");
        else card.BorderBrush = System.Windows.Media.Brushes.Transparent;
        card.MouseLeftButtonUp += (_, e) => { e.Handled = true; _actions.Pick(d.Id); };
        AutomationName(card, Strings.T(d.NameKey));
        return card;
    }

    static ToggleButton Chip(string text, bool on)
    {
        var c = new ToggleButton { Content = text, IsChecked = on };
        c.SetResourceReference(StyleProperty, "Chip");
        return c;
    }

    static void AutomationName(DependencyObject o, string name) =>
        System.Windows.Automation.AutomationProperties.SetName(o, name);
}
