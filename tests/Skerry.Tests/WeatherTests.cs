using Skerry.Core;
using Skerry.Modules.Weather;

namespace Skerry.Tests;

public class WeatherTests
{
    const string ForecastJson = """
        {"latitude":50.45,"longitude":30.52,"utc_offset_seconds":7200,
         "current":{"time":1763200800,"interval":900,"temperature_2m":7.4,"apparent_temperature":4.1,"relative_humidity_2m":81,
                    "weather_code":2,"is_day":1,"wind_speed_10m":14.2,"precipitation":0.0},
         "minutely_15":{"time":[1763200800,1763201700,1763202600,1763203500],"precipitation":[0.0,0.0,0.4,1.2]},
         "hourly":{"time":[1763200800,1763204400],"temperature_2m":[7.4,8.1],"weather_code":[2,61],
                   "precipitation_probability":[5,80],"is_day":[1,1]},
         "daily":{"time":[1763157600,1763244000],"weather_code":[61,3],"temperature_2m_max":[9.5,6.0],
                  "temperature_2m_min":[2.1,null],"precipitation_probability_max":[80,10]}}
        """;

    static readonly DateTimeOffset Fetched = DateTimeOffset.FromUnixTimeSeconds(1763200800);

    [Fact]
    public void ParsesForecast()
    {
        var r = OpenMeteoParser.Forecast(ForecastJson, "Kyiv", Fetched)!;
        Assert.Equal("Kyiv", r.Place);
        Assert.Equal(7200, r.UtcOffsetSeconds);
        Assert.Equal(7.4, r.Current.Temp);
        Assert.Equal(81, r.Current.Humidity);
        Assert.Equal(2, r.Current.Code);
        Assert.True(r.Current.IsDay);
        Assert.Equal(2, r.Hours.Count);
        Assert.Equal(61, r.Hours[1].Code);
        Assert.Equal(80, r.Hours[1].PrecipProb);
        Assert.Equal(2, r.Days.Count);
        Assert.Equal(0, r.Days[1].Min); // null → fallback
        Assert.Equal(4, r.Minutely.Count);
        Assert.Equal(12, r.Local(r.Current.Time).Hour); // 10:00 UTC + 2 h
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("{\"error\":true,\"reason\":\"bad\"}")]
    public void BadForecastIsNull(string json) => Assert.Null(OpenMeteoParser.Forecast(json, "x", Fetched));

    [Fact]
    public void ParsesPlaces()
    {
        var json = """
            {"results":[{"name":"Kyiv","latitude":50.45466,"longitude":30.5238,"country":"Ukraine","admin1":"Kyiv City"},
                        {"name":"Amsterdam","latitude":52.37,"longitude":4.89,"country":"Netherlands","admin1":"North Holland"},
                        {"name":"","latitude":1,"longitude":1}]}
            """;
        var list = OpenMeteoParser.Places(json);
        Assert.Equal(2, list.Count);
        Assert.Equal("Kyiv", list[0].Name);
        Assert.Equal("Kyiv City, Ukraine", list[0].Detail);
        Assert.Equal(52.37, list[1].Lat);
        Assert.Empty(OpenMeteoParser.Places("{\"generationtime_ms\":0.5}"));
        Assert.Empty(OpenMeteoParser.Places("not json"));
    }

    [Fact]
    public void ParsesIpPlace()
    {
        var p = OpenMeteoParser.IpPlace("{\"city\":\"Lviv\",\"country\":\"Ukraine\",\"latitude\":\"49.8383\",\"longitude\":\"24.0232\"}")!;
        Assert.Equal("Lviv", p.Name);
        Assert.Equal(49.8383, p.Lat);
        Assert.Null(OpenMeteoParser.IpPlace("{\"city\":\"x\"}"));
        Assert.Null(OpenMeteoParser.IpPlace("<html>"));
    }

    [Theory]
    [InlineData(0, WeatherKind.Clear)]
    [InlineData(2, WeatherKind.PartlyCloudy)]
    [InlineData(3, WeatherKind.Cloudy)]
    [InlineData(48, WeatherKind.Fog)]
    [InlineData(55, WeatherKind.Drizzle)]
    [InlineData(65, WeatherKind.Rain)]
    [InlineData(81, WeatherKind.Rain)]
    [InlineData(75, WeatherKind.Snow)]
    [InlineData(86, WeatherKind.Snow)]
    [InlineData(99, WeatherKind.Thunder)]
    [InlineData(42, WeatherKind.Cloudy)]
    public void MapsWmoCodes(int code, WeatherKind kind) => Assert.Equal(kind, WeatherCodes.Kind(code));

    [Fact]
    public void EveryCodeHasTextAndIconInBothLanguages()
    {
        foreach (var code in new[] { 0, 1, 2, 3, 45, 48, 51, 53, 55, 56, 57, 61, 63, 65, 66, 67, 71, 73, 75, 77, 80, 81, 82, 85, 86, 95, 96, 99 })
        {
            Assert.True(Strings.English.ContainsKey(WeatherCodes.TextKey(code)), $"en {code}");
            Assert.True(Strings.Ukrainian.ContainsKey(WeatherCodes.TextKey(code)), $"uk {code}");
            Assert.StartsWith("Wx.", WeatherCodes.IconKey(code, false));
        }
        Assert.Equal("Wx.Moon", WeatherCodes.IconKey(0, false));
        Assert.Equal("Wx.Sun", WeatherCodes.IconKey(0, true));
    }

    [Theory]
    [InlineData(7.4, false, "7°")]
    [InlineData(-0.4, false, "0°")]
    [InlineData(-2.5, false, "-3°")]
    [InlineData(0, true, "32°")]
    [InlineData(37, true, "99°")]
    public void FormatsTemperature(double c, bool f, string expected) => Assert.Equal(expected, WeatherFormat.Temp(c, f));

    static WeatherReport Report(long now, double[] slots, int currentCode = 2, double currentPrecip = 0)
    {
        var r = new WeatherReport { Current = new CurrentWeather { Time = now, Code = currentCode, Precip = currentPrecip } };
        var start = now - now % 900;
        for (var i = 0; i < slots.Length; i++) r.Minutely.Add(new PrecipSlot(start + i * 900, slots[i]));
        r.Hours.Add(new HourWeather(start - start % 3600, 5, 61, 80, true));
        return r;
    }

    [Fact]
    public void RainAlertFindsFirstWetSlot()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1763200800);
        var a = RainAlerts.Find(Report(now.ToUnixTimeSeconds(), [0, 0, 0.4, 1.2]), now)!;
        Assert.Equal(30, a.Minutes);
        Assert.False(a.Snow);
        Assert.Equal(now.ToUnixTimeSeconds() + 1800, a.Start);
    }

    [Fact]
    public void RainAlertRoundsToFiveMinutesAndAtLeastFive()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1763200800 + 840); // 14 min into the slot
        var a = RainAlerts.Find(Report(now.ToUnixTimeSeconds(), [0, 0.3]), now)!;
        Assert.Equal(5, a.Minutes); // 1 min away → at least 5
    }

    [Fact]
    public void NoAlertWhenAlreadyWetOrDryOrTooFar()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1763200800);
        var t = now.ToUnixTimeSeconds();
        Assert.Null(RainAlerts.Find(Report(t, [0.5, 1]), now));                       // raining now
        Assert.Null(RainAlerts.Find(Report(t, [0, 1], currentCode: 63), now));         // current code wet
        Assert.Null(RainAlerts.Find(Report(t, [0, 0, 0, 0, 0, 0, 0, 0]), now));        // dry
        Assert.Null(RainAlerts.Find(Report(t, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1]), now)); // 2.5 h away
    }

    [Fact]
    public void RainAlertFallsBackToHourly()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1763200800);
        var t = now.ToUnixTimeSeconds();
        var r = new WeatherReport { Current = new CurrentWeather { Time = t, Code = 3 } };
        r.Hours.Add(new HourWeather(t, 5, 3, 10, true));
        r.Hours.Add(new HourWeather(t + 3600, 0, 73, 90, true));
        var a = RainAlerts.Find(r, now)!;
        Assert.Equal(60, a.Minutes);
        Assert.True(a.Snow);
        r.Hours[1] = r.Hours[1] with { PrecipProb = 30 };
        Assert.Null(RainAlerts.Find(r, now));
    }

    [Fact]
    public void RainAnnouncedOncePerEvent()
    {
        var a = new RainAlert(10_000, 20, false);
        Assert.True(RainAlerts.ShouldAnnounce(a, null));
        Assert.False(RainAlerts.ShouldAnnounce(a, 10_000 - 1800));
        Assert.True(RainAlerts.ShouldAnnounce(a, 10_000 - 4 * 3600));
    }

    sealed class FakeSource : IWeatherSource
    {
        public WeatherReport? Next;
        public bool Throw;
        public int Calls;
        public Task<WeatherReport?> ForecastAsync(Place place, CancellationToken ct)
        {
            Calls++;
            if (Throw) throw new System.Net.Http.HttpRequestException("down");
            return Task.FromResult(Next);
        }
        public Task<List<Place>> SearchAsync(string name, string language, CancellationToken ct) => Task.FromResult(new List<Place>());
        public Task<Place?> LocateByIpAsync(CancellationToken ct) => Task.FromResult<Place?>(null);
    }

    static string TempFile() => Path.Combine(Path.GetTempPath(), "skerry-wx-" + Guid.NewGuid().ToString("N") + ".json");

    [Fact]
    public async Task ServiceCachesAndKeepsReportOffline()
    {
        var file = TempFile();
        try
        {
            var place = new Place("Kyiv", "", 50.45, 30.52);
            var fake = new FakeSource { Next = OpenMeteoParser.Forecast(ForecastJson, "Kyiv", DateTimeOffset.Now) };
            var svc = new WeatherService(fake, file);
            Assert.Null(svc.Report);
            Assert.True(svc.IsStale(DateTimeOffset.Now));
            await svc.RefreshAsync(place, default);
            Assert.False(svc.LastFailed);
            Assert.False(svc.IsStale(DateTimeOffset.Now));
            Assert.True(File.Exists(file));

            // a new instance (restart) reads the cache; a failed refresh keeps it
            var fake2 = new FakeSource { Throw = true };
            var svc2 = new WeatherService(fake2, file);
            Assert.Equal(7.4, svc2.Report!.Current.Temp);
            var kept = await svc2.RefreshAsync(place, default);
            Assert.True(svc2.LastFailed);
            Assert.Equal(7.4, kept!.Current.Temp);
            Assert.True(svc2.IsStale(DateTimeOffset.Now.AddMinutes(31)));

            svc2.Clear();
            Assert.Null(svc2.Report);
            Assert.False(File.Exists(file));
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void IpChipReportsBothOffAndOn()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var values = new List<bool>();
                var view = new Skerry.Views.Weather.WeatherView(new(
                    (_, _) => Task.FromResult(new List<Place>()), _ => { }, values.Add,
                    () => { }, _ => { }, _ => { }, _ => { }));
                view.ShowSetup(true);
                var body = (System.Windows.Controls.ContentControl)view.Children[0];
                var grid = (System.Windows.Controls.Grid)body.Content;
                var chips = (System.Windows.Controls.WrapPanel)grid.Children[1];
                var ip = (System.Windows.Controls.Primitives.ToggleButton)chips.Children[0];
                foreach (var on in new[] { false, true })
                {
                    ip.IsChecked = on;
                    ip.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                }
                Assert.Equal(new[] { false, true }, values);
            }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error is not null) throw new Exception("STA test failed", error);
    }

    [Fact]
    public void ErrorAllowsRetryAndChangeAndForecastExposesScrollbar()
    {
        FakeSink.Run(_ =>
        {
            var retries = 0;
            var view = new Skerry.Views.Weather.WeatherView(new(
                (_, _) => Task.FromResult(new List<Place>()), _ => { }, _ => { },
                () => retries++, _ => { }, _ => { }, _ => { }));
            var body = (System.Windows.Controls.ContentControl)view.Children[0];
            view.ShowError();
            var actions = ((System.Windows.Controls.StackPanel)body.Content).Children.OfType<System.Windows.Controls.Button>().ToList();
            Assert.Equal(2, actions.Count);
            actions[0].RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Assert.Equal(1, retries);
            actions[1].RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Assert.IsType<System.Windows.Controls.Grid>(body.Content);
            view.ShowReport(new WeatherReport(), false, false);
            var report = (System.Windows.Controls.Grid)body.Content;
            var scroll = Assert.IsType<System.Windows.Controls.ScrollViewer>(report.Children[1]);
            Assert.Equal(System.Windows.Controls.ScrollBarVisibility.Auto, scroll.HorizontalScrollBarVisibility);
        });
    }

    [Fact]
    public void IpOffPersistsAndRemovesLegacyIpCoordinates()
    {
        using var dir = new TempDir();
        var settings = new Settings();
        settings.SetOpt("weather.ip", true);
        settings.SetOpt("weather.city", "IP city");
        settings.SetOpt("weather.lat", "50");
        settings.SetOpt("weather.lon", "30");
        WeatherModule.ChangeIpOption(settings, false);
        var file = Path.Combine(dir.Path, "settings.json");
        JsonStore.Save(file, settings);
        var loaded = Settings.Load(file);
        Assert.False(loaded.OptBool("weather.ip"));
        Assert.Equal("", loaded.Opt("weather.lat"));
        Assert.Equal("", loaded.Opt("weather.lon"));
        Assert.Equal("", loaded.Opt("weather.city"));
    }

    [Fact]
    public void IpTogglePreservesNamedCity()
    {
        var settings = new Settings();
        settings.SetOpt("weather.city", "Kyiv");
        settings.SetOpt("weather.lat", "50.45");
        settings.SetOpt("weather.lon", "30.52");
        WeatherModule.ChangeIpOption(settings, true);
        Assert.True(settings.OptBool("weather.ip"));
        WeatherModule.ChangeIpOption(settings, false);
        Assert.False(settings.OptBool("weather.ip"));
        Assert.Equal("Kyiv", settings.Opt("weather.city"));
        Assert.Equal("50.45", settings.Opt("weather.lat"));
        Assert.Equal("30.52", settings.Opt("weather.lon"));
    }

    sealed class DelayedSource : IWeatherSource
    {
        public TaskCompletionSource<WeatherReport?> Result = new();
        public Task<WeatherReport?> ForecastAsync(Place place, CancellationToken ct) => Result.Task;
        public Task<List<Place>> SearchAsync(string name, string language, CancellationToken ct) => Task.FromResult(new List<Place>());
        public Task<Place?> LocateByIpAsync(CancellationToken ct) => Task.FromResult<Place?>(null);
    }

    [Fact]
    public async Task CancelledForecastCannotRestoreClearedLocation()
    {
        using var dir = new TempDir();
        using var cts = new CancellationTokenSource();
        var source = new DelayedSource();
        var file = Path.Combine(dir.Path, "weather.json");
        var service = new WeatherService(source, file);
        var pending = service.RefreshAsync(new Place("old", "", 1, 2), cts.Token);
        cts.Cancel();
        service.Clear();
        source.Result.SetResult(new WeatherReport { Place = "old" });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Null(service.Report);
        Assert.False(File.Exists(file));
    }

    [Fact]
    public void CorruptCacheIsIgnored()
    {
        var file = TempFile();
        try
        {
            File.WriteAllText(file, "{{ not json");
            Assert.Null(new WeatherService(new FakeSource(), file).Report);
        }
        finally
        {
            File.Delete(file);
            foreach (var bak in Directory.GetFiles(Path.GetDirectoryName(file)!, Path.GetFileName(file) + ".bak-*")) File.Delete(bak);
        }
    }
}
