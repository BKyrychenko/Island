using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Isle.Core;
using Isle.Modules.Commands;
using Isle.Views.Ports;

namespace Isle.Modules.Ports;

/// <summary>
/// Ports (module id "ports"): the Ports tab listing what developers' processes listen on, and a short pill when
/// a new one starts listening ("node · :5173", click opens it in the browser). The table is read every 5 s.
/// </summary>
public sealed class PortsModule(IslandWindow island) : IModule, IDisposable
{
    public const string TabId = "ports";
    const string PillId = "ports.new";

    DispatcherTimer? _watch, _pillTimer;
    List<Listener>? _known;

    public string Id => "ports";

    public void Start()
    {
        island.AddTab(new TabInfo(TabId, "tab.ports", "Icon.Server", () => new PortsView(() => PortScanner.Scan(), island, ct => Containers.ListAsync(ct)), 96));
        if (!Settings.Current.OptBool("ports.notify", true)) return;
        _watch = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(5) };
        _watch.Tick += async (_, _) => await WatchAsync();
        _watch.Start();
        _ = WatchAsync();
    }

    async Task WatchAsync()
    {
        try
        {
            var now = await Task.Run(() => PortScanner.Scan());
            if (_known is not null)
                foreach (var l in PortFilter.Added(_known, now).Take(1)) Announce(l);
            _known = now;
        }
        catch (Exception ex) { Logger.Error("ports watch", ex); }
    }

    void Announce(Listener l)
    {
        island.Pill.Set(new PillItem
        {
            Id = PillId, Priority = PillPriority.Transient, Width = 220, Peek = true,
            View = () => PillView(l),
            Click = () => ProcessOps.Open(PortsView.Url(l.Port)),
        });
        _pillTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        _pillTimer.Tick -= HidePill;
        _pillTimer.Tick += HidePill;
        _pillTimer.Stop();
        _pillTimer.Start();
    }

    void HidePill(object? sender, EventArgs e) { _pillTimer?.Stop(); island.Pill.Remove(PillId); }

    internal static FrameworkElement PillView(Listener l)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var icon = Views.Ui.MakeIcon("Icon.Server", 14, "OkBrush");
        icon.Margin = new Thickness(0, 0, 6, 0);
        row.Children.Add(icon);
        var t = new TextBlock { Text = $"{l.Process} · :{l.Port}", FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
        t.SetResourceReference(TextBlock.ForegroundProperty, "FgBrush");
        row.Children.Add(t);
        return row;
    }

    public void Stop()
    {
        _watch?.Stop();
        _watch = null;
        _pillTimer?.Stop();
        island.Pill.Remove(PillId);
        island.RemoveTab(TabId);
        _known = null;
    }

    public void Dispose() => Stop();
}

/// <summary>"port 3000", ":3000" in the command bar: who listens there, open it, or end that process.</summary>
public sealed partial class PortsProvider(Func<IReadOnlyList<Listener>>? scan = null) : ICommandProvider
{
    [GeneratedRegex(@"^(?:port\s+|порт\s+|:)(\d{1,5})$", RegexOptions.IgnoreCase)]
    private static partial Regex Query();

    public Task<IEnumerable<CommandItem>> QueryAsync(string text, CancellationToken ct)
    {
        var m = Query().Match(text.Trim());
        if (!m.Success || !int.TryParse(m.Groups[1].Value, out var port) || port is < 1 or > 65535) return Task.FromResult(Enumerable.Empty<CommandItem>());
        var hits = (scan ?? (() => PortScanner.Scan(interestingOnly: false)))().Where(l => l.Port == port).ToList();
        if (hits.Count == 0)
            return Task.FromResult<IEnumerable<CommandItem>>([new CommandItem { Title = Strings.T("ports.free", port), Subtitle = Strings.T("ports.freeHint"), IconKey = "Icon.Server", Score = 4700 }]);
        var items = new List<CommandItem>();
        foreach (var l in hits)
        {
            items.Add(new CommandItem
            {
                Title = Strings.T("ports.listening", l.Process, l.Pid, port), Subtitle = PortsView.Url(port), IconKey = "Icon.Server", Score = 4700,
                Run = () => ProcessOps.Open(PortsView.Url(port)),
            });
            items.Add(new CommandItem
            {
                Title = Strings.T("sysmon.end", l.Process), Subtitle = Strings.T("ports.endHint", port), IconKey = "Icon.Close", Score = 4690,
                Confirm = true, Confirmation = Strings.T("ports.endHint", port),
                Run = () => ProcessOps.TryEnd(l.Pid, l.Process),
            });
        }
        return Task.FromResult<IEnumerable<CommandItem>>(items);
    }
}
