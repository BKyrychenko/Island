using System.Diagnostics;
using System.Runtime.InteropServices;
using Isle.Core;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.Storage.Streams;
using WinRT;

#pragma warning disable CS0618 // NAudio 3 obsoletes WasapiCapture/WasapiLoopbackCapture in favour of a builder; they still work

namespace Isle.Modules.Tools;

/// <summary>Recording options. Region in physical pixels relative to the monitor; empty = whole monitor.</summary>
public sealed record RecordOptions(ScreenInfo Monitor, PxRect Region, bool SystemAudio, bool Microphone, int Fps = 30, int MaxHeight = 1080);

/// <summary>
/// Screen recorder: Windows.Graphics.Capture frames (GPU) → region copied into fresh D3D11 textures → MediaStreamSource →
/// MediaTranscoder (hardware H.264 + AAC, MP4). Optional system audio (WASAPI loopback) and microphone are resampled to
/// 48 kHz stereo and mixed with NAudio. Frames are throttled to <see cref="RecordOptions.Fps"/>; output is scaled down to
/// <see cref="RecordOptions.MaxHeight"/>. Everything is created on Start and disposed on Stop.
/// </summary>
public sealed class Recorder : IDisposable
{
    const int SampleRate = 48000, Channels = 2;

    readonly RecordOptions _o;
    readonly string _path;
    ID3D11Device? _d3d;
    ID3D11DeviceContext? _ctx;
    IDirect3DDevice? _device;
    GraphicsCaptureItem? _item;
    Direct3D11CaptureFramePool? _pool;
    GraphicsCaptureSession? _session;
    MediaStreamSource? _mss;
    IRandomAccessStream? _stream;
    Task? _transcode;
    readonly Stopwatch _clock = new();
    readonly AutoResetEvent _frameArrived = new(false);
    readonly ManualResetEvent _stopSignal = new(false);
    readonly object _frameGate = new();
    Direct3D11CaptureFrame? _latest;
    TimeSpan _lastVideo = TimeSpan.MinValue;
    volatile bool _stopping;
    int _w, _h, _cropX, _cropY;

    // audio
    WasapiLoopbackCapture? _loop;
    WasapiCapture? _mic;
    MixingSampleProvider? _mixer;
    long _audioFrames;

    public string FilePath => _path;
    public TimeSpan Elapsed => _clock.Elapsed;

    public Recorder(RecordOptions options, string folder)
    {
        _o = options;
        _path = System.IO.Path.Combine(folder, $"Isle {DateTime.Now:yyyy-MM-dd HH.mm.ss}.mp4");
    }

    public async Task StartAsync()
    {
        CreateDevice();
        _item = CaptureItemForMonitor(_o.Monitor.Handle);
        var size = _item.Size;
        var region = _o.Region.IsEmpty ? new PxRect(0, 0, size.Width, size.Height) : _o.Region.Intersect(new PxRect(0, 0, size.Width, size.Height));
        // encoders want even sizes
        _w = region.Width & ~1;
        _h = region.Height & ~1;
        _cropX = region.X;
        _cropY = region.Y;
        if (_w < 64 || _h < 64) throw new InvalidOperationException("region too small");

        _pool = Direct3D11CaptureFramePool.CreateFreeThreaded(_device!, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, size);
        _pool.FrameArrived += OnFrame;
        _session = _pool.CreateCaptureSession(_item);
        try { _session.IsCursorCaptureEnabled = true; } catch { }

        var input = VideoEncodingProperties.CreateUncompressed(MediaEncodingSubtypes.Bgra8, (uint)_w, (uint)_h);
        input.FrameRate.Numerator = (uint)_o.Fps;
        input.FrameRate.Denominator = 1;
        var withAudio = _o.SystemAudio || _o.Microphone;
        if (withAudio) StartAudio();
        var scale = Math.Min(1.0, (double)_o.MaxHeight / _h);
        var profile = new MediaEncodingProfile();
        profile.Container.Subtype = "MPEG4";
        profile.Video.Subtype = "H264";
        profile.Video.Width = (uint)((int)(_w * scale) & ~1);
        profile.Video.Height = (uint)((int)(_h * scale) & ~1);
        profile.Video.Bitrate = (uint)Math.Clamp(profile.Video.Width * profile.Video.Height * _o.Fps / 8, 2_000_000, 12_000_000);
        profile.Video.FrameRate.Numerator = (uint)_o.Fps;
        profile.Video.FrameRate.Denominator = 1;
        profile.Video.PixelAspectRatio.Numerator = 1;
        profile.Video.PixelAspectRatio.Denominator = 1;
        if (withAudio) profile.Audio = AudioEncodingProperties.CreateAac(SampleRate, Channels, 192_000);
        else profile.Audio = null;

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
        // hardware-accelerated processing first; some drivers reject GPU surfaces from another D3D device ("Unknown"),
        // then the transcoder's software path is used (found on this PC; the H.264 encoder MFT is still chosen by MF)
        PrepareTranscodeResult? prepared = null;
        foreach (var hw in new[] { true, false })
        {
            var video = new VideoStreamDescriptor(input);
            _mss = withAudio ? new MediaStreamSource(video, new AudioStreamDescriptor(AudioEncodingProperties.CreatePcm(SampleRate, Channels, 16))) : new MediaStreamSource(video);
            _mss.BufferTime = TimeSpan.Zero;
            _mss.CanSeek = false;
            _mss.SampleRequested += OnSampleRequested;
            _stream = await FileRandomAccessStream.OpenAsync(_path, FileAccessMode.ReadWrite, StorageOpenOptions.None, FileOpenDisposition.CreateAlways);
            var transcoder = new MediaTranscoder { HardwareAccelerationEnabled = hw };
            prepared = await transcoder.PrepareMediaStreamSourceTranscodeAsync(_mss, _stream, profile);
            if (prepared.CanTranscode) { Logger.Info($"recording transcoder: {(hw ? "hardware" : "software")} processing"); break; }
            Logger.Warn($"transcode setup ({(hw ? "hw" : "sw")}) failed: {prepared.FailureReason}");
            _mss.SampleRequested -= OnSampleRequested;
            _stream.Dispose();
            _stream = null;
        }
        if (prepared is not { CanTranscode: true }) throw new InvalidOperationException($"cannot transcode: {prepared?.FailureReason}");

        _session.StartCapture();
        _clock.Start();
        _transcode = prepared.TranscodeAsync().AsTask();
    }

    /// <summary>Ends the stream and waits for the file to be finalised.</summary>
    public async Task StopAsync()
    {
        Logger.Info($"recording pool: {_all.Count} textures of {_w}x{_h}");
        _stopping = true;
        _frameArrived.Set();
        _stopSignal.Set();
        if (_transcode is not null)
        {
            try { await _transcode.WaitAsync(TimeSpan.FromSeconds(20)); }
            catch (Exception ex) { Logger.Error("recording finalise", ex); }
        }
        Dispose();
    }

    // ---- video ----

    void OnFrame(Direct3D11CaptureFramePool pool, object args)
    {
        var frame = pool.TryGetNextFrame();
        if (frame is null) return;
        lock (_frameGate)
        {
            _latest?.Dispose();
            _latest = frame;
        }
        _frameArrived.Set();
    }

    void OnSampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
    {
        try
        {
            if (args.Request.StreamDescriptor is AudioStreamDescriptor) { args.Request.Sample = _stopping ? null : NextAudio(); return; }
            args.Request.Sample = _stopping ? null : NextVideo();
        }
        catch (Exception ex)
        {
            Logger.Error("recording sample", ex);
            args.Request.Sample = null;
        }
    }

    MediaStreamSample? NextVideo()
    {
        var minGap = TimeSpan.FromSeconds(1.0 / _o.Fps);
        while (!_stopping)
        {
            // throttle to the frame rate, then take the newest frame (or repeat the last one when the screen is still)
            var wait = _lastVideo == TimeSpan.MinValue ? TimeSpan.Zero : _lastVideo + minGap - _clock.Elapsed;
            if (wait > TimeSpan.Zero) _frameArrived.WaitOne(wait);
            if (_stopping) break;
            Direct3D11CaptureFrame? frame;
            lock (_frameGate) { frame = _latest; _latest = null; }
            if (frame is null && !_haveLast) { _frameArrived.WaitOne(100); continue; }
            var now = _clock.Elapsed;
            if (frame is not null)
            {
                using (frame) CopyIntoLast(frame.Surface);
                _haveLast = true;
            }
            _lastVideo = now;
            return SampleFromLast(now);
        }
        return null;
    }

    // The newest region lives in one texture (_last); each sample gets a pooled copy that comes back when the
    // transcoder reports it processed, so memory stays flat instead of one new 8 MB texture per frame.
    ID3D11Texture2D? _last;
    bool _haveLast;
    readonly Stack<(ID3D11Texture2D Tex, IDirect3DSurface Surface)> _free = new();
    readonly List<(ID3D11Texture2D Tex, IDirect3DSurface Surface)> _all = [];

    Texture2DDescription RegionDesc => new()
    {
        Width = (uint)_w, Height = (uint)_h, MipLevels = 1, ArraySize = 1, Format = Format.B8G8R8A8_UNorm,
        SampleDescription = new SampleDescription(1, 0), Usage = ResourceUsage.Default, BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
    };

    void CopyIntoLast(IDirect3DSurface source)
    {
        using var src = TextureFrom(source);
        _last ??= _d3d!.CreateTexture2D(RegionDesc);
        lock (_ctx!) _ctx.CopySubresourceRegion(_last, 0, 0, 0, 0, src, 0, new Vortice.Mathematics.Box(_cropX, _cropY, 0, _cropX + _w, _cropY + _h, 1));
    }

    MediaStreamSample SampleFromLast(TimeSpan time)
    {
        (ID3D11Texture2D Tex, IDirect3DSurface Surface) slot;
        lock (_free)
        {
            if (_free.Count > 0) slot = _free.Pop();
            else
            {
                var tex = _d3d!.CreateTexture2D(RegionDesc);
                using var dxgi = tex.QueryInterface<IDXGISurface>();
                slot = (tex, SurfaceFromDxgi(dxgi));
                _all.Add(slot);
            }
        }
        lock (_ctx!) _ctx.CopyResource(slot.Tex, _last!);
        var sample = MediaStreamSample.CreateFromDirect3D11Surface(slot.Surface, time);
        sample.Processed += (_, _) => { lock (_free) _free.Push(slot); };
        return sample;
    }

    // ---- audio ----

    void StartAudio()
    {
        var format = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels);
        _mixer = new MixingSampleProvider(format) { ReadFully = true };
        if (_o.SystemAudio)
        {
            try
            {
                _loop = new WasapiLoopbackCapture();
                _mixer.AddMixerInput(Feed(_loop));
                _loop.StartRecording();
            }
            catch (Exception ex) { Logger.Warn($"system audio unavailable: {ex.GetType().Name}"); _loop = null; }
        }
        if (_o.Microphone)
        {
            try
            {
                var dev = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                _mic = new WasapiCapture(dev);
                _mixer.AddMixerInput(Feed(_mic));
                _mic.StartRecording();
            }
            catch (Exception ex) { Logger.Warn($"microphone unavailable: {ex.GetType().Name}"); _mic = null; }
        }
    }

    /// <summary>A capture device as a 48 kHz stereo float input of the mixer (buffered; silence when it has nothing).</summary>
    static ISampleProvider Feed(IWaveIn capture)
    {
        var buffer = new BufferedWaveProvider(capture.WaveFormat, TimeSpan.FromSeconds(3)) { DiscardOnBufferOverflow = true, ReadFully = true };
        capture.DataAvailable += (_, e) => buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
        ISampleProvider s = buffer.ToSampleProvider();
        if (s.WaveFormat.Channels == 1) s = new MonoToStereoSampleProvider(s);
        else if (s.WaveFormat.Channels > 2) s = new MultiplexingSampleProvider([s], 2);
        if (s.WaveFormat.SampleRate != SampleRate) s = new WdlResamplingSampleProvider(s, SampleRate);
        return s;
    }

    MediaStreamSample? NextAudio()
    {
        // 20 ms blocks, timestamped by sample count, paced to the wall clock so audio and video stay together
        const int Block = SampleRate / 50;
        var start = TimeSpan.FromSeconds((double)_audioFrames / SampleRate);
        var ahead = start - _clock.Elapsed;
        if (ahead > TimeSpan.Zero) _stopSignal.WaitOne(ahead); // transcoder worker thread: one timed wait, woken early on stop
        if (_stopping) return null;
        var floats = new float[Block * Channels];
        _mixer!.Read(floats.AsSpan());
        var pcm = new byte[floats.Length * 2];
        for (var i = 0; i < floats.Length; i++)
        {
            var v = (short)Math.Clamp(floats[i] * 32767f, short.MinValue, short.MaxValue);
            pcm[i * 2] = (byte)v;
            pcm[i * 2 + 1] = (byte)(v >> 8);
        }
        _audioFrames += Block;
        var sample = MediaStreamSample.CreateFromBuffer(System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.AsBuffer(pcm), start);
        sample.Duration = TimeSpan.FromSeconds((double)Block / SampleRate);
        return sample;
    }

    // ---- interop ----

    void CreateDevice()
    {
        D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.BgraSupport, [FeatureLevel.Level_11_0, FeatureLevel.Level_10_1], out _d3d, out _ctx).CheckError();
        using var dxgiDevice = _d3d!.QueryInterface<IDXGIDevice>();
        Marshal.ThrowExceptionForHR(CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out var inspectable));
        _device = MarshalInterface<IDirect3DDevice>.FromAbi(inspectable);
        Marshal.Release(inspectable);
    }

    static GraphicsCaptureItem CaptureItemForMonitor(IntPtr monitor)
    {
        var factory = ActivationFactory.Get("Windows.Graphics.Capture.GraphicsCaptureItem");
        var interop = factory.AsInterface<IGraphicsCaptureItemInterop>();
        var iid = typeof(GraphicsCaptureItem).GetInterface("IGraphicsCaptureItem")?.GUID ?? new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");
        var ptr = interop.CreateForMonitor(monitor, ref iid);
        try { return GraphicsCaptureItem.FromAbi(ptr); }
        finally { Marshal.Release(ptr); }
    }

    static ID3D11Texture2D TextureFrom(IDirect3DSurface surface)
    {
        var access = surface.As<IDirect3DDxgiInterfaceAccess>();
        var iid = typeof(ID3D11Texture2D).GUID;
        var ptr = access.GetInterface(ref iid);
        return new ID3D11Texture2D(ptr);
    }

    static IDirect3DSurface SurfaceFromDxgi(IDXGISurface dxgi)
    {
        Marshal.ThrowExceptionForHR(CreateDirect3D11SurfaceFromDXGISurface(dxgi.NativePointer, out var inspectable));
        try { return MarshalInterface<IDirect3DSurface>.FromAbi(inspectable); }
        finally { Marshal.Release(inspectable); }
    }

    [DllImport("d3d11.dll")]
    static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    [DllImport("d3d11.dll")]
    static extern int CreateDirect3D11SurfaceFromDXGISurface(IntPtr dxgiSurface, out IntPtr graphicsSurface);

    [ComImport, Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IGraphicsCaptureItemInterop
    {
        IntPtr CreateForWindow(IntPtr window, ref Guid iid);
        IntPtr CreateForMonitor(IntPtr monitor, ref Guid iid);
    }

    [ComImport, Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDirect3DDxgiInterfaceAccess
    {
        IntPtr GetInterface(ref Guid iid);
    }

    public void Dispose()
    {
        _stopping = true;
        try { _session?.Dispose(); } catch { }
        if (_pool is not null) { _pool.FrameArrived -= OnFrame; try { _pool.Dispose(); } catch { } }
        lock (_frameGate) { _latest?.Dispose(); _latest = null; }
        lock (_free)
        {
            foreach (var (tex, surface) in _all) { try { surface.Dispose(); } catch { } tex.Dispose(); }
            _all.Clear();
            _free.Clear();
        }
        _last?.Dispose();
        _last = null;
        try { _loop?.StopRecording(); } catch { }
        try { _mic?.StopRecording(); } catch { }
        _loop?.Dispose();
        _mic?.Dispose();
        _loop = null;
        _mic = null;
        try { _stream?.Dispose(); } catch { }
        _stream = null;
        _session = null;
        _pool = null;
        _item = null;
        (_device as IDisposable)?.Dispose();
        _device = null;
        _ctx?.Dispose();
        _d3d?.Dispose();
        _ctx = null;
        _d3d = null;
        _clock.Stop();
    }
}
