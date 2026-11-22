using System.Globalization;
using Isle.Core;

namespace Isle.Modules.Weather;

public interface IWeatherSource
{
    Task<WeatherReport?> ForecastAsync(Place place, CancellationToken ct);
    Task<List<Place>> SearchAsync(string name, string language, CancellationToken ct);
    Task<Place?> LocateByIpAsync(CancellationToken ct);
}

/// <summary>Open-Meteo forecast + geocoding (free, no key) and geojs.io for the optional approximate IP location.</summary>
public sealed class OpenMeteoSource(IdleHttp http) : IWeatherSource
{
    static string F(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);

    public async Task<WeatherReport?> ForecastAsync(Place place, CancellationToken ct)
    {
        var url = "https://api.open-meteo.com/v1/forecast?latitude=" + F(place.Lat) + "&longitude=" + F(place.Lon) +
                  "&current=temperature_2m,apparent_temperature,relative_humidity_2m,weather_code,is_day,wind_speed_10m,precipitation" +
                  "&hourly=temperature_2m,weather_code,precipitation_probability,is_day" +
                  "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max" +
                  "&minutely_15=precipitation&forecast_minutely_15=12&forecast_hours=25&forecast_days=7&timezone=auto&timeformat=unixtime";
        var (status, body) = await http.GetAsync(url, ct).ConfigureAwait(false);
        return status == 200 ? OpenMeteoParser.Forecast(body, place.Name, DateTimeOffset.Now) : null;
    }

    public async Task<List<Place>> SearchAsync(string name, string language, CancellationToken ct)
    {
        var url = "https://geocoding-api.open-meteo.com/v1/search?count=6&format=json&language=" + Uri.EscapeDataString(language) +
                  "&name=" + Uri.EscapeDataString(name.Trim());
        var (status, body) = await http.GetAsync(url, ct).ConfigureAwait(false);
        return status == 200 ? OpenMeteoParser.Places(body) : [];
    }

    public async Task<Place?> LocateByIpAsync(CancellationToken ct)
    {
        var (status, body) = await http.GetAsync("https://get.geojs.io/v1/ip/geo.json", ct).ConfigureAwait(false);
        return status == 200 ? OpenMeteoParser.IpPlace(body) : null;
    }
}

/// <summary>
/// Last report with a disk cache (`weather.json`) for offline display. <see cref="RefreshAsync"/> fetches a new one;
/// on failure the cached report stays and <see cref="LastFailed"/> is set.
/// </summary>
public sealed class WeatherService(IWeatherSource source, string cacheFile)
{
    public static readonly TimeSpan RefreshEvery = TimeSpan.FromMinutes(30);
    WeatherReport? _report;
    bool _loaded;

    public bool LastFailed { get; private set; }

    public WeatherReport? Report
    {
        get
        {
            if (!_loaded)
            {
                _loaded = true;
                if (File.Exists(cacheFile))
                {
                    var r = JsonStore.Load<WeatherReport>(cacheFile);
                    if (r.Hours.Count > 0 || r.Current.Time > 0) _report = r;
                }
            }
            return _report;
        }
    }

    public bool IsStale(DateTimeOffset now) => Report is not { } r || now - r.Fetched >= RefreshEvery || now < r.Fetched;

    public async Task<WeatherReport?> RefreshAsync(Place place, CancellationToken ct)
    {
        WeatherReport? fresh = null;
        try { fresh = await source.ForecastAsync(place, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { Logger.Warn($"weather fetch failed: {ex.GetType().Name}"); }
        ct.ThrowIfCancellationRequested();
        LastFailed = fresh is null;
        if (fresh is null) return Report;
        _loaded = true;
        _report = fresh;
        JsonStore.Save(cacheFile, fresh);
        return fresh;
    }

    /// <summary>Forget the cached report (location changed).</summary>
    public void Clear()
    {
        _loaded = true;
        _report = null;
        LastFailed = false;
        try { File.Delete(cacheFile); } catch { }
    }
}
