using System.Runtime.InteropServices;
using System.Windows.Threading;
using Isle.Core;

namespace Isle.Modules.ControlCenter;

/// <summary>
/// Master volume + mute of the default playback device (Core Audio). Event-driven: endpoint volume callback for
/// external changes, device notifications for a changed default device. Callbacks arrive on COM threads and are
/// marshalled to the UI thread.
/// </summary>
public sealed class AudioVolume : IDisposable
{
    readonly Dispatcher _ui;
    IMMDeviceEnumerator? _enumerator;
    IAudioEndpointVolume? _endpoint;
    readonly VolumeCallback _volumeCallback;
    readonly DeviceCallback _deviceCallback;
    Guid _ourContext = Guid.NewGuid();

    public float Level { get; private set; }
    public bool Muted { get; private set; }
    public bool Available => _endpoint is not null;

    /// <summary>Raised on the UI thread after volume/mute/device changes.</summary>
    public event Action? Changed;

    public AudioVolume(Dispatcher ui)
    {
        _ui = ui;
        _volumeCallback = new VolumeCallback(this);
        _deviceCallback = new DeviceCallback(this);
        try
        {
            _enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(Type.GetTypeFromCLSID(CoreAudioIds.MMDeviceEnumerator)!)!;
            _enumerator.RegisterEndpointNotificationCallback(_deviceCallback);
            Bind();
        }
        catch (Exception ex) { Logger.Error("audio volume init", ex); }
    }

    void Bind()
    {
        Unbind();
        if (_enumerator is null) return;
        try
        {
            if (_enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Multimedia, out var device) != 0 || device is null) return;
            var iid = CoreAudioIds.IAudioEndpointVolume;
            if (device.Activate(ref iid, CoreAudioIds.CLSCTX_ALL, IntPtr.Zero, out var obj) != 0) return;
            _endpoint = (IAudioEndpointVolume)obj;
            _endpoint.RegisterControlChangeNotify(_volumeCallback);
            Read();
        }
        catch (Exception ex) { Logger.Error("audio volume bind", ex); _endpoint = null; }
    }

    void Unbind()
    {
        if (_endpoint is null) return;
        try { _endpoint.UnregisterControlChangeNotify(_volumeCallback); } catch { /* device gone */ }
        try { Marshal.ReleaseComObject(_endpoint); } catch { }
        _endpoint = null;
    }

    void Read()
    {
        if (_endpoint is null) return;
        if (_endpoint.GetMasterVolumeLevelScalar(out var level) == 0) Level = level;
        if (_endpoint.GetMute(out var muted) == 0) Muted = muted;
    }

    public void SetLevel(float level)
    {
        if (_endpoint is null) return;
        level = Math.Clamp(level, 0f, 1f);
        Level = level;
        try
        {
            _endpoint.SetMasterVolumeLevelScalar(level, ref _ourContext);
            if (Muted && level > 0) { _endpoint.SetMute(false, ref _ourContext); Muted = false; }
        }
        catch (Exception ex) { Logger.Error("set volume", ex); }
    }

    public void SetMuted(bool muted)
    {
        if (_endpoint is null) return;
        try { _endpoint.SetMute(muted, ref _ourContext); Muted = muted; }
        catch (Exception ex) { Logger.Error("set mute", ex); }
        Raise();
    }

    void Raise()
    {
        try { Changed?.Invoke(); }
        catch (Exception ex) { Logger.Error("volume changed handler", ex); }
    }

    void OnVolumeNotify(Guid context, bool muted, float level) => _ui.BeginInvoke(() =>
    {
        Level = level;
        Muted = muted;
        if (context != _ourContext) Raise(); // our own changes are already on screen
    });

    void OnDefaultChanged() => _ui.BeginInvoke(() => { Bind(); Raise(); });

    public void Dispose()
    {
        Unbind();
        if (_enumerator is not null)
        {
            try { _enumerator.UnregisterEndpointNotificationCallback(_deviceCallback); } catch { }
            try { Marshal.ReleaseComObject(_enumerator); } catch { }
            _enumerator = null;
        }
    }

    [ComVisible(true)]
    sealed class VolumeCallback(AudioVolume owner) : IAudioEndpointVolumeCallback
    {
        public int OnNotify(IntPtr data)
        {
            try
            {
                var d = Marshal.PtrToStructure<AudioVolumeNotificationData>(data);
                owner.OnVolumeNotify(d.guidEventContext, d.bMuted, d.fMasterVolume);
            }
            catch { /* never throw into Core Audio */ }
            return 0;
        }
    }

    [ComVisible(true)]
    sealed class DeviceCallback(AudioVolume owner) : IMMNotificationClient
    {
        public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }
        public void OnDeviceAdded(string deviceId) { }
        public void OnDeviceRemoved(string deviceId) { }
        public void OnDefaultDeviceChanged(EDataFlow flow, ERole role, string? defaultDeviceId)
        {
            if (flow == EDataFlow.Render && role == ERole.Multimedia) owner.OnDefaultChanged();
        }
        public void OnPropertyValueChanged(string deviceId, PropertyKey key) { }
    }
}
