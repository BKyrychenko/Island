using System.Diagnostics;
using System.Text;

namespace Isle.Core;

/// <summary>
/// Event-driven foreground-window tracking (SetWinEventHook, no polling). Start once on the UI thread.
/// Also remembers the last foreground window that is not ours, which is the paste target for clipboard/voice/AI.
/// </summary>
public sealed class ForegroundWatcher : IDisposable
{
    const uint EVENT_SYSTEM_FOREGROUND = 3, EVENT_SYSTEM_MINIMIZEEND = 0x17, WINEVENT_OUTOFCONTEXT = 0, WINEVENT_SKIPOWNPROCESS = 2;

    readonly Native.WinEventDelegate _callback; // keep alive
    IntPtr _hook;

    public event Action<IntPtr>? Changed;

    /// <summary>Most recent foreground window belonging to another process.</summary>
    public IntPtr LastExternal { get; private set; }

    public ForegroundWatcher() => _callback = OnEvent;

    public void Start()
    {
        if (_hook != IntPtr.Zero) return;
        _hook = Native.SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _callback, 0, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
        var fg = Native.GetForegroundWindow();
        if (fg != IntPtr.Zero && !IsOwn(fg)) LastExternal = fg;
    }

    void OnEvent(IntPtr hook, uint evt, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
    {
        if (hwnd == IntPtr.Zero || idObject != 0) return;
        try
        {
            if (!IsOwn(hwnd)) LastExternal = hwnd;
            Changed?.Invoke(hwnd);
        }
        catch (Exception ex) { Logger.Error("foreground handler", ex); }
    }

    public static bool IsOwn(IntPtr hwnd)
    {
        Native.GetWindowThreadProcessId(hwnd, out var pid);
        return pid == (uint)Environment.ProcessId;
    }

    /// <summary>Process name without extension ("chrome"), or "" when it cannot be read.</summary>
    public static string ProcessName(IntPtr hwnd)
    {
        try
        {
            Native.GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0) return "";
            using var p = Process.GetProcessById((int)pid);
            return p.ProcessName;
        }
        catch { return ""; }
    }

    public static string ClassName(IntPtr hwnd)
    {
        var sb = new StringBuilder(128);
        return Native.GetClassName(hwnd, sb, sb.Capacity) > 0 ? sb.ToString() : "";
    }

    /// <summary>True when <paramref name="hwnd"/> is a real fullscreen window (game, video, F11 browser) on the given monitor.</summary>
    public static bool IsFullscreen(IntPtr hwnd, ScreenInfo screen)
    {
        if (hwnd == IntPtr.Zero || IsOwn(hwnd)) return false;
        if (Native.SHQueryUserNotificationState(out var state) == 0 && state == 3) return true; // exclusive D3D fullscreen
        var cls = ClassName(hwnd);
        if (cls is "Progman" or "WorkerW" or "Shell_TrayWnd" or "XamlExplorerHostIslandWindow" or "Windows.UI.Core.CoreWindow") return false;
        if (!Native.GetWindowRect(hwnd, out var r)) return false;
        var covers = r.Left <= screen.Left && r.Top <= screen.Top && r.Right >= screen.Left + screen.Width && r.Bottom >= screen.Top + screen.Height;
        if (!covers) return false;
        var style = (long)Native.GetWindowLongPtr(hwnd, Native.GWL_STYLE);
        const long WS_CAPTION = 0x00C00000, WS_MAXIMIZE = 0x01000000;
        var maximizedNormalWindow = (style & WS_MAXIMIZE) != 0 && (style & WS_CAPTION) == WS_CAPTION;
        return !maximizedNormalWindow;
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            Native.UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }
    }
}
