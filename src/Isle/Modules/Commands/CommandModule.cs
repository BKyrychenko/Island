using Isle.Core;
using Isle.Modules.Timers;
using Isle.Views.Commands;

namespace Isle.Modules.Commands;

/// <summary>Command bar (module id "commands", hotkey "commandBar", default Ctrl+Alt+Space). The index and rates are created on first open.</summary>
public sealed class CommandModule(IslandWindow island, Func<TimersController?> timers) : IModule, IDisposable
{
    CommandBarView? _view;
    AppIndex? _index;
    IdleHttp? _http;

    public string Id => "commands";

    readonly HashSet<string> _reportedConflicts = [];

    public void Start()
    {
        island.Hotkeys.RegistrationFailed += OnRegistrationFailed;
        island.Hotkeys.Register("commandBar", "hotkey.commandBar", "Ctrl+Alt+Space", Toggle);
        foreach (var key in island.Hotkeys.List().Where(k => !k.Registered && k.Gesture.Length > 0)) OnRegistrationFailed(key.Gesture);
    }

    void OnRegistrationFailed(string gesture)
    {
        if (!_reportedConflicts.Add(gesture)) return;
        island.ShowToast(Strings.T("hotkey.conflict", gesture), Strings.T("tray.settings"), () => App.OpenSettings("hotkeys"));
    }

    public void Toggle()
    {
        if (_view is not null && island.IsOverlayShown(_view)) { island.Collapse(); return; }
        island.SetHidden("user", false);
        var view = _view ??= Create();
        _index?.Refresh();
        view.Reset();
        island.ShowOverlay(view);
    }

    CommandBarView Create()
    {
        _index = new AppIndex(Paths.File("index.json"), () => Settings.Current.Opt("cmd.folders").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        _http = new IdleHttp();
        var rates = new CurrencyService(new NbuRatesSource(_http), Paths.File("rates.json"));
        ICommandProvider[] providers =
        [
            new CalculatorProvider(Copy),
            new CurrencyProvider(rates, Copy),
            new TimerProvider(timers, message => island.ShowToast(message)),
            new SystemProvider(),
            new WebProvider(),
            new AppsProvider(_index),
            .. Settings.Current.OptBool("cmd.devTools", Edition.IsDev) ? [new DevToolsProvider(Copy), new Ports.PortsProvider(), new DevSystemProvider(Copy, m => island.Dispatcher.BeginInvoke(() => island.ShowToast(m)))] : Array.Empty<ICommandProvider>(),
        ];
        var view = new CommandBarView(providers, Run, island);
        _index.Updated += () => island.Dispatcher.BeginInvoke(view.Requery);
        return view;
    }

    void Run(CommandItem item)
    {
        island.Collapse();
        try
        {
            item.Run?.Invoke();
            if (item.Undo is { } undo) island.ShowToast(Strings.T("cmd.shutdownScheduled"), Strings.T("cmd.abortShutdown"), undo, seconds: 10);
        }
        catch (Exception ex) { Logger.Error("command run", ex); }
    }

    static void Copy(string text)
    {
        try { System.Windows.Clipboard.SetText(text); }
        catch (Exception ex) { Logger.Warn($"copy result: {ex.GetType().Name}"); }
    }

    public void Stop()
    {
        island.Hotkeys.RegistrationFailed -= OnRegistrationFailed;
        _reportedConflicts.Clear();
        island.Hotkeys.Unregister("commandBar");
        if (_view is not null) island.HideOverlay(_view);
        _view = null;
        _http?.Dispose();
        _http = null;
        _index = null;
    }

    public void Dispose() => Stop();
}
