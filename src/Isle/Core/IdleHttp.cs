using System.Net.Http;

namespace Isle.Core;

/// <summary>HttpClient created on first use and disposed after <paramref name="idle"/> without requests (spec: heavy resources are lazy).</summary>
public sealed class IdleHttp(TimeSpan? idle = null) : Modules.Lyrics.ILyricsHttp, IDisposable
{
    readonly TimeSpan _idle = idle ?? TimeSpan.FromMinutes(2);
    readonly object _gate = new();
    HttpClient? _client;
    Timer? _timer;

    public async Task<(int Status, string Body)> GetAsync(string url, CancellationToken ct)
    {
        try
        {
            using var resp = await Client().GetAsync(url, ct).ConfigureAwait(false);
            return ((int)resp.StatusCode, await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            Logger.Warn($"http failed: {ex.GetType().Name}");
            return (0, "");
        }
    }

    HttpClient Client()
    {
        lock (_gate)
        {
            if (_client is null)
            {
                _client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                _client.DefaultRequestHeaders.UserAgent.ParseAdd("Isle/1.0 (Windows desktop overlay; personal use)");
            }
            _timer ??= new Timer(_ => Release());
            _timer.Change(_idle, Timeout.InfiniteTimeSpan);
            return _client;
        }
    }

    void Release()
    {
        lock (_gate)
        {
            _client?.Dispose();
            _client = null;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _timer?.Dispose();
            _timer = null;
            _client?.Dispose();
            _client = null;
        }
    }
}
