using System.Windows.Threading;
using Isle.Core;
using Isle.Views.Live;
using Isle.Views.Voice;

namespace Isle.Modules.Voice;

/// <summary>
/// Voice to text (module id "voice"): hold Win+Alt+V to record the default microphone, release to transcribe offline
/// with Whisper and paste into the window that was active. The model is downloaded on first use; the engine is
/// created on the first transcription and unloads itself 2 minutes later. Options: <c>voice.model</c> (base|small),
/// <c>voice.lang</c> (auto|en|uk|nl).
/// </summary>
public sealed class VoiceModule(IslandWindow island) : IModule, IDisposable
{
    const string PillId = "voice", DownloadPillId = "voice.dl", MessagePillId = "voice.msg";
    static readonly TimeSpan MinHold = TimeSpan.FromMilliseconds(400);

    enum Phase { Idle, Recording, Transcribing }

    Phase _phase;
    MicCapture? _mic;
    VoicePill? _pill;
    DispatcherTimer? _tick, _msgTimer;
    int _peakLevel; // float bits, max since the last tick (written by the capture thread)
    IntPtr _target;
    DateTime _startedUtc;
    ITranscriber? _engine;
    CancellationTokenSource? _cts;
    Task? _download;

    public string Id => "voice";

    public void Start() =>
        island.Hotkeys.Register("voice", "hotkey.voice", "Win+Alt+V", Pressed, Released);

    static WhisperModel Model => WhisperModels.Selected(Settings.Current.Opt("voice.model"));

    void Pressed()
    {
        if (_phase != Phase.Idle) return;
        if (_download is not null) { Message("Icon.Download", Strings.T("voice.downloading")); return; }
        var model = Model;
        if (!model.IsDownloaded) { StartDownload(model); return; }

        var fg = Native.GetForegroundWindow();
        _target = fg == island.Handle ? island.PasteTarget : fg;
        try { _mic = MicCapture.TryStart(); }
        catch (Exception ex) { Logger.Error("voice mic", ex); _mic = null; }
        if (_mic is null) { Message("Icon.Mic", Strings.T("voice.noMic")); return; }
        _mic.Level += OnLevel;
        _phase = Phase.Recording;
        _startedUtc = DateTime.UtcNow;
        island.Pill.Remove(MessagePillId);
        _pill = new VoicePill();
        _pill.SetElapsed("0:00");
        island.Pill.Set(new PillItem
        {
            Id = PillId, Priority = PillPriority.Recording + 1, Width = 230, Peek = true, HoverExpands = false,
            View = () => _pill!,
        });
        _tick ??= new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Render, OnTick, island.Dispatcher);
        _tick.Start();
        Logger.Info("voice recording");
    }

    void OnLevel(float level)
    {
        // keep the loudest chunk since the last UI tick; float bits compare like ints for non-negative values
        var bits = BitConverter.SingleToInt32Bits(level);
        int seen;
        while (bits > (seen = Volatile.Read(ref _peakLevel)) && Interlocked.CompareExchange(ref _peakLevel, bits, seen) != seen) { }
    }

    void OnTick(object? s, EventArgs e)
    {
        if (_phase != Phase.Recording || _pill is null) { _tick?.Stop(); return; }
        _pill.PushLevel(BitConverter.Int32BitsToSingle(Interlocked.Exchange(ref _peakLevel, 0)));
        _pill.SetElapsed(Timers.DurationParser.Format(DateTime.UtcNow - _startedUtc));
        if (DateTime.UtcNow - _startedUtc >= TimeSpan.FromSeconds(MicCapture.MaxSeconds)) Released();
    }

    async void Released()
    {
        if (_phase != Phase.Recording || _mic is null) return;
        _tick?.Stop();
        var mic = _mic;
        _mic = null;
        mic.Level -= OnLevel;
        var held = DateTime.UtcNow - _startedUtc;
        float[] samples;
        try { samples = mic.Stop(); }
        finally { mic.Dispose(); }

        if (held < MinHold) { Finish(); Message("Icon.Mic", Strings.T("voice.holdHint")); return; }
        if (AudioMath.IsSilent(samples))
        {
            var peak = AudioMath.Peak(samples);
            Logger.Info($"voice: silent recording ({held.TotalSeconds:0.0} s, peak {(peak > 0 ? 20 * Math.Log10(peak) : double.NegativeInfinity):0} dBFS)");
            Finish();
            Message("Icon.Mic", Strings.T(peak == 0 ? "voice.muted" : "voice.silence"));
            return;
        }

        _phase = Phase.Transcribing;
        _pill?.SetBusy(Strings.T("voice.transcribing"));
        _cts = new CancellationTokenSource();
        try
        {
            _engine ??= new WhisperEngine();
            var started = Environment.TickCount64;
            var result = await _engine.TranscribeAsync(samples, Model.LocalPath, LanguagePick.Normalize(Settings.Current.Opt("voice.lang")), _cts.Token);
            Logger.Info($"voice: {samples.Length / AudioMath.WhisperRate} s audio → {result.Text.Length} chars ({result.Language}) in {Environment.TickCount64 - started} ms");
            Finish();
            if (result.Text.Length == 0) { Message("Icon.Mic", Strings.T("voice.nothing")); return; }
            var pasted = await ClipboardOps.PasteTextAsync(result.Text, _target);
            if (ClipboardOps.PasteNotice(pasted) is { } notice) Message("Icon.Mic", notice);
        }
        catch (OperationCanceledException) { Finish(); }
        catch (Exception ex)
        {
            Logger.Error("voice transcribe", ex);
            Finish();
            Message("Icon.Mic", Strings.T("voice.failed"));
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            Memory.TrimSoon(3);
        }
    }

    void Finish()
    {
        _phase = Phase.Idle;
        island.Pill.Remove(PillId);
        _pill = null;
    }

    void StartDownload(WhisperModel model)
    {
        var pill = new IndicatorPill("Icon.Download", Strings.T("voice.dlTitle"), "0 %");
        pill.SetProgress(0);
        island.Pill.Set(new PillItem { Id = DownloadPillId, Priority = PillPriority.Download, Width = 260, Peek = true, View = () => pill });
        Message("Icon.Download", Strings.T("voice.dlStarted", model.SizeMb), seconds: 6);
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        var progress = new Progress<double>(p => { pill.SetProgress(p); pill.Set("Icon.Download", Strings.T("voice.dlTitle"), $"{p * 100:0} %"); });
        Logger.Info($"voice model download {model.Name}");
        _download = Run();

        async Task Run()
        {
            try
            {
                await ModelDownloader.DownloadAsync(model, progress, ct);
                Logger.Info("voice model ready");
                Message("Icon.Check", Strings.T("voice.dlDone", island.Hotkeys.List().FirstOrDefault(h => h.Id == "voice")?.Gesture ?? "Win+Alt+V"), seconds: 6);
            }
            catch (OperationCanceledException) { }
            catch (ModelDownloader.ChecksumException) { Logger.Warn("voice model checksum mismatch"); Message("Icon.Download", Strings.T("voice.dlBad")); }
            catch (Exception ex) { Logger.Error("voice model download", ex); Message("Icon.Download", Strings.T("voice.dlFailed")); }
            finally
            {
                island.Pill.Remove(DownloadPillId);
                _download = null;
                _cts?.Dispose();
                _cts = null;
                Memory.TrimSoon(2);
            }
        }
    }

    /// <summary>Brief pill message (no hover expansion).</summary>
    void Message(string icon, string text, double seconds = 4)
    {
        island.Pill.Set(new PillItem
        {
            Id = MessagePillId, Priority = PillPriority.Transient, Width = IndicatorPill.MessageWidth(text), Peek = true, HoverExpands = false,
            View = () => new IndicatorPill(icon, text), Click = () => island.Pill.Remove(MessagePillId),
        });
        _msgTimer?.Stop();
        _msgTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
        _msgTimer.Tick += (_, _) => { _msgTimer?.Stop(); island.Pill.Remove(MessagePillId); };
        _msgTimer.Start();
    }

    public void Stop()
    {
        island.Hotkeys.Unregister("voice");
        _cts?.Cancel();
        _tick?.Stop();
        _msgTimer?.Stop();
        if (_mic is { } mic) { _mic = null; mic.Level -= OnLevel; mic.Dispose(); }
        if (_phase != Phase.Idle) Finish();
        island.Pill.Remove(DownloadPillId);
        island.Pill.Remove(MessagePillId);
        _engine?.Dispose();
        _engine = null;
    }

    public void Dispose() => Stop();
}
