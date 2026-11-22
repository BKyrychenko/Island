using Isle.Core;
using Isle.Modules.Media;
using Isle.Views.Music;

namespace Isle.Modules.Lyrics;

/// <summary>
/// Home's part of music, attached to each started media module (Music pack): the Music tab, the lyrics tracker, the
/// pill's lyric line (option "lyrics.inPill") and prefetching (option "lyrics.alwaysFetch").
/// </summary>
static class MusicTab
{
    static Action? _detach;

    public static bool LyricsInPill => Settings.Current.OptBool("lyrics.inPill");
    public static bool LyricsAlwaysFetch => Settings.Current.OptBool("lyrics.alwaysFetch") || LyricsInPill;

    public static void Register()
    {
        MediaModule.Started += Attach;
        MediaModule.Stopping += _ => { _detach?.Invoke(); _detach = null; };
    }

    static void Attach(MediaModule media)
    {
        if (media.Source is not { } source || App.Island is not { } island) return;
        var tracker = new LyricsTracker(source, () => App.Host.Get<LyricsModule>("lyrics")?.Service);
        string? track = null;
        void Apply()
        {
            media.PillLyric = LyricsInPill ? (s, now) => Line(tracker, s, now) : null;
            if (LyricsAlwaysFetch) tracker.Ensure();
            media.RefreshPill(resize: true);
        }
        void OnSettings(string _) => Apply();
        void OnTrack()
        {
            if (source.Current?.TrackKey is not { } key || key == track) return;
            track = key;
            if (LyricsAlwaysFetch) tracker.Ensure();
        }
        tracker.Updated += () => media.RefreshPill(resize: false);
        source.Changed += OnTrack;
        Settings.Changed += OnSettings;
        island.AddTab(new TabInfo(MediaModule.TabId, "tab.music", "Icon.Music", () => new MusicView(source, tracker), 10));
        Apply();
        _detach = () =>
        {
            Settings.Changed -= OnSettings;
            source.Changed -= OnTrack;
            island.RemoveTab(MediaModule.TabId);
        };
    }

    /// <summary>The synced line at <paramref name="now"/> ("" before the first), or null without synced lyrics for the track.</summary>
    internal static string? Line(LyricsTracker tracker, MediaState track, DateTimeOffset now) =>
        tracker.ForCurrent is { Status: LyricsStatus.Synced } r ? Lrc.IndexAt(r.Lines, track.PositionAt(now)) is var i and >= 0 ? r.Lines[i].Text : "" : null;

    // ---- --snapshot states (fake source, the Music pack's fixed clock) ----

    static readonly DateTimeOffset Now = MediaSnapshots.Now;

    public const string DemoLrc = """
        [00:58.10]Lanterns drifting where the water bends
        [01:05.40]Every light a letter that the evening sends
        [01:12.80]Hold the rope a little longer, friend
        [01:20.20]We are paper, we are flame, we are the end
        [01:27.60]Of a long and patient summer
        [01:34.90]Counting boats until the morning
        """;

    public static LyricsResult DemoLyrics(DateTimeOffset now) => new(LyricsStatus.Synced, Lrc.Parse(DemoLrc), "", now);

    public static void RegisterSnapshots(Dictionary<string, Action<IslandWindow>> states)
    {
        states["music-pill-lyric"] = w =>
        {
            Settings.Current.SetOpt("lyrics.inPill", true);
            var s = MediaSnapshots.Demo();
            var (_, tracker) = Sources(s);
            MediaSnapshots.Pill(w, s, () => (t, now) => Line(tracker, t, now));
        };
        states["music-tab"] = w => Tab(w, MediaSnapshots.Demo(), lyrics: false);
        states["music-tab-noart"] = w => Tab(w, FakeMediaSource.Demo(Now, withArt: false, playing: false), lyrics: false);
        states["music-lyrics"] = w => Tab(w, MediaSnapshots.Demo(), lyrics: true);
        states["music-lyrics-notfound"] = w => Tab(w, MediaSnapshots.Demo(), lyrics: true, LyricsResult.NotFound(Now));
        states["music-empty"] = w => Tab(w, null, lyrics: false);
    }

    static (FakeMediaSource, LyricsTracker) Sources(MediaState? s, LyricsResult? lyrics = null)
    {
        var fake = new FakeMediaSource { Current = s };
        var tracker = new LyricsTracker(fake, () => null);
        if (s is not null) tracker.Set(s.TrackKey, lyrics ?? DemoLyrics(Now));
        if (s is not null) Theme.SetAccent(s.Accent);
        return (fake, tracker);
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
