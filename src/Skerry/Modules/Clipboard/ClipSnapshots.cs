using System.Windows.Media;
using System.Windows.Media.Imaging;
using Skerry.Core;
using Skerry.Modules.Shelf;
using Skerry.Views.Clipboard;
using Skerry.Views.Shelf;

namespace Skerry.Modules.Clipboard;

/// <summary>--snapshot states for the Clipboard and Shelf tabs (temporary stores; the real history is never read).</summary>
static class ClipSnapshots
{
    public static void Register(Dictionary<string, Action<IslandWindow>> states)
    {
        states["clipboard-tab"] = w => Tab(w, "clipboard", "tab.clipboard", "Icon.Clipboard", 40, () =>
        {
            var store = TempClipStore();
            var t = DateTimeOffset.Now;
            var pin = store.AddText("ssh deploy@build-01 -p 2222")!;
            store.SetPinned(pin.Id, true);
            store.AddImage(SamplePng(), "S", 320, 180);
            store.AddText("Meeting notes: move the release to Thursday, Olena reviews the installer, Taras updates the docs and screenshots.");
            store.AddText("https://example.com/invoice/2026-11/0042");
            return new ClipboardView(store, _ => { }, _ => { });
        });
        states["clipboard-empty"] = w => Tab(w, "clipboard", "tab.clipboard", "Icon.Clipboard", 40, () => new ClipboardView(TempClipStore(), _ => { }, _ => { }));
        states["shelf-tab"] = w => Tab(w, "shelf", "tab.shelf", "Icon.Shelf", 50, () => new ShelfView(SampleShelf(), null));
        states["shelf-drop"] = w => Tab(w, "shelf", "tab.shelf", "Icon.Shelf", 50, () =>
        {
            var v = new ShelfView(SampleShelf(), null);
            v.ShowDropHint(true);
            return v;
        });
        states["shelf-empty"] = w => Tab(w, "shelf", "tab.shelf", "Icon.Shelf", 50, () => new ShelfView(TempShelf(), null));
    }

    static void Tab(IslandWindow w, string id, string title, string icon, int order, Func<System.Windows.FrameworkElement> view)
    {
        w.AddTab(new TabInfo("music", "tab.music", "Icon.Music", () => new System.Windows.Controls.Grid(), 10));
        w.AddTab(new TabInfo(id, title, icon, view, order));
        w.Expand(id);
    }

    static string TempDir() => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "skerry-snap-" + Guid.NewGuid().ToString("N"))).FullName;

    static ClipStore TempClipStore()
    {
        var d = TempDir();
        return new ClipStore(Path.Combine(d, "c.json"), Path.Combine(d, "clip"));
    }

    static ShelfStore TempShelf() => new(Path.Combine(TempDir(), "shelf.json"));

    static ShelfStore SampleShelf()
    {
        var s = TempShelf();
        var win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        s.Add([Path.Combine(win, "Gone report.docx"), Path.Combine(win, "System32"), Path.Combine(win, "win.ini"), Path.Combine(win, "notepad.exe")]);
        return s;
    }

    static byte[] SamplePng()
    {
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawRectangle(new LinearGradientBrush(Color.FromRgb(0x2F, 0x6F, 0xD6), Color.FromRgb(0x8E, 0x5B, 0xE8), 30), null, new System.Windows.Rect(0, 0, 320, 180));
            dc.DrawEllipse(Brushes.Orange, null, new System.Windows.Point(160, 90), 50, 50);
        }
        var bmp = new RenderTargetBitmap(320, 180, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(dv);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp));
        using var ms = new MemoryStream();
        enc.Save(ms);
        return ms.ToArray();
    }
}
