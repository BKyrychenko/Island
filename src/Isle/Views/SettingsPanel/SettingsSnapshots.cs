using Isle.Core;
using Isle.Modules.Hotkeys;

namespace Isle.Views.SettingsPanel;

/// <summary>--snapshot states for the settings panel (one per section, plus a scrolled options page).</summary>
static class SettingsSnapshots
{
    static readonly HotkeyInfo[] DemoHotkeys =
    [
        new("toggle", "hotkey.toggle", "Win+Alt+Space", "Win+Alt+Space", true),
        new("commandBar", "hotkey.commandBar", "Ctrl+Alt+K", "Alt+Space", true),
        new("newNote", "hotkey.newNote", "Win+Alt+N", "Win+Alt+N", true),
        new("textActions", "hotkey.textActions", "Win+Alt+A", "Win+Alt+A", false),
        new("voice", "hotkey.voice", "", "Win+Alt+V", false),
        new("screenshot", "hotkey.screenshot", "Win+Alt+S", "Win+Alt+S", true),
    ];

    static SettingsView Make() => new(new SettingsActions(
        Hotkeys: () => DemoHotkeys, Rebind: (_, _) => true, SuspendHotkeys: _ => { },
        StateOf: id => id == "voice" ? ModuleState.Failed : ModuleState.Running,
        SyncModules: () => { }, RestartModule: _ => { }, Open: _ => { }, Close: () => { }));

    public static void Register(Dictionary<string, Action<IslandWindow>> states)
    {
        foreach (var section in SettingsView.Sections)
            states["settings-" + section] = w =>
            {
                if (section == "modules") Settings.Current.Modules["weather"] = false;
                if (section == "options") Settings.Current.SetOpt("weather.city", "Kyiv");
                var v = Make();
                v.ShowSection(section);
                w.ShowOverlay(v);
            };
        states["settings-options-end"] = w =>
        {
            var v = Make();
            v.ShowSection("options");
            w.ShowOverlay(v);
            v.ScrollToEnd();
        };
    }
}
