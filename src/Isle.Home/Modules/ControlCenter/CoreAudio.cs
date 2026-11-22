using System.Runtime.InteropServices;

namespace Isle.Modules.ControlCenter;

// Minimal Core Audio COM interop (mmdeviceapi.h / endpointvolume.h / devicetopology.h). Method order = vtable order.

static class CoreAudioIds
{
    public static readonly Guid MMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    public static readonly Guid IAudioEndpointVolume = new("5CDF2C82-841E-4546-9722-0CF74078229A");
    public static readonly Guid IDeviceTopology = new("2A07407E-6497-4A18-9787-32F79BD0D98F");
    public static readonly Guid IKsControl = new("28F54685-06FD-11D2-B27A-00A0C9223196");
    public const int CLSCTX_ALL = 23;
}

enum EDataFlow { Render = 0, Capture = 1, All = 2 }
enum ERole { Console = 0, Multimedia = 1, Communications = 2 }

[Flags]
enum DeviceState : uint { Active = 1, Disabled = 2, NotPresent = 4, Unplugged = 8, All = 15 }

[StructLayout(LayoutKind.Sequential)]
struct PropertyKey
{
    public Guid fmtid;
    public int pid;
}

[ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceEnumerator
{
    [PreserveSig] int EnumAudioEndpoints(EDataFlow dataFlow, DeviceState stateMask, out IMMDeviceCollection devices);
    [PreserveSig] int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
    [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
    [PreserveSig] int RegisterEndpointNotificationCallback(IMMNotificationClient client);
    [PreserveSig] int UnregisterEndpointNotificationCallback(IMMNotificationClient client);
}

[ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceCollection
{
    [PreserveSig] int GetCount(out uint count);
    [PreserveSig] int Item(uint index, out IMMDevice device);
}

[ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDevice
{
    [PreserveSig] int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object iface);
    [PreserveSig] int OpenPropertyStore(int access, out IPropertyStore store);
    [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
    [PreserveSig] int GetState(out DeviceState state);
}

[ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IPropertyStore
{
    [PreserveSig] int GetCount(out uint count);
    [PreserveSig] int GetAt(uint index, out PropertyKey key);
    [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value);
    [PreserveSig] int SetValue(ref PropertyKey key, ref PropVariant value);
    [PreserveSig] int Commit();
}

[StructLayout(LayoutKind.Explicit)]
struct PropVariant
{
    [FieldOffset(0)] public ushort vt;
    [FieldOffset(8)] public IntPtr pointer;

    [DllImport("ole32.dll")]
    public static extern int PropVariantClear(ref PropVariant pvar);
}

[ComImport, Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMNotificationClient
{
    void OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string deviceId, DeviceState newState);
    void OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string deviceId);
    void OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string deviceId);
    void OnDefaultDeviceChanged(EDataFlow flow, ERole role, [MarshalAs(UnmanagedType.LPWStr)] string? defaultDeviceId);
    void OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string deviceId, PropertyKey key);
}

[ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioEndpointVolume
{
    [PreserveSig] int RegisterControlChangeNotify(IAudioEndpointVolumeCallback notify);
    [PreserveSig] int UnregisterControlChangeNotify(IAudioEndpointVolumeCallback notify);
    [PreserveSig] int GetChannelCount(out uint count);
    [PreserveSig] int SetMasterVolumeLevel(float levelDb, ref Guid context);
    [PreserveSig] int SetMasterVolumeLevelScalar(float level, ref Guid context);
    [PreserveSig] int GetMasterVolumeLevel(out float levelDb);
    [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
    [PreserveSig] int SetChannelVolumeLevel(uint channel, float levelDb, ref Guid context);
    [PreserveSig] int SetChannelVolumeLevelScalar(uint channel, float level, ref Guid context);
    [PreserveSig] int GetChannelVolumeLevel(uint channel, out float levelDb);
    [PreserveSig] int GetChannelVolumeLevelScalar(uint channel, out float level);
    [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid context);
    [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
    [PreserveSig] int GetVolumeStepInfo(out uint step, out uint stepCount);
    [PreserveSig] int VolumeStepUp(ref Guid context);
    [PreserveSig] int VolumeStepDown(ref Guid context);
    [PreserveSig] int QueryHardwareSupport(out uint mask);
    [PreserveSig] int GetVolumeRange(out float min, out float max, out float increment);
}

[ComImport, Guid("657804FA-D6AD-4496-8A60-352752AF4F89"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioEndpointVolumeCallback
{
    [PreserveSig] int OnNotify(IntPtr notifyData);
}

[StructLayout(LayoutKind.Sequential)]
struct AudioVolumeNotificationData
{
    public Guid guidEventContext;
    [MarshalAs(UnmanagedType.Bool)] public bool bMuted;
    public float fMasterVolume;
    public uint nChannels;
}

// ---- device topology (for the Bluetooth audio reconnect trick) ----

[ComImport, Guid("2A07407E-6497-4A18-9787-32F79BD0D98F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IDeviceTopology
{
    [PreserveSig] int GetConnectorCount(out uint count);
    [PreserveSig] int GetConnector(uint index, out IConnector connector);
    [PreserveSig] int GetSubunitCount(out uint count);
    [PreserveSig] int GetSubunit(uint index, out IntPtr subunit);
    [PreserveSig] int GetPartById(uint id, out IPart part);
    [PreserveSig] int GetDeviceId([MarshalAs(UnmanagedType.LPWStr)] out string id);
    [PreserveSig] int GetSignalPath(IPart from, IPart to, [MarshalAs(UnmanagedType.Bool)] bool rejectMixedPaths, out IntPtr parts);
}

[ComImport, Guid("9C2C4058-23F5-41DE-877A-DF3AF236A09E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IConnector
{
    [PreserveSig] int GetType(out int type);
    [PreserveSig] int GetDataFlow(out int flow);
    [PreserveSig] int ConnectTo(IConnector to);
    [PreserveSig] int Disconnect();
    [PreserveSig] int IsConnected([MarshalAs(UnmanagedType.Bool)] out bool connected);
    [PreserveSig] int GetConnectedTo(out IConnector to);
    [PreserveSig] int GetConnectorIdConnectedTo([MarshalAs(UnmanagedType.LPWStr)] out string id);
    [PreserveSig] int GetDeviceIdConnectedTo([MarshalAs(UnmanagedType.LPWStr)] out string id);
}

[ComImport, Guid("AE2DE0E4-5BCA-4F2D-AA46-5D13F8FDB3A9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IPart
{
    [PreserveSig] int GetName([MarshalAs(UnmanagedType.LPWStr)] out string name);
    [PreserveSig] int GetLocalId(out uint id);
    [PreserveSig] int GetGlobalId([MarshalAs(UnmanagedType.LPWStr)] out string id);
    [PreserveSig] int GetPartType(out int type);
    [PreserveSig] int GetSubType(out Guid subType);
    [PreserveSig] int GetControlInterfaceCount(out uint count);
    [PreserveSig] int GetControlInterface(uint index, out IntPtr control);
    [PreserveSig] int EnumPartsIncoming(out IntPtr parts);
    [PreserveSig] int EnumPartsOutgoing(out IntPtr parts);
    [PreserveSig] int GetTopologyObject(out IDeviceTopology topology);
    [PreserveSig] int Activate(int clsContext, ref Guid iid, [MarshalAs(UnmanagedType.IUnknown)] out object iface);
    [PreserveSig] int RegisterControlChangeCallback(ref Guid iid, IntPtr notify);
    [PreserveSig] int UnregisterControlChangeCallback(IntPtr notify);
}

[StructLayout(LayoutKind.Sequential)]
struct KsProperty
{
    public Guid Set;
    public uint Id;
    public uint Flags;
}

[ComImport, Guid("28F54685-06FD-11D2-B27A-00A0C9223196"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IKsControl
{
    [PreserveSig] int KsProperty(ref KsProperty property, uint propertyLength, IntPtr data, uint dataLength, out uint bytesReturned);
    [PreserveSig] int KsMethod(IntPtr method, uint methodLength, IntPtr data, uint dataLength, out uint bytesReturned);
    [PreserveSig] int KsEvent(IntPtr evt, uint eventLength, IntPtr data, uint dataLength, out uint bytesReturned);
}
