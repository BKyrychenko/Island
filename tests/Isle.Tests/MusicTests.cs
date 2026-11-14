using System.Text.Json.Nodes;
using System.Windows.Media;
using Isle.Modules.Lyrics;
using Isle.Modules.Media;

namespace Isle.Tests;

public class LrcTests
{
    [Fact]
    public void Parses_basic_lines_and_sorts()
    {
        var lines = Lrc.Parse("[ar:Someone]\n[00:12.50]second\n[00:01.00]first\n\n[01:02.345]third");
        Assert.Equal(3, lines.Count);
        Assert.Equal(TimeSpan.FromSeconds(1), lines[0].Time);
        Assert.Equal("first", lines[0].Text);
        Assert.Equal(TimeSpan.FromMilliseconds(12500), lines[1].Time);
        Assert.Equal(new TimeSpan(0, 0, 1, 2, 345), lines[2].Time);
    }

    [Fact]
    public void Multiple_stamps_share_text()
    {
        var lines = Lrc.Parse("[00:10.00][00:40.00]chorus\n[00:20.00]verse");
        Assert.Equal(["chorus", "verse", "chorus"], lines.Select(l => l.Text));
    }

    [Fact]
    public void Offset_and_fraction_lengths()
    {
        var lines = Lrc.Parse("[offset:+500]\n[00:10.5]a\n[00:20]b\n[00:30:25]c");
        Assert.Equal(TimeSpan.FromMilliseconds(10000), lines[0].Time);   // 10.5 s − 0.5 s
        Assert.Equal(TimeSpan.FromMilliseconds(19500), lines[1].Time);
        Assert.Equal(TimeSpan.FromMilliseconds(29750), lines[2].Time);   // [mm:ss:xx] variant
    }

    [Fact]
    public void Strips_word_stamps_and_keeps_empty_lines()
    {
        var lines = Lrc.Parse("[00:01.00]<00:01.00>hello <00:01.50>world\r\n[00:05.00]");
        Assert.Equal("hello world", lines[0].Text);
        Assert.Equal("", lines[1].Text);
    }

    [Fact]
    public void Ignores_garbage()
    {
        Assert.Empty(Lrc.Parse(null));
        Assert.Empty(Lrc.Parse("just text\n[ti:title]\n[xx:yy]nope"));
    }

    [Theory]
    [InlineData(0.5, -1)]
    [InlineData(1.0, 0)]
    [InlineData(11.9, 0)]
    [InlineData(12.5, 1)]
    [InlineData(500, 2)]
    public void IndexAt_finds_current_line(double seconds, int expected)
    {
        var lines = Lrc.Parse("[00:01.00]a\n[00:12.50]b\n[01:00.00]c");
        Assert.Equal(expected, Lrc.IndexAt(lines, TimeSpan.FromSeconds(seconds)));
    }
}

public class PlaybackTests
{
    static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Extrapolates_only_while_playing_and_clamps()
    {
        var d = TimeSpan.FromSeconds(100);
        Assert.Equal(TimeSpan.FromSeconds(15), PlaybackMath.Extrapolate(TimeSpan.FromSeconds(10), T0, T0.AddSeconds(5), true, 1, d));
        Assert.Equal(TimeSpan.FromSeconds(10), PlaybackMath.Extrapolate(TimeSpan.FromSeconds(10), T0, T0.AddSeconds(5), false, 1, d));
        Assert.Equal(TimeSpan.FromSeconds(20), PlaybackMath.Extrapolate(TimeSpan.FromSeconds(10), T0, T0.AddSeconds(5), true, 2, d));
        Assert.Equal(d, PlaybackMath.Extrapolate(TimeSpan.FromSeconds(99), T0, T0.AddSeconds(30), true, 1, d));
        Assert.Equal(TimeSpan.FromSeconds(10), PlaybackMath.Extrapolate(TimeSpan.FromSeconds(10), T0, T0.AddSeconds(-3), true, 1, d)); // clock skew
    }

    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(65, "1:05")]
    [InlineData(3723, "1:02:03")]
    public void Formats_time(int seconds, string expected) => Assert.Equal(expected, PlaybackMath.Format(TimeSpan.FromSeconds(seconds)));

    [Theory]
    [InlineData("Spotify.exe", "Spotify")]
    [InlineData("Microsoft.ZuneMusic_8wekyb3d8bbwe!Microsoft.ZuneMusic", "Media Player")]
    [InlineData("MSEdge", "Edge")]
    [InlineData("308046B0AF4A39CB", "308046B0AF4A39CB")]
    [InlineData(@"C:\Apps\coolplayer.exe", "Coolplayer")]
    [InlineData("Company.App_abc123!App", "App")]
    [InlineData("", "")]
    public void Friendly_source_names(string id, string expected) => Assert.Equal(expected, PlaybackMath.FriendlySource(id));

    [Fact]
    public void Chooses_playing_then_current_then_recent()
    {
        Assert.Equal(-1, PlaybackMath.Choose([]));
        Assert.Equal(1, PlaybackMath.Choose([(false, true, T0), (true, false, T0)]));
        Assert.Equal(0, PlaybackMath.Choose([(false, true, T0), (false, false, T0.AddMinutes(5))]));
        Assert.Equal(1, PlaybackMath.Choose([(false, false, T0), (false, false, T0.AddMinutes(5))]));
        Assert.Equal(1, PlaybackMath.Choose([(true, false, T0), (true, true, T0)]));
    }
}

public class ArtColorTests
{
    static byte[] Pixels(params (byte R, byte G, byte B, int Count)[] groups)
    {
        var list = new List<byte>();
        foreach (var (r, g, b, n) in groups)
            for (var i = 0; i < n; i++) list.AddRange([b, g, r, 255]);
        return [.. list];
    }

    [Fact]
    public void Saturated_colour_beats_large_grey_area()
    {
        var c = ArtColors.Dominant(Pixels((128, 128, 128, 200), (20, 20, 20, 30), (220, 40, 40, 26)));
        var (h, s, _) = ArtColors.ToHsl(c);
        Assert.True(h < 15 || h > 345, $"hue {h}");
        Assert.True(s > 0.45);
    }

    [Fact]
    public void Greyscale_art_falls_back_to_default_accent()
    {
        Assert.Equal(ArtColors.Fallback, ArtColors.Dominant(Pixels((128, 128, 128, 100), (250, 250, 250, 100))));
        Assert.Equal(ArtColors.Fallback, ArtColors.Dominant(Array.Empty<byte>()));
    }

    [Fact]
    public void Readable_keeps_lightness_in_band()
    {
        foreach (var c in new[] { Colors.Black, Colors.White, Color.FromRgb(10, 0, 40), Color.FromRgb(255, 250, 200) })
        {
            var (_, s, l) = ArtColors.ToHsl(ArtColors.Readable(c));
            Assert.InRange(l, 0.44, 0.66);
        }
    }

    [Fact]
    public void Placeholder_is_deterministic_and_varies()
    {
        Assert.Equal(ArtColors.PlaceholderColors("abc"), ArtColors.PlaceholderColors("abc"));
        Assert.NotEqual(ArtColors.PlaceholderColors("abc"), ArtColors.PlaceholderColors("abd"));
    }

    [Fact]
    public void Hsl_roundtrip()
    {
        var c = Color.FromRgb(0x5A, 0xA9, 0xFF);
        var (h, s, l) = ArtColors.ToHsl(c);
        var back = ArtColors.FromHsl(h, s, l);
        Assert.InRange(Math.Abs(back.R - c.R) + Math.Abs(back.G - c.G) + Math.Abs(back.B - c.B), 0, 3);
    }
}

public class LyricsServiceTests
{
    sealed class FakeHttp : ILyricsHttp
    {
        public readonly List<string> Urls = [];
        public Func<string, (int, string)> Respond = _ => (404, "");
        public Task<(int Status, string Body)> GetAsync(string url, CancellationToken ct)
        {
            Urls.Add(url);
            return Task.FromResult(Respond(url));
        }
    }

    static readonly DateTimeOffset T0 = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
    static readonly TrackQuery Q = new("Some Artist", "Some Song", "Some Album", TimeSpan.FromSeconds(200));

    const string Record = """{"id":1,"trackName":"Some Song","duration":200,"instrumental":false,"plainLyrics":"a\nb","syncedLyrics":"[00:01.00]a\n[00:02.00]b"}""";

    [Fact]
    public async Task Get_hit_is_synced_and_cached_on_disk()
    {
        using var dir = new TempDir();
        var http = new FakeHttp { Respond = u => u.Contains("/get?") ? (200, Record) : (404, "") };
        var svc = new LyricsService(http, dir.Path, () => T0);
        var r = await svc.GetAsync(Q);
        Assert.Equal(LyricsStatus.Synced, r.Status);
        Assert.Equal(2, r.Lines.Count);
        Assert.Contains("artist_name=Some%20Artist", http.Urls[0]);
        Assert.Contains("duration=200", http.Urls[0]);

        // a fresh service reads the disk cache: no network
        var http2 = new FakeHttp();
        var again = await new LyricsService(http2, dir.Path, () => T0).GetAsync(Q);
        Assert.Equal(LyricsStatus.Synced, again.Status);
        Assert.Equal(["a", "b"], again.Lines.Select(l => l.Text));
        Assert.Empty(http2.Urls);
    }

    [Fact]
    public async Task Falls_back_to_search_with_cleaned_title()
    {
        using var dir = new TempDir();
        var search = """[{"duration":300,"syncedLyrics":"[00:01.00]wrong"},{"duration":201,"plainLyrics":"plain only"},{"duration":199,"syncedLyrics":"[00:01.00]right"}]""";
        var http = new FakeHttp { Respond = u => u.Contains("/search?") ? (200, search) : (404, """{"message":"not found"}""") };
        var q = Q with { Title = "Some Song (feat. Other) [Official Video]" };
        var r = await new LyricsService(http, dir.Path, () => T0).GetAsync(q);
        Assert.Equal(LyricsStatus.Synced, r.Status);
        Assert.Equal("right", r.Lines[0].Text);
        Assert.Contains("track_name=Some%20Song&", http.Urls[1]);
    }

    [Fact]
    public async Task Network_errors_are_not_cached_but_not_found_is_until_a_week()
    {
        using var dir = new TempDir();
        var now = T0;
        var http = new FakeHttp { Respond = _ => (0, "") };
        var svc = new LyricsService(http, dir.Path, () => now);
        Assert.Equal(LyricsStatus.Error, (await svc.GetAsync(Q)).Status);

        http.Respond = u => u.Contains("/search?") ? (200, "[]") : (404, "");
        Assert.Equal(LyricsStatus.NotFound, (await svc.GetAsync(Q)).Status);
        var calls = http.Urls.Count;

        var svc2 = new LyricsService(http, dir.Path, () => now.AddDays(3));
        Assert.Equal(LyricsStatus.NotFound, (await svc2.GetAsync(Q)).Status);
        Assert.Equal(calls, http.Urls.Count); // from disk

        var svc3 = new LyricsService(http, dir.Path, () => now.AddDays(8));
        await svc3.GetAsync(Q);
        Assert.True(http.Urls.Count > calls); // retried
    }

    [Fact]
    public void Instrumental_and_plain_records()
    {
        Assert.Equal(LyricsStatus.Instrumental, LyricsService.FromRecord(JsonNode.Parse("""{"instrumental":true}"""), T0)!.Status);
        var plain = LyricsService.FromRecord(JsonNode.Parse("""{"instrumental":false,"plainLyrics":" la la ","syncedLyrics":null}"""), T0)!;
        Assert.Equal(LyricsStatus.Plain, plain.Status);
        Assert.Equal("la la", plain.Plain);
        Assert.Equal(LyricsStatus.NotFound, LyricsService.FromRecord(JsonNode.Parse("{}"), T0)!.Status);
    }

    [Theory]
    [InlineData("Song (feat. X)", "Song")]
    [InlineData("Song [Official Music Video]", "Song")]
    [InlineData("Song - Remastered 2011", "Song")]
    [InlineData("Song - 2011 Remaster", "Song")]
    [InlineData("Song (Live at Wembley)", "Song")]
    [InlineData("Song (Interlude)", "Song (Interlude)")]
    public void Cleans_titles(string raw, string expected) => Assert.Equal(expected, LyricsService.CleanTitle(raw));

    [Fact]
    public void Cleans_youtube_artists()
    {
        Assert.Equal("Adele", LyricsService.CleanArtist("AdeleVEVO"));
        Assert.Equal("Adele", LyricsService.CleanArtist("Adele - Topic"));
    }
}
