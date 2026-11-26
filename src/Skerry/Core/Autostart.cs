using Microsoft.Win32;

namespace Skerry.Core;

/// <summary>"Start with Windows" via HKCU\...\Run (no admin needed).</summary>
public static class Autostart
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "Skerry";
    const string LegacyValueName = "Isle";   // the app's former name

    public static bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey);
                return key?.GetValue(ValueName) is string s && s.Length > 0;
            }
            catch { return false; }
        }
    }

    /// <summary>Replaces a "Start with Windows" entry left by the former name (it points at the old Isle.exe).</summary>
    public static void MigrateLegacy()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key?.GetValue(LegacyValueName) is null) return;
            key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
            Set(true);
        }
        catch (Exception ex) { Logger.Error("autostart", ex); }
    }

    public static void Set(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled)
            {
                if (Environment.ProcessPath is not { } app) return;
                var launcher = Path.ChangeExtension(app, ".Launcher.exe");
                key.SetValue(ValueName, $"\"{(File.Exists(launcher) ? launcher : app)}\"");
            }
            else key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch (Exception ex) { Logger.Error("autostart", ex); }
    }
}
