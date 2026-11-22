using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Isle.Modules.Voice;

#pragma warning disable CS0618 // NAudio 3 obsoletes WasapiCapture in favour of a builder; it still works (same as the recorder)

/// <summary>
/// Records the default microphone (shared-mode WASAPI, device format) into a mono buffer; <see cref="Stop"/> returns
/// 16 kHz mono float samples for Whisper. <see cref="Level"/> is raised from the capture thread (0..1).
/// </summary>
public sealed class MicCapture : IDisposable
{
    public const int MaxSeconds = 300;
    readonly WasapiCapture _capture;
    readonly List<float> _mono = new(48_000 * 10);
    readonly int _rate, _channels;
    readonly bool _float;
    readonly int _bytesPerSample;
    readonly object _lock = new();
    bool _stopped;

    public event Action<float>? Level;

    MicCapture(MMDevice device)
    {
        _capture = new WasapiCapture(device) { ShareMode = AudioClientShareMode.Shared };
        var f = _capture.WaveFormat;
        _rate = f.SampleRate;
        _channels = Math.Max(1, f.Channels);
        // the shared-mode mix format is WAVE_FORMAT_EXTENSIBLE; at 32 bits it is float in practice
        _float = f.Encoding == WaveFormatEncoding.IeeeFloat || (f.Encoding == WaveFormatEncoding.Extensible && f.BitsPerSample == 32);
        _bytesPerSample = f.BitsPerSample / 8;
        if (!_float && _bytesPerSample is not (2 or 3 or 4)) throw new NotSupportedException($"mic format {f}");
        _capture.DataAvailable += OnData;
    }

    /// <summary>Starts recording, or returns null when there is no microphone.</summary>
    public static MicCapture? TryStart()
    {
        MMDevice? device;
        try
        {
            using var en = new MMDeviceEnumerator();
            if (!en.HasDefaultAudioEndpoint(DataFlow.Capture, Role.Console)) return null;
            device = en.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
        }
        catch (System.Runtime.InteropServices.COMException) { return null; }
        var mic = new MicCapture(device);
        mic._capture.StartRecording();
        return mic;
    }

    public TimeSpan Duration { get { lock (_lock) return TimeSpan.FromSeconds(_mono.Count / (double)_rate); } }

    void OnData(object? sender, WaveInEventArgs e)
    {
        var frameBytes = _bytesPerSample * _channels;
        var frames = e.BytesRecorded / frameBytes;
        if (frames == 0) return;
        double sum = 0;
        lock (_lock)
        {
            if (_stopped || _mono.Count >= _rate * MaxSeconds) return;
            for (var i = 0; i < frames; i++)
            {
                float acc = 0;
                for (var c = 0; c < _channels; c++) acc += Sample(e.Buffer, i * frameBytes + c * _bytesPerSample);
                var v = acc / _channels;
                _mono.Add(v);
                sum += v * v;
            }
        }
        Level?.Invoke(AudioMath.LevelFromRms(Math.Sqrt(sum / frames)));
    }

    float Sample(byte[] b, int o) => _float
        ? BitConverter.ToSingle(b, o)
        : _bytesPerSample switch
        {
            2 => BitConverter.ToInt16(b, o) / 32768f,
            3 => ((b[o] << 8 | b[o + 1] << 16 | b[o + 2] << 24) >> 8) / 8388608f,
            _ => BitConverter.ToInt32(b, o) / 2147483648f,
        };

    /// <summary>Stops the device and returns everything recorded as 16 kHz mono.</summary>
    public float[] Stop()
    {
        try { _capture.StopRecording(); } catch { }
        float[] raw;
        lock (_lock)
        {
            _stopped = true;
            raw = [.. _mono];
            _mono.Clear();
            _mono.TrimExcess();
        }
        return AudioMath.Resample(raw, _rate, AudioMath.WhisperRate);
    }

    public void Dispose()
    {
        _capture.DataAvailable -= OnData;
        try { _capture.StopRecording(); } catch { }
        _capture.Dispose();
    }
}

/// <summary>Pure audio helpers (tested).</summary>
public static class AudioMath
{
    public const int WhisperRate = 16_000;

    /// <summary>RMS → 0..1 on a −55..−5 dBFS scale (speech sits roughly in the middle).</summary>
    public static float LevelFromRms(double rms)
    {
        if (rms <= 1e-6) return 0;
        var db = 20 * Math.Log10(rms);
        return (float)Math.Clamp((db + 55) / 50, 0, 1);
    }

    /// <summary>True when nothing louder than faint background noise was recorded (peak below −40 dBFS).</summary>
    public static bool IsSilent(ReadOnlySpan<float> samples, float peakThreshold = 0.01f) => Peak(samples) <= peakThreshold;

    /// <summary>Largest absolute sample (0 = digital silence: a muted or blocked microphone).</summary>
    public static float Peak(ReadOnlySpan<float> samples)
    {
        float peak = 0;
        foreach (var s in samples) peak = Math.Max(peak, Math.Abs(s));
        return peak;
    }

    /// <summary>Band-limited resampling (NAudio's WDL resampler) of a mono buffer.</summary>
    public static float[] Resample(float[] input, int fromRate, int toRate)
    {
        if (fromRate == toRate || input.Length == 0) return input;
        var source = new ArraySampleProvider(input, fromRate);
        var resampler = new WdlResamplingSampleProvider(source, toRate);
        var expected = (int)((long)input.Length * toRate / fromRate);
        var output = new List<float>(expected + 1024);
        var buf = new float[8192];
        int n;
        while ((n = resampler.Read(buf.AsSpan())) > 0)
            output.AddRange(buf.AsSpan(0, n));
        return [.. output];
    }

    sealed class ArraySampleProvider(float[] data, int rate) : ISampleProvider
    {
        int _pos;
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(rate, 1);

        public int Read(Span<float> buffer)
        {
            var n = Math.Min(buffer.Length, data.Length - _pos);
            if (n <= 0) return 0;
            data.AsSpan(_pos, n).CopyTo(buffer);
            _pos += n;
            return n;
        }
    }
}
