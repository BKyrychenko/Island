using System.Runtime.InteropServices;

namespace Skerry.Core;

/// <summary>One physical monitor, in physical pixels.</summary>
public readonly record struct ScreenInfo(IntPtr Handle, int Left, int Top, int Width, int Height, bool IsPrimary, double DpiScale, string Device);

public static class Screens
{
    public static List<ScreenInfo> All()
    {
        var list = new List<ScreenInfo>();
        Native.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (h, _, _, _) =>
        {
            var mi = new Native.MONITORINFOEX { cbSize = Marshal.SizeOf<Native.MONITORINFOEX>() };
            if (Native.GetMonitorInfo(h, ref mi))
            {
                var scale = Native.GetDpiForMonitor(h, 0, out var dpiX, out _) == 0 ? dpiX / 96.0 : 1.0;
                list.Add(new ScreenInfo(h, mi.rcMonitor.Left, mi.rcMonitor.Top, mi.rcMonitor.Width, mi.rcMonitor.Height,
                    (mi.dwFlags & Native.MONITORINFOF_PRIMARY) != 0, scale, mi.szDevice));
            }
            return true;
        }, IntPtr.Zero);
        // stable order: primary first, then left-to-right
        list.Sort((a, b) => a.IsPrimary != b.IsPrimary ? (a.IsPrimary ? -1 : 1) : a.Left != b.Left ? a.Left.CompareTo(b.Left) : a.Top.CompareTo(b.Top));
        return list;
    }

    /// <summary>The monitor chosen in settings (-1 or out of range = primary).</summary>
    public static ScreenInfo Selected(int index, string? device = null)
    {
        var all = All();
        if (all.Count == 0) return new ScreenInfo(IntPtr.Zero, 0, 0, 1920, 1080, true, 1.0, "");
        return Resolve(all, index, device);
    }

    internal static ScreenInfo Resolve(IReadOnlyList<ScreenInfo> all, int index, string? device)
    {
        if (!string.IsNullOrEmpty(device))
            return all.FirstOrDefault(s => string.Equals(s.Device, device, StringComparison.OrdinalIgnoreCase), all[0]);
        return index >= 0 && index < all.Count ? all[index] : all[0];
    }
}
