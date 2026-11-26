using Skerry.Core;
using Skerry.Modules.Media;

namespace Skerry.Modules.Lyrics;

/// <summary>Owns the LRCLIB service (module id "lyrics"). Nothing touches the network until lyrics are actually needed.</summary>
public sealed class LyricsModule : IModule, IDisposable
{
    LyricsService? _service;

    public string Id => "lyrics";
    public LyricsService Service => _service ??= new LyricsService();

    public void Start() { }

    public void Stop()
    {
        _service?.Dispose();
        _service = null;
    }

    public void Dispose() => Stop();
}

/// <summary>
/// Lyrics for the track that is playing now. Fetches only when asked (<see cref="Ensure"/>: lyrics view open, or the
/// "always fetch" option for the pill line). One request per track; a newer track cancels the older request.
/// </summary>
public sealed class LyricsTracker(IMediaSource media, Func<LyricsService?> service)
{
    CancellationTokenSource? _cts;

    public string? TrackKey { get; private set; }
    public LyricsResult? Result { get; private set; }
    public bool Loading { get; private set; }

    /// <summary>Raised on the UI thread when <see cref="Result"/>/<see cref="Loading"/> change.</summary>
    public event Action? Updated;

    /// <summary>The result if it belongs to the current track.</summary>
    public LyricsResult? ForCurrent => media.Current is { } c && c.TrackKey == TrackKey ? Result : null;

    public async void Ensure()
    {
        var cur = media.Current;
        if (cur is null) return;
        if (cur.TrackKey == TrackKey && (Result is not null || Loading)) return;
        var svc = service();
        if (svc is null) return;

        _cts?.Cancel();
        var cts = _cts = new CancellationTokenSource();
        TrackKey = cur.TrackKey;
        Result = null;
        Loading = true;
        Raise();
        try
        {
            var r = await svc.GetAsync(new TrackQuery(cur.Artist, cur.Title, cur.Album, cur.Duration), cts.Token);
            if (cts.IsCancellationRequested) return;
            Result = r;
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            Logger.Error("lyrics fetch", ex);
            Result = LyricsResult.Error();
        }
        Loading = false;
        Raise();
    }

    /// <summary>Forget the cached error so the next <see cref="Ensure"/> retries.</summary>
    public void Retry()
    {
        if (Result?.Status == LyricsStatus.Error) { Result = null; TrackKey = null; }
        Ensure();
    }

    /// <summary>Snapshot/test hook.</summary>
    internal void Set(string trackKey, LyricsResult result)
    {
        TrackKey = trackKey;
        Result = result;
        Loading = false;
    }

    void Raise()
    {
        try { Updated?.Invoke(); }
        catch (Exception ex) { Logger.Error("lyrics updated handler", ex); }
    }
}
