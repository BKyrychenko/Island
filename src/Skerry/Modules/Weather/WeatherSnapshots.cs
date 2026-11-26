using Skerry.Core;
using Skerry.Views.Live;
using Skerry.Views.Weather;

namespace Skerry.Modules.Weather;

/// <summary>--snapshot states for weather (demo report anchored to the current hour, no network).</summary>
static class WeatherSnapshots
{
    static readonly WeatherActions NoOp = new((_, _) => Task.FromResult(new List<Place>()), _ => { }, _ => { }, () => { }, _ => { }, _ => { }, _ => { });

    public static void Register(Dictionary<string, Action<IslandWindow>> states)
    {
        states["weather-tab"] = w => Tab(w, v => v.ShowReport(Demo(), false, false));
        states["weather-days"] = w => Tab(w, v => { v.ShowReport(Demo(), false, false); v.SwitchToDays(); });
        states["weather-offline-f"] = w => Tab(w, v => v.ShowReport(Demo(), true, true));
        states["weather-setup"] = w => Tab(w, v => v.ShowSetup(false));
        states["weather-results"] = w => Tab(w, v => v.ShowSetupWithResults(
        [
            new Place("Kyiv", "Kyiv City, Ukraine", 50.45, 30.52),
            new Place("Kyiv", "Mykolaiv Oblast, Ukraine", 47.2, 31.9),
            new Place("Amsterdam", "North Holland, Netherlands", 52.37, 4.89),
        ]));
        states["weather-loading"] = w => Tab(w, v => v.ShowLoading());
        states["weather-error"] = w => Tab(w, v => v.ShowError());
        states["weather-pill"] = w =>
        {
            var p = new WeatherPill();
            p.Set("Wx.PartlyDay", "7°");
            w.IdleView.RightSlot = p;
        };
        states["weather-rain"] = w => w.Pill.Set(new PillItem
        {
            Id = "weather.rain", Priority = PillPriority.Transient, Width = 280, Peek = true,
            View = () => new IndicatorPill("Wx.Rain", WeatherModule.AlertText(new RainAlert(0, 20, false)), "Kyiv", "AccentBrush"),
        });
    }

    static void Tab(IslandWindow w, Action<WeatherView> setup)
    {
        var view = new WeatherView(NoOp);
        setup(view);
        w.AddTab(new TabInfo(WeatherModule.TabId, "tab.weather", "Icon.Weather", () => view, 80));
        w.Expand(WeatherModule.TabId);
    }

    /// <summary>A plausible autumn day: cloudy now, rain in the afternoon, clearing later.</summary>
    internal static WeatherReport Demo()
    {
        var now = DateTimeOffset.Now;
        var hour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);
        var r = new WeatherReport
        {
            Place = "Kyiv", Fetched = now.AddMinutes(-7), UtcOffsetSeconds = (int)now.Offset.TotalSeconds,
            Current = new CurrentWeather { Time = hour.ToUnixTimeSeconds(), Temp = 7.4, Feels = 4.2, Humidity = 81, Code = 2, IsDay = true, Wind = 14 },
        };
        int[] codes = [2, 2, 3, 3, 61, 63, 61, 3, 2, 1, 0, 0, 0, 1, 2, 3, 3, 45, 45, 3, 2, 2, 1, 1, 1];
        for (var i = 0; i < 25; i++)
        {
            var t = hour.AddHours(i);
            var temp = 7.4 + 3 * Math.Sin((t.Hour - 9) / 24.0 * 2 * Math.PI);
            var prob = codes[i] >= 61 ? 70 + i : codes[i] == 3 ? 25 : 5;
            r.Hours.Add(new HourWeather(t.ToUnixTimeSeconds(), temp, codes[i], prob, t.Hour is >= 7 and < 17));
        }
        int[] dCodes = [63, 3, 0, 71, 2, 80, 1];
        for (var d = 0; d < 7; d++)
        {
            var t = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset).AddDays(d);
            r.Days.Add(new DayWeather(t.ToUnixTimeSeconds(), 10 - d * 0.8, 3 - d, dCodes[d], dCodes[d] >= 61 ? 80 : 10));
        }
        return r;
    }
}
