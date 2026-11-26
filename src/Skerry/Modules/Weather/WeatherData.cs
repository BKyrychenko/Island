using System.Globalization;
using System.Text.Json;

namespace Skerry.Modules.Weather;

/// <summary>One forecast as fetched from Open-Meteo. Temperatures are always °C (converted only for display);
/// times are Unix seconds, shown in the place's own UTC offset.</summary>
public sealed class WeatherReport
{
    public string Place { get; set; } = "";
    public DateTimeOffset Fetched { get; set; }
    public int UtcOffsetSeconds { get; set; }
    public CurrentWeather Current { get; set; } = new();
    public List<HourWeather> Hours { get; set; } = [];
    public List<DayWeather> Days { get; set; } = [];
    /// <summary>15-minute precipitation for the next few hours (rain alerts).</summary>
    public List<PrecipSlot> Minutely { get; set; } = [];

    public DateTimeOffset Local(long unix) => DateTimeOffset.FromUnixTimeSeconds(unix).ToOffset(TimeSpan.FromSeconds(UtcOffsetSeconds));
}

public sealed class CurrentWeather
{
    public long Time { get; set; }
    public double Temp { get; set; }
    public double Feels { get; set; }
    public int Humidity { get; set; }
    public int Code { get; set; }
    public bool IsDay { get; set; } = true;
    public double Wind { get; set; }
    public double Precip { get; set; }
}

public sealed record HourWeather(long Time, double Temp, int Code, int PrecipProb, bool IsDay);
public sealed record DayWeather(long Time, double Max, double Min, int Code, int PrecipProb);
public sealed record PrecipSlot(long Time, double Mm);
public sealed record Place(string Name, string Detail, double Lat, double Lon);

public enum WeatherKind { Clear, PartlyCloudy, Cloudy, Fog, Drizzle, Rain, Snow, Thunder }

/// <summary>WMO weather interpretation codes (the ones Open-Meteo returns).</summary>
public static class WeatherCodes
{
    public static WeatherKind Kind(int code) => code switch
    {
        0 => WeatherKind.Clear,
        1 or 2 => WeatherKind.PartlyCloudy,
        3 => WeatherKind.Cloudy,
        45 or 48 => WeatherKind.Fog,
        >= 51 and <= 57 => WeatherKind.Drizzle,
        (>= 61 and <= 67) or (>= 80 and <= 82) => WeatherKind.Rain,
        (>= 71 and <= 77) or 85 or 86 => WeatherKind.Snow,
        >= 95 and <= 99 => WeatherKind.Thunder,
        _ => WeatherKind.Cloudy,
    };

    /// <summary>Strings key of the condition text.</summary>
    public static string TextKey(int code) => code switch
    {
        0 => "wx.clear",
        1 => "wx.mainlyClear",
        2 => "wx.partly",
        3 => "wx.overcast",
        45 or 48 => "wx.fog",
        51 or 53 or 55 => "wx.drizzle",
        56 or 57 => "wx.freezingDrizzle",
        61 or 63 or 65 => "wx.rain",
        66 or 67 => "wx.freezingRain",
        71 or 73 or 75 or 77 => "wx.snow",
        80 or 81 or 82 => "wx.showers",
        85 or 86 => "wx.snowShowers",
        95 => "wx.thunder",
        96 or 99 => "wx.hail",
        _ => "wx.overcast",
    };

    public static string IconKey(int code, bool isDay) => Kind(code) switch
    {
        WeatherKind.Clear => isDay ? "Wx.Sun" : "Wx.Moon",
        WeatherKind.PartlyCloudy => isDay ? "Wx.PartlyDay" : "Wx.PartlyNight",
        WeatherKind.Fog => "Wx.Fog",
        WeatherKind.Drizzle => "Wx.Drizzle",
        WeatherKind.Rain => "Wx.Rain",
        WeatherKind.Snow => "Wx.Snow",
        WeatherKind.Thunder => "Wx.Thunder",
        _ => "Wx.Cloud",
    };

    /// <summary>Any falling precipitation (drizzle, rain, snow, showers, thunder).</summary>
    public static bool IsWet(int code) => Kind(code) is WeatherKind.Drizzle or WeatherKind.Rain or WeatherKind.Snow or WeatherKind.Thunder;
}

/// <summary>Display helpers: °C/°F (wind km/h or mph with it).</summary>
public static class WeatherFormat
{
    public static double Convert(double celsius, bool fahrenheit) => fahrenheit ? celsius * 9 / 5 + 32 : celsius;

    public static string Temp(double celsius, bool fahrenheit)
    {
        var v = (int)Math.Round(Convert(celsius, fahrenheit), MidpointRounding.AwayFromZero);
        return (v == 0 ? 0 : v).ToString(CultureInfo.InvariantCulture) + "°";
    }

    public static string Wind(double kmh, bool fahrenheit) =>
        fahrenheit ? $"{Math.Round(kmh / 1.609344):0} mph" : $"{Math.Round(kmh):0} " + Core.Strings.T("wx.kmh");
}

public sealed record RainAlert(long Start, int Minutes, bool Snow);

/// <summary>"Rain starts in ~20 min": the first wet 15-minute slot within the next 2 hours while it is dry now.</summary>
public static class RainAlerts
{
    public const double MinMm = 0.1;
    public static readonly TimeSpan Window = TimeSpan.FromHours(2);

    public static RainAlert? Find(WeatherReport r, DateTimeOffset now)
    {
        var nowS = now.ToUnixTimeSeconds();
        var endS = nowS + (long)Window.TotalSeconds;
        // already wet: nothing to announce (the current block may be up to 15 min old)
        if (r.Current.Precip >= MinMm || WeatherCodes.IsWet(r.Current.Code)) return null;
        var wetNow = r.Minutely.FirstOrDefault(s => s.Time <= nowS && nowS < s.Time + 900);
        if (wetNow is not null && wetNow.Mm >= MinMm) return null;

        long? start = null;
        if (r.Minutely.Count > 0)
            start = r.Minutely.FirstOrDefault(s => s.Time > nowS - 900 && s.Time <= endS && s.Mm >= MinMm)?.Time;
        else
            start = r.Hours.FirstOrDefault(h => h.Time > nowS - 3600 && h.Time <= endS && h.PrecipProb >= 60 && WeatherCodes.IsWet(h.Code))?.Time;
        if (start is null) return null;

        var minutes = (int)Math.Max(0, (start.Value - nowS) / 60);
        minutes = Math.Max(5, (int)(Math.Round(minutes / 5.0) * 5));
        var hour = r.Hours.LastOrDefault(h => h.Time <= start.Value) ?? r.Hours.FirstOrDefault();
        var snow = hour is not null && WeatherCodes.Kind(hour.Code) == WeatherKind.Snow;
        return new RainAlert(start.Value, minutes, snow);
    }

    /// <summary>Once per event: a new alert only if its start is more than 3 h away from the last announced one.</summary>
    public static bool ShouldAnnounce(RainAlert alert, long? lastStart) =>
        lastStart is null || Math.Abs(alert.Start - lastStart.Value) > 3 * 3600;
}

/// <summary>Parsers for the Open-Meteo forecast / geocoding JSON and the geojs.io IP location.</summary>
public static class OpenMeteoParser
{
    public static WeatherReport? Forecast(string json, string place, DateTimeOffset fetched)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var r = new WeatherReport { Place = place, Fetched = fetched };
            if (root.TryGetProperty("utc_offset_seconds", out var off)) r.UtcOffsetSeconds = off.GetInt32();
            var c = root.GetProperty("current");
            r.Current = new CurrentWeather
            {
                Time = c.GetProperty("time").GetInt64(),
                Temp = Num(c, "temperature_2m"),
                Feels = Num(c, "apparent_temperature"),
                Humidity = (int)Num(c, "relative_humidity_2m"),
                Code = (int)Num(c, "weather_code"),
                IsDay = Num(c, "is_day", 1) != 0,
                Wind = Num(c, "wind_speed_10m"),
                Precip = Num(c, "precipitation"),
            };
            if (root.TryGetProperty("hourly", out var h))
            {
                var t = h.GetProperty("time");
                for (var i = 0; i < t.GetArrayLength(); i++)
                    r.Hours.Add(new HourWeather(t[i].GetInt64(), At(h, "temperature_2m", i), (int)At(h, "weather_code", i),
                        (int)At(h, "precipitation_probability", i), At(h, "is_day", i, 1) != 0));
            }
            if (root.TryGetProperty("daily", out var d))
            {
                var t = d.GetProperty("time");
                for (var i = 0; i < t.GetArrayLength(); i++)
                    r.Days.Add(new DayWeather(t[i].GetInt64(), At(d, "temperature_2m_max", i), At(d, "temperature_2m_min", i),
                        (int)At(d, "weather_code", i), (int)At(d, "precipitation_probability_max", i)));
            }
            if (root.TryGetProperty("minutely_15", out var m))
            {
                var t = m.GetProperty("time");
                for (var i = 0; i < t.GetArrayLength(); i++)
                    r.Minutely.Add(new PrecipSlot(t[i].GetInt64(), At(m, "precipitation", i)));
            }
            return r;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or IndexOutOfRangeException) { return null; }
    }

    public static List<Place> Places(string json)
    {
        var list = new List<Place>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var res)) return list;
            foreach (var e in res.EnumerateArray())
            {
                var name = Str(e, "name");
                if (name.Length == 0) continue;
                var detail = string.Join(", ", new[] { Str(e, "admin1"), Str(e, "country") }.Where(s => s.Length > 0 && s != name).Distinct());
                list.Add(new Place(name, detail, e.GetProperty("latitude").GetDouble(), e.GetProperty("longitude").GetDouble()));
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or FormatException) { list.Clear(); }
        return list;
    }

    /// <summary>geojs.io returns latitude/longitude as strings.</summary>
    public static Place? IpPlace(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var e = doc.RootElement;
            if (!double.TryParse(Str(e, "latitude"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
                !double.TryParse(Str(e, "longitude"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)) return null;
            return new Place(Str(e, "city"), Str(e, "country"), lat, lon);
        }
        catch (JsonException) { return null; }
    }

    static string Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) ? v.ValueKind switch
        {
            JsonValueKind.String => v.GetString() ?? "",
            JsonValueKind.Number => v.GetRawText(),
            _ => "",
        } : "";

    static double Num(JsonElement e, string name, double fallback = 0) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : fallback;

    static double At(JsonElement block, string name, int i, double fallback = 0)
    {
        if (!block.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array || i >= arr.GetArrayLength()) return fallback;
        var v = arr[i];
        return v.ValueKind == JsonValueKind.Number ? v.GetDouble() : fallback;
    }
}
