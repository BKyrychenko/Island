using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Threading;
using Isle.Core;
using Isle.Modules.ControlCenter;
using Isle.Views.Live;
using Microsoft.Win32;

namespace Isle.Modules.LiveEvents;

/// <summary>Where sources put things: the island's pill queue, plus timed "flash" items.</summary>
public interface ILiveSink
{
    Dispatcher Ui { get; }
    void Show(PillItem item);
    void Hide(string id);
    /// <summary>Shows a transient item for <paramref name="duration"/> (replaces an earlier flash with the same id).</summary>
    void Flash(string id, Func<FrameworkElement> view, double width, TimeSpan duration, double height = 32);
}

/// <summary>One live-event source. Start/Stop on the UI thread; must never throw out of callbacks.</summary>
public interface ILiveSource : IDisposable
{
    void Start();
}

// ---------------------------------------------------------------------------------------------------------------
// Downloads

/// <summary>Watches the Downloads folder for browser partial files; shows name, size and speed while they grow.</summary>
public sealed class DownloadsSource(ILiveSink sink, string? folder = null) : ILiveSource
{
    sealed class Item { public long Size; public DateTimeOffset LastGrowth = DateTimeOffset.Now; public readonly SpeedMeter Speed = new(); }

    const string PillId = "download";
    readonly Dictionary<string, Item> _active = new(StringComparer.OrdinalIgnoreCase);
    FileSystemWatcher? _watcher;
    DispatcherTimer? _tick;
    IndicatorPill? _view;
    string _dir = "";
    bool _disposed;
    internal bool Sampling => _tick?.IsEnabled == true;

    public void Start()
    {
        _dir = folder ?? KnownFolders.Downloads;
        if (!Directory.Exists(_dir)) { Logger.Warn("downloads folder not found"); return; }
        _watcher = new FileSystemWatcher(_dir) { NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite, IncludeSubdirectories = false };
        _watcher.Created += (_, e) => sink.Ui.BeginInvoke(() => Touch(e.FullPath));
        _watcher.Changed += (_, e) => sink.Ui.BeginInvoke(() => Touch(e.FullPath));
        _watcher.Renamed += (_, e) => sink.Ui.BeginInvoke(() => Renamed(e.OldFullPath, e.FullPath));
        _watcher.Deleted += (_, e) => sink.Ui.BeginInvoke(() => Removed(e.FullPath));
        _watcher.EnableRaisingEvents = true;
        _tick = new DispatcherTimer(DispatcherPriority.Background, sink.Ui) { Interval = TimeSpan.FromSeconds(1) };
        _tick.Tick += (_, _) => Sample(DateTimeOffset.Now);
        try { foreach (var f in Directory.EnumerateFiles(_dir)) if (DownloadDetector.IsPartial(f)) Touch(f, existing: true); }
        catch (Exception ex) { Logger.Warn($"downloads scan: {ex.GetType().Name}"); }
    }

    void Touch(string path, bool existing = false)
    {
        if (_disposed || !DownloadDetector.IsPartial(path)) return;
        if (!_active.ContainsKey(path))
        {
            var item = new Item();
            // a leftover partial file from an earlier, abandoned download only shows once it grows
            if (existing) item.LastGrowth = DateTimeOffset.MinValue;
            item.Size = SizeOf(path);
            item.Speed.Add(DateTimeOffset.Now, item.Size);
            _active[path] = item;
        }
        if (!existing && !_tick!.IsEnabled) _tick.Start();
    }

    void Renamed(string oldPath, string newPath)
    {
        if (_disposed) return;
        if (_active.Remove(oldPath))
        {
            if (DownloadDetector.IsPartial(newPath)) Touch(newPath);
            else Finished(newPath);
        }
        else if (DownloadDetector.IsPartial(newPath)) Touch(newPath);
        Update();
    }

    void Removed(string path)
    {
        if (_disposed) return;
        if (_active.Remove(path)) Update();
    }

    void Finished(string path)
    {
        var name = Path.GetFileName(path);
        sink.Flash("download.done", () =>
        {
            var v = new IndicatorPill("Icon.Check", name, Strings.T("live.downloaded"));
            v.MouseLeftButtonUp += (_, e) => { e.Handled = true; OpenFolder(path); };
            return v;
        }, 280, TimeSpan.FromSeconds(4));
    }

    internal void Sample(DateTimeOffset now)
    {
        foreach (var (path, item) in _active.ToList())
        {
            if (!File.Exists(path)) { _active.Remove(path); continue; }
            var size = SizeOf(path);
            if (size != item.Size) { item.Size = size; item.LastGrowth = now; }
            item.Speed.Add(now, size);
        }
        if (!_active.Values.Any(item => now - item.LastGrowth < TimeSpan.FromSeconds(60))) _tick!.Stop();
        Update();
    }

    void Update()
    {
        var now = DateTimeOffset.Now;
        var live = _active.Where(kv => now - kv.Value.LastGrowth < TimeSpan.FromSeconds(60)).ToList();
        if (live.Count == 0) { _tick?.Stop(); sink.Hide(PillId); _view = null; return; }
        var (path, item) = live[0];
        var name = DownloadDetector.FinalName(path) ?? Strings.T("live.downloading");
        var speed = item.Speed.BytesPerSecond;
        var sub = DownloadDetector.FormatSize(item.Size) + (speed > 0 ? $" · {DownloadDetector.FormatSize((long)speed)}/s" : "") + (live.Count > 1 ? $" · +{live.Count - 1}" : "");
        if (_view is null)
        {
            _view = new IndicatorPill("Icon.Download", name, sub);
            var dir = _dir;
            sink.Show(new PillItem
            {
                Id = PillId, Priority = PillPriority.Download, View = () => _view!, Width = 290,
                Compact = () => new IndicatorPill("Icon.Download", "", "", center: true), CompactWidth = 40,
                Click = () => OpenFolder(dir),
            });
        }
        else _view.Set("Icon.Download", name, sub);
    }

    static long SizeOf(string path)
    {
        try { return new FileInfo(path).Length; } catch { return 0; }
    }

    static void OpenFolder(string path)
    {
        try
        {
            var psi = File.Exists(path) ? new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") : new ProcessStartInfo(path) { UseShellExecute = true };
            Process.Start(psi);
        }
        catch (Exception ex) { Logger.Error("open downloads", ex); }
    }

    public void Dispose()
    {
        _disposed = true;
        _watcher?.Dispose();
        _watcher = null;
        _tick?.Stop();
        sink.Hide(PillId);
    }
}

public static class KnownFolders
{
    [DllImport("shell32.dll")]
    static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid id, uint flags, IntPtr token, out IntPtr path);

    public static string Downloads
    {
        get
        {
            try
            {
                if (SHGetKnownFolderPath(new Guid("374DE290-123F-4565-9164-39C4925E467B"), 0, IntPtr.Zero, out var p) == 0)
                {
                    var s = Marshal.PtrToStringUni(p);
                    Marshal.FreeCoTaskMem(p);
                    if (!string.IsNullOrEmpty(s)) return s;
                }
            }
            catch { /* fall back */ }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
    }
}

// ---------------------------------------------------------------------------------------------------------------
// Microphone / camera

/// <summary>Privacy indicator: who uses the microphone or camera, from the consent store. Event-driven (RegNotifyChangeKeyValue).</summary>
public sealed class MicCameraSource(ILiveSink sink) : ILiveSource
{
    const string Root = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore";
    const string PillId = "miccam";
    RegistryWatcher? _watcher;
    DispatcherTimer? _debounce;
    string _last = "";

    public void Start()
    {
        _debounce = new DispatcherTimer(DispatcherPriority.Background, sink.Ui) { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) => { _debounce.Stop(); Scan(); };
        _watcher = new RegistryWatcher(Registry.CurrentUser, Root, () => sink.Ui.BeginInvoke(() => { _debounce.Stop(); _debounce.Start(); }));
        Scan();
    }

    void Scan()
    {
        List<string> mic, cam;
        try
        {
            mic = InUse("microphone");
            cam = InUse("webcam");
        }
        catch (Exception ex) { Logger.Error("consent store", ex); return; }
        var key = string.Join("|", mic) + "#" + string.Join("|", cam);
        if (key == _last) return;
        _last = key;
        if (mic.Count == 0 && cam.Count == 0) { sink.Hide(PillId); return; }
        var apps = mic.Concat(cam).Distinct().ToList();
        var label = apps.Count == 1 ? apps[0] : $"{apps[0]} +{apps.Count - 1}";
        bool m = mic.Count > 0, c = cam.Count > 0;
        sink.Show(new PillItem
        {
            Id = PillId, Priority = PillPriority.MicCamera, Peek = true, Width = 190,
            View = () => new IndicatorPill(c ? "Icon.Camera" : "Icon.Mic", label, "", c ? "OkBrush" : "WarnBrush"),
            Compact = () => IndicatorPill.Dots(m, c), CompactWidth = 34,
        });
    }

    static List<string> InUse(string capability)
    {
        var list = new List<string>();
        using var cap = Registry.CurrentUser.OpenSubKey($@"{Root}\{capability}");
        if (cap is null) return list;
        void Check(RegistryKey parent, string name)
        {
            using var k = parent.OpenSubKey(name);
            if (k is null) return;
            var start = Convert.ToInt64(k.GetValue("LastUsedTimeStart") ?? 0L);
            var stop = Convert.ToInt64(k.GetValue("LastUsedTimeStop") ?? 0L);
            if (ConsentStore.InUse(start, stop)) list.Add(ConsentStore.AppName(name));
        }
        foreach (var name in cap.GetSubKeyNames())
        {
            if (name == "NonPackaged")
            {
                using var np = cap.OpenSubKey(name);
                if (np is not null) foreach (var n in np.GetSubKeyNames()) Check(np, n);
            }
            else Check(cap, name);
        }
        return list;
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _watcher = null;
        _debounce?.Stop();
        sink.Hide(PillId);
    }
}

/// <summary>RegNotifyChangeKeyValue on a key tree; calls back (thread pool) after every change. No polling.</summary>
public sealed class RegistryWatcher : IDisposable
{
    [DllImport("advapi32.dll")]
    static extern int RegNotifyChangeKeyValue(Microsoft.Win32.SafeHandles.SafeRegistryHandle key, bool watchSubtree, uint filter, Microsoft.Win32.SafeHandles.SafeWaitHandle evt, bool async);

    const uint REG_NOTIFY_CHANGE_NAME = 1, REG_NOTIFY_CHANGE_LAST_SET = 4;
    readonly RegistryKey? _key;
    readonly AutoResetEvent _event = new(false);
    readonly RegisteredWaitHandle? _wait;
    readonly Action _changed;
    volatile bool _disposed;

    public RegistryWatcher(RegistryKey hive, string path, Action changed)
    {
        _changed = changed;
        _key = hive.OpenSubKey(path);
        if (_key is null) return;
        Arm();
        _wait = ThreadPool.RegisterWaitForSingleObject(_event, (_, _) =>
        {
            if (_disposed) return;
            Arm();
            try { _changed(); } catch (Exception ex) { Logger.Error("registry watcher", ex); }
        }, null, Timeout.Infinite, executeOnlyOnce: false);
    }

    void Arm()
    {
        if (_key is null || _disposed) return;
        RegNotifyChangeKeyValue(_key.Handle, true, REG_NOTIFY_CHANGE_NAME | REG_NOTIFY_CHANGE_LAST_SET, _event.SafeWaitHandle, true);
    }

    public void Dispose()
    {
        _disposed = true;
        _wait?.Unregister(null);
        _key?.Dispose(); // closing the key ends the pending notification
        _event.Dispose();
    }
}

// ---------------------------------------------------------------------------------------------------------------
// OBS

/// <summary>OBS Studio recording state over obs-websocket v5 (ws://localhost:4455). Silent when OBS is not running.</summary>
public sealed class ObsSource(ILiveSink sink, Func<string> password, int port = 4455) : ILiveSource
{
    const string PillId = "obs";
    DispatcherTimer? _probe, _tick;
    CancellationTokenSource? _cts;
    bool _disposed;
    int _failures;
    DateTimeOffset? _recordingSince;
    IndicatorPill? _view;

    public void Start()
    {
        _probe = new DispatcherTimer(DispatcherPriority.Background, sink.Ui) { Interval = TimeSpan.FromSeconds(60) };
        _probe.Tick += (_, _) => Probe();
        _tick = new DispatcherTimer(DispatcherPriority.Background, sink.Ui) { Interval = TimeSpan.FromSeconds(1) };
        _tick.Tick += (_, _) => UpdatePill();
        Probe();
    }

    async void Probe()
    {
        if (_disposed || _cts is not null) return;
        _probe?.Stop();
        using var cts = new CancellationTokenSource();
        _cts = cts;
        try
        {
            var secret = password();
            var connected = await Task.Run(() => RunAsync(secret, cts));
            _failures = connected ? 0 : Math.Min(_failures + 1, 3);
        }
        catch (Exception ex) { Logger.Error("obs probe", ex); }
        finally
        {
            _cts = null;
            if (!_disposed)
            {
                SetRecording(false, TimeSpan.Zero);
                _probe!.Interval = RetryDelay(_failures);
                _probe.Start();
            }
        }
    }

    internal static TimeSpan RetryDelay(int failures) => TimeSpan.FromSeconds(Math.Min(300, 60 * Math.Pow(2, Math.Clamp(failures, 0, 3))));

    async Task<bool> RunAsync(string secret, CancellationTokenSource cts)
    {
        using var ws = new ClientWebSocket();
        using var handshake = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
        handshake.CancelAfter(TimeSpan.FromSeconds(5));
        var token = handshake.Token;
        var helloReceived = false;
        var identified = false;
        try
        {
            using (var connect = CancellationTokenSource.CreateLinkedTokenSource(cts.Token))
            {
                connect.CancelAfter(TimeSpan.FromMilliseconds(500));
                await ws.ConnectAsync(new Uri($"ws://127.0.0.1:{port}"), connect.Token).ConfigureAwait(false);
            }
            var buffer = new byte[64 * 1024];
            while (ws.State == WebSocketState.Open && !cts.IsCancellationRequested)
            {
                var msg = await ReceiveAsync(ws, buffer, token);
                if (msg is null) break;
                var node = JsonNode.Parse(msg);
                var op = (int?)node?["op"] ?? -1;
                var d = node?["d"] as JsonObject;
                var evt = (string?)d?["eventType"];
                if (op == 0 && !helloReceived && !identified) // Hello → Identify (Outputs events = 1 << 6)
                {
                    if ((int?)d?["rpcVersion"] != 1) return false;
                    helloReceived = true;
                    var identify = new JsonObject { ["rpcVersion"] = 1, ["eventSubscriptions"] = 64 };
                    if (d?["authentication"] is JsonObject auth)
                        identify["authentication"] = ObsAuth.Response(secret, (string?)auth["salt"] ?? "", (string?)auth["challenge"] ?? "");
                    await SendAsync(ws, new JsonObject { ["op"] = 1, ["d"] = identify }, token);
                }
                else if (op == 2 && helloReceived && !identified) // Identified → ask for the current state
                {
                    identified = true;
                    handshake.CancelAfter(Timeout.InfiniteTimeSpan);
                    await SendAsync(ws, new JsonObject { ["op"] = 6, ["d"] = new JsonObject { ["requestType"] = "GetRecordStatus", ["requestId"] = "isle-rec" } }, token);
                }
                else if (identified && op == 5 && evt == "RecordStateChanged")
                {
                    var ev = d!["eventData"] as JsonObject;
                    var active = (bool?)ev?["outputActive"] == true;
                    _ = sink.Ui.BeginInvoke(() => { if (!_disposed) SetRecording(active, TimeSpan.Zero); });
                }
                else if (identified && op == 5 && evt == "ExitStarted") cts.Cancel();
                else if (identified && op == 7 && (string?)d?["requestId"] == "isle-rec")
                {
                    var data = d!["responseData"] as JsonObject;
                    var on = (bool?)data?["outputActive"] == true;
                    var ms = (double?)data?["outputDuration"] ?? 0;
                    _ = sink.Ui.BeginInvoke(() => { if (!_disposed) SetRecording(on, TimeSpan.FromMilliseconds(ms)); });
                }
                else if (!identified) return false;
            }
            return identified;
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException) { /* OBS closed or websocket server disabled: retry with backoff. */ }
        catch (Exception ex) { Logger.Error("obs", ex); }
        return false;
    }

    static async Task<string?> ReceiveAsync(ClientWebSocket ws, byte[] buffer, CancellationToken ct)
    {
        var sb = new StringBuilder();
        while (true)
        {
            var r = await ws.ReceiveAsync(buffer, ct);
            if (r.MessageType == WebSocketMessageType.Close) return null;
            if (r.MessageType != WebSocketMessageType.Text || sb.Length + r.Count > 1 << 20) return null;
            sb.Append(Encoding.UTF8.GetString(buffer, 0, r.Count));
            if (r.EndOfMessage) return sb.ToString();
        }
    }

    static Task SendAsync(ClientWebSocket ws, JsonNode msg, CancellationToken ct) =>
        ws.SendAsync(Encoding.UTF8.GetBytes(msg.ToJsonString()), WebSocketMessageType.Text, true, ct);

    void SetRecording(bool on, TimeSpan already)
    {
        if (on)
        {
            _recordingSince ??= DateTimeOffset.Now - already;
            _tick?.Start();
            UpdatePill();
        }
        else
        {
            _recordingSince = null;
            _tick?.Stop();
            _view = null;
            sink.Hide(PillId);
        }
    }

    void UpdatePill()
    {
        if (_recordingSince is not { } since) return;
        var elapsed = Timers.DurationParser.Format(DateTimeOffset.Now - since - TimeSpan.FromSeconds(0.999));
        if (_view is null)
        {
            _view = new IndicatorPill(null, "REC", elapsed, "DangerBrush");
            sink.Show(new PillItem
            {
                Id = PillId, Priority = PillPriority.Recording, View = () => _view!, Width = 150, Peek = true,
                Compact = () => new IndicatorPill(null, "", "", "DangerBrush", center: true), CompactWidth = 30,
            });
        }
        else _view.Set(null, "REC", elapsed, "DangerBrush");
    }

    public void Dispose()
    {
        _disposed = true;
        _probe?.Stop();
        _tick?.Stop();
        _cts?.Cancel();
        sink.Hide(PillId);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// Steam

/// <summary>Steam game downloads from appmanifest_*.acf files (watched, not polled).</summary>
public sealed class SteamSource(ILiveSink sink) : ILiveSource
{
    const string PillId = "steam";
    readonly List<FileSystemWatcher> _watchers = [];
    readonly Dictionary<string, (SteamDownload Dl, SpeedMeter Speed)> _active = new(StringComparer.OrdinalIgnoreCase);
    bool _disposed;
    IndicatorPill? _view;

    public void Start()
    {
        string? steam;
        using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")) steam = k?.GetValue("SteamPath") as string;
        if (string.IsNullOrWhiteSpace(steam) || !Directory.Exists(steam)) return; // Steam not installed: nothing to do
        steam = steam.Replace('/', '\\');
        var vdfPath = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
        string? vdf = null;
        try { if (File.Exists(vdfPath)) vdf = File.ReadAllText(vdfPath); } catch { }
        foreach (var lib in Steam.Libraries(steam, vdf).Where(Directory.Exists))
            WatchLibrary(lib);
    }

    internal void WatchLibrary(string lib)
    {
        var w = new FileSystemWatcher(lib) { NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size };
        w.Changed += (_, e) => sink.Ui.BeginInvoke(() => Read(e.FullPath));
        w.Created += (_, e) => sink.Ui.BeginInvoke(() => Read(e.FullPath));
        w.Deleted += (_, e) => sink.Ui.BeginInvoke(() => Remove(e.FullPath));
        w.Renamed += (_, e) => sink.Ui.BeginInvoke(() => { Remove(e.OldFullPath); Read(e.FullPath); });
        w.EnableRaisingEvents = true;
        _watchers.Add(w);
        try { foreach (var f in Directory.EnumerateFiles(lib, "appmanifest_*.acf")) Read(f); } catch { }
    }

    void Remove(string path)
    {
        if (_disposed) return;
        _active.Remove(path);
        Update();
    }

    void Read(string path)
    {
        if (_disposed || !System.IO.Enumeration.FileSystemName.MatchesSimpleExpression("appmanifest_*.acf", Path.GetFileName(path), ignoreCase: true)) return;
        if (!File.Exists(path)) { Remove(path); return; }
        string? text = null;
        for (var i = 0; i < 3 && text is null; i++)
        {
            try { text = File.ReadAllText(path); }
            catch (IOException) { Thread.Yield(); }
            catch { return; }
        }
        if (text is null) return;
        var dl = Steam.FromManifest(text);
        if (dl is null) _active.Remove(path);
        else
        {
            var meter = _active.TryGetValue(path, out var old) ? old.Speed : new SpeedMeter(TimeSpan.FromSeconds(30));
            meter.Add(DateTimeOffset.Now, dl.Downloaded);
            _active[path] = (dl, meter);
        }
        Update();
    }

    void Update()
    {
        if (_active.Count == 0) { sink.Hide(PillId); _view = null; return; }
        var (dl, speed) = _active.Values.First();
        var bps = speed.BytesPerSecond;
        var sub = $"{dl.Progress * 100:0}%" + (bps > 0 ? $" · {DownloadDetector.FormatSize((long)bps)}/s" : "");
        if (_view is null)
        {
            _view = new IndicatorPill("Icon.Download", dl.Name, sub);
            sink.Show(new PillItem
            {
                Id = PillId, Priority = PillPriority.Download, View = () => _view!, Width = 290,
                Compact = () => new IndicatorPill("Icon.Download", "", "", center: true), CompactWidth = 40,
            });
        }
        else _view.Set("Icon.Download", dl.Name, sub);
        _view.SetProgress(dl.Progress);
    }

    public void Dispose()
    {
        _disposed = true;
        _active.Clear();
        foreach (var w in _watchers) w.Dispose();
        _watchers.Clear();
        sink.Hide(PillId);
    }
}

// ---------------------------------------------------------------------------------------------------------------
// VPN, keyboard layout, Caps Lock

/// <summary>Brief "VPN connected / disconnected" indicator on network changes.</summary>
public sealed class VpnIndicatorSource(ILiveSink sink) : ILiveSource
{
    readonly MachineVpnEnvironment _env = new();
    HashSet<string> _connected = [];
    DispatcherTimer? _debounce;

    public void Start()
    {
        _connected = Current();
        _debounce = new DispatcherTimer(DispatcherPriority.Background, sink.Ui) { Interval = TimeSpan.FromSeconds(1) };
        _debounce.Tick += (_, _) => { _debounce.Stop(); Check(); };
        NetworkChange.NetworkAddressChanged += OnChanged;
    }

    HashSet<string> Current() => [.. VpnDetector.Detect(_env).Where(v => v.Connected).Select(v => v.Name)];

    void OnChanged(object? s, EventArgs e) => sink.Ui.BeginInvoke(() => { _debounce!.Stop(); _debounce.Start(); });

    void Check()
    {
        var now = Current();
        var up = now.Except(_connected).FirstOrDefault();
        var down = _connected.Except(now).FirstOrDefault();
        _connected = now;
        if (up is not null) sink.Flash("vpn", () => new IndicatorPill("Icon.Shield", up, Strings.T("live.vpnOn"), "OkBrush"), 260, TimeSpan.FromSeconds(2.5));
        else if (down is not null) sink.Flash("vpn", () => new IndicatorPill("Icon.Shield", down, Strings.T("live.vpnOff")), 260, TimeSpan.FromSeconds(2.5));
    }

    public void Dispose()
    {
        NetworkChange.NetworkAddressChanged -= OnChanged;
        _debounce?.Stop();
    }
}

/// <summary>Shows the input language ("EN", "UK") briefly when the foreground keyboard layout changes.</summary>
public sealed class KeyboardLayoutSource(ILiveSink sink, ForegroundWatcher watcher) : ILiveSource
{
    DispatcherTimer? _timer;
    string? _last;

    public void Start()
    {
        _last = Current();
        watcher.Changed += OnForeground;
        // Foreground changes are immediate; catch layout switches inside an unchanged window at a quiet cadence.
        _timer = new DispatcherTimer(DispatcherPriority.Background, sink.Ui) { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => Check();
        _timer.Start();
    }

    void OnForeground(IntPtr hwnd) => sink.Ui.BeginInvoke(Check);

    static string? Current()
    {
        try
        {
            var fg = Native.GetForegroundWindow();
            if (fg == IntPtr.Zero) return null;
            var thread = Native.GetWindowThreadProcessId(fg, out _);
            var hkl = Native.GetKeyboardLayout(thread);
            return LayoutCode(hkl.ToInt64());
        }
        catch { return null; }
    }

    public static string LayoutCode(long hkl)
    {
        var langId = (int)(hkl & 0xFFFF);
        try { return CultureInfo.GetCultureInfo(langId).TwoLetterISOLanguageName.ToUpperInvariant(); }
        catch (CultureNotFoundException) { return $"{langId:X4}"; }
    }

    void Check()
    {
        var code = Current();
        if (code is null || code == _last) return;
        var first = _last is null;
        _last = code;
        if (!first) sink.Flash("layout", () => new IndicatorPill("Icon.Keyboard", code, "", center: true), 104, TimeSpan.FromSeconds(1.3));
    }

    public void Dispose()
    {
        watcher.Changed -= OnForeground;
        _timer?.Stop();
    }
}

/// <summary>Caps Lock on/off indicator via a low-level keyboard hook (the callback only looks for the Caps Lock key-up).</summary>
public sealed class CapsLockSource(ILiveSink sink) : ILiveSource
{
    CancellationTokenSource? _stop;

    public void Start()
    {
        if (_stop is not null) return;
        _stop = new CancellationTokenSource();
        var cancellation = _stop.Token;
        new Thread(() => RunHook(cancellation)) { IsBackground = true, Name = "Isle Caps Lock hook" }.Start();
    }

    void RunHook(CancellationToken cancellation)
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        using var stop = cancellation.Register(() => dispatcher.BeginInvokeShutdown(DispatcherPriority.Send));
        if (cancellation.IsCancellationRequested) return;
        Native.LowLevelKeyboardProc callback = (code, message, data) =>
        {
            if (!cancellation.IsCancellationRequested && code >= 0 &&
                (message == (IntPtr)Native.WM_KEYUP || message == (IntPtr)Native.WM_SYSKEYUP) && Marshal.ReadInt32(data) == 0x14)
                sink.Ui.BeginInvoke(() => { if (!cancellation.IsCancellationRequested) Show(); });
            return Native.CallNextHookEx(IntPtr.Zero, code, message, data);
        };
        var hook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, callback, Native.GetModuleHandle(null), 0);
        if (hook == IntPtr.Zero) { Logger.Warn("caps lock hook unavailable"); return; }
        try { Dispatcher.Run(); }
        finally
        {
            Native.UnhookWindowsHookEx(hook);
            GC.KeepAlive(callback);
        }
    }

    void Show()
    {
        var on = (Native.GetKeyState(0x14) & 1) != 0;
        sink.Flash("caps", () => new IndicatorPill("Icon.CapsLock", Strings.T(on ? "live.capsOn" : "live.capsOff"), "", on ? "AccentBrush" : null, center: true), 170, TimeSpan.FromSeconds(1.3));
    }

    public void Dispose()
    {
        var stop = _stop;
        _stop = null;
        stop?.Cancel();
        stop?.Dispose();
    }
}

// ---------------------------------------------------------------------------------------------------------------
// Windows notifications

/// <summary>
/// Toast banners from <c>UserNotificationListener</c>. That API needs package identity; for this unpackaged app it
/// reports "denied"/throws, in which case the source logs once, sets <see cref="Unavailable"/> and does nothing.
/// </summary>
public sealed class NotificationsSource(ILiveSink sink) : ILiveSource
{
    public static bool? Unavailable { get; private set; }
    Windows.UI.Notifications.Management.UserNotificationListener? _listener;
    readonly HashSet<uint> _seen = [];

    public void Start() => _ = StartAsync();

    async Task StartAsync()
    {
        try
        {
            var l = Windows.UI.Notifications.Management.UserNotificationListener.Current;
            var access = await l.RequestAccessAsync();
            if (access != Windows.UI.Notifications.Management.UserNotificationListenerAccessStatus.Allowed)
            {
                Unavailable = true;
                Logger.Info($"notifications unavailable: access {access}");
                return;
            }
            foreach (var n in await l.GetNotificationsAsync(Windows.UI.Notifications.NotificationKinds.Toast)) _seen.Add(n.Id);
            l.NotificationChanged += OnChanged;
            _listener = l;
            Unavailable = false;
        }
        catch (Exception ex)
        {
            Unavailable = true;
            Logger.Info($"notifications unavailable: {ex.GetType().Name} 0x{ex.HResult:X8}");
        }
    }

    void OnChanged(Windows.UI.Notifications.Management.UserNotificationListener l, Windows.UI.Notifications.UserNotificationChangedEventArgs e)
    {
        if (e.ChangeKind != Windows.UI.Notifications.UserNotificationChangedKind.Added || !_seen.Add(e.UserNotificationId)) return;
        try
        {
            var n = l.GetNotification(e.UserNotificationId);
            if (n is null) return;
            var app = n.AppInfo?.DisplayInfo?.DisplayName ?? Strings.T("live.notification");
            var binding = n.Notification?.Visual?.GetBinding(Windows.UI.Notifications.KnownNotificationBindings.ToastGeneric);
            var texts = binding?.GetTextElements().Select(t => t.Text).Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? [];
            var text = string.Join(" — ", texts.Take(2));
            sink.Ui.BeginInvoke(() => sink.Flash("notification", () => new IndicatorPill("Icon.Bell", app, text.Length > 60 ? text[..57] + "…" : text), 380, TimeSpan.FromSeconds(5)));
        }
        catch (Exception ex) { Logger.Error("notification read", ex); }
    }

    public void Dispose()
    {
        if (_listener is not null) try { _listener.NotificationChanged -= OnChanged; } catch { }
        _listener = null;
    }
}
