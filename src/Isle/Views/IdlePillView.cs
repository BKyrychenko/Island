using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Isle.Core;

namespace Isle.Views;

/// <summary>
/// The idle pill: nothing by default; optional clock in the centre, and two small slots that other modules
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
    DispatcherTimer? _timer;

    /// <summary>Test/snapshot hook for a fixed time.</summary>
    public Func<DateTime> Now { get; set; } = () => DateTime.Now;

    public IdlePillView()
    {
        Children.Add(_left);
        Children.Add(_clock);
        Children.Add(_right);
        IsVisibleChanged += (_, _) => UpdateTimer();
    }

    public object? LeftSlot { get => _left.Content; set { _left.Content = value; } }
    public object? RightSlot { get => _right.Content; set { _right.Content = value; } }

    public double DesiredPillWidth => 120;

    public void Refresh()
    {
        var show = Settings.Current.ShowClock;
        _clock.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show) _clock.Text = Now().ToString("HH:mm");
        UpdateTimer();
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
