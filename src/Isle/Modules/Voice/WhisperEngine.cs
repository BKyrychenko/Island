using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Isle.Core;
using Whisper.net;

namespace Isle.Modules.Voice;

/// <summary>Speech-to-text seam (fake in tests).</summary>
public interface ITranscriber : IDisposable
{
    /// <param name="language">"auto" or a Whisper language code.</param>
    Task<Transcript> TranscribeAsync(float[] samples16k, string modelPath, string language, CancellationToken ct);
}

public sealed record Transcript(string Text, string Language);

/// <summary>
/// Whisper.net (whisper.cpp) loaded on demand; the model is unloaded <see cref="IdleUnload"/> after the last use.
/// One transcription at a time. Must be created on the UI thread (the unload timer lives there).
/// </summary>
public sealed partial class WhisperEngine : ITranscriber, IAsyncDisposable
{
    public static readonly TimeSpan IdleUnload = TimeSpan.FromMinutes(2);
    readonly DispatcherTimer _idle;
    readonly SemaphoreSlim _gate = new(1, 1);
    WhisperFactory? _factory;
    string? _loadedPath;
    volatile bool _disposed;
    Task? _disposal;

    public WhisperEngine()
    {
        _idle = new DispatcherTimer(DispatcherPriority.Background) { Interval = IdleUnload };
        _idle.Tick += (_, _) => { _idle.Stop(); _ = UnloadAsync(); };
    }

    public bool IsLoaded => _factory is not null;

    public async Task<Transcript> TranscribeAsync(float[] samples, string modelPath, string language, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _idle.Stop();
        await _gate.WaitAsync(ct);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return await Task.Run(async () =>
            {
                if (_factory is null || _loadedPath != modelPath)
                {
                    _factory?.Dispose();
                    _factory = WhisperFactory.FromPath(modelPath);
                    _loadedPath = modelPath;
                    Logger.Info("whisper model loaded");
                }
                var threads = Math.Clamp(Environment.ProcessorCount / 2, 2, 8);
                await using var processor = _factory.CreateBuilder().WithThreads(threads).WithLanguage("auto").Build();
                var lang = language;
                if (lang == "auto")
                {
                    var (top, p) = processor.DetectLanguageWithProbability(samples);
                    string? preferred = null;
                    if (top is null || !LanguagePick.Preferred.Contains(top))
                        preferred = processor.DetectLanguageWithProbability(samples, LanguagePick.Preferred).language;
                    lang = LanguagePick.Choose(top, p, preferred);
                }
                processor.ChangeLanguage(lang);
                var parts = new List<string>();
                await foreach (var seg in processor.ProcessAsync(samples, ct)) parts.Add(seg.Text);
                return new Transcript(TranscriptText.Clean(parts), lang);
            }, ct);
        }
        finally
        {
            _gate.Release();
            if (!_disposed) _idle.Start();
        }
    }

    async Task UnloadAsync()
    {
        if (_disposed) return;
        if (!await _gate.WaitAsync(0)) { _idle.Start(); return; } // busy: try again later
        try
        {
            if (_factory is null) return;
            _factory.Dispose();
            _factory = null;
            _loadedPath = null;
            Logger.Info("whisper model unloaded");
        }
        finally { _gate.Release(); }
        GC.Collect();
        Memory.TrimSoon(1);
    }

    public void Dispose() => _ = DisposeAsync();

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        _idle.Stop();
        return new ValueTask(_disposal ??= DisposeWhenIdleAsync());
    }

    async Task DisposeWhenIdleAsync()
    {
        await _gate.WaitAsync();
        try
        {
            _factory?.Dispose();
            _factory = null;
            _loadedPath = null;
        }
        finally { _gate.Release(); }
    }
}

/// <summary>"Automatic, English / Ukrainian / Dutch preferred" (tested).</summary>
public static class LanguagePick
{
    public static readonly string[] Preferred = ["en", "uk", "nl"];
    public const float Confident = 0.75f;

    /// <summary>A preferred detection wins; another language only when Whisper is confident about it, otherwise the
    /// best of the preferred languages (e.g. Ukrainian heard as Russian with low confidence becomes Ukrainian).</summary>
    public static string Choose(string? detected, float probability, string? bestPreferred)
    {
        if (detected is not null && Preferred.Contains(detected)) return detected;
        if (detected is not null && probability >= Confident) return detected;
        return bestPreferred is not null && Preferred.Contains(bestPreferred) ? bestPreferred : detected ?? "en";
    }

    public static string Normalize(string? option) => option is "en" or "uk" or "nl" ? option : "auto";
}

/// <summary>Joins Whisper segments and drops non-speech markers (tested).</summary>
public static partial class TranscriptText
{
    [GeneratedRegex(@"\[[^\]]*\]|\*[^*]*\*")]
    private static partial Regex Markers();

    [GeneratedRegex(@"^\s*\([^)]*\)\s*$")]
    private static partial Regex WhollyParenthesized();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    public static string Clean(IEnumerable<string> segments)
    {
        var sb = new StringBuilder();
        foreach (var raw in segments)
        {
            var s = Markers().Replace(raw, " ");
            if (WhollyParenthesized().IsMatch(s)) continue; // "(music)", "(тиша)"
            sb.Append(' ').Append(s);
        }
        return Spaces().Replace(sb.ToString(), " ").Trim();
    }
}
