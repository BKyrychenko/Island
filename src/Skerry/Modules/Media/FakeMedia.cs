using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Skerry.Modules.Lyrics;

namespace Skerry.Modules.Media;

/// <summary>In-memory media source for snapshots and tests.</summary>
public sealed class FakeMediaSource : IMediaSource
{
    MediaState? _current;
    public MediaState? Current { get => _current; set { _current = value; Changed?.Invoke(); } }
    public event Action? Changed;
    public int PlayPauseCount, NextCount, PrevCount;
    public TimeSpan? LastSeek;
    public void PlayPause() => PlayPauseCount++;
    public void Next() => NextCount++;
    public void Previous() => PrevCount++;
    public void Seek(TimeSpan position) => LastSeek = position;

    /// <summary>A demo track, fixed in time (position stamp = <paramref name="now"/>).</summary>
    public static MediaState Demo(DateTimeOffset now, bool withArt = true, bool playing = true)
    {
        var art = withArt ? DemoArt() : null;
        return new MediaState
        {
            Title = "Paper Lanterns Over the Harbour",
            Artist = "The Quiet Meridian",
            Album = "Low Tide Hours",
            Source = "Spotify",
            IsPlaying = playing,
            CanSeek = true,
            Duration = TimeSpan.FromSeconds(222),
            Position = TimeSpan.FromSeconds(81),
            PositionStamp = now,
            Art = art,
            Accent = art is not null ? ArtColors.Dominant(art) : ArtColors.Readable(ArtColors.PlaceholderColors("Paper Lanterns Over the Harbour").A),
        };
    }

    /// <summary>Original abstract "album art" drawn in code (a sunset over water).</summary>
    public static BitmapSource DemoArt()
    {
        var v = new DrawingVisual();
        using (var dc = v.RenderOpen())
        {
            dc.DrawRectangle(new LinearGradientBrush(Color.FromRgb(0xF2, 0x8C, 0x45), Color.FromRgb(0x8E, 0x2D, 0x5B), 90), null, new Rect(0, 0, 192, 192));
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xFF, 0xD3, 0x6E)), null, new Point(96, 110), 42, 42);
            dc.DrawRectangle(new LinearGradientBrush(Color.FromRgb(0x2B, 0x2E, 0x6B), Color.FromRgb(0x14, 0x15, 0x33), 90), null, new Rect(0, 118, 192, 74));
            for (var i = 0; i < 5; i++)
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(120, 0xFF, 0xD3, 0x6E)), null, new Rect(66 + i * 3, 128 + i * 11, 60 - i * 6, 3));
        }
        var bmp = new RenderTargetBitmap(192, 192, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(v);
        bmp.Freeze();
        return bmp;
    }

    public const string DemoLrc = """
        [00:58.10]Lanterns drifting where the water bends
        [01:05.40]Every light a letter that the evening sends
        [01:12.80]Hold the rope a little longer, friend
        [01:20.20]We are paper, we are flame, we are the end
        [01:27.60]Of a long and patient summer
        [01:34.90]Counting boats until the morning
        """;

    public static LyricsResult DemoLyrics(DateTimeOffset now) => new(LyricsStatus.Synced, Lrc.Parse(DemoLrc), "", now);
}
