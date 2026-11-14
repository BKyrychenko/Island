using System.Windows.Threading;
using Isle.Core;
using Isle.Modules.Lyrics;
using Isle.Views.Music;

namespace Isle.Modules.Media;

/// <summary>
/// Music (module id "media"): follows the system media session, shows it in the pill (art + equalizer, title banner on
/// track change, optional lyric line), provides the Music tab, and tints the accent from the album art.
/// </summary>
public sealed class MediaModule(IslandWindow island) : IModule, IDisposable
{
    public const string TabId = "music";
    const string PillId = "music", BannerId = "music.track";
    static readonly TimeSpan PausedLinger = TimeSpan.FromMinutes(2), BannerTime = TimeSpan.FromSeconds(2.2);

    MediaSession? _session;
    MusicPill? _pill;
    LyricsTracker? _lyrics;
    DispatcherTimer? _bannerTimer, _lingerTimer;
    string? _lastTrack, _banneredTrack;
    bool _pillShown;

    public string Id => "media";
    public IMediaSource? Source => _session;
    public LyricsTracker? Lyrics => _lyrics;

    public static bool LyricsInPill => Settings.Current.OptBool("lyrics.inPill");
    public static bool LyricsAlwaysFetch => Settings.Current.OptBool("lyrics.alwaysFetch") || LyricsInPill;

    public void Start()
    {
        _session = new MediaSession(island.Dispatcher);
        _lyrics = new LyricsTracker(_session, () => App.Host.Get<LyricsModule>("lyrics")?.Service);
        _session.Changed += OnChanged;
        _lyrics.Updated += () => _pill?.UpdateLyric();
        island.AddTab(new TabInfo(TabId, "tab.music", "Icon.Music", () => new MusicView(_session, _lyrics), 10));
        Settings.Changed += OnSettingsChanged;
        _ = StartSessionAsync(_session);
    }

    static async Task StartSessionAsync(MediaSession s)
    {
        try { await s.StartAsync(); }
        catch (Exception ex) { Logger.Error("media start (GSMTC unavailable?)", ex); }
    }

    void OnSettingsChanged(string what)
    {
        if (_session?.Current is null) return;
        if (LyricsAlwaysFetch) _lyrics?.Ensure();
        _pill?.UpdateLyric();
        ShowPill(force: true);
    }

    void OnChanged()
    {
        var cur = _session?.Current;
        if (cur is null)
        {
            _lastTrack = null;
            HidePill();
            island.Pill.Remove(BannerId);
            Theme.SetAccent(null);
            return;
        }

        Theme.SetAccent(cur.Accent);
        var newTrack = cur.TrackKey != _lastTrack;
        _lastTrack = cur.TrackKey;

        if (cur.IsPlaying)
        {
            _lingerTimer?.Stop();
            ShowPill();
        }
        else if (_pillShown)
        {
            // keep the paused track around for a while, then give the pill back to idle
            _lingerTimer ??= NewTimer(PausedLinger, () => { if (_session?.Current is { IsPlaying: false }) HidePill(); });
            if (!_lingerTimer.IsEnabled) _lingerTimer.Start();
        }
        _pill?.Update(cur);

        if (cur.IsPlaying && cur.TrackKey != _banneredTrack)
        {
            _banneredTrack = cur.TrackKey; // a track that first shows up paused gets its banner when it starts playing
            ShowBanner(cur);
        }
        if (newTrack)
        {
            island.RequestTab(TabId); // open Music first after a track change
            if (LyricsAlwaysFetch) _lyrics?.Ensure();
        }
    }

    void ShowPill(bool force = false)
    {
        if (_session?.Current is not { } cur) return;
        _pill ??= new MusicPill(_session, () => _lyrics);
        _pill.Update(cur);
        if (_pillShown && !force) return;
        _pillShown = true;
        island.Pill.Set(new PillItem
        {
            Id = PillId,
            Priority = PillPriority.Music,
            View = () => _pill,
            Compact = () => _pill.CreateCompact(),
            Width = _pill.PillWidth,
            CompactWidth = 40,
            TabId = TabId,
        });
    }

    void HidePill()
    {
        _pillShown = false;
        _lingerTimer?.Stop();
        island.Pill.Remove(PillId);
    }

    void ShowBanner(MediaState cur)
    {
        island.Pill.Set(new PillItem
        {
            Id = BannerId,
            Priority = PillPriority.Transient,
            View = () => new TrackBanner(cur),
            Width = TrackBanner.BannerWidth,
            Height = TrackBanner.BannerHeight,
            IsBanner = true,
            Peek = true,
            TabId = TabId,
        });
        _bannerTimer ??= NewTimer(BannerTime, () => island.Pill.Remove(BannerId));
        _bannerTimer.Stop();
        _bannerTimer.Start();
    }

    DispatcherTimer NewTimer(TimeSpan interval, Action tick)
    {
        var t = new DispatcherTimer(DispatcherPriority.Background, island.Dispatcher) { Interval = interval };
        t.Tick += (_, _) => { t.Stop(); tick(); };
        return t;
    }

    public void Stop()
    {
        Settings.Changed -= OnSettingsChanged;
        _bannerTimer?.Stop();
        _lingerTimer?.Stop();
        island.Pill.Remove(PillId);
        island.Pill.Remove(BannerId);
        island.RemoveTab(TabId);
        if (_session is not null)
        {
            _session.Changed -= OnChanged;
            _session.Dispose();
            _session = null;
        }
        _pill?.Dispose();
        _pill = null;
        _lyrics = null;
        Theme.SetAccent(null);
    }

    public void Dispose() { if (_session is not null) Stop(); }
}
