using System.Diagnostics;
using System.Text;
using Skerry.Core;
using NAudio.Wave;

namespace Skerry.Modules.Voice;

/// <summary>
/// Debug CLI: <c>--transcribe &lt;in.wav&gt; &lt;out.txt&gt; [--lang auto|en|uk|nl] [--model base|small] [--unload]</c>. Downloads the
/// model if needed, transcribes the file with the same engine as hold-to-talk and writes language, text, timings and
/// peak memory to out.txt. Used to verify the pipeline without a microphone.
/// </summary>
static class VoiceSelfTest
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length < 3) return 2;
        var report = new StringBuilder();
        try
        {
            var lang = LanguagePick.Normalize(Arg(args, "--lang"));
            var model = WhisperModels.Selected(Arg(args, "--model"));
            var sw = Stopwatch.StartNew();
            if (!model.IsDownloaded)
            {
                await ModelDownloader.DownloadAsync(model, null, CancellationToken.None);
                report.AppendLine($"download {model.Name}: {sw.ElapsedMilliseconds} ms");
            }
            float[] samples;
            using (var file = new WaveFileReader(args[1]))
            {
                var reader = file.ToSampleProvider();
                var all = new List<float>();
                var buf = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
                int n;
                while ((n = reader.Read(buf.AsSpan())) > 0)
                    for (var i = 0; i < n; i += reader.WaveFormat.Channels)
                    {
                        float acc = 0;
                        for (var c = 0; c < reader.WaveFormat.Channels; c++) acc += buf[i + c];
                        all.Add(acc / reader.WaveFormat.Channels);
                    }
                samples = AudioMath.Resample([.. all], reader.WaveFormat.SampleRate, AudioMath.WhisperRate);
            }
            report.AppendLine($"audio: {samples.Length / (double)AudioMath.WhisperRate:0.0} s, silent={AudioMath.IsSilent(samples)}");
            using var engine = new WhisperEngine();
            for (var run = 1; run <= 2; run++) // second run shows the warm (model already loaded) time
            {
                sw.Restart();
                var t = await engine.TranscribeAsync(samples, model.LocalPath, lang, CancellationToken.None);
                Process.GetCurrentProcess().Refresh();
                report.AppendLine($"run {run}: {sw.ElapsedMilliseconds} ms, lang={t.Language}, peakWS={Process.GetCurrentProcess().PeakWorkingSet64 / 1_048_576} MB");
                report.AppendLine("text: " + t.Text);
            }
            if (args.Contains("--unload"))
            {
                await Task.Delay(WhisperEngine.IdleUnload + TimeSpan.FromSeconds(8));
                Process.GetCurrentProcess().Refresh();
                report.AppendLine($"after idle unload: loaded={engine.IsLoaded}, WS={Process.GetCurrentProcess().WorkingSet64 / 1_048_576} MB");
            }
        }
        catch (Exception ex) { report.AppendLine("error: " + ex); }
        File.WriteAllText(args[2], report.ToString(), Encoding.UTF8);
        return 0;
    }

    static string? Arg(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
