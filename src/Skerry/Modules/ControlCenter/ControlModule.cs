using System.Net.NetworkInformation;
using System.Windows.Threading;
using Skerry.Core;
using Skerry.Views.Control;

namespace Skerry.Modules.ControlCenter;

/// <summary>Everything the Control tab talks to. Created on the first open of the tab, not at start-up.</summary>
public sealed class ControlServices(Dispatcher ui) : IDisposable
{
    AudioVolume? _audio;
    public AudioVolume Audio => _audio ??= new AudioVolume(ui);
    public Radios Radios { get; } = new(ui);
    public Brightness Brightness { get; } = new();
    public IVpnEnvironment VpnEnvironment { get; init; } = new MachineVpnEnvironment();

    public void Dispose()
    {
        _audio?.Dispose();
        _audio = null;
        Radios.Dispose();
    }
}

/// <summary>Control Center (module id "control"): Wi-Fi, Bluetooth (+ devices), dark mode, VPN, volume, brightness.</summary>
public sealed class ControlModule(IslandWindow island) : IModule, IDisposable
{
    public const string TabId = "control";
    ControlServices? _services;
    bool _started;

    public string Id => "control";

    /// <summary>Raised on the UI thread when network interfaces change (VPN up/down) — used by the VPN live indicator (M6).</summary>
    public event Action? NetworkChanged;

    public void Start()
    {
        _started = true;
        island.AddTab(new TabInfo(TabId, "tab.control", "Icon.Control", () => new ControlView(_services ??= new ControlServices(island.Dispatcher)), 60));
        NetworkChange.NetworkAddressChanged += OnNetworkChanged;
    }

    void OnNetworkChanged(object? sender, EventArgs e) => island.Dispatcher.BeginInvoke(() =>
    {
        try { NetworkChanged?.Invoke(); }
        catch (Exception ex) { Logger.Error("network changed handler", ex); }
        if (island.TabViewFor(TabId) is ControlView v && v.IsVisible) v.RefreshVpn();
    });

    public void Stop()
    {
        if (!_started) return;
        _started = false;
        NetworkChange.NetworkAddressChanged -= OnNetworkChanged;
        island.RemoveTab(TabId);
        _services?.Dispose();
        _services = null;
    }

    public void Dispose() => Stop();
}
