using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Isle.Core;
using Microsoft.Win32;
using Windows.Devices.Radios;

namespace Isle.Modules.ControlCenter;

/// <summary>Wi-Fi and Bluetooth radios (Windows.Devices.Radios). Degrades to "unavailable" when access is denied or a radio is missing.</summary>
public sealed class Radios(Dispatcher ui) : IDisposable
{
    Radio? _wifi, _bt;
    bool _initialised;

    public bool Denied { get; private set; }
    public bool? WifiOn => _wifi is null ? null : _wifi.State == RadioState.On;
    public bool? BluetoothOn => _bt is null ? null : _bt.State == RadioState.On;

    /// <summary>Raised on the UI thread when a radio changes state.</summary>
    public event Action? Changed;

    public async Task InitAsync()
    {
        if (_initialised) return;
        _initialised = true;
        try
        {
            var access = await Radio.RequestAccessAsync();
            if (access != RadioAccessStatus.Allowed) { Denied = true; Logger.Warn($"radio access: {access}"); return; }
            foreach (var r in await Radio.GetRadiosAsync())
            {
                if (r.Kind == RadioKind.WiFi && _wifi is null) _wifi = r;
                else if (r.Kind == RadioKind.Bluetooth && _bt is null) _bt = r;
                else continue;
                r.StateChanged += OnStateChanged;
            }
        }
        catch (Exception ex) { Logger.Error("radios", ex); Denied = true; }
        Raise();
    }

    void OnStateChanged(Radio sender, object args) => ui.BeginInvoke(Raise);

    void Raise()
    {
        try { Changed?.Invoke(); }
        catch (Exception ex) { Logger.Error("radios changed handler", ex); }
    }

    /// <summary>Turns a radio on/off. Returns false when Windows refused.</summary>
    public async Task<bool> SetAsync(RadioKind kind, bool on)
    {
        var r = kind == RadioKind.WiFi ? _wifi : _bt;
        if (r is null) return false;
        try
        {
            var result = await r.SetStateAsync(on ? RadioState.On : RadioState.Off);
            return result == RadioAccessStatus.Allowed;
        }
        catch (Exception ex) { Logger.Error("set radio", ex); return false; }
    }

    public void Dispose()
    {
        if (_wifi is not null) _wifi.StateChanged -= OnStateChanged;
        if (_bt is not null) _bt.StateChanged -= OnStateChanged;
        _wifi = _bt = null;
    }
}

/// <summary>The Windows app/system colour mode (HKCU Personalize), with the broadcast Windows itself sends.</summary>
public static class WindowsTheme
{
    const string Key = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public static bool IsDark
    {
        get
        {
            try
            {
                using var k = Registry.CurrentUser.OpenSubKey(Key);
                return k?.GetValue("AppsUseLightTheme") is int v && v == 0;
            }
            catch { return false; }
        }
    }

    public static bool SetDark(bool dark)
    {
        try
        {
            using var k = Registry.CurrentUser.CreateSubKey(Key, writable: true);
            k.SetValue("AppsUseLightTheme", dark ? 0 : 1, RegistryValueKind.DWord);
            k.SetValue("SystemUsesLightTheme", dark ? 0 : 1, RegistryValueKind.DWord);
            Native.SendMessageTimeout(Native.HWND_BROADCAST, Native.WM_SETTINGCHANGE, IntPtr.Zero, "ImmersiveColorSet",
                Native.SMTO_ABORTIFHUNG, 1000, out _);
            return true;
        }
        catch (Exception ex) { Logger.Error("set windows theme", ex); return false; }
    }
}

/// <summary>Built-in display brightness through WMI (laptop panels). <see cref="Supported"/> is false on desktops.</summary>
public sealed class Brightness
{
    public bool Supported { get; private set; }
    public int Level { get; private set; }

    /// <summary>Reads the current level (background thread). Returns false when unsupported.</summary>
    public async Task<bool> RefreshAsync()
    {
        var (ok, level) = await Task.Run(() =>
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT CurrentBrightness FROM WmiMonitorBrightness");
                foreach (var o in searcher.Get())
                    using (o) return (true, Convert.ToInt32(o["CurrentBrightness"]));
            }
            catch (ManagementException) { /* not supported */ }
            catch (COMException) { }
            catch (Exception ex) { Logger.Error("brightness read", ex); }
            return (false, 0);
        });
        Supported = ok;
        if (ok) Level = level;
        return ok;
    }

    int _pending = -1;
    bool _writing;

    /// <summary>Sets brightness 0–100. Calls coalesce while a WMI write is in flight (slider drags).</summary>
    public async Task SetAsync(int level)
    {
        level = Math.Clamp(level, 0, 100);
        Level = level;
        _pending = level;
        if (_writing) return;
        _writing = true;
        try
        {
            while (_pending >= 0)
            {
                var value = _pending;
                _pending = -1;
                await Task.Run(() =>
                {
                    try
                    {
                        using var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM WmiMonitorBrightnessMethods");
                        foreach (ManagementObject o in searcher.Get())
                            using (o) o.InvokeMethod("WmiSetBrightness", [(uint)1, (byte)value]);
                    }
                    catch (Exception ex) { Logger.Error("brightness write", ex); }
                });
            }
        }
        finally { _writing = false; }
    }
}
