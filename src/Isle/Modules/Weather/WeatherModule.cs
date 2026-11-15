using System.Globalization;
using System.Windows.Threading;
using Isle.Core;
using Isle.Views.Live;
using Isle.Views.Weather;

namespace Isle.Modules.Weather;

/// <summary>
/// Weather (module id "weather"): Open-Meteo forecast for the place chosen in the tab (or the approximate IP location,
/// off by default). No network until a place is set; then a refresh every 30 min while the module is enabled.
/// Optional temperature in the idle pill, a once-per-event rain banner, `weather.json` cache for offline display.
/// Options: weather.city / weather.lat / weather.lon, weather.ip, weather.units ("C"/"F"), weather.inPill,
/// weather.rainAlerts, weather.lastAlert.
/// </summary>
public sealed class WeatherModule(IslandWindow island) : IModule, IDisposable
{
    public const string TabId = "weather";
    const string RainPillId = "weather.rain";

    IdleHttp? _http;
    WeatherService? _svc;
    DispatcherTimer? _timer;
    WeatherView? _view;
    CancellationTokenSource? _cts;
    WeatherPill? _pill;
    bool _busy;

    public string Id => "weather";

    static Settings S => Settings.Current;
    static bool Fahrenheit => S.Opt("weather.units") == "F";
    static bool HasPlace => S.OptBool("weather.ip") || S.Opt("weather.lat").Length > 0;

    public void Start()
    {
        _http = new IdleHttp();
        _cts = new CancellationTokenSource();
        _svc = new WeatherService(new OpenMeteoSource(_http), Paths.File("weather.json"));
        island.AddTab(new TabInfo(TabId, "tab.weather", "Icon.Weather", () =>
        {
            _view = new WeatherView(new WeatherActions(Search, Pick, UseIp, () => _ = RefreshNow(), SetUnits, SetInPill, SetRainAlerts));
            ShowCurrent();
            return _view;
        }, 80));
        Settings.Changed += OnSettingsChanged;
        UpdatePill();
        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = WeatherService.RefreshEvery };
        _timer.Tick += (_, _) => _ = RefreshNow();
        if (HasPlace)
        {
            _timer.Start();
            if (_svc.IsStale(DateTimeOffset.Now)) _ = RefreshNow();
        }
    }

    void OnSettingsChanged(string name)
    {
        if (name == "Language") { UpdatePill(); ShowCurrent(); }
    }

    async Task RefreshNow()
    {
        if (_busy || _svc is null || _cts is null) return;
        if (!HasPlace) { ShowCurrent(); return; }
        _busy = true;
        try
        {
            if (_svc.Report is null) _view?.ShowLoading();
            var place = await ResolvePlace(_cts.Token);
            if (place is null) { ShowCurrent(failed: true); return; }
            var report = await _svc.RefreshAsync(place, _cts.Token);
            Logger.Info(_svc.LastFailed ? "weather refresh failed (cache kept)" : "weather refreshed");
            ShowCurrent(_svc.LastFailed);
            UpdatePill();
            if (report is not null && !_svc.LastFailed) CheckRain(report);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Logger.Error("weather refresh", ex); }
        finally { _busy = false; }
    }

    /// <summary>The chosen place, or the IP location (falls back to the last stored coordinates when that fails).</summary>
    async Task<Place?> ResolvePlace(CancellationToken ct)
    {
        if (S.OptBool("weather.ip") && _svc is not null)
        {
            Place? ip = null;
            try { ip = await new OpenMeteoSource(_http!).LocateByIpAsync(ct); }
            catch (Exception ex) when (ex is not OperationCanceledException) { Logger.Warn($"ip location: {ex.GetType().Name}"); }
            if (ip is not null)
            {
                StorePlace(ip, keepIp: true);
                return ip;
            }
        }
        return StoredPlace();
    }

    static Place? StoredPlace()
    {
        if (!double.TryParse(S.Opt("weather.lat"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(S.Opt("weather.lon"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)) return null;
        return new Place(S.Opt("weather.city"), "", lat, lon);
    }

    static void StorePlace(Place p, bool keepIp)
    {
        S.SetOpt("weather.city", p.Name);
        S.SetOpt("weather.lat", p.Lat.ToString("0.####", CultureInfo.InvariantCulture));
        S.SetOpt("weather.lon", p.Lon.ToString("0.####", CultureInfo.InvariantCulture));
        S.SetOpt("weather.ip", keepIp);
        S.Save("Options");
    }

    void ShowCurrent(bool failed = false)
    {
        if (_view is null || _svc is null) return;
        if (!HasPlace) { _view.ShowSetup(S.OptBool("weather.ip")); return; }
        if (_svc.Report is { } r) _view.ShowReport(r, failed || _svc.LastFailed, Fahrenheit);
        else if (failed) _view.ShowError();
        else _view.ShowLoading();
    }

    // ---- tab actions ----

    async Task<List<Place>> Search(string text)
    {
        if (_http is null || string.IsNullOrWhiteSpace(text)) return [];
        try { return await new OpenMeteoSource(_http).SearchAsync(text, Strings.Language, _cts?.Token ?? default); }
        catch (Exception ex) when (ex is not OperationCanceledException) { Logger.Warn($"geocoding: {ex.GetType().Name}"); return []; }
    }

    void Pick(Place p)
    {
        StorePlace(p, keepIp: false);
        PlaceChanged();
    }

    void UseIp()
    {
        S.SetOpt("weather.ip", true);
        S.Save("Options");
        PlaceChanged();
    }

    void PlaceChanged()
    {
        _svc?.Clear();
        S.Options.Remove("weather.lastAlert");
        island.Pill.Remove(RainPillId);
        UpdatePill();
        _timer?.Start();
        _ = RefreshNow();
    }

    void SetUnits(bool fahrenheit)
    {
        S.SetOpt("weather.units", fahrenheit ? "F" : "C");
        S.Save("Options");
        UpdatePill();
        ShowCurrent();
    }

    void SetInPill(bool on)
    {
        S.SetOpt("weather.inPill", on);
        S.Save("Options");
        UpdatePill();
    }

    static void SetRainAlerts(bool on)
    {
        S.SetOpt("weather.rainAlerts", on);
        S.Save("Options");
    }

    // ---- pill ----

    void UpdatePill()
    {
        var r = _svc?.Report;
        var show = r is not null && HasPlace && S.OptBool("weather.inPill", true);
        if (!show)
        {
            if (ReferenceEquals(island.IdleView.RightSlot, _pill)) island.IdleView.RightSlot = null;
            return;
        }
        _pill ??= new WeatherPill();
        _pill.Set(WeatherCodes.IconKey(r!.Current.Code, r.Current.IsDay), WeatherFormat.Temp(r.Current.Temp, Fahrenheit));
        island.IdleView.RightSlot = _pill;
    }

    void CheckRain(WeatherReport r)
    {
        if (!S.OptBool("weather.rainAlerts", true)) return;
        var alert = RainAlerts.Find(r, DateTimeOffset.Now);
        if (alert is null) return;
        long? last = long.TryParse(S.Opt("weather.lastAlert"), out var l) ? l : null;
        if (!RainAlerts.ShouldAnnounce(alert, last)) return;
        S.SetOpt("weather.lastAlert", alert.Start.ToString(CultureInfo.InvariantCulture));
        S.Save("Options");
        ShowRainBanner(alert, r.Place);
    }

    public static string AlertText(RainAlert a) => Strings.T(a.Snow ? "wx.snowIn" : "wx.rainIn", a.Minutes);

    void ShowRainBanner(RainAlert a, string place)
    {
        Logger.Info($"rain alert in {a.Minutes} min");
        island.Pill.Set(new PillItem
        {
            Id = RainPillId, Priority = PillPriority.Transient, Width = 280, Peek = true, TabId = TabId,
            View = () => new IndicatorPill(a.Snow ? "Wx.Snow" : "Wx.Rain", AlertText(a), place, "AccentBrush"),
        });
        var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        t.Tick += (_, _) => { t.Stop(); island.Pill.Remove(RainPillId); };
        t.Start();
    }

    public void Stop()
    {
        Settings.Changed -= OnSettingsChanged;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _timer?.Stop();
        _timer = null;
        island.Pill.Remove(RainPillId);
        if (ReferenceEquals(island.IdleView.RightSlot, _pill)) island.IdleView.RightSlot = null;
        island.RemoveTab(TabId);
        _view = null;
        _http?.Dispose();
        _http = null;
        _svc = null;
    }

    public void Dispose() => Stop();
}
