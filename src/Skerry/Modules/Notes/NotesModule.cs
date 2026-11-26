using Skerry.Core;
using Skerry.Views.Notes;

namespace Skerry.Modules.Notes;

/// <summary>Notes (module id "notes"): the Notes tab, notes.json, and the quick-capture hotkey (Win+Alt+N).</summary>
public sealed class NotesModule(IslandWindow island) : IModule, IDisposable
{
    public const string TabId = "notes";
    NotesStore? _store;

    public string Id => "notes";
    public NotesStore? Store => _store;

    public void Start()
    {
        _store = new NotesStore(Paths.File("notes.json"));
        _store.SaveFailed += OnSaveFailed;
        island.AddTab(new TabInfo(TabId, "tab.notes", "Icon.Notes", () => new NotesView(_store, island), 30));
        island.Hotkeys.Register("newNote", "hotkey.newNote", "Win+Alt+N", QuickCapture);
    }

    void OnSaveFailed() => island.ShowToast(Strings.T("notes.saveFailed"));

    /// <summary>Opens the island on a new empty note with the caret in it.</summary>
    public void QuickCapture()
    {
        island.SetHidden("user", false);
        island.Expand(TabId, sticky: true);
        if (island.TabViewFor(TabId) is NotesView v) v.NewNote(focus: true);
    }

    public void Stop()
    {
        island.Hotkeys.Unregister("newNote");
        if (island.TabViewFor(TabId) is NotesView v) v.SaveNow();
        _store?.Flush();
        if (_store is not null) _store.SaveFailed -= OnSaveFailed;
        island.RemoveTab(TabId);
        _store = null;
    }

    public void Dispose() { if (_store is not null) Stop(); }
}
