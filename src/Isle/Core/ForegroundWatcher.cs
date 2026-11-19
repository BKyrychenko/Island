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
        // Windows' own verdict: 2 = busy (full-screen app), 3 = D3D exclusive full screen, 4 = presentation mode
        if (screen.Handle != IntPtr.Zero && Native.MonitorFromWindow(hwnd, 0) == screen.Handle &&
            Native.SHQueryUserNotificationState(out var state) == 0 && state is 2 or 3 or 4) return true;
        var cls = ClassName(hwnd);
        if (cls is "Progman" or "WorkerW" or "Shell_TrayWnd" or "XamlExplorerHostIslandWindow" or "Windows.UI.Core.CoreWindow") return false;
        // a maximized window (browsers draw their own title bar, so no WS_CAPTION) is not full screen
        if (Native.IsZoomed(hwnd)) return false;
        if (!Native.GetWindowRect(hwnd, out var r)) return false;
        return r.Left <= screen.Left && r.Top <= screen.Top && r.Right >= screen.Left + screen.Width && r.Bottom >= screen.Top + screen.Height;
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
