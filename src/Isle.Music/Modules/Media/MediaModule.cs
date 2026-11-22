using System.Windows.Threading;
using Isle.Core;
using Isle.Views.Music;

namespace Isle.Modules.Media;

/// <summary>
/// Music (module id "media"): follows the system media session, shows it in the pill (art + equalizer, title banner on
/// track change) and tints the accent from the album art. The Home pack adds the Music tab and the pill's lyric line
/// through <see cref="Started"/> / <see cref="Stopping"/> and <see cref="PillLyric"/>.
/// </summary>
public sealed class MediaModule(IslandWindow island) : IModule, IDisposable
{
    public const string TabId = "music";
    const string PillId = "music", BannerId = "music.track";
    static readonly TimeSpan PausedLinger = TimeSpan.FromMinutes(2), BannerTime = TimeSpan.FromSeconds(2.2);

    MediaSession? _session;
    MusicPill? _pill;
    DispatcherTimer? _bannerTimer, _lingerTimer;
    string? _lastTrack, _banneredTrack;
    bool _pillShown;

    bool _playingPublished;

    public string Id => "media";
    /// <summary>True while a media session is playing (characters dance).</summary>
    public bool IsPlaying => _session?.Current?.IsPlaying == true;
    public IMediaSource? Source => _session;

    /// <summary>Raised after a media module started / before it stops (the Home pack attaches the Music tab and lyrics).</summary>
    public static event Action<MediaModule>? Started, Stopping;

    /// <summary>The pill's lyric line, set by the Home pack while "lyrics.inPill" is on; null = plain pill.</summary>
    public LyricLine? PillLyric { get; set; }

    public void Start()
    {
        _session = new MediaSession(island.Dispatcher);
        _session.Changed += OnChanged;
        Raise(Started);
        _ = StartSessionAsync(_session);
    }

    void Raise(Action<MediaModule>? e)
    {
        try { e?.Invoke(this); }
        catch (Exception ex) { Logger.Error("media extension", ex); }
    }

    /// <summary>The lyric line or <see cref="PillLyric"/> changed; <paramref name="resize"/> also re-measures the pill.</summary>
    public void RefreshPill(bool resize)
    {
        _pill?.UpdateLyric();
        if (resize) ShowPill(force: true);
    }

    static async Task StartSessionAsync(MediaSession s)
    {
        try { await s.StartAsync(); }
        catch (Exception ex) { Logger.Error("media start (GSMTC unavailable?)", ex); }
    }

    void OnChanged()
    {
        var cur = _session?.Current;
        var playing = cur?.IsPlaying == true;
        if (playing != _playingPublished) { _playingPublished = playing; EventBus.Publish(new MusicPlaying(playing)); }
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
        if (newTrack) island.RequestTab(TabId); // open Music first after a track change
    }

    void ShowPill(bool force = false)
    {
        if (_session?.Current is not { } cur) return;
        _pill ??= new MusicPill(_session, () => PillLyric);
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
        Raise(Stopping);
        _bannerTimer?.Stop();
        _lingerTimer?.Stop();
        island.Pill.Remove(PillId);
        island.Pill.Remove(BannerId);
        if (_session is not null)
        {
            _session.Changed -= OnChanged;
            _session.Dispose();
            _session = null;
        }
        _pill?.Dispose();
        _pill = null;
        PillLyric = null;
        Theme.SetAccent(null);
        if (_playingPublished) { _playingPublished = false; EventBus.Publish(new MusicPlaying(false)); }
    }

    public void Dispose() { if (_session is not null) Stop(); }
}

/// <summary>Published when playback starts or stops (any source).</summary>
public sealed record MusicPlaying(bool Playing);
