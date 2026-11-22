using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Isle.Core;
using Isle.Modules.Media;
using Isle.Modules.Timers;
using Isle.Views.Characters;

namespace Isle.Modules.Characters;

/// <summary>
/// Characters (module id "characters"): one of three original creatures, shown on the expanded panel (next to the
/// gear; click opens the gallery) and optionally in the idle pill. Dances while music plays, hops when a timer
/// finishes, naps after a while without input. Options: chars.kind (pebb|sprig|kubo), chars.inPill (off),
/// chars.sleepMin (5; 0 = never).
/// </summary>
public sealed class CharactersModule(IslandWindow island) : IModule, IDisposable
{
    public const double PanelSize = 28, PillSize = 22;

    readonly Dispatcher _ui = Application.Current.Dispatcher;
    readonly List<IDisposable> _subs = [];
    CreatureBrain? _brain;
    CreatureView? _panelView, _pillView;
    Button? _panelHost;
    CharacterGallery? _gallery;

    public string Id => "characters";

    static Settings S => Settings.Current;
    public static CreatureDesign Selected => CreatureDesign.ById(S.Opt("chars.kind", "pebb"));
    public static bool InPill => S.OptBool("chars.inPill");
    public static int SleepMinutes => Math.Clamp(S.OptInt("chars.sleepMin", 5), 0, 120);


    public void Start()
    {
        _brain = new CreatureBrain { SleepAfter = TimeSpan.FromMinutes(SleepMinutes) };
        _brain.MusicPlaying = App.Host.Get<MediaModule>("media")?.IsPlaying == true;
        _subs.Add(EventBus.Subscribe<MusicPlaying>(m => _ui.BeginInvoke(() => _brain?.SetMusic(m.Playing))));
        _subs.Add(EventBus.Subscribe<TimerFinished>(_ => _ui.BeginInvoke(() => _brain?.Cheer())));

        _panelView = new CreatureView(Selected, _brain) { Width = PanelSize, Height = PanelSize, IsHitTestVisible = false };
        _panelHost = new Button
        {
            Content = _panelView, Width = PanelSize, Height = PanelSize,
            ToolTip = Strings.T("chars.title"),
        };
        _panelHost.SetResourceReference(FrameworkElement.StyleProperty, "IconButton");
        _panelHost.Click += (_, _) => OpenGallery();
        island.Tabs.Mascot = _panelHost;
        ApplyPill();
        Settings.Changed += OnSettingsChanged;
    }

    void OnSettingsChanged(string name)
    {
        if (name == "Language" && _panelHost is not null) _panelHost.ToolTip = Strings.T("chars.title");
    }

    void ApplyPill()
    {
        if (_brain is null) return;
        if (InPill)
        {
            _pillView ??= new CreatureView(Selected, _brain) { Width = PillSize, Height = PillSize };
            _pillView.Design = Selected;
            island.IdleView.LeftSlot = _pillView;
        }
        else ClearPill();
    }

    void ClearPill()
    {
        if (_pillView is not null && ReferenceEquals(island.IdleView.LeftSlot, _pillView)) island.IdleView.LeftSlot = null;
        _pillView = null;
    }

    public void OpenGallery()
    {
        var keyboardInIsland = island.IsKeyboardFocusWithin;
        _gallery ??= new CharacterGallery(new GalleryActions(
            Pick: id => { S.SetOpt("chars.kind", id); Save(); },
            InPill: on => { S.SetOpt("chars.inPill", on); Save(); },
            SleepMinutes: m => { S.SetOpt("chars.sleepMin", m.ToString(System.Globalization.CultureInfo.InvariantCulture)); Save(); },
            Close: () => island.HideOverlay()), _brain);
        RefreshGallery();
        island.ShowOverlay(_gallery);
        if (keyboardInIsland)
            island.Dispatcher.InvokeAsync(() =>
            {
                if (_gallery is { IsVisible: true } gallery && island.IsOverlayShown(gallery)) gallery.FocusSelected();
            }, DispatcherPriority.Loaded);
    }

    void RefreshGallery() => _gallery?.Show(Selected.Id, InPill, SleepMinutes);

    void Save()
    {
        S.Save("Options");
        if (_brain is not null) _brain.SleepAfter = TimeSpan.FromMinutes(SleepMinutes);
        if (_panelView is not null) _panelView.Design = Selected;
        ApplyPill();
        RefreshGallery();
    }

    public void Stop()
    {
        Settings.Changed -= OnSettingsChanged;
        foreach (var s in _subs) s.Dispose();
        _subs.Clear();
        if (ReferenceEquals(island.Tabs.Mascot, _panelHost)) island.Tabs.Mascot = null;
        if (_gallery is not null && island.IsOverlayShown(_gallery)) island.HideOverlay();
        ClearPill();
        _panelHost = null;
        _panelView = null;
        _gallery = null;
        _brain = null;
    }

    public void Dispose() => Stop();
}
