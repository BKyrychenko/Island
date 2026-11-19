using System.Windows.Threading;
using Isle.Core;
using Isle.Views.Clipboard;

namespace Isle.Modules.Clipboard;

/// <summary>Clipboard history (module id "clipboard"): listener, encrypted store, tab. Limit from option "clip.limit" (default 200).</summary>
public sealed class ClipboardModule(IslandWindow island) : IModule, IDisposable
{
    public const string TabId = "clipboard";
    ClipStore? _store;
    ClipboardService? _service;
    DispatcherTimer? _save;

    public string Id => "clipboard";

    public void Start()
    {
        _store = ClipStore.OpenDefault();
        _store.Limit = Math.Clamp(Settings.Current.OptInt("clip.limit", 200), 10, 5000);
        _store.Load();
        _save = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => { _save!.Stop(); _store?.Save(); }, island.Dispatcher);
        _save.Stop();
        _store.Changed += () => { _save.Stop(); _save.Start(); };
        _service = new ClipboardService(_store, () => island.PasteTarget, island.Dispatcher);
        _service.Start();
        var store = _store;
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
        if (_save?.IsEnabled == true) _store?.Save();
        _save?.Stop();
        _store = null;
    }

    public void Dispose() => Stop();
}
