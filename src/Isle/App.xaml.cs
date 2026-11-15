using System.Windows;
using System.Windows.Threading;
using Isle.Core;
using Isle.Modules.Tray;
using Microsoft.Win32;

namespace Isle;

public partial class App : Application
{
    const string MutexName = @"Local\Isle.SingleInstance.7c1f0b52";
    Mutex? _mutex;

    public static ModuleHost Host { get; } = new();
    public static IslandWindow? Island { get; private set; }
    /// <summary>True while running the --snapshot renderer.</summary>
    public static bool IsSnapshot { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HookExceptionHandlers();

        if (e.Args.Length > 0 && e.Args[0] == "--snapshot")
        {
            IsSnapshot = true;
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            Shutdown(IslandWindow.RunSnapshot(e.Args));
            return;
        }

        _mutex = new Mutex(initiallyOwned: true, MutexName, out var isFirst);
        if (!isFirst)
        {
            _mutex.Dispose();
            _mutex = null;
            Shutdown(0);
            return;
        }

        Strings.Language = Settings.Current.Language;
        Theme.Apply();

        // Software rendering keeps the GPU driver (~80 MB) out of the process; see DECISIONS.md.
        if (!Settings.Current.OptBool("render.hardware"))
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
        Island = new IslandWindow();
        Island.Show();

        // Everything else waits until the pill has been drawn once.
        Dispatcher.BeginInvoke(StartModules, DispatcherPriority.ApplicationIdle);
    }

    void StartModules()
    {
        try
        {
            Host.Register("tray", () => new TrayModule(
                isIslandVisible: () => Island?.IsHiddenByUser == false,
                toggleIsland: ToggleIsland,
                openSettings: () => { /* settings panel arrives in a later milestone */ },
                quit: Quit));
            Host.Register("lyrics", () => new Modules.Lyrics.LyricsModule());
            Host.Register("media", () => new Modules.Media.MediaModule(Island!));
            Host.Register("timers", () => new Modules.Timers.TimersModule(Island!));
            Host.Register("notes", () => new Modules.Notes.NotesModule(Island!));
            Host.Register("control", () => new Modules.ControlCenter.ControlModule(Island!));
            Host.Register("live", () => new Modules.LiveEvents.LiveEventsModule(Island!));
            Host.Register("clipboard", () => new Modules.Clipboard.ClipboardModule(Island!));
            Host.Register("shelf", () => new Modules.Shelf.ShelfModule(Island!));
            Host.Register("commands", () => new Modules.Commands.CommandModule(Island!, () => Host.Get<Modules.Timers.TimersModule>("timers")?.Controller));
            Host.Register("tools", () => new Modules.Tools.ToolsModule(Island!));
            Host.Register("weather", () => new Modules.Weather.WeatherModule(Island!));
            Host.Register("ai", () => new Modules.AiChat.AiModule(Island!));
            Host.Sync();
            Island?.Hotkeys.Register("toggle", "hotkey.toggle", "Win+Alt+Space", () => { if (Island is null) return; Island.SetHidden("user", false); Island.Toggle(); });

            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            Logger.Info($"Isle started, {Host.Ids.Count} module(s) registered");
            Memory.TrimSoon();
        }
        catch (Exception ex) { Logger.Error("module startup", ex); }
    }

    void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
            Dispatcher.BeginInvoke(() => Island?.Reposition());
    }

    public static void ToggleIsland()
    {
        if (Island is null) return;
        Island.SetHidden("user", !Island.IsHiddenByUser);
    }

    public static void Quit() => Current.Shutdown(0);

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            Host.StopAll();
        }
        catch (Exception ex) { Logger.Error("shutdown", ex); }
        if (_mutex is not null)
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
        base.OnExit(e);
    }

    void HookExceptionHandlers()
    {
        DispatcherUnhandledException += (_, a) =>
        {
            Logger.Error("unhandled (UI)", a.Exception);
            a.Handled = true; // keep the island alive
        };
        AppDomain.CurrentDomain.UnhandledException += (_, a) =>
            Logger.Error("unhandled (domain)", a.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, a) =>
        {
            Logger.Error("unobserved task", a.Exception);
            a.SetObserved();
        };
    }
}
