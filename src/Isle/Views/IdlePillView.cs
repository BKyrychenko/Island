using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Isle.Core;

namespace Isle.Views;

/// <summary>
/// The idle pill: a first-use hint, an optional clock in the centre, and two small slots that other modules
/// may fill (left: mascot, right: weather temperature).
/// </summary>
public sealed class IdlePillView : Grid
{
    readonly TextBlock _clock = new()
    {
        FontSize = 12.5, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center, Visibility = Visibility.Collapsed,
    };
    readonly ContentPresenter _left = new() { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
    readonly ContentPresenter _right = new() { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
    readonly TextBlock _hint = new()
    {
        FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
        IsHitTestVisible = false, Visibility = Visibility.Collapsed,
    };
    DispatcherTimer? _timer;

    /// <summary>Test/snapshot hook for a fixed time.</summary>
    public Func<DateTime> Now { get; set; } = () => DateTime.Now;

    public IdlePillView()
    {
        Children.Add(_left);
        Children.Add(_clock);
        _hint.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
        Children.Add(_hint);
        Children.Add(_right);
        IsVisibleChanged += (_, _) => UpdateTimer();
    }

    public object? LeftSlot { get => _left.Content; set { _left.Content = value; RefreshHint(); } }
    public object? RightSlot { get => _right.Content; set { _right.Content = value; RefreshHint(); } }

    public double DesiredPillWidth => 120;

    public void Refresh()
    {
        var show = Settings.Current.ShowClock;
        _clock.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show) _clock.Text = Now().ToString("HH:mm");
        RefreshHint();
        UpdateTimer();
    }

    internal void DismissHint()
    {
        var settings = Settings.Current;
        if (settings.OptBool("shell.opened") || settings.LastTab.Length > 0) return;
        settings.SetOpt("shell.opened", true);
        RefreshHint();
        JsonStore.Save(Paths.SettingsFile, settings);
    }

    void RefreshHint()
    {
        var settings = Settings.Current;
        var show = !settings.OptBool("shell.opened") && settings.LastTab.Length == 0 &&
            !settings.ShowClock && LeftSlot is null && RightSlot is null;
        _hint.Text = Strings.T("idle.openHint");
        _hint.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    // The clock ticks once a minute, aligned to the minute, and only while it is on screen.
    void UpdateTimer()
    {
        var needed = IsVisible && Settings.Current.ShowClock;
        if (!needed) { _timer?.Stop(); return; }
        _timer ??= new DispatcherTimer(DispatcherPriority.Background, Dispatcher);
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
        _clock.Text = Now().ToString("HH:mm");
        _timer.Interval = TimeSpan.FromSeconds(60 - Now().Second + 0.2);
        _timer.Start();
    }

    void OnTick(object? sender, EventArgs e) => UpdateTimer();
}
