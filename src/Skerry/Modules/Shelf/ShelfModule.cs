using Skerry.Core;
using Skerry.Views.Shelf;

namespace Skerry.Modules.Shelf;

/// <summary>File shelf (module id "shelf"): drop files on the island to park them, drag them out again.</summary>
public sealed class ShelfModule(IslandWindow island) : IModule, IDisposable
{
    public const string TabId = "shelf";
    ShelfStore? _store;
    ShelfView? _view;

    public string Id => "shelf";

    public void Start()
    {
        var store = _store = ShelfStore.OpenDefault();
        store.Load();
        island.AddTab(new TabInfo(TabId, "tab.shelf", "Icon.Shelf", () => _view = new ShelfView(store, island), 50));
        island.FileDropTarget = (TabId, files => store.TryAdd(files, out _), on => (_view ??= island.TabViewFor(TabId) as ShelfView)?.ShowDropHint(on));
    }

    public void Stop()
    {
        island.FileDropTarget = null;
        island.RemoveTab(TabId);
        _view = null;
        _store = null;
    }

    public void Dispose() => Stop();
}
