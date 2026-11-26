using System.Runtime.InteropServices;
using System.Windows.Interop;
using Skerry.Core;

namespace Skerry.Modules.Tray;

/// <summary>
/// Notification-area icon with a native popup menu (Show/Hide, Settings, Start with Windows, Quit).
/// Uses Shell_NotifyIcon directly so WinForms is never loaded.
/// </summary>
public sealed class TrayModule : IModule
{
    const int CallbackMessage = Native.WM_USER + 71;
    const int CmdToggle = 1, CmdSettings = 2, CmdAutostart = 3, CmdQuit = 4;

    readonly Func<bool> _isIslandVisible;
    readonly Action _toggleIsland, _openSettings, _quit;
    HwndSource? _source;
    IntPtr _icon;
    int _taskbarCreated;

    public string Id => "tray";

    public TrayModule(Func<bool> isIslandVisible, Action toggleIsland, Action openSettings, Action quit)
    {
        _isIslandVisible = isIslandVisible;
        _toggleIsland = toggleIsland;
        _openSettings = openSettings;
        _quit = quit;
    }

    public void Start()
    {
        var p = new HwndSourceParameters("SkerryTray")
        {
            Width = 0, Height = 0,
            WindowStyle = Native.WS_POPUP,
            ExtendedWindowStyle = Native.WS_EX_TOOLWINDOW,
        };
        _source = new HwndSource(p);
        _source.AddHook(WndProc);
        _taskbarCreated = Native.RegisterWindowMessage("TaskbarCreated");
        _icon = TrayIcon.Create(32);
        AddIcon();
    }

    public void Stop()
    {
        if (_source is not null)
        {
            var data = NewData();
            Native.Shell_NotifyIcon(Native.NIM_DELETE, ref data);
            _source.RemoveHook(WndProc);
            _source.Dispose();
            _source = null;
        }
        if (_icon != IntPtr.Zero)
        {
            Native.DestroyIcon(_icon);
            _icon = IntPtr.Zero;
        }
    }

    Native.NOTIFYICONDATA NewData() => new()
    {
        cbSize = Marshal.SizeOf<Native.NOTIFYICONDATA>(),
        hWnd = _source!.Handle,
        uID = 1,
        uFlags = Native.NIF_MESSAGE | Native.NIF_ICON | Native.NIF_TIP,
        uCallbackMessage = CallbackMessage,
        hIcon = _icon,
        szTip = Strings.T("app.name"),
        szInfo = "",
        szInfoTitle = "",
    };

    void AddIcon()
    {
        var data = NewData();
        if (!Native.Shell_NotifyIcon(Native.NIM_ADD, ref data))
            Logger.Warn("tray icon could not be added (explorer not ready?)");
    }

    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        try
        {
            if (msg == CallbackMessage)
            {
                var mouse = (int)lParam & 0xFFFF;
                if (mouse == Native.WM_LBUTTONUP) _toggleIsland();
                else if (mouse is Native.WM_RBUTTONUP or Native.WM_CONTEXTMENU) ShowMenu();
                handled = true;
            }
            else if (msg == _taskbarCreated && _taskbarCreated != 0)
            {
                AddIcon(); // explorer restarted
            }
        }
        catch (Exception ex) { Logger.Error("tray", ex); }
        return IntPtr.Zero;
    }

    void ShowMenu()
    {
        var menu = Native.CreatePopupMenu();
        try
        {
            Native.AppendMenu(menu, Native.MF_STRING, CmdToggle, Strings.T(_isIslandVisible() ? "tray.hide" : "tray.show"));
            Native.AppendMenu(menu, Native.MF_STRING, CmdSettings, Strings.T("tray.settings"));
            Native.AppendMenu(menu, Native.MF_STRING | (Autostart.IsEnabled ? Native.MF_CHECKED : 0), CmdAutostart, Strings.T("tray.autostart"));
            Native.AppendMenu(menu, Native.MF_SEPARATOR, 0, null);
            Native.AppendMenu(menu, Native.MF_STRING, CmdQuit, Strings.T("tray.quit"));

            Native.GetCursorPos(out var pt);
            Native.SetForegroundWindow(_source!.Handle); // required so the menu closes when clicking elsewhere
            var cmd = Native.TrackPopupMenuEx(menu, Native.TPM_RIGHTBUTTON | Native.TPM_RETURNCMD | Native.TPM_NONOTIFY, pt.X, pt.Y, _source.Handle, IntPtr.Zero);
            Native.PostMessage(_source.Handle, Native.WM_NULL, IntPtr.Zero, IntPtr.Zero);

            switch (cmd)
            {
                case CmdToggle: _toggleIsland(); break;
                case CmdSettings: _openSettings(); break;
                case CmdAutostart: Autostart.Set(!Autostart.IsEnabled); break;
                case CmdQuit: _quit(); break;
            }
        }
        finally { Native.DestroyMenu(menu); }
    }
}

/// <summary>Draws Skerry's tray icon in code: a dark capsule with a light ring and a blue dot.</summary>
public static class TrayIcon
{
    /// <summary>Straight-alpha BGRA pixels, top-down.</summary>
    public static byte[] Render(int size)
    {
        var px = new byte[size * size * 4];
        double s = size / 32.0;
        double r = 7.5 * s, x0 = 2.5 * s + r, x1 = size - 2.5 * s - r, cy = size / 2.0;
        double dotX = x1 + 0.5 * s, dotR = 3.2 * s;

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            double cx = x + 0.5, py = y + 0.5;
            double nx = Math.Clamp(cx, x0, x1);
            double d = Math.Sqrt((cx - nx) * (cx - nx) + (py - cy) * (py - cy)) - r; // signed distance to capsule
            double alpha = Math.Clamp(0.5 - d, 0, 1);
            if (alpha <= 0) continue;

            double ring = Math.Clamp(1.0 - Math.Abs(d + 1.2 * s) / (0.9 * s), 0, 1);   // light outline just inside the edge
            double dd = Math.Sqrt((cx - dotX) * (cx - dotX) + (py - cy) * (py - cy)) - dotR;
            double dot = Math.Clamp(0.5 - dd, 0, 1);

            // base near-black, ring light grey, dot soft blue (B, G, R)
            double b = 14, g = 14, rr = 14;
            b += (235 - b) * ring; g += (235 - g) * ring; rr += (235 - rr) * ring;
            b += (255 - b) * dot; g += (170 - g) * dot; rr += (90 - rr) * dot;

            var i = (y * size + x) * 4;
            px[i] = (byte)b; px[i + 1] = (byte)g; px[i + 2] = (byte)rr; px[i + 3] = (byte)(alpha * 255);
        }
        return px;
    }

    public static IntPtr Create(int size)
    {
        var color = Native.CreateBitmap(size, size, 1, 32, Render(size));
        var mask = Native.CreateBitmap(size, size, 1, 1, new byte[size * size / 8 + size]);
        try
        {
            var info = new Native.ICONINFO { fIcon = true, hbmColor = color, hbmMask = mask };
            return Native.CreateIconIndirect(ref info);
        }
        finally
        {
            Native.DeleteObject(color);
            Native.DeleteObject(mask);
        }
    }
}
