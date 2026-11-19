using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Isle.Core;
using Isle.Modules.Hotkeys;

namespace Isle;

/// <summary>
/// The island: a fixed-size, transparent, topmost, non-activating tool window at the top-centre of the
/// selected monitor. The window never resizes during animation; only the inner shape (a clip) changes.
/// Split into partials: this file (window + native plumbing), Motion (springs/shape), Hover (pointer, focus,
/// visibility rules) and Content (pill queue, tabs, toast).
/// </summary>
public partial class IslandWindow : Window
{
    /// <summary>Window size at 100% UI scale (DIPs): room for the largest expanded state plus overshoot and shadow.</summary>
    public const double BaseWidth = 640, BaseHeight = 300;
    public const double ExpandedWidth = 520, ExpandedHeight = 220, TopGap = 6, MaxRadius = 30;

    IntPtr _hwnd;
    HwndSource? _source;
    readonly bool _snapshotMode;
    DispatcherTimer? _housekeeping;

    public PillQueue Pill { get; } = new();
    public HotkeyService Hotkeys { get; } = new();
    public ForegroundWatcher Watcher { get; } = new();
    public IntPtr Handle => _hwnd;

    /// <param name="snapshotMode">True for the --snapshot renderer: no hooks, timers or hover logic.</param>
    public IslandWindow(bool snapshotMode = false)
    {
        _snapshotMode = snapshotMode;
        InitializeComponent();
        InitMotion();
        InitContent();
        ApplyScale();
        if (snapshotMode) return;

        SourceInitialized += OnSourceInitialized;
        InitHover();
        InitDrop();
        Settings.Changed += OnSettingsChanged;
        Theme.Changed += OnThemeChanged;
    }

    void ApplyScale()
    {
        var s = Settings.Current.Scale;
        UiScale.ScaleX = UiScale.ScaleY = s;
        Width = BaseWidth * s;
        Height = BaseHeight * s;
    }

    void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(WndProc);
        ApplyExStyle();
        Reposition();
        Hotkeys.Attach(_hwnd);
        Watcher.Changed += OnForegroundChanged;
        Watcher.Start();
        UpdateHousekeeping();
    }

    void OnSettingsChanged(string what)
    {
        try
        {
            Strings.Language = Settings.Current.Language;
            Theme.Apply();
            if (Math.Abs(UiScale.ScaleX - Settings.Current.Scale) > 0.001)
            {
                ApplyScale();
                Dispatcher.BeginInvoke(Reposition, DispatcherPriority.Loaded);
            }
            else if (what is "Monitor" or "") Reposition();
            ApplyExStyle();
            UpdateIdlePill();
            UpdateHousekeeping();
            CheckForeground();
            UpdateEdgeHide();
        }
        catch (Exception ex) { Logger.Error("apply settings", ex); }
    }

    void OnThemeChanged() => ApplyShape();

    /// <summary>Tool window (no taskbar/Alt+Tab), never activated by clicks; optionally fully click-through.</summary>
    public void ApplyExStyle()
    {
        if (_hwnd == IntPtr.Zero) return;
        var ex = (long)Native.GetWindowLongPtr(_hwnd, Native.GWL_EXSTYLE);
        ex |= Native.WS_EX_TOOLWINDOW;
        // no-activate always, except while a text box inside the island owns the keyboard
        if (_hasFocus) ex &= ~(long)Native.WS_EX_NOACTIVATE;
        else ex |= Native.WS_EX_NOACTIVATE;
        // click-through, except while the settings panel is open (otherwise it could never be switched off there)
        if (Settings.Current.ClickThrough && !(_expanded && _overlay is Views.SettingsPanel.SettingsView { IsVisible: true })) ex |= Native.WS_EX_TRANSPARENT;
        else ex &= ~(long)Native.WS_EX_TRANSPARENT;
        Native.SetWindowLongPtr(_hwnd, Native.GWL_EXSTYLE, new IntPtr(ex));
    }

    /// <summary>The monitor the island currently lives on.</summary>
    public ScreenInfo Screen => Screens.Selected(Settings.Current.Monitor);

    /// <summary>Places the window at the top-centre of the selected monitor (physical pixels, mixed-DPI safe).</summary>
    public void Reposition()
    {
        if (_hwnd == IntPtr.Zero) return;
        try
        {
            var screen = Screen;
            const uint flags = Native.SWP_NOSIZE | Native.SWP_NOACTIVATE;
            // First move onto the monitor so WPF adopts its DPI, then centre using the resulting pixel width.
            Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, screen.Left, screen.Top, 0, 0, flags);
            var dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;
            var widthPx = (int)Math.Round(Width * dpi);
            Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, screen.Left + (screen.Width - widthPx) / 2, screen.Top, 0, 0, flags);
        }
        catch (Exception ex) { Logger.Error("reposition", ex); }
    }

    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case Native.WM_MOUSEACTIVATE:
                handled = true;
                return new IntPtr(Native.MA_NOACTIVATE);
            case Native.WM_HOTKEY:
                var id = wParam.ToInt32();
                Hotkeys.HandleHotkey(id);
                handled = true;
                break;
            case Native.WM_DISPLAYCHANGE:
            case Native.WM_DPICHANGED:
                Dispatcher.BeginInvoke(Reposition, DispatcherPriority.Background); // after WPF's own handling
                break;
            case Native.WM_SETTINGCHANGE:
                Dispatcher.BeginInvoke(() =>
                {
                    Theme.Apply();   // "System" theme follows Windows live
                    Reposition();    // work area / taskbar changes
                }, DispatcherPriority.Background);
                break;
        }
        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        Settings.Changed -= OnSettingsChanged;
        Theme.Changed -= OnThemeChanged;
        _housekeeping?.Stop();
        _hoverTimer?.Stop();
        StopAnimation();
        Hotkeys.Detach();
        Watcher.Dispose();
        _source?.RemoveHook(WndProc);
        base.OnClosed(e);
    }
}
