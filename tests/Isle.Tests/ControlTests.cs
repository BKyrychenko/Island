using System.Net.NetworkInformation;
using Isle.Modules.ControlCenter;

namespace Isle.Tests;

public class VpnDetectorTests
{
    sealed class FakeEnv : IVpnEnvironment
    {
        public HashSet<string> Files = new(StringComparer.OrdinalIgnoreCase);
        public List<(string, string, NetworkInterfaceType, bool)> Nics = [];
        public List<string> Books = [];
        public bool FileExists(string path) => Files.Contains(path);
        public IReadOnlyList<(string Name, string Description, NetworkInterfaceType Type, bool Up)> Adapters() => Nics;
        public IEnumerable<string> Phonebooks() => Books;
        public string ProgramFiles => @"C:\Program Files";
        public string LocalAppData => @"C:\Users\u\AppData\Local";
    }

    [Fact]
    public void Nothing_installed()
    {
        Assert.Empty(VpnDetector.Detect(new FakeEnv { Nics = [("Ethernet", "Intel(R) Ethernet", NetworkInterfaceType.Ethernet, true)] }));
    }

    [Fact]
    public void Builtin_profiles_from_phonebook_with_state()
    {
        var env = new FakeEnv
        {
            Books = ["[Office VPN]\r\nMEDIA=rastapi\r\n\r\n[Home]\nType=2\n", "[Office VPN]\n"],
            Nics = [("Office VPN", "WAN Miniport (IKEv2)", NetworkInterfaceType.Ppp, true)],
        };
        var list = VpnDetector.Detect(env);
        Assert.Equal(["Office VPN", "Home"], list.Select(v => v.Name));
        Assert.True(list[0].Connected);
        Assert.False(list[1].Connected);
        Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "rasdial.exe"), list[0].Cli);
        Assert.True(Path.IsPathFullyQualified(list[0].Cli!));
        Assert.Equal(["Home", "/disconnect"], list[1].DisconnectArgs);
    }

    [Fact]
    public void Clients_by_path_and_adapter()
    {
        var env = new FakeEnv
        {
            Files = [@"C:\Program Files\Tailscale\tailscale.exe", @"C:\Program Files\Cloudflare\Cloudflare WARP\warp-cli.exe"],
            Nics =
            [
                ("Tailscale", "Tailscale Tunnel", NetworkInterfaceType.Unknown, true),
                ("CloudflareWARP", "Cloudflare WARP Interface Tunnel", NetworkInterfaceType.Unknown, false),
                ("ProtonVPN", "ProtonVPN Tunnel", NetworkInterfaceType.Unknown, false),
            ],
        };
        var list = VpnDetector.Detect(env).ToDictionary(v => v.Kind);
        Assert.True(list["tailscale"].Connected);
        Assert.Equal(["down"], list["tailscale"].DisconnectArgs);
        Assert.False(list["warp"].Connected);
        Assert.True(list["warp"].CanToggle);
        Assert.False(list["proton"].CanToggle);           // status only: no CLI
        Assert.DoesNotContain("mullvad", list.Keys);
    }

    [Fact]
    public void Phonebook_parsing_ignores_other_lines()
    {
        Assert.Equal(["A", "B c"], VpnDetector.ParsePhonebook("x=1\n[A]\n  [B c]  \n[]\nfoo [bar]"));
    }

    [Theory]
    [InlineData("rasdial.exe")]
    [InlineData(".\\rasdial.exe")]
    public async Task Relative_cli_paths_are_rejected(string path)
    {
        var result = await VpnDetector.RunAsync(path, []);
        Assert.False(result.Ok);
        Assert.False(result.NeedsAdmin);
        Assert.Empty(result.Output);
    }
}
