using Isle.Core;
using Isle.Modules.Commands;
using Isle.Views.SettingsPanel;

namespace Isle.Dev;

/// <summary>
/// Entry point of the developer pack. Isle's <c>Core.DevPack</c> calls <see cref="Register"/> once at start-up (and in
/// snapshot mode); it adds the pack's text, modules (on by default), settings rows, command providers and snapshot states.
/// </summary>
public static class Pack
{
    public static void Register()
    {
        Strings.Add(DevStrings.En, DevStrings.Uk);

        App.Host.Register("sysmon", () => new Modules.SystemMonitor.SystemMonitorModule(App.Island!));
        App.Host.Register("ports", () => new Modules.Ports.PortsModule(App.Island!));
        App.Host.Register("projects", () => new Modules.Projects.ProjectsModule(App.Island!));
        App.Host.Register("logs", () => new Modules.Logs.LogWatchModule(App.Island!));
        App.Host.Register("kube", () => new Modules.Kube.KubeModule(App.Island!));
        App.Host.Register("usage", () => new Modules.Usage.UsageModule(App.Island!));
        SettingsView.ModuleIds.InsertRange(SettingsView.ModuleIds.IndexOf("characters"), ["sysmon", "ports", "projects", "logs", "kube", "usage"]);
        SettingsView.DevOptions.Add(Options);
        CommandModule.DevProviders.Add((island, copy) =>
            [new DevToolsProvider(copy), new Modules.Ports.PortsProvider(), new DevSystemProvider(copy, m => island.Dispatcher.BeginInvoke(() => island.ShowToast(m)))]);

        var states = IslandWindow.SnapshotStates;
        Modules.SystemMonitor.SystemMonitorSnapshots.Register(states);
        Modules.Ports.PortsSnapshots.Register(states);
        Modules.Projects.ProjectsSnapshots.Register(states);
        Modules.Kube.KubeSnapshots.Register(states);
        Modules.Usage.UsageSnapshots.Register(states);
        states["logs-pill"] = w => w.Pill.Set(new PillItem { Id = "logs.match", Priority = PillPriority.Transient, Width = 300, Peek = true, View = () => Modules.Logs.LogWatchModule.PillView("app.log", "ERROR 500 POST /api/orders: NullReferenceException in OrderService.Create") });
        const string jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI0MiIsIm5hbWUiOiJBZGEiLCJleHAiOjE3NjcyMjU2MDB9.sig";
        states["command-dev-jwt"] = w => CommandSnapshots.Show(w, jwt, DevResults(jwt));
        states["command-dev-color"] = w => CommandSnapshots.Show(w, "#ff8800", DevResults("#ff8800"));
    }

    static IEnumerable<CommandItem> DevResults(string text) =>
        new DevToolsProvider(_ => { }, () => new DateTimeOffset(2026, 11, 21, 10, 0, 0, TimeSpan.Zero)).QueryAsync(text, default).Result;

    static void Options(SettingsView v, Action<string> group)
    {
        var s = Settings.Current;
        v.Row("set.cmdDev", v.OptSwitch("cmd.devTools", true, restart: "commands"), "set.cmdDev.hint");

        group("sysmon");
        v.Row("set.sysmonAlerts", v.OptSwitch("sysmon.alerts", true, restart: "sysmon"), "set.sysmonAlerts.hint");

        group("ports");
        v.Row("set.portsNotify", v.OptSwitch("ports.notify", true, restart: "ports"), "set.portsNotify.hint");

        group("projects");
        v.Row("set.projectsPaths", SettingsView.TextField(s.Opt("projects.paths"), t => { s.SetOpt("projects.paths", t.Trim()); v.Save("Options", restart: "projects"); }, @"C:\Users\me\Projects\app"), "set.projectsPaths.hint", wide: true);
        v.Row("set.projectsBuilds", v.OptSwitch("projects.builds", true, restart: "projects"), "set.projectsBuilds.hint");
        v.Row("set.projectsToken", v.SecretField("projects.githubToken", restart: "projects"), "set.projectsToken.hint");

        group("logs");
        v.Row("set.logsFiles", SettingsView.TextField(s.Opt("logs.files"), t => { s.SetOpt("logs.files", t.Trim()); v.Save("Options", restart: "logs"); }, @"C:\Users\me\Projects\api\logs\app.log"), "set.logsFiles.hint", wide: true);
        v.Row("set.logsPattern", SettingsView.TextField(s.Opt("logs.pattern", Modules.Logs.LogWatchModule.DefaultPattern), t => { s.SetOpt("logs.pattern", t.Trim()); v.Save("Options", restart: "logs"); }, Modules.Logs.LogWatchModule.DefaultPattern), "set.logsPattern.hint", wide: true);

        group("kube");
        v.Row("set.kubePattern", SettingsView.TextField(s.Opt("kube.prodPattern", Modules.Kube.KubeConfig.DefaultPattern), t => { s.SetOpt("kube.prodPattern", t.Trim()); v.Save("Options", restart: "kube"); }, Modules.Kube.KubeConfig.DefaultPattern), "set.kubePattern.hint", wide: true);
    }
}
