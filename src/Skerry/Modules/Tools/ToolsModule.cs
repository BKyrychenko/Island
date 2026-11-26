using System.Diagnostics;
using Skerry.Core;
using Skerry.Views.Tools;

namespace Skerry.Modules.Tools;

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
            v.SetRecording(_recorder is not null);
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
        MarkupWindow.Open(sel, System.Windows.Clipboard.SetText);
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

    Recorder? _recorder;
    Task? _startingRecording;
    Task<bool>? _savingRecording;
    int _recordingRequest;
    bool _exiting;
    System.Windows.Threading.DispatcherTimer? _recTick;
    Views.Live.IndicatorPill? _recPill;
    const string RecPillId = "rec";

    async void ToggleRecording()
    {
        if (_exiting || _savingRecording is { IsCompleted: false }) return;
        if (_recorder is not null) { await StopRecording(); return; }
        if (_busy) return;
        var request = _recordingRequest;
        _busy = true;
        try
        {
            island.Collapse();
            var sel = await Select(SelectMode.Region);
            if (sel is null || request != _recordingRequest) return;
            await (_startingRecording = StartRecording(sel));
        }
        catch (Exception ex)
        {
            Logger.Error("recording start", ex);
            if (request == _recordingRequest || _exiting)
            {
                island.SetHidden("user", false);
                island.Expand(TabId, sticky: true);
                island.ShowToast(Strings.T("tools.recordFailed"));
            }
        }
        finally { _startingRecording = null; _busy = false; }
    }

    async Task StartRecording(Selection sel)
    {
        var region = new PxRect(sel.Rect.X - sel.Monitor.Left, sel.Rect.Y - sel.Monitor.Top, sel.Rect.Width, sel.Rect.Height);
        var opts = new RecordOptions(sel.Monitor, region, Settings.Current.OptBool("rec.systemAudio", true), Settings.Current.OptBool("rec.mic"));
        var rec = new Recorder(opts, ScreenGrab.SaveFolder("Videos"));
        island.ExcludeFromCapture(true); // the REC pill must not end up in the video
        try { await rec.StartAsync(); }
        catch
        {
            rec.Dispose();
            island.ExcludeFromCapture(false);
            if (rec.FilePath.Length > 0) { try { File.Delete(rec.FilePath); } catch { } }
            throw;
        }
        _recorder = rec;
        Logger.Info($"recording started {region.Width}x{region.Height}");
        ShowRecPill();
        _view?.SetRecording(true);
    }

    internal async Task<bool> FinishRecordingAsync(bool exiting = false)
    {
        _exiting |= exiting;
        ++_recordingRequest; // A pending selection must not start a new recording.
        var saved = false;
        try { return saved = await StopRecording(); }
        finally { if (exiting && !saved) _exiting = false; }
    }

    Task<bool> StopRecording()
    {
        if (_savingRecording is { IsCompleted: false } saving) return saving;
        if (_recorder is null && _startingRecording is null) return Task.FromResult(true);
        return _savingRecording = SaveRecording(_startingRecording);
    }

    async Task<bool> SaveRecording(Task? starting)
    {
        // The island may be inside its Closing event. Return the shared save before updating its UI.
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Normal);
        if (starting is not null)
        {
            try { await starting; }
            catch (Exception) { return false; } // ToggleRecording reports the start failure.
        }
        if (_recorder is not { } rec) return true;
        _recorder = null;
        _recTick?.Stop();
        island.Pill.Remove(RecPillId);
        _view?.SetRecording(false);
        var saved = false;
        try
        {
            await rec.StopAsync();
            saved = true;
        }
        catch (Exception ex) { Logger.Error("recording finalise", ex); }
        finally { island.ExcludeFromCapture(false); }
        if (saved) Logger.Info("recording saved");
        if (!saved || !_exiting) ShowRecordingResult(rec.FilePath, saved);
        GC.Collect();
        Memory.TrimSoon(2);
        return saved;
    }

    void ShowRecordingResult(string path, bool saved)
    {
        const string id = "rec.result";
        var label = Strings.T(saved ? "tools.recordSaved" : "tools.recordSaveFailed");
        var file = saved ? Path.GetFileName(path) : "";
        if (!saved)
        {
            island.SetHidden("user", false);
            if (island.IsExpanded) island.ShowToast(label);
        }
        island.Pill.Set(new PillItem
        {
            Id = id, Priority = PillPriority.Transient, Width = Views.Live.IndicatorPill.MessageWidth(label, file), Peek = true,
            View = () => new Views.Live.IndicatorPill(saved ? "Icon.Check" : "Icon.Record", label, file, saved ? "AccentBrush" : "DangerBrush"),
            Click = () =>
            {
                if (saved) { try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"")); } catch { } }
                island.Pill.Remove(id);
            },
        });
        var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        t.Tick += (_, _) => { t.Stop(); island.Pill.Remove(id); };
        t.Start();
    }

    void ShowRecPill()
    {
        _recPill = new Views.Live.IndicatorPill(null, "REC", "0:00", "DangerBrush");
        island.Pill.Set(new PillItem
        {
            Id = RecPillId, Priority = PillPriority.Recording, Width = 150, Peek = true, HoverExpands = false,
            View = () => _recPill!, Compact = () => new Views.Live.IndicatorPill(null, "", "", "DangerBrush", center: true), CompactWidth = 30,
            Click = () => _ = StopRecording(),
        });
        _recTick ??= new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _recTick.Tick -= OnRecTick;
        _recTick.Tick += OnRecTick;
        _recTick.Start();
    }

    void OnRecTick(object? s, EventArgs e)
    {
        if (_recorder is { } r) _recPill?.Set(null, "REC", Timers.DurationParser.Format(r.Elapsed), "DangerBrush");
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
        _ = FinishRecordingAsync();
        foreach (var id in new[] { "screenshot", "capture", "colorPicker", "record" }) island.Hotkeys.Unregister(id);
        island.RemoveTab(TabId);
        _view = null;
    }

    public void Dispose() => Stop();
}
