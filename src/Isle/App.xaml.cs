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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HookExceptionHandlers();

        _mutex = new Mutex(initiallyOwned: true, MutexName, out var isFirst);
        if (!isFirst)
        {
            _mutex.Dispose();
            _mutex = null;
            Shutdown(0);
            return;
        }

        Strings.Language = Settings.Current.Language;

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
                isIslandVisible: () => Island?.IsVisible == true,
                toggleIsland: ToggleIsland,
                openSettings: () => { /* settings panel arrives in a later milestone */ },
                quit: Quit));
            Host.Sync();

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
        if (Island.IsVisible) Island.Hide();
        else Island.Show();
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
