using System.Windows.Threading;
using Skerry.Core;
using Skerry.Views.Clipboard;

namespace Skerry.Modules.Clipboard;

/// <summary>Clipboard history (module id "clipboard"): listener, encrypted store, tab. Limit from option "clip.limit" (default 200).</summary>
public sealed class ClipboardModule(IslandWindow island) : IModule, IDisposable
{
    public const string TabId = "clipboard";
    ClipboardService? _service;
    ClipboardAutosave? _save;

    public string Id => "clipboard";

    public void Start()
    {
        var store = ClipStore.OpenDefault();
        store.Limit = Math.Clamp(Settings.Current.OptInt("clip.limit", 200), 10, 5000);
        store.Load();
        _save = new ClipboardAutosave(store, island.Dispatcher);
        _service = new ClipboardService(store, () => island.PasteTarget, island.Dispatcher);
        _service.Start();
        island.AddTab(new TabInfo(TabId, "tab.clipboard", "Icon.Clipboard", () => new ClipboardView(store, Paste, e => _service?.Copy(e), island), 40));
    }

    void Paste(ClipEntry e)
    {
        island.Collapse();
        _service?.Paste(e);
    }

    public void Stop()
    {
        island.RemoveTab(TabId);
        _service?.Dispose();
        _service = null;
        if (_save?.Stop() == false) island.ShowToast(Strings.T("clip.captureSaveStopped"));
        _save = null;
    }

    public void Dispose() => Stop();
}

/// <summary>Coalesces clipboard captures, retries pending writes and flushes once more on shutdown.</summary>
internal sealed class ClipboardAutosave
{
    readonly ClipStore _store;
    readonly DispatcherTimer _timer;

    public ClipboardAutosave(ClipStore store, Dispatcher dispatcher)
    {
        _store = store;
        _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher);
        _timer.Tick += Flush;
        _store.Changed += Schedule;
    }

    void Schedule()
    {
        _timer.Stop();
        if (!_store.HasPendingChanges) return;
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Start();
    }

    void Flush(object? sender, EventArgs e)
    {
        _timer.Stop();
        if (_store.HasPendingChanges && !_store.Save())
        {
            _timer.Interval = TimeSpan.FromSeconds(10);
            _timer.Start();
        }
    }

    public bool Stop()
    {
        _store.Changed -= Schedule;
        _timer.Stop();
        return !_store.HasPendingChanges || _store.Save();
    }
}
