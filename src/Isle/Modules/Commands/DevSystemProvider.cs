using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Isle.Core;

namespace Isle.Modules.Commands;

/// <summary>Parsers for the developer system commands (pure; unit tested).</summary>
public static partial class DevSystem
{
    /// <summary>Concrete host aliases from an OpenSSH config (patterns with * ? ! are skipped).</summary>
    public static List<string> SshHosts(string config)
    {
        var hosts = new List<string>();
        foreach (var raw in config.Split('\n'))
        {
            var line = raw.Trim();
            if (!line.StartsWith("Host ", StringComparison.OrdinalIgnoreCase) && !line.StartsWith("Host\t", StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var h in line[5..].Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
                if (h.IndexOfAny(['*', '?', '!']) < 0 && !hosts.Contains(h)) hosts.Add(h);
        }
        return hosts;
    }

    public enum PowerPlan { Saver, Balanced, Performance }

    /// <summary>Windows' built-in power scheme GUIDs.</summary>
    public static string Guid(PowerPlan p) => p switch
    {
        PowerPlan.Saver => "a1841308-3bce-3fc8-ab6b-4b5d0a6b3f1c",
        PowerPlan.Performance => "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
        _ => "381b4222-f694-41f0-9685-ff5bb260df2e",
    };

    [GeneratedRegex(@"^(?:power|живлення|режим)\s+(?<p>saver|save|eco|економ\w*|balanced|баланс\w*|performance|perf|high|продуктив\w*|максим\w*)$", RegexOptions.IgnoreCase)]
    private static partial Regex Power();

    public static PowerPlan? ParsePower(string q)
    {
        var m = Power().Match(q.Trim());
        if (!m.Success) return null;
        var p = m.Groups["p"].Value.ToLowerInvariant();
        return p.StartsWith("sav") || p == "eco" || p.StartsWith("економ") ? PowerPlan.Saver
            : p.StartsWith("bal") || p.StartsWith("баланс") ? PowerPlan.Balanced
            : PowerPlan.Performance;
    }

    [GeneratedRegex(@"^ping\s+(?<host>[\w.-]+\.[a-z]{2,}|localhost|\d{1,3}(?:\.\d{1,3}){3})$", RegexOptions.IgnoreCase)]
    private static partial Regex PingQuery();

    public static string? ParsePing(string q) => PingQuery().Match(q.Trim()) is { Success: true } m ? m.Groups["host"].Value : null;

    public static IEnumerable<string> LocalAddresses() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses.Select(a => (n.Name, a.Address)))
            .Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(x => $"{x.Address} · {x.Name}");
}

/// <summary>"ip", "ping host", "flushdns", "power saver|balanced|performance", "ssh host" in the command bar.</summary>
public sealed class DevSystemProvider(Action<string> copy, Action<string> notify) : ICommandProvider
{
    public async Task<IEnumerable<CommandItem>> QueryAsync(string text, CancellationToken ct)
    {
        var q = text.Trim();
        var lower = q.ToLowerInvariant();
        var items = new List<CommandItem>();
        if (lower is "ip" or "my ip" or "мій ip")
        {
            foreach (var a in DevSystem.LocalAddresses())
            {
                var ip = a[..a.IndexOf(' ')];
                items.Add(new CommandItem { Title = a, Subtitle = Strings.T("dev.localIp") + " · " + Strings.T("cmd.copyResult"), IconKey = "Icon.Wifi", Score = 4700, Run = () => copy(ip) });
            }
        }
        else if (DevSystem.ParsePing(q) is { } host)
        {
            var times = new List<long>();
            using var ping = new Ping();
            for (var i = 0; i < 3 && !ct.IsCancellationRequested; i++)
            {
                try
                {
                    var reply = await ping.SendPingAsync(host, TimeSpan.FromSeconds(1), cancellationToken: ct).ConfigureAwait(false);
                    if (reply.Status == IPStatus.Success) times.Add(reply.RoundtripTime);
                }
                catch (PingException) { break; }
            }
            ct.ThrowIfCancellationRequested();
            var title = times.Count == 0 ? Strings.T("dev.pingNone", host) : Strings.T("dev.pingOk", host, Math.Round(times.Average()), times.Count);
            items.Add(new CommandItem { Title = title, Subtitle = "ping", IconKey = "Icon.Globe", Score = 4700 });
        }
        else if (lower is "flushdns" or "flush dns" or "dns flush")
        {
            items.Add(new CommandItem
            {
                Title = Strings.T("dev.flushDns"), Subtitle = "ipconfig /flushdns", IconKey = "Icon.Reset", Score = 4700,
                Run = () => _ = Run("ipconfig.exe", "/flushdns", "dev.flushDnsDone"),
            });
        }
        else if (DevSystem.ParsePower(q) is { } plan)
        {
            items.Add(new CommandItem
            {
                Title = Strings.T("dev.power." + plan.ToString().ToLowerInvariant()), Subtitle = "powercfg", IconKey = "Icon.Power", Score = 4700,
                Run = () => _ = Run("powercfg.exe", "/setactive " + DevSystem.Guid(plan), "dev.powerDone"),
            });
        }
        else if (lower == "ssh" || lower.StartsWith("ssh "))
        {
            var filter = lower.Length > 4 ? lower[4..].Trim() : "";
            var config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "config");
            if (File.Exists(config))
                foreach (var h in DevSystem.SshHosts(await File.ReadAllTextAsync(config, ct).ConfigureAwait(false)).Where(h => h.Contains(filter, StringComparison.OrdinalIgnoreCase)).Take(8))
                    items.Add(new CommandItem { Title = "ssh " + h, Subtitle = Strings.T("dev.sshOpen"), IconKey = "Icon.Agents", Score = 4650, Run = () => OpenSsh(h) });
        }
        return items;
    }

    async Task Run(string exe, string args, string doneKey)
    {
        var (code, _) = await Cli.RunAsync(Path.Combine(Environment.SystemDirectory, exe), args.Split(' '), null, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        notify(code == 0 ? Strings.T(doneKey) : Strings.T("dev.failed", exe));
    }

    static void OpenSsh(string host)
    {
        try
        {
            // Windows Terminal when present, otherwise a console window
            var wt = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "wt.exe");
            var info = File.Exists(wt) ? new ProcessStartInfo(wt) : new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe")) { ArgumentList = { "/k" } };
            info.ArgumentList.Add("ssh");
            info.ArgumentList.Add(host);
            info.UseShellExecute = false;
            Process.Start(info)?.Dispose();
        }
        catch (System.ComponentModel.Win32Exception ex) { Logger.Warn($"ssh: {ex.GetType().Name}"); }
    }
}
