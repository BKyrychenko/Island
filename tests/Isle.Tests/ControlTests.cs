using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Windows.Threading;
using Isle.Modules.ControlCenter;
using Windows.Devices.Radios;

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

    [Theory]
    [InlineData(NetworkInterfaceType.Ethernet, true)]
    [InlineData(NetworkInterfaceType.Wireless80211, true)]
    [InlineData(NetworkInterfaceType.Ppp, false)]
    public void Same_name_requires_an_up_ppp_adapter(NetworkInterfaceType type, bool up)
    {
        var env = new FakeEnv { Books = ["[Office VPN]"], Nics = [("Office VPN", "Unrelated", type, up)] };
        Assert.False(Assert.Single(VpnDetector.Detect(env)).Connected);
    }

    [Fact]
    public async Task Timeout_terminates_the_started_command()
    {
        var pidFile = Path.Combine(Path.GetTempPath(), $"isle-vpn-test-{Guid.NewGuid():N}.pid");
        var exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
        try
        {
            var result = await VpnDetector.RunAsync(exe,
                ["-NoProfile", "-NonInteractive", "-Command", $"$PID | Set-Content -LiteralPath '{pidFile.Replace("'", "''")}'; Start-Sleep -Seconds 60"], TimeSpan.FromSeconds(3));
            Assert.False(result.Ok);
            Assert.Equal("timeout", result.Output);
            Assert.True(File.Exists(pidFile), "The harmless stub must start before the timeout.");
            var pid = int.Parse(File.ReadAllText(pidFile).Trim());
            Assert.DoesNotContain(Process.GetProcesses(), p => { using (p) return p.Id == pid; });
        }
        finally
        {
            if (File.Exists(pidFile))
            {
                var pid = int.Parse(File.ReadAllText(pidFile).Trim());
                try { using var process = Process.GetProcessById(pid); process.Kill(entireProcessTree: true); }
                catch (ArgumentException) { }
                File.Delete(pidFile);
            }
        }
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

public class RadioDiscoveryTests
{
    [Fact]
    public async Task Reopen_retries_denial_absence_and_discovery_failure()
    {
        int accessCalls = 0, discoveries = 0, changes = 0;
        using var radios = new Radios(Dispatcher.CurrentDispatcher)
        {
            RequestAccess = () => Task.FromResult(++accessCalls == 1 ? RadioAccessStatus.DeniedByUser : RadioAccessStatus.Allowed),
            Discover = () => ++discoveries == 2
                ? Task.FromException<IReadOnlyList<Radio>>(new InvalidOperationException("test discovery failure"))
                : Task.FromResult<IReadOnlyList<Radio>>([]),
        };
        radios.Changed += () => changes++;
        await radios.InitAsync();
        Assert.True(radios.Denied);
        await radios.InitAsync();
        Assert.False(radios.Denied);
        Assert.Null(radios.WifiOn);
        await radios.InitAsync();
        Assert.True(radios.Denied);
        await radios.InitAsync();
        Assert.False(radios.Denied);
        Assert.Equal(4, accessCalls);
        Assert.Equal(3, discoveries);
        Assert.Equal(4, changes);
    }
}
