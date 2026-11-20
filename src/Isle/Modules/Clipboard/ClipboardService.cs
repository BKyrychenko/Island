using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Isle.Core;

namespace Isle.Modules.Clipboard;

/// <summary>Privacy rules for clipboard capture (pure parts are unit-tested).</summary>
public static class ClipPrivacy
{
    /// <summary>Formats that password managers and other apps set to keep content out of clipboard history.</summary>
    public const string ExcludeFormat = "ExcludeClipboardContentFromMonitorProcessing";
    public const string HistoryFormat = "CanIncludeInClipboardHistory";
    public const string ViewerIgnoreFormat = "Clipboard Viewer Ignore";

    /// <summary>True when the owner/foreground process is in the excluded list (names without ".exe", case-insensitive).</summary>
    public static bool IsExcludedApp(string processName, IEnumerable<string> excluded)
    {
        if (string.IsNullOrEmpty(processName)) return false;
        foreach (var raw in excluded)
        {
            var x = raw.Trim();
            if (x.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) x = x[..^4];
            if (x.Length > 0 && string.Equals(x, processName, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>The apps excluded from the history: the island's excluded apps plus option "clip.excludedApps" (comma-separated).</summary>
    public static IEnumerable<string> ExcludedApps() =>
        Settings.Current.ExcludedApps.Concat(Settings.Current.Opt("clip.excludedApps").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

/// <summary>
/// Clipboard listener (AddClipboardFormatListener on a message-only window — event-driven), reader that honours the
/// privacy formats, and "paste into the previous window" (clipboard + SendInput Ctrl+V, then the old clipboard comes back).
/// Never logs clipboard content.
/// </summary>
public sealed class ClipboardService : IDisposable
{
    readonly ClipStore _store;
    readonly Func<IntPtr> _pasteTarget;
    readonly Dispatcher _ui;
    HwndSource? _source;
    readonly uint _fmtExclude = Native.RegisterClipboardFormat(ClipPrivacy.ExcludeFormat);
    readonly uint _fmtHistory = Native.RegisterClipboardFormat(ClipPrivacy.HistoryFormat);
    readonly uint _fmtViewerIgnore = Native.RegisterClipboardFormat(ClipPrivacy.ViewerIgnoreFormat);
    DispatcherTimer? _retry;
    int _retries;

    public ClipboardService(ClipStore store, Func<IntPtr> pasteTarget, Dispatcher ui)
    {
        _store = store;
        _pasteTarget = pasteTarget;
        _ui = ui;
    }

    public void Start()
    {
        var p = new HwndSourceParameters("IsleClipboard") { Width = 0, Height = 0, WindowStyle = 0, ParentWindow = Native.HWND_MESSAGE };
        _source = new HwndSource(p);
        _source.AddHook(WndProc);
        if (!Native.AddClipboardFormatListener(_source.Handle)) Logger.Warn($"clipboard listener failed ({Marshal.GetLastWin32Error()})");
    }

    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Native.WM_CLIPBOARDUPDATE)
        {
            handled = true;
            _retries = 0;
            Capture();
        }
        return IntPtr.Zero;
    }

    void Capture()
    {
        try
        {
            var seq = Native.GetClipboardSequenceNumber();
            if (ClipboardOps.IgnoreCapture(seq)) return;
            if (IsPrivate()) return;
            var owner = Native.GetClipboardOwner();
            var app = owner != IntPtr.Zero ? ForegroundWatcher.ProcessName(owner) : "";
            if (app.Length == 0) app = ForegroundWatcher.ProcessName(Native.GetForegroundWindow());
            if (ClipPrivacy.IsExcludedApp(app, ClipPrivacy.ExcludedApps())) return;

            if (System.Windows.Clipboard.ContainsText())
            {
                var text = System.Windows.Clipboard.GetText();
                if (text.Length <= 1_000_000) _store.AddText(text); // ignore huge blobs
            }
            else if (System.Windows.Clipboard.ContainsImage() && System.Windows.Clipboard.GetImage() is { } img)
                AddImage(img);
        }
        catch (COMException)
        {
            // clipboard is held by another process: try again shortly (max 4 times)
            if (_retries++ >= 4) return;
            _retry ??= new DispatcherTimer(TimeSpan.FromMilliseconds(80), DispatcherPriority.Background, (_, _) => { _retry!.Stop(); Capture(); }, _ui);
            _retry.Stop();
            _retry.Start();
        }
        catch (Exception ex) { Logger.Error("clipboard capture", ex); }
    }

    /// <summary>Content marked private by its owner (password managers etc.).</summary>
    bool IsPrivate()
    {
        if (Native.IsClipboardFormatAvailable(_fmtExclude) || Native.IsClipboardFormatAvailable(_fmtViewerIgnore)) return true;
        if (!Native.IsClipboardFormatAvailable(_fmtHistory)) return false;
        if (!Native.OpenClipboard(_source?.Handle ?? IntPtr.Zero)) return true; // can't verify: be safe
        try
        {
            var h = Native.GetClipboardData(_fmtHistory);
            if (h == IntPtr.Zero) return false;
            var ptr = Native.GlobalLock(h);
            if (ptr == IntPtr.Zero) return true;
            try { return (ulong)Native.GlobalSize(h) >= 4 && Marshal.ReadInt32(ptr) == 0; }
            finally { Native.GlobalUnlock(h); }
        }
        finally { Native.CloseClipboard(); }
    }

    void AddImage(BitmapSource img)
    {
        if (img.PixelWidth * (long)img.PixelHeight > 40_000_000) return; // > 40 MP: skip
        var bgra = new FormatConvertedBitmap(img, PixelFormats.Bgra32, null, 0);
        var stride = bgra.PixelWidth * 4;
        var pixels = new byte[stride * bgra.PixelHeight];
        bgra.CopyPixels(pixels, stride, 0);
        // clipboard DIBs often have alpha 0 everywhere: treat as opaque so the PNG is visible
        if (AllTransparent(pixels)) for (var i = 3; i < pixels.Length; i += 4) pixels[i] = 255;
        var frame = BitmapSource.Create(bgra.PixelWidth, bgra.PixelHeight, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(frame));
        using var ms = new MemoryStream();
        enc.Save(ms);
        _store.AddImage(ms.ToArray(), ClipStore.HashPixels(pixels), bgra.PixelWidth, bgra.PixelHeight);
    }

    static bool AllTransparent(byte[] bgra)
    {
        for (var i = 3; i < bgra.Length; i += 4) if (bgra[i] != 0) return false;
        return true;
    }

    public async void Paste(ClipEntry e)
    {
        try
        {
            var payload = new DataObject();
            if (e.Kind == ClipKind.Text) payload.SetText(e.Text);
            else
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(_store.ImagePath(e));
                image.EndInit();
                payload.SetImage(image);
            }
            var result = await ClipboardOps.PasteDataAsync(payload, _pasteTarget());
            if (ClipboardOps.PasteNotice(result) is { } notice) App.Island?.ShowToast(notice);
        }
        catch (Exception ex) { Logger.Error("clipboard paste", ex); App.Island?.ShowToast(Strings.T("clipboard.pasteFailed")); }
    }

    /// <summary>Copies the entry to the clipboard only (no paste).</summary>
    public bool Copy(ClipEntry e) => SetClipboard(e);

    bool SetClipboard(ClipEntry e)
    {
        try
        {
            if (e.Kind == ClipKind.Text) ClipboardOps.Write(() => System.Windows.Clipboard.SetText(e.Text));
            else
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(_store.ImagePath(e));
                bmp.EndInit();
                ClipboardOps.Write(() => System.Windows.Clipboard.SetImage(bmp));
            }
            return true;
        }
        catch (Exception ex) { Logger.Error("clipboard set", ex); return false; }
    }

    public void Dispose()
    {
        _retry?.Stop();
        if (_source is not null)
        {
            Native.RemoveClipboardFormatListener(_source.Handle);
            _source.RemoveHook(WndProc);
            _source.Dispose();
            _source = null;
        }
    }
}
