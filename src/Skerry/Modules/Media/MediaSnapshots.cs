using Skerry.Core;
using Skerry.Modules.Lyrics;
using Skerry.Views.Music;

namespace Skerry.Modules.Media;

/// <summary>--snapshot states for music (fake source, fixed clock).</summary>
static class MediaSnapshots
{
    static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public static void Register(Dictionary<string, Action<IslandWindow>> states)
    {
        states["music-pill"] = w => Pill(w, Demo());
        states["music-pill-lyric"] = w => { Settings.Current.SetOpt("lyrics.inPill", true); Pill(w, Demo()); };
        states["music-banner"] = w =>
        {
            var s = Demo();
            w.Pill.Set(new PillItem { Id = "b", Priority = PillPriority.Transient, View = () => new TrackBanner(s), Width = TrackBanner.BannerWidth, Height = TrackBanner.BannerHeight, IsBanner = true });
        };
        states["music-tab"] = w => Tab(w, Demo(), lyrics: false);
        states["music-tab-noart"] = w => Tab(w, FakeMediaSource.Demo(Now, withArt: false, playing: false), lyrics: false);
        states["music-lyrics"] = w => Tab(w, Demo(), lyrics: true);
        states["music-lyrics-notfound"] = w => Tab(w, Demo(), lyrics: true, LyricsResult.NotFound(Now));
        states["music-empty"] = w => Tab(w, null, lyrics: false);
    }

    static MediaState Demo() => FakeMediaSource.Demo(Now);

    static (FakeMediaSource, LyricsTracker) Sources(MediaState? s, LyricsResult? lyrics = null)
    {
        var fake = new FakeMediaSource { Current = s };
        var tracker = new LyricsTracker(fake, () => null);
        if (s is not null) tracker.Set(s.TrackKey, lyrics ?? FakeMediaSource.DemoLyrics(Now));
        if (s is not null) Theme.SetAccent(s.Accent);
        return (fake, tracker);
    }

    static void Pill(IslandWindow w, MediaState s)
    {
        var (fake, tracker) = Sources(s);
        var pill = new MusicPill(fake, () => tracker, () => Now);
        pill.Update(s);
        w.Pill.Set(new PillItem { Id = "music", Priority = PillPriority.Music, View = () => pill, Width = pill.PillWidth });
    }

    static void Tab(IslandWindow w, MediaState? s, bool lyrics, LyricsResult? result = null)
    {
        var (fake, tracker) = Sources(s, result);
        MusicView.LyricsOpen = lyrics;
        w.AddTab(new TabInfo("music", "tab.music", "Icon.Music", () => new MusicView(fake, tracker, () => Now), 10));
        w.AddTab(new TabInfo("timers", "tab.timers", "Icon.Timer", () => new System.Windows.Controls.Grid(), 20));
        w.AddTab(new TabInfo("notes", "tab.notes", "Icon.Notes", () => new System.Windows.Controls.Grid(), 30));
        w.Expand("music");
    }
}
