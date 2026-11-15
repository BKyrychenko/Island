using System.Diagnostics;
using Isle.Core;
using Isle.Views.Tools;

namespace Isle.Modules.Tools;

/// <summary>
/// Screen tools (module id "tools"): screenshot + markup, text capture (OCR), QR reading, colour picker, recording.
/// Hotkeys Win+Alt+S / Q / C / R. Nothing heavy exists until a tool is used; OCR engines and captures are per use.
/// </summary>
public sealed class ToolsModule(IslandWindow island) : IModule, IDisposable
{
    public const string TabId = "tools";
    const int ColorHistory = 12;
    ToolsView? _view;
    bool _busy;

    public string Id => "tools";

    public void Start()
    {
        island.AddTab(new TabInfo(TabId, "tab.tools", "Icon.Tools", () =>
        {
            var v = new ToolsView(
                () => Run(Screenshot), () => Run(() => Recognize(qr: false, ocr: true)), () => Run(() => Recognize(qr: true, ocr: false)), () => Run(PickColor), ToggleRecording,
                Copy, OpenUrl, SetOcrLanguage);
            v.ShowColors(Colors());
            return _view = v;
        }, 70));
        island.Hotkeys.Register("screenshot", "hotkey.screenshot", "Win+Alt+S", () => Run(Screenshot));
        island.Hotkeys.Register("capture", "hotkey.capture", "Win+Alt+Q", () => Run(() => Recognize(qr: true, ocr: true)));
        island.Hotkeys.Register("colorPicker", "hotkey.colorPicker", "Win+Alt+C", () => Run(PickColor));
        island.Hotkeys.Register("record", "hotkey.record", "Win+Alt+R", ToggleRecording);
    }

    ToolsView View => _view ??= island.TabViewFor(TabId) as ToolsView ?? CreateViewNow();

    ToolsView CreateViewNow()
    {
        island.SelectTab(TabId);
        return (ToolsView)island.TabViewFor(TabId)!;
    }

    /// <summary>One tool at a time; the island collapses so it is not in the way (and is excluded from the capture).</summary>
    async void Run(Func<Task> tool)
    {
        if (_busy) return;
        _busy = true;
        try
        {
            island.Collapse();
            await tool();
        }
        catch (Exception ex) { Logger.Error("screen tool", ex); }
        finally { _busy = false; }
    }

    Task<Selection?> Select(SelectMode mode) => SelectionSession.RunAsync(mode, island.ExcludeFromCapture);

    async Task Screenshot()
    {
        var sel = await Select(SelectMode.Region);
        if (sel is null) return;
        MarkupWindow.Open(sel, Copy);
    }

    /// <summary>Region → QR first (when asked), then OCR; the result goes to the clipboard and the Tools tab.</summary>
    async Task Recognize(bool qr, bool ocr)
    {
        var sel = await Select(SelectMode.Region);
        Logger.Info(sel is null ? "capture cancelled" : $"capture {sel.Rect.Width}x{sel.Rect.Height}");
        if (sel is null) return;
        if (qr)
        {
            var code = await Task.Run(() => QrReader.Decode(sel.Image));
            if (code is not null || !ocr)
            {
                island.Expand(TabId, sticky: true);
                View.ShowCode(code);
                return;
            }
        }
        var lang = Settings.Current.Opt("ocr.lang");
        var text = await OcrReader.RecognizeAsync(sel.Image, lang.Length > 0 ? lang : null);
        Logger.Info(text is null ? "ocr unavailable" : $"ocr {text.Length} chars");
        island.Expand(TabId, sticky: true);
        if (text is null)
        {
            View.ShowText("", OcrReader.Languages(), lang);
            island.ShowToast(Strings.T("tools.ocrUnavailable"));
            return;
        }
        if (text.Length > 0) Copy(text);
        View.ShowText(text, OcrReader.Languages(), lang.Length > 0 ? lang : OcrReader.Languages().FirstOrDefault().Tag);
        if (text.Length > 0) island.ShowToast(Strings.T("tools.textCopied"));
    }

    async Task PickColor()
    {
        var sel = await Select(SelectMode.Color);
        if (sel?.Picked is not { } c) return;
        var hex = ColorText.Hex(c);
        Copy(hex);
        var history = Colors().Where(h => !h.Equals(hex, StringComparison.OrdinalIgnoreCase)).Prepend(hex).Take(ColorHistory).ToList();
        Settings.Current.SetOpt("tools.colors", string.Join(",", history));
        Settings.Current.Save("Options");
        island.Expand(TabId, sticky: true);
        View.ShowColors(history);
        island.ShowToast(Strings.T("tools.colorCopied", hex));
    }

    static List<string> Colors() => [.. Settings.Current.Opt("tools.colors").Split(',', StringSplitOptions.RemoveEmptyEntries)];

    void ToggleRecording()
    {
        // screen recording arrives in M9 part 3
    }

    void SetOcrLanguage(string tag)
    {
        Settings.Current.SetOpt("ocr.lang", tag);
        Settings.Current.Save("Options");
    }

    static void Copy(string text)
    {
        try { System.Windows.Clipboard.SetText(text); }
        catch (Exception ex) { Logger.Warn($"copy: {ex.GetType().Name}"); }
    }

    static void OpenUrl(string url)
    {
        if (!QrReader.IsWebUrl(url)) return;
        try { Process.Start(new ProcessStartInfo(url.Trim()) { UseShellExecute = true }); }
        catch (Exception ex) { Logger.Error("open url", ex); }
    }

    public void Stop()
    {
        foreach (var id in new[] { "screenshot", "capture", "colorPicker", "record" }) island.Hotkeys.Unregister(id);
        island.RemoveTab(TabId);
        _view = null;
    }

    public void Dispose() => Stop();
}
