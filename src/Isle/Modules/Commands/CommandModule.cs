using Isle.Core;
using Isle.Modules.Timers;
using Isle.Views.Commands;

namespace Isle.Modules.Commands;

/// <summary>Command bar (module id "commands", hotkey "commandBar", default Alt+Space). The index and rates are created on first open.</summary>
public sealed class CommandModule(IslandWindow island, Func<TimersController?> timers) : IModule, IDisposable
{
    CommandBarView? _view;
    AppIndex? _index;
    IdleHttp? _http;

    public string Id => "commands";

    public void Start() => island.Hotkeys.Register("commandBar", "hotkey.commandBar", "Alt+Space", Toggle);

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
            new TimerProvider(timers, _ => { }),
            new SystemProvider(),
            new WebProvider(),
            new AppsProvider(_index),
        ];
        var view = new CommandBarView(providers, Run, island);
        _index.Updated += () => island.Dispatcher.BeginInvoke(view.Requery);
        return view;
    }

    void Run(CommandItem item)
    {
        island.Collapse();
        try { item.Run?.Invoke(); }
        catch (Exception ex) { Logger.Error("command run", ex); }
    }

    static void Copy(string text)
    {
        try { System.Windows.Clipboard.SetText(text); }
        catch (Exception ex) { Logger.Warn($"copy result: {ex.GetType().Name}"); }
    }

    public void Stop()
    {
        island.Hotkeys.Unregister("commandBar");
        if (_view is not null) island.HideOverlay();
        _view = null;
        _http?.Dispose();
        _http = null;
        _index = null;
    }

    public void Dispose() => Stop();
}
