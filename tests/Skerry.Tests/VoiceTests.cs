using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using Skerry.Modules.Voice;
using Xunit;

namespace Skerry.Tests;

public class VoiceTests
{
    [Fact]
    public void Whisper_disposal_waits_for_worker_gate_and_rejects_queued_work()
    {
        FakeSink.Run(sink =>
        {
            var engine = new WhisperEngine();
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var gate = (SemaphoreSlim)typeof(WhisperEngine).GetField("_gate", flags)!.GetValue(engine)!;
            var path = typeof(WhisperEngine).GetField("_loadedPath", flags)!;
            path.SetValue(engine, "synthetic-loaded-model");
            gate.Wait(); // stand in for a worker still inside the native model
            var queued = engine.TranscribeAsync([], "must-not-load", "auto", CancellationToken.None);
            engine.Dispose();
            var disposal = engine.DisposeAsync().AsTask();
            Assert.False(disposal.IsCompleted);
            Assert.Equal("synthetic-loaded-model", path.GetValue(engine));
            gate.Release();
            Assert.True(sink.PumpUntil(() => disposal.IsCompleted && queued.IsCompleted));
            disposal.GetAwaiter().GetResult();
            Assert.Null(path.GetValue(engine));
            Assert.Throws<ObjectDisposedException>(() => queued.GetAwaiter().GetResult());
            Assert.Throws<ObjectDisposedException>(() => engine.TranscribeAsync([], "must-not-load", "auto", CancellationToken.None).GetAwaiter().GetResult());
        });
    }

    [Theory]
    [InlineData("en", 0.9f, null, "en")]
    [InlineData("uk", 0.3f, null, "uk")]
    [InlineData("nl", 0.5f, null, "nl")]
    [InlineData("ru", 0.45f, "uk", "uk")]   // Ukrainian heard as Russian with low confidence
    [InlineData("de", 0.6f, "nl", "nl")]
    [InlineData("de", 0.92f, "nl", "de")]   // confident other language is kept
    [InlineData("fr", 0.75f, "en", "fr")]
    [InlineData(null, 0f, "en", "en")]
    [InlineData(null, 0f, null, "en")]
    [InlineData("ja", 0.2f, null, "ja")]
    public void LanguagePick_PrefersEnUkNl(string? detected, float p, string? best, string expected) =>
        Assert.Equal(expected, LanguagePick.Choose(detected, p, best));

    [Theory]
    [InlineData(null, "auto")]
    [InlineData("", "auto")]
    [InlineData("uk", "uk")]
    [InlineData("nl", "nl")]
    [InlineData("klingon", "auto")]
    public void LanguageOption_Normalized(string? opt, string expected) => Assert.Equal(expected, LanguagePick.Normalize(opt));

    [Fact]
    public void Transcript_JoinsSegmentsAndDropsMarkers()
    {
        Assert.Equal("Hello there. How are you?", TranscriptText.Clean([" Hello there.", "  How are you? "]));
        Assert.Equal("", TranscriptText.Clean(["[BLANK_AUDIO]"]));
        Assert.Equal("", TranscriptText.Clean([" (music) ", "[ Silence ]"]));
        Assert.Equal("Привіт, світе", TranscriptText.Clean([" Привіт, *сміється* світе"]));
        Assert.Equal("Call me (maybe) later", TranscriptText.Clean(["Call me (maybe) later"]));
        Assert.Equal("", TranscriptText.Clean([]));
    }

    [Fact]
    public void Level_MapsDecibels()
    {
        Assert.Equal(0f, AudioMath.LevelFromRms(0));
        Assert.Equal(0f, AudioMath.LevelFromRms(0.0001));      // −80 dB
        Assert.Equal(1f, AudioMath.LevelFromRms(1));           // 0 dB
        var mid = AudioMath.LevelFromRms(0.0316);              // −30 dB
        Assert.InRange(mid, 0.45f, 0.55f);
    }

    [Fact]
    public void Silence_DetectedByPeak()
    {
        Assert.True(AudioMath.IsSilent(new float[1000]));
        var quiet = Enumerable.Range(0, 1000).Select(i => (float)(0.005 * Math.Sin(i))).ToArray();
        Assert.True(AudioMath.IsSilent(quiet));
        quiet[500] = -0.2f;
        Assert.False(AudioMath.IsSilent(quiet));
        Assert.Equal(0.2f, AudioMath.Peak(quiet), 4);
        Assert.Equal(0f, AudioMath.Peak(new float[10]));
    }

    [Fact]
    public void Resample_48kTo16k_KeepsLengthAndTone()
    {
        const int from = 48_000, to = 16_000;
        var tone = Enumerable.Range(0, from).Select(i => (float)(0.5 * Math.Sin(2 * Math.PI * 440 * i / from))).ToArray(); // 1 s of 440 Hz
        var out16 = AudioMath.Resample(tone, from, to);
        Assert.InRange(out16.Length, to - 200, to + 200);
        // the tone survives: count zero crossings in the middle (≈ 2 per period → ~880 per second)
        var mid = out16.AsSpan(2000, 8000);
        var crossings = 0;
        for (var i = 1; i < mid.Length; i++) if (mid[i - 1] < 0 != mid[i] < 0) crossings++;
        Assert.InRange(crossings, 420, 460); // 0.5 s → ~440
        Assert.InRange(out16.Skip(2000).Take(8000).Max(), 0.4f, 0.6f);
    }

    [Fact]
    public void Resample_SameRateOrEmpty_ReturnsInput()
    {
        float[] a = [0.1f, 0.2f];
        Assert.Same(a, AudioMath.Resample(a, 16_000, 16_000));
        Assert.Empty(AudioMath.Resample([], 48_000, 16_000));
    }

    [Fact]
    public void Models_SelectedByOption()
    {
        Assert.Equal("base", WhisperModels.Selected(null).Name);
        Assert.Equal("small", WhisperModels.Selected("Small").Name);
        Assert.Equal("base", WhisperModels.Selected("huge").Name);
        Assert.StartsWith("https://huggingface.co/ggerganov/whisper.cpp/resolve/main/", WhisperModels.Base.Url);
        Assert.All(WhisperModels.All, m => Assert.Equal(64, m.Sha256.Length));
    }

    static WhisperModel FakeModel(byte[] data, string? sha = null) =>
        new("test", $"ggml-test-{Guid.NewGuid():N}.bin", data.Length, sha ?? Convert.ToHexStringLower(SHA256.HashData(data)));

    [Fact]
    public async Task Download_VerifiesAndMovesIntoPlace()
    {
        var data = RandomNumberGenerator.GetBytes(300_000);
        var model = FakeModel(data);
        var reports = new List<double>();
        var progress = new SyncProgress(reports.Add);
        var handler = new BytesHandler(data);
        await ModelDownloader.DownloadAsync(model, progress, CancellationToken.None, handler);
        Assert.True(model.IsDownloaded);
        Assert.Equal(data, File.ReadAllBytes(model.LocalPath));
        Assert.False(File.Exists(model.LocalPath + ".part"));
        Assert.Equal(model.Url, handler.Url);
        Assert.Contains("Skerry", handler.UserAgent);
        Assert.Equal(1.0, reports[^1], 3);
        File.Delete(model.LocalPath);
    }

    [Fact]
    public async Task Download_BadChecksum_DeletesPartAndThrows()
    {
        var data = RandomNumberGenerator.GetBytes(10_000);
        var model = FakeModel(data, sha: new string('0', 64));
        await Assert.ThrowsAsync<ModelDownloader.ChecksumException>(() => ModelDownloader.DownloadAsync(model, null, CancellationToken.None, new BytesHandler(data)));
        Assert.False(File.Exists(model.LocalPath));
        Assert.False(File.Exists(model.LocalPath + ".part"));
    }

    [Fact]
    public async Task Download_Truncated_Throws()
    {
        var data = RandomNumberGenerator.GetBytes(10_000);
        var model = FakeModel(data);
        await Assert.ThrowsAsync<ModelDownloader.ChecksumException>(() => ModelDownloader.DownloadAsync(model, null, CancellationToken.None, new BytesHandler(data[..5000])));
        Assert.False(model.IsDownloaded);
    }

    [Fact]
    public async Task Download_HttpError_Throws()
    {
        var model = FakeModel([1, 2, 3]);
        await Assert.ThrowsAsync<HttpRequestException>(() => ModelDownloader.DownloadAsync(model, null, CancellationToken.None, new BytesHandler([], HttpStatusCode.NotFound)));
        Assert.False(File.Exists(model.LocalPath + ".part"));
    }

    sealed class SyncProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }

    sealed class BytesHandler(byte[] data, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? Url, UserAgent;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Url = request.RequestUri?.ToString();
            UserAgent = request.Headers.UserAgent.ToString();
            return Task.FromResult(new HttpResponseMessage(status) { Content = new ByteArrayContent(data) });
        }
    }
}
