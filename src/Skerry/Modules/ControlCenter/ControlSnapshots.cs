using Skerry.Core;
using Skerry.Views.Control;

namespace Skerry.Modules.ControlCenter;

/// <summary>--snapshot states for the Control tab (fixed state; nothing on the machine is changed).</summary>
static class ControlSnapshots
{
    static readonly List<VpnEntry> Vpns =
    [
        new("windows", "Office VPN", false, "rasdial.exe", ["Office VPN"], ["Office VPN", "/disconnect"], null),
        new("tailscale", "Tailscale", true, "tailscale.exe", ["up"], ["down"], null),
        new("proton", "Proton VPN", false, null, null, null, "ProtonVPN.exe"),
    ];

    static readonly List<BtDevice> Devices =
    [
        new("1", "Studio Buds", 0x112233445566, true, true),
        new("2", "Travel Headphones", 0x112233445567, false, true),
        new("3", "Keyboard K3", 0x112233445568, false, false),
    ];

    public static void Register(Dictionary<string, Action<IslandWindow>> states)
    {
        states["control-tab"] = w => Tab(w, v => v.SetForSnapshot(true, true, true, 0.62f, 70, Vpns, Devices));
        states["control-vpn"] = w => Tab(w, v => v.SetForSnapshot(true, false, false, 0.3f, null, Vpns, null, overlay: "vpn"));
        states["control-bt"] = w => Tab(w, v => v.SetForSnapshot(true, true, true, 0.62f, 70, Vpns, Devices, overlay: "bt"));
    }

    static void Tab(IslandWindow w, Action<ControlView> setup)
    {
        w.AddTab(new TabInfo("music", "tab.music", "Icon.Music", () => new System.Windows.Controls.Grid(), 10));
        w.AddTab(new TabInfo("control", "tab.control", "Icon.Control", () =>
        {
            var v = new ControlView(new ControlServices(w.Dispatcher) { VpnEnvironment = new EmptyVpnEnvironment() });
            setup(v);
            return v;
        }, 60));
        w.Expand("control");
    }

    sealed class EmptyVpnEnvironment : IVpnEnvironment
    {
        public bool FileExists(string path) => false;
        public IReadOnlyList<(string Name, string Description, System.Net.NetworkInformation.NetworkInterfaceType Type, bool Up)> Adapters() => [];
        public IEnumerable<string> Phonebooks() => [];
        public string ProgramFiles => @"C:\PF";
        public string LocalAppData => @"C:\LAD";
    }
}
