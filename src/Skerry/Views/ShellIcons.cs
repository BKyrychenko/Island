using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Skerry.Views;

/// <summary>Shell icons (SHGetFileInfo, large) for files, folders and shortcuts. UI thread; small LRU-less cache (cleared at 300).</summary>
public static class ShellIcons
{
    static readonly Dictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static ImageSource? For(string path)
    {
        if (Cache.TryGetValue(path, out var cached)) return cached;
        if (Cache.Count > 300) Cache.Clear();
        var info = new Native.SHFILEINFO();
        ImageSource? src = null;
        try
        {
            var size = (uint)Marshal.SizeOf<Native.SHFILEINFO>();
            bool ok;
            if (path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
            {
                // Store apps and other virtual items: icon through the item's PIDL
                ok = false;
                if (Native.SHParseDisplayName(path, IntPtr.Zero, out var pidl, 0, out _) == 0 && pidl != IntPtr.Zero)
                {
                    try { ok = Native.SHGetFileInfoPidl(pidl, 0, ref info, size, Native.SHGFI_ICON | Native.SHGFI_LARGEICON | Native.SHGFI_PIDL) != IntPtr.Zero; }
                    finally { Marshal.FreeCoTaskMem(pidl); }
                }
            }
            else ok = Native.SHGetFileInfo(path, 0, ref info, size, Native.SHGFI_ICON | Native.SHGFI_LARGEICON) != IntPtr.Zero;
            if (ok && info.hIcon != IntPtr.Zero)
            {
                var bmp = Imaging.CreateBitmapSourceFromHIcon(info.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                bmp.Freeze();
                src = bmp;
            }
        }
        catch { src = null; }
        finally { if (info.hIcon != IntPtr.Zero) Native.DestroyIcon(info.hIcon); }
        Cache[path] = src;
        return src;
    }
}
