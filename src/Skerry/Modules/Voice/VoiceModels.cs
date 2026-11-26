using System.Net.Http;
using System.Security.Cryptography;
using Skerry.Core;

namespace Skerry.Modules.Voice;

/// <summary>A multilingual whisper.cpp ggml model from the official Hugging Face repository.</summary>
public sealed record WhisperModel(string Name, string FileName, long Size, string Sha256)
{
    public string Url => $"https://huggingface.co/ggerganov/whisper.cpp/resolve/main/{FileName}";
    public string LocalPath => Path.Combine(Paths.Dir("models"), FileName);
    public bool IsDownloaded => File.Exists(LocalPath) && new FileInfo(LocalPath).Length == Size;
    public int SizeMb => (int)Math.Round(Size / 1_000_000.0);
}

public static class WhisperModels
{
    // Sizes and SHA-256 as published by the repository (LFS object ids, huggingface.co/api/models/ggerganov/whisper.cpp/tree/main).
    public static readonly WhisperModel Base = new("base", "ggml-base.bin", 147_951_465, "60ed5bc3dd14eea856493d334349b405782ddcaf0028d4b5df4088345fba2efe");
    public static readonly WhisperModel Small = new("small", "ggml-small.bin", 487_601_967, "1be3a9b2063867b937e64e2ec7483364a79917e157fa98c5d94b5c1fffea987b");

    public static IReadOnlyList<WhisperModel> All { get; } = [Base, Small];

    /// <summary>The model chosen with option <c>voice.model</c> (default base).</summary>
    public static WhisperModel Selected(string? name) =>
        All.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase)) ?? Base;
}

/// <summary>Downloads a model to <c>*.part</c>, checks size and SHA-256, then moves it into place.</summary>
public static class ModelDownloader
{
    public sealed class ChecksumException() : Exception("model checksum mismatch");

    /// <param name="progress">0..1, reported at most every ~1 % from the download thread.</param>
    public static async Task DownloadAsync(WhisperModel model, IProgress<double>? progress, CancellationToken ct, HttpMessageHandler? handler = null)
    {
        var target = model.LocalPath;
        var part = target + ".part";
        using var http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        http.Timeout = Timeout.InfiniteTimeSpan; // large file; cancellation comes from ct
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Skerry/1.0 (Windows desktop overlay; personal use)");
        try
        {
            using (var resp = await http.GetAsync(model.Url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
            {
                resp.EnsureSuccessStatusCode();
                var total = resp.Content.Headers.ContentLength ?? model.Size;
                await using var src = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                await using var dst = new FileStream(part, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, useAsync: true);
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                var buffer = new byte[1 << 16];
                long done = 0;
                var lastReported = -1;
                int n;
                while ((n = await src.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                {
                    hash.AppendData(buffer, 0, n);
                    await dst.WriteAsync(buffer.AsMemory(0, n), ct).ConfigureAwait(false);
                    done += n;
                    var pct = total > 0 ? (int)(done * 100 / total) : 0;
                    if (pct != lastReported) { lastReported = pct; progress?.Report(Math.Min(1, done / (double)Math.Max(1, total))); }
                }
                var sha = Convert.ToHexStringLower(hash.GetHashAndReset());
                if (done != model.Size || !string.Equals(sha, model.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new ChecksumException();
            }
            File.Move(part, target, overwrite: true);
        }
        catch
        {
            try { File.Delete(part); } catch { }
            throw;
        }
    }
}
