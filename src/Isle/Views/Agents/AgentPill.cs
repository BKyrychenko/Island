using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Isle.Modules.Agents;

namespace Isle.Views.Agents;

/// <summary>
/// Agent status in the collapsed pill: a dot (working = slow pulse, waiting = attention colour, done = check) plus
/// project text. The pulse runs at 12 fps and only while the element is loaded (on screen).
/// </summary>
public sealed class AgentPill : Grid
{
    readonly FrameworkElement _mark;
    readonly bool _pulse;

    public AgentPill(AgentState state, string text, string sub = "")
    {
        _mark = Mark(state);
        _pulse = state == AgentState.Working;
        var row = new DockPanel { Margin = new Thickness(14, 0, 14, 0) };
        _mark.Margin = new Thickness(0, 0, 8, 0);
        DockPanel.SetDock(_mark, Dock.Left);
        row.Children.Add(_mark);
        if (sub.Length > 0)
        {
            var s = new TextBlock { Text = sub, FontSize = 11.5, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0), MaxWidth = 150, TextTrimming = TextTrimming.CharacterEllipsis };
            s.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
            DockPanel.SetDock(s, Dock.Right);
            row.Children.Add(s);
        }
        var t = new TextBlock { Text = text, FontSize = 12.5, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
        t.SetResourceReference(TextBlock.ForegroundProperty, "FgBrush");
        row.Children.Add(t);
        Children.Add(row);
        if (_pulse) { Loaded += (_, _) => StartPulse(_mark); Unloaded += (_, _) => _mark.BeginAnimation(OpacityProperty, null); }
    }

    /// <summary>Just the status mark, for the pill's side slot.</summary>
    public static FrameworkElement Compact(AgentState state)
    {
        var m = Mark(state);
        m.HorizontalAlignment = HorizontalAlignment.Center;
        if (state == AgentState.Working) { m.Loaded += (_, _) => StartPulse(m); m.Unloaded += (_, _) => m.BeginAnimation(OpacityProperty, null); }
        return m;
    }

    static FrameworkElement Mark(AgentState state)
    {
        if (state == AgentState.Done)
        {
            var i = new Icon { Width = 15, Height = 15, VerticalAlignment = VerticalAlignment.Center };
            i.SetResourceReference(Icon.DataProperty, "Icon.Check");
            i.SetResourceReference(Icon.ForegroundProperty, "OkBrush");
            return i;
        }
        var e = new Ellipse { Width = 8, Height = 8, VerticalAlignment = VerticalAlignment.Center };
        e.SetResourceReference(Shape.FillProperty, state == AgentState.Waiting ? "WarnBrush" : "AccentTextBrush");
        return e;
    }

    static void StartPulse(FrameworkElement e)
    {
        var a = new DoubleAnimation(1, 0.3, TimeSpan.FromMilliseconds(700)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase() };
        Timeline.SetDesiredFrameRate(a, 12);
        e.BeginAnimation(OpacityProperty, a);
    }
}
