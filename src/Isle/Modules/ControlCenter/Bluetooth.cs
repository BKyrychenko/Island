using System.Runtime.InteropServices;
using Isle.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace Isle.Modules.ControlCenter;

public sealed record BtDevice(string Id, string Name, ulong Address, bool Connected, bool IsAudio);

/// <summary>
/// Paired Bluetooth devices (WinRT) and connect/disconnect for audio devices. Windows has no public "connect headphones"
/// API; the working approach is the one the Settings app uses internally: ask the device's Bluetooth audio KS filter for a
/// one-shot reconnect/disconnect (KSPROPSETID_BtAudio) through the Core Audio device topology.
/// </summary>
public static class BluetoothDevices
{
    static readonly Guid KsPropSetIdBtAudio = new("7FA06C40-B8F6-4C7E-8556-E8C33A12E54D");
    const uint KsPropertyOneShotReconnect = 0, KsPropertyOneShotDisconnect = 1, KsPropertyTypeGet = 1;

    public static async Task<List<BtDevice>> ListAsync()
    {
        var list = new List<BtDevice>();
        try
        {
            var infos = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true));
            foreach (var info in infos)
            {
                try
                {
                    using var dev = await BluetoothDevice.FromIdAsync(info.Id);
                    if (dev is null) continue;
                    var audio = dev.ClassOfDevice?.MajorClass == BluetoothMajorClass.AudioVideo;
                    list.Add(new BtDevice(info.Id, string.IsNullOrWhiteSpace(dev.Name) ? info.Name : dev.Name, dev.BluetoothAddress,
                        dev.ConnectionStatus == BluetoothConnectionStatus.Connected, audio));
                }
                catch (Exception ex) { Logger.Warn($"bluetooth device skipped: {ex.GetType().Name}"); }
            }
        }
        catch (Exception ex) { Logger.Error("bluetooth list", ex); }
        return [.. list.OrderByDescending(d => d.Connected).ThenByDescending(d => d.IsAudio).ThenBy(d => d.Name)];
    }

    /// <summary>Connects or disconnects a Bluetooth audio device. Returns false when no matching audio endpoint was found.</summary>
    public static Task<bool> SetConnectedAsync(BtDevice device, bool connect) => Task.Run(() => SetConnected(device, connect));

    static bool SetConnected(BtDevice device, bool connect)
    {
        var address = device.Address.ToString("X12");
        var done = false;
        IMMDeviceEnumerator? enumerator = null;
        try
        {
            enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(Type.GetTypeFromCLSID(CoreAudioIds.MMDeviceEnumerator)!)!;
            foreach (var flow in new[] { EDataFlow.Render, EDataFlow.Capture })
            {
                if (enumerator.EnumAudioEndpoints(flow, DeviceState.Active | DeviceState.Unplugged, out var coll) != 0) continue;
                coll.GetCount(out var count);
                for (uint i = 0; i < count && !done; i++)
                {
                    if (coll.Item(i, out var endpoint) != 0) continue;
                    try { done = TryEndpoint(endpoint, address, device.Name, connect); }
                    catch (Exception ex) { Logger.Warn($"bt audio endpoint skipped: {ex.GetType().Name}"); }
                    finally { Marshal.ReleaseComObject(endpoint); }
                }
                Marshal.ReleaseComObject(coll);
                if (done) break;
            }
        }
        catch (Exception ex) { Logger.Error("bluetooth audio connect", ex); }
        finally { if (enumerator is not null) Marshal.ReleaseComObject(enumerator); }
        return done;
    }

    static bool TryEndpoint(IMMDevice endpoint, string address, string name, bool connect)
    {
        var iid = CoreAudioIds.IDeviceTopology;
        if (endpoint.Activate(ref iid, CoreAudioIds.CLSCTX_ALL, IntPtr.Zero, out var topoObj) != 0) return false;
        var topology = (IDeviceTopology)topoObj;
        try
        {
            if (topology.GetConnector(0, out var connector) != 0) return false;
            if (connector.GetConnectedTo(out var other) != 0 || other is not IPart part) return false;
            if (part.GetTopologyObject(out var filterTopology) != 0) return false;
            filterTopology.GetDeviceId(out var filterId);
            // the KS filter of a Bluetooth audio device has "BTHENUM"/"BTHHFENUM" and the device address in its id
            var isBt = filterId.Contains("BTH", StringComparison.OrdinalIgnoreCase);
            if (!isBt || !filterId.Contains(address, StringComparison.OrdinalIgnoreCase)) return false;
            var ksIid = CoreAudioIds.IKsControl;
            if (part.Activate(CoreAudioIds.CLSCTX_ALL, ref ksIid, out var ksObj) != 0) return false;
            var ks = (IKsControl)ksObj;
            var prop = new KsProperty
            {
                Set = KsPropSetIdBtAudio,
                Id = connect ? KsPropertyOneShotReconnect : KsPropertyOneShotDisconnect,
                Flags = KsPropertyTypeGet,
            };
            var hr = ks.KsProperty(ref prop, (uint)Marshal.SizeOf<KsProperty>(), IntPtr.Zero, 0, out _);
            Marshal.ReleaseComObject(ks);
            Logger.Info($"bluetooth audio {(connect ? "connect" : "disconnect")} request: hr=0x{hr:X8}");
            return hr >= 0;
        }
        finally { Marshal.ReleaseComObject(topology); }
    }
}
