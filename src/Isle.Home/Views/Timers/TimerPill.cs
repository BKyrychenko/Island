using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Isle.Core;
using Isle.Modules.Reminders;
using Isle.Modules.Timers;

namespace Isle.Views.Timers;

/// <summary>Thin progress ring (track + accent arc), drawn directly.</summary>
public sealed class Ring : FrameworkElement
{
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(nameof(Progress), typeof(double), typeof(Ring),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(nameof(Brush), typeof(Brush), typeof(Ring),
        new FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty TrackProperty = DependencyProperty.Register(nameof(Track), typeof(Brush), typeof(Ring),
        new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Progress { get => (double)GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }
    public Brush Brush { get => (Brush)GetValue(BrushProperty); set => SetValue(BrushProperty, value); }
    public Brush Track { get => (Brush)GetValue(TrackProperty); set => SetValue(TrackProperty, value); }
    public double Thickness { get; set; } = 2.5;

    public Ring(double size)
    {
        Width = Height = size;
        IsHitTestVisible = false;
        SetResourceReference(BrushProperty, "AccentTextBrush");
        SetResourceReference(TrackProperty, "SurfaceHoverBrush");
    }

    protected override void OnRender(DrawingContext dc)
    {
        var r = Math.Min(ActualWidth, ActualHeight) / 2 - Thickness / 2;
        if (r <= 0) return;
        var c = new Point(ActualWidth / 2, ActualHeight / 2);
        dc.DrawEllipse(null, new Pen(Track, Thickness), c, r, r);
        // the remaining part is drawn (it shrinks as time passes, like a countdown)
        var remaining = Math.Clamp(1 - Progress, 0, 1);
        if (remaining <= 0.001) return;
        var pen = new Pen(Brush, Thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        if (remaining >= 0.999) { dc.DrawEllipse(null, pen, c, r, r); return; }
        var angle = remaining * 2 * Math.PI;
        var start = new Point(c.X, c.Y - r);
        var end = new Point(c.X + r * Math.Sin(angle), c.Y - r * Math.Cos(angle));
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(start, false, false);
            ctx.ArcTo(end, new Size(r, r), 0, remaining > 0.5, SweepDirection.Clockwise, true, false);
        }
        g.Freeze();
        dc.DrawGeometry(null, pen, g);
    }
}

/// <summary>Pill view for the soonest timer: ring on the left, remaining time on the right.</summary>
public sealed class TimerPill : Grid
{
    public const double PillWidth = 150, CompactWidth = 62;

    readonly TimersController _ctl;
    readonly Ring _ring = new(18) { HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(10, 0, 0, 0) };
    readonly TextBlock _time = new()
    {
        FontSize = 14, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 14, 0),
    };
    readonly TextBlock _label = new()
    {
        FontSize = 11.5, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(36, 0, 70, 0),
        TextTrimming = TextTrimming.CharacterEllipsis,
    };
    readonly List<(Ring Ring, TextBlock Text)> _compacts = [];
    string? _id;

    public TimerPill(TimersController ctl)
    {
        _ctl = ctl;
        _time.Typography.NumeralAlignment = FontNumeralAlignment.Tabular;
        _label.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
        Children.Add(_ring);
        Children.Add(_label);
        Children.Add(_time);
    }

    public void Show(string id)
    {
        _id = id;
        Refresh();
    }

    public FrameworkElement CreateCompact()
    {
        var ring = new Ring(14) { Thickness = 2, Margin = new Thickness(0, 0, 5, 0) };
        var text = new TextBlock { FontSize = 12, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        text.Typography.NumeralAlignment = FontNumeralAlignment.Tabular;
        var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 12, 0) };
        panel.Children.Add(ring);
        panel.Children.Add(text);
        _compacts.Add((ring, text));
        if (_compacts.Count > 4) _compacts.RemoveAt(0);
        Refresh();
        return panel;
    }

    public void Refresh()
    {
        if (_id is null || _ctl.Timers.Find(_id) is not { } t) return;
        var now = _ctl.Clock();
        var remaining = t.Remaining(now);
        _ring.Progress = t.Progress(now);
        _time.Text = DurationParser.Format(remaining);
        _time.Opacity = t.IsPaused ? 0.55 : 1;
        _label.Text = t.Label;
        foreach (var (ring, text) in _compacts)
        {
            ring.Progress = _ring.Progress;
            text.Text = remaining.TotalMinutes >= 1 ? $"{Math.Ceiling(remaining.TotalMinutes - 1e-9)}{Strings.T("unit.mShort")}" : $"{Math.Ceiling(remaining.TotalSeconds)}{Strings.T("unit.sShort")}";
        }
    }
}

/// <summary>"Time's up" banner: label, +1 min and Dismiss.</summary>
public sealed class TimerDoneBanner : Grid
{
    public const double BannerWidth = 380, BannerHeight = 68;

    public TimerDoneBanner(TimersController ctl, TimerItem t)
    {
        var ring = new Ring(26) { Progress = 0, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(12, 0, 0, 0) };
        var check = new Icon { Width = 14, Height = 14, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(18, 0, 0, 0) };
        check.SetResourceReference(Icon.DataProperty, "Icon.Check");
        check.SetResourceReference(Icon.ForegroundProperty, "AccentTextBrush");
        var title = new TextBlock { FontSize = 13, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, MaxHeight = 36 };
        title.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("[timers.timesUp]") { Source = Strings.Instance });
        var sub = new TextBlock { Text = t.Label.Length > 0 ? t.Label : DurationParser.Describe(t.Duration, UnitName), TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = t.Label };
        sub.SetResourceReference(StyleProperty, "TextDim");
        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(48, 0, 150, 0) };
        text.Children.Add(title);
        text.Children.Add(sub);

        var plus = BannerButton("timers.plus1", accent: false, () => ctl.AddMinute(t.Id));
        var dismiss = BannerButton("timers.dismiss", accent: true, () => ctl.Dismiss(t.Id));
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
        buttons.Children.Add(plus);
        buttons.Children.Add(dismiss);

        Children.Add(ring);
        Children.Add(check);
        Children.Add(text);
        Children.Add(buttons);
    }

    internal static string UnitName(string u) => Strings.T("unit." + u);

    internal static Button BannerButton(string key, bool accent, Action click)
    {
        var b = Ui.TextButton("", accent ? "AccentButton" : "FlatButton", new Thickness(10, 3, 10, 3), click, "banner button", minHeight: 26);
        b.Margin = new Thickness(6, 0, 0, 0);
        b.SetBinding(ContentControl.ContentProperty, Ui.Loc(key));
        return b;
    }
}

/// <summary>Reminder banner: text on top, Done / Snooze 5 min / 1 h / Tomorrow below.</summary>
public sealed class ReminderBanner : Grid
{
    public const double BannerWidth = 420, BannerHeight = 98;

    public ReminderBanner(TimersController ctl, Reminder r)
    {
        var bell = new Icon { Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        bell.SetResourceReference(Icon.DataProperty, "Icon.Bell");
        bell.SetResourceReference(Icon.ForegroundProperty, "AccentTextBrush");
        var title = new TextBlock { FontSize = 13, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, MaxHeight = 38, VerticalAlignment = VerticalAlignment.Center };
        title.Text = r.Text.Length > 0 ? r.Text : Strings.T("rem.title");
        title.ToolTip = title.Text;
        var top = new DockPanel { Margin = new Thickness(16, 10, 16, 0), VerticalAlignment = VerticalAlignment.Top, LastChildFill = true };
        top.Children.Add(bell);
        top.Children.Add(title);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(10, 0, 0, 10) };
        buttons.Children.Add(TimerDoneBanner.BannerButton("rem.done", accent: true, () => ctl.ReminderDone(r.Id)));
        buttons.Children.Add(TimerDoneBanner.BannerButton("rem.snooze5", accent: false, () => ctl.Snooze(r.Id, TimeSpan.FromMinutes(5))));
        buttons.Children.Add(TimerDoneBanner.BannerButton("rem.snooze1h", accent: false, () => ctl.Snooze(r.Id, TimeSpan.FromHours(1))));
        buttons.Children.Add(TimerDoneBanner.BannerButton("rem.tomorrow", accent: false, () => ctl.SnoozeTomorrow(r.Id)));
        Children.Add(top);
        Children.Add(buttons);
    }
}
