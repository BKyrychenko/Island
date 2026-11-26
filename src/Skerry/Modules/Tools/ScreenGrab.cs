using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Skerry.Core;

namespace Skerry.Modules.Tools;

/// <summary>A rectangle in physical (virtual-screen) pixels.</summary>
public readonly record struct PxRect(int X, int Y, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;
    public bool Contains(int x, int y) => x >= X && y >= Y && x < X + Width && y < Y + Height;

    public PxRect Intersect(PxRect o)
    {
        int l = Math.Max(X, o.X), t = Math.Max(Y, o.Y), r = Math.Min(X + Width, o.X + o.Width), b = Math.Min(Y + Height, o.Y + o.Height);
        return r <= l || b <= t ? default : new PxRect(l, t, r - l, b - t);
    }

    /// <summary>Normalised rectangle from two corner points (any drag direction).</summary>
    public static PxRect FromPoints(int x1, int y1, int x2, int y2) =>
        new(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x2 - x1), Math.Abs(y2 - y1));
}

/// <summary>GDI screen capture (BitBlt, physical pixels — the process is per-monitor DPI aware) and window rectangles.</summary>
public static class ScreenGrab
{
    /// <summary>Captures a virtual-screen rectangle into a frozen BGRA bitmap (96 dpi: 1 px = 1 unit).</summary>
    public static BitmapSource Capture(PxRect r)
    {
        var screen = Native.GetDC(IntPtr.Zero);
        var mem = Native.CreateCompatibleDC(screen);
        var bmp = Native.CreateCompatibleBitmap(screen, r.Width, r.Height);
        var old = Native.SelectObject(mem, bmp);
        try
        {
            Native.BitBlt(mem, 0, 0, r.Width, r.Height, screen, r.X, r.Y, Native.SRCCOPY | Native.CAPTUREBLT);
            Native.SelectObject(mem, old);
            var src = Imaging.CreateBitmapSourceFromHBitmap(bmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            // normalise to Bgra32 at 96 dpi so crops and encoders see exact pixels
            var conv = new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
            var stride = r.Width * 4;
            var pixels = new byte[stride * r.Height];
            conv.CopyPixels(pixels, stride, 0);
            for (var i = 3; i < pixels.Length; i += 4) pixels[i] = 255;
            var result = BitmapSource.Create(r.Width, r.Height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
            result.Freeze();
            return result;
        }
        finally
        {
            Native.DeleteObject(bmp);
            Native.DeleteDC(mem);
            Native.ReleaseDC(IntPtr.Zero, screen);
        }
    }

    public static BitmapSource Crop(BitmapSource source, Int32Rect r)
    {
        var c = new CroppedBitmap(source, r);
        c.Freeze();
        return c;
    }

    /// <summary>Visible top-level windows (top of the Z-order first) with their visible frame, excluding our own and cloaked ones.</summary>
    public static List<PxRect> WindowRects()
    {
        var list = new List<PxRect>();
        Native.EnumWindows((h, _) =>
        {
            if (!ForegroundWatcher.TryGetExternalWindowBounds(h, out var rc) || rc.Width < 40 || rc.Height < 30) return true;
            list.Add(new PxRect(rc.Left, rc.Top, rc.Width, rc.Height));
            return true;
        }, IntPtr.Zero);
        return list;
    }

    public static string SaveFolder(string sub)
    {
        var root = sub == "Videos" ? Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var dir = Path.Combine(root, "Skerry");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string SavePng(BitmapSource image)
    {
        var path = Path.Combine(SaveFolder("Pictures"), $"Skerry {DateTime.Now:yyyy-MM-dd HH.mm.ss}.png");
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(image));
        using (var fs = File.Create(path)) enc.Save(fs);
        Logger.Info("screenshot saved");
        return path;
    }
}
