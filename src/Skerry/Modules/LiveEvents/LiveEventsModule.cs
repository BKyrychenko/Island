using System.Windows;
using System.Windows.Threading;
using Skerry.Core;

namespace Skerry.Modules.LiveEvents;

/// <summary>
/// Live events (module id "live"): owns the small sources that feed the pill. Each source has its own option
/// <c>live.&lt;name&gt;</c> (default on, except notifications) and is constructed only when enabled.
/// </summary>
public sealed class LiveEventsModule(IslandWindow island) : IModule, IDisposable
{
    public static readonly string[] SourceNames = ["downloads", "miccam", "notifications", "obs", "steam", "vpn", "layout", "caps"];

    readonly Dictionary<string, ILiveSource> _running = [];
    PillSink? _sink;

    public string Id => "live";

    public static bool IsSourceEnabled(string name) =>
        Settings.Current.OptBool($"live.{name}", name != "notifications");

    public void Start()
    {
        _sink ??= new PillSink(island.Pill, island.Dispatcher);
        Settings.Changed += OnSettingsChanged;
        Sync();
    }

    void OnSettingsChanged(string _) => island.Dispatcher.BeginInvoke(Sync);

    void Sync()
    {
        if (_sink is null) return;
        foreach (var name in SourceNames)
        {
            var on = IsSourceEnabled(name);
            if (on && !_running.ContainsKey(name))
            {
                try
                {
                    var src = Create(name, _sink);
                    src.Start();
                    _running[name] = src;
                }
                catch (Exception ex) { Logger.Error($"live source {name}", ex); }
            }
            else if (!on && _running.Remove(name, out var src)) SafeDispose(src);
        }
    }

    ILiveSource Create(string name, ILiveSink sink) => name switch
    {
        "downloads" => new DownloadsSource(sink, NullIfEmpty(Settings.Current.Opt("live.downloadsFolder"))),
        "miccam" => new MicCameraSource(sink),
        "notifications" => new NotificationsSource(sink),
        "obs" => new ObsSource(sink, () => Secret.Unprotect(Settings.Current.Opt("live.obsPassword")), Settings.Current.OptInt("live.obsPort", 4455)),
        "steam" => new SteamSource(sink),
        "vpn" => new VpnIndicatorSource(sink),
        "layout" => new KeyboardLayoutSource(sink, island.Watcher),
        "caps" => new CapsLockSource(sink),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    static void SafeDispose(ILiveSource s)
    {
        try { s.Dispose(); } catch (Exception ex) { Logger.Error("live source stop", ex); }
    }

    public void Stop()
    {
        Settings.Changed -= OnSettingsChanged;
        foreach (var s in _running.Values) SafeDispose(s);
        _running.Clear();
        _sink?.Dispose();
        _sink = null;
    }

    public void Dispose() => Stop();
}

/// <summary><see cref="ILiveSink"/> on top of the island's <see cref="PillQueue"/>; flashes remove themselves after their duration.</summary>
public sealed class PillSink(PillQueue pill, Dispatcher ui) : ILiveSink, IDisposable
{
    readonly Dictionary<string, DispatcherTimer> _flashes = [];

    public Dispatcher Ui => ui;
    public void Show(PillItem item) => pill.Set(item);
    public void Hide(string id) => pill.Remove(id);

    public void Flash(string id, Func<FrameworkElement> view, double width, TimeSpan duration, double height = 32)
    {
        var key = "flash." + id;
        if (!_flashes.TryGetValue(key, out var timer))
        {
            timer = new DispatcherTimer(DispatcherPriority.Background, ui);
            timer.Tick += (_, _) => { timer.Stop(); pill.Remove(key); };
            _flashes[key] = timer;
        }
        timer.Stop();
        timer.Interval = duration;
        // a new item each time so the view is rebuilt with the new text
        pill.Set(new PillItem { Id = key, Priority = PillPriority.Transient, View = view, Width = width, Height = height, Peek = true });
        timer.Start();
    }

    public void Dispose()
    {
        foreach (var (key, t) in _flashes) { t.Stop(); pill.Remove(key); }
        _flashes.Clear();
    }
}
