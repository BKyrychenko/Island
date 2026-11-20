using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using Isle.Agents;
using Isle.Core;

namespace Isle.Modules.Agents;

internal static class AgentTerminal
{
    public static async Task<IntPtr> FindAsync(HookProtocol.ConsoleTarget target)
    {
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, HookInstaller.ExeName))
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardInput = true,
        };
        var probe = Guid.NewGuid().ToString("N");
        start.ArgumentList.Add("--terminal-probe");
        start.ArgumentList.Add(target.ProcessId.ToString(CultureInfo.InvariantCulture));
        start.ArgumentList.Add(target.StartedUtcTicks.ToString(CultureInfo.InvariantCulture));
        start.ArgumentList.Add(probe);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            using var helper = Process.Start(start);
            if (helper is null) return IntPtr.Zero;
            try
            {
                var reply = await helper.StandardOutput.ReadLineAsync(timeout.Token).ConfigureAwait(false);
                if (reply == "title")
                    return await Task.Run(() => SelectWindowsTerminalTab("Isle-" + probe, timeout.Token))
                        .WaitAsync(timeout.Token).ConfigureAwait(false);

                if (reply?.Split(' ') is ["window", var hwnd, var owner] &&
                    long.TryParse(hwnd, NumberStyles.None, CultureInfo.InvariantCulture, out var value) &&
                    uint.TryParse(owner, NumberStyles.None, CultureInfo.InvariantCulture, out var pid))
                {
                    var window = new IntPtr(value);
                    if (Native.IsWindowVisible(window) && Native.GetWindowThreadProcessId(window, out var actual) != 0 && actual == pid)
                        return window;
                }
            }
            finally { helper.StandardInput.Close(); }
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException or System.ComponentModel.Win32Exception or InvalidOperationException) { }
        return IntPtr.Zero;
    }

    static IntPtr SelectWindowsTerminalTab(string marker, CancellationToken ct)
    {
        var windows = new List<IntPtr>();
        Native.EnumWindows((window, _) =>
        {
            if (Native.IsWindowVisible(window) && ForegroundWatcher.ClassName(window) == "CASCADIA_HOSTING_WINDOW_CLASS") windows.Add(window);
            return true;
        }, IntPtr.Zero);
        if (windows.Count == 0) return IntPtr.Zero;

        var tabItems = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem);
        while (!ct.IsCancellationRequested)
        {
            foreach (var window in windows)
            {
                if (ct.IsCancellationRequested) return IntPtr.Zero;
                try
                {
                    var root = AutomationElement.FromHandle(window);
                    foreach (AutomationElement item in root.FindAll(TreeScope.Descendants, tabItems))
                    {
                        if (!item.Current.Name.Contains(marker, StringComparison.Ordinal) || ct.IsCancellationRequested) continue;
                        if (item.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pattern) && pattern is SelectionItemPattern selection)
                        {
                            if (!selection.Current.IsSelected) selection.Select();
                            if (selection.Current.IsSelected) return window;
                        }
                    }
                    // Terminal can hide its tab strip when there's only one tab.
                    if (root.Current.Name.Contains(marker, StringComparison.Ordinal)) return window;
                }
                catch (Exception ex) when (ex is ElementNotAvailableException or InvalidOperationException or COMException) { }
            }
            ct.WaitHandle.WaitOne(60);
        }
        return IntPtr.Zero;
    }

    public static bool Activate(IntPtr window)
    {
        if (window == IntPtr.Zero || !Native.IsWindow(window)) return false;
        if (Native.IsIconic(window)) ShowWindowAsync(window, 9); // SW_RESTORE
        return Native.GetForegroundWindow() == window || Native.SetForegroundWindow(window);
    }

    [DllImport("user32.dll")] static extern bool ShowWindowAsync(IntPtr window, int command);
}
