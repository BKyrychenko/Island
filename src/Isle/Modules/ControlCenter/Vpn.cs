using System.Diagnostics;
using System.Net.NetworkInformation;
using Isle.Core;

namespace Isle.Modules.ControlCenter;

/// <summary>One VPN the user can see: a Windows built-in profile or a detected client.</summary>
public sealed record VpnEntry(string Kind, string Name, bool Connected, string? Cli, string[]? ConnectArgs, string[]? DisconnectArgs, string? OpenPath)
{
    public bool CanToggle => Cli is not null;
}

/// <summary>What the detector looks at; real implementation reads the machine, tests pass a fake.</summary>
public interface IVpnEnvironment
{
    bool FileExists(string path);
    /// <summary>Network adapters: (name, description, type, is up).</summary>
    IReadOnlyList<(string Name, string Description, NetworkInterfaceType Type, bool Up)> Adapters();
    /// <summary>Contents of the rasphone.pbk phonebooks (user + all users).</summary>
    IEnumerable<string> Phonebooks();
    string ProgramFiles { get; }
    string LocalAppData { get; }
}

/// <summary>Finds Windows VPN profiles and common VPN clients, with their connect/disconnect CLI where one exists.</summary>
public static class VpnDetector
{
    sealed record Client(string Kind, string Name, string[] Paths, string[] AdapterHints, string[]? Connect, string[]? Disconnect, bool CliIsToggle, string[] OpenHints);

    static IEnumerable<Client> Clients(IVpnEnvironment env)
    {
        string pf = env.ProgramFiles, local = env.LocalAppData;
        yield return new("tailscale", "Tailscale", [Path.Combine(pf, "Tailscale", "tailscale.exe")], ["tailscale"], ["up"], ["down"], true, [Path.Combine(pf, "Tailscale", "tailscale-ipn.exe")]);
        yield return new("warp", "Cloudflare WARP", [Path.Combine(pf, "Cloudflare", "Cloudflare WARP", "warp-cli.exe")], ["cloudflare warp", "cloudflarewarp"], ["connect"], ["disconnect"], true, [Path.Combine(pf, "Cloudflare", "Cloudflare WARP", "Cloudflare WARP.exe")]);
        yield return new("mullvad", "Mullvad VPN", [Path.Combine(pf, "Mullvad VPN", "resources", "mullvad.exe")], ["mullvad"], ["connect"], ["disconnect"], true, [Path.Combine(pf, "Mullvad VPN", "Mullvad VPN.exe")]);
        yield return new("nordvpn", "NordVPN", [Path.Combine(pf, "NordVPN", "NordVPN.exe")], ["nordlynx", "nordvpn"], ["-c"], ["-d"], true, [Path.Combine(pf, "NordVPN", "NordVPN.exe")]);
        yield return new("proton", "Proton VPN", [Path.Combine(pf, "Proton", "VPN", "ProtonVPN.Launcher.exe"), Path.Combine(pf, "Proton Technologies", "ProtonVPN", "ProtonVPN.exe"), Path.Combine(local, "Programs", "Proton", "VPN", "ProtonVPN.Launcher.exe")], ["protonvpn"], null, null, false, []);
        yield return new("wireguard", "WireGuard", [Path.Combine(pf, "WireGuard", "wireguard.exe")], ["wireguard"], null, null, false, []);
        yield return new("openvpn", "OpenVPN", [Path.Combine(pf, "OpenVPN", "bin", "openvpn-gui.exe"), Path.Combine(pf, "OpenVPN Connect", "OpenVPNConnect.exe")], ["openvpn", "tap-windows"], null, null, false, []);
    }

    public static List<VpnEntry> Detect(IVpnEnvironment env)
    {
        var result = new List<VpnEntry>();
        var adapters = env.Adapters();

        // Windows built-in profiles: names from the phonebooks; connected = an up PPP adapter with that name
        foreach (var name in env.Phonebooks().SelectMany(ParsePhonebook).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var up = adapters.Any(a => a.Up && string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
            var rasdial = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "rasdial.exe");
            result.Add(new VpnEntry("windows", name, up, rasdial, [name], [name, "/disconnect"], null));
        }

        foreach (var c in Clients(env))
        {
            var path = c.Paths.FirstOrDefault(env.FileExists);
            var adapter = adapters.Where(a => c.AdapterHints.Any(h => a.Name.Contains(h, StringComparison.OrdinalIgnoreCase) || a.Description.Contains(h, StringComparison.OrdinalIgnoreCase))).ToList();
            if (path is null && adapter.Count == 0) continue;
            var connected = adapter.Any(a => a.Up);
            var open = c.OpenHints.FirstOrDefault(env.FileExists) ?? (c.CliIsToggle ? null : path);
            result.Add(new VpnEntry(c.Kind, c.Name, connected, c.CliIsToggle ? path : null, c.Connect, c.Disconnect, open));
        }
        return result;
    }

    /// <summary>Entry names of a rasphone.pbk file ("[Name]" section headers).</summary>
    public static IEnumerable<string> ParsePhonebook(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length > 2 && line[0] == '[' && line[^1] == ']') yield return line[1..^1];
        }
    }

    /// <summary>Runs a client CLI hidden. Returns (ok, needsElevation, output).</summary>
    public static async Task<(bool Ok, bool NeedsAdmin, string Output)> RunAsync(string exe, string[] args)
    {
        if (!Path.IsPathFullyQualified(exe)) return (false, false, "");
        try
        {
            var psi = new ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var a in args) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi);
            if (p is null) return (false, false, "");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            var stdout = p.StandardOutput.ReadToEndAsync(cts.Token);
            var stderr = p.StandardError.ReadToEndAsync(cts.Token);
            await p.WaitForExitAsync(cts.Token);
            var output = (await stdout + "\n" + await stderr).Trim();
            var admin = p.ExitCode == 5 || output.Contains("elevat", StringComparison.OrdinalIgnoreCase) || output.Contains("access is denied", StringComparison.OrdinalIgnoreCase)
                        || output.Contains("administrator", StringComparison.OrdinalIgnoreCase);
            return (p.ExitCode == 0, admin, output);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 740) { return (false, true, ""); } // requires elevation
        catch (OperationCanceledException) { return (false, false, "timeout"); }
        catch (Exception ex) { Logger.Error("vpn command", ex); return (false, false, ""); }
    }
}

/// <summary>The real machine.</summary>
public sealed class MachineVpnEnvironment : IVpnEnvironment
{
    public bool FileExists(string path) => File.Exists(path);

    public IReadOnlyList<(string Name, string Description, NetworkInterfaceType Type, bool Up)> Adapters()
    {
        try
        {
            return [.. NetworkInterface.GetAllNetworkInterfaces().Select(n => (n.Name, n.Description, n.NetworkInterfaceType, n.OperationalStatus == OperationalStatus.Up))];
        }
        catch (Exception ex) { Logger.Error("adapters", ex); return []; }
    }

    public IEnumerable<string> Phonebooks()
    {
        foreach (var root in new[] { Environment.SpecialFolder.ApplicationData, Environment.SpecialFolder.CommonApplicationData })
        {
            var path = Path.Combine(Environment.GetFolderPath(root), "Microsoft", "Network", "Connections", "Pbk", "rasphone.pbk");
            string? text = null;
            try { if (File.Exists(path)) text = File.ReadAllText(path); }
            catch (Exception ex) { Logger.Warn($"phonebook unreadable: {ex.GetType().Name}"); }
            if (text is not null) yield return text;
        }
    }

    public string ProgramFiles => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
    public string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
}
