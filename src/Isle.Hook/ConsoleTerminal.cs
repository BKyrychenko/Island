using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Isle.Agents;

internal static class ConsoleTerminal
{
    // A hook exits after sending its event. Keep the oldest other member of the same console,
    // typically the shell, and its creation time so a recycled PID can't redirect the card.
    public static HookProtocol.ConsoleTarget? Capture()
    {
        if (!OperatingSystem.IsWindows()) return null;
        var ids = new uint[32];
        var count = GetConsoleProcessList(ids, (uint)ids.Length);
        var attached = count == 0 && AttachConsole(uint.MaxValue);
        try
        {
            if (attached) count = GetConsoleProcessList(ids, (uint)ids.Length);
            if (count > ids.Length && count <= 256)
            {
                ids = new uint[count];
                count = GetConsoleProcessList(ids, count);
            }
            if (count == 0 || count > ids.Length) return null;
            HookProtocol.ConsoleTarget? oldest = null;
            foreach (var id in ids.AsSpan(0, (int)count))
            {
                if (id == 0 || id > int.MaxValue || id == Environment.ProcessId) continue;
                var started = StartTicks((int)id);
                if (started > 0 && (oldest is null || started < oldest.StartedUtcTicks))
                    oldest = new((int)id, started);
            }
            return oldest;
        }
        finally { if (attached) FreeConsole(); }
    }

    public static async Task<int> Probe(string[] args)
    {
        if (!OperatingSystem.IsWindows() || args.Length != 4 ||
            !int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out var pid) || pid <= 0 ||
            !long.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out var started) || started <= 0 ||
            !Guid.TryParseExact(args[3], "N", out var id) || StartTicks(pid) != started) return 1;

        using var input = new StreamReader(Console.OpenStandardInput());
        using var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
        FreeConsole();
        if (!AttachConsole((uint)pid)) return 1;
        try
        {
            if (StartTicks(pid) != started) return 1;
            var hwnd = GetConsoleWindow();
            var windowClass = new StringBuilder(64);
            if (hwnd != IntPtr.Zero && IsWindowVisible(hwnd) && GetClassName(hwnd, windowClass, windowClass.Capacity) > 0 &&
                windowClass.ToString() == "ConsoleWindowClass" && GetWindowThreadProcessId(hwnd, out var owner) != 0)
            {
                await output.WriteLineAsync(FormattableString.Invariant($"window {hwnd.ToInt64()} {owner}"));
                return 0;
            }

            // ConPTY exposes no visible console HWND. Use a unique temporary title so the UI
            // can identify its exact Windows Terminal tab even when titles are otherwise identical.
            var original = ReadTitle();
            var marker = $"Isle-{id:N}";
            if (original is null || !SetConsoleTitle(marker)) return 1;
            try
            {
                await output.WriteLineAsync("title");
                // Console input is a redirected pipe. Bound the marker lifetime even if the UI exits.
                await Task.Run(input.ReadLine).WaitAsync(TimeSpan.FromSeconds(3));
            }
            catch (TimeoutException) { }
            finally { if (ReadTitle() == marker) SetConsoleTitle(original); }
            return 0;
        }
        finally { FreeConsole(); }
    }

    static string? ReadTitle()
    {
        var title = new StringBuilder(24_576);
        return GetConsoleTitle(title, (uint)title.Capacity) > 0 || Marshal.GetLastPInvokeError() == 0 ? title.ToString() : null;
    }

    static long StartTicks(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return process.StartTime.ToUniversalTime().Ticks;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception) { return 0; }
    }

    [DllImport("kernel32.dll")] static extern uint GetConsoleProcessList([Out] uint[] processList, uint count);
    [DllImport("kernel32.dll")] static extern bool AttachConsole(uint processId);
    [DllImport("kernel32.dll")] static extern bool FreeConsole();
    [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern uint GetConsoleTitle(StringBuilder title, uint size);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern bool SetConsoleTitle(string title);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr window, StringBuilder name, int size);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
