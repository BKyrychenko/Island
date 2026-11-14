using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Isle.Core;

namespace Isle;

/// <summary>
/// The island: a fixed-size, transparent, topmost, non-activating tool window at the top-centre of the
/// selected monitor. The window never resizes during animation; only the inner shape changes.
/// </summary>
public partial class IslandWindow : Window
{
    /// <summary>Window size at 100% UI scale (DIPs): room for the largest expanded state plus shadow.</summary>
    public const double BaseWidth = 640, BaseHeight = 300;

    IntPtr _hwnd;
    HwndSource? _source;

    public IslandWindow()
    {
        InitializeComponent();
        ApplyScale();
        SourceInitialized += OnSourceInitialized;
    }

    public IntPtr Handle => _hwnd;

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
    }

    /// <summary>Tool window (no taskbar/Alt+Tab), never activates; optionally fully click-through.</summary>
    public void ApplyExStyle()
    {
        if (_hwnd == IntPtr.Zero) return;
        var ex = (long)Native.GetWindowLongPtr(_hwnd, Native.GWL_EXSTYLE);
        ex |= Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
        if (Settings.Current.ClickThrough) ex |= Native.WS_EX_TRANSPARENT;
        else ex &= ~(long)Native.WS_EX_TRANSPARENT;
        Native.SetWindowLongPtr(_hwnd, Native.GWL_EXSTYLE, new IntPtr(ex));
    }

    /// <summary>Places the window at the top-centre of the selected monitor (physical pixels, mixed-DPI safe).</summary>
    public void Reposition()
    {
        if (_hwnd == IntPtr.Zero) return;
        try
        {
            var screen = Screens.Selected(Settings.Current.Monitor);
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
            case Native.WM_DISPLAYCHANGE:
            case Native.WM_DPICHANGED:
            case Native.WM_SETTINGCHANGE:
                // let WPF finish its own handling first
                Dispatcher.BeginInvoke(Reposition, System.Windows.Threading.DispatcherPriority.Background);
                break;
        }
        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        _source?.RemoveHook(WndProc);
        base.OnClosed(e);
    }
}
