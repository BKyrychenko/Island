using System.Windows.Input;
using Isle.Core;
using Isle.Modules.Hotkeys;
using Isle.Views.SettingsPanel;

namespace Isle.Tests;

public class SettingsPanelTests
{
    [Theory]
    [InlineData(ModifierKeys.Alt, false, 0x4B, "Alt+K")]
    [InlineData(ModifierKeys.Control | ModifierKeys.Shift, true, 0x46, "Ctrl+Win+Shift+F")]
    [InlineData(ModifierKeys.None, true, 0x20, "Win+Space")]
    [InlineData(ModifierKeys.None, false, 0x74, "F5")]
    [InlineData(ModifierKeys.Shift, false, 0x77, "Shift+F8")]
    [InlineData(ModifierKeys.Alt, false, 0xBC, "Alt+,")]
    public void Key_press_becomes_gesture_text(ModifierKeys mods, bool win, uint vk, string expected)
    {
        Assert.Equal(expected, SettingsView.HotkeyText(mods, win, vk));
        Assert.True(Gesture.TryParse(expected, out var g));
        Assert.Equal(vk, g.Key);
    }

    [Theory]
    [InlineData(ModifierKeys.None, false, 0x4B)]   // bare letter would swallow typing
    [InlineData(ModifierKeys.Shift, false, 0x4B)]  // shifted letter too
    [InlineData(ModifierKeys.Alt, false, 0xFF)]    // unsupported key
    public void Unusable_key_presses_are_rejected(ModifierKeys mods, bool win, uint vk) =>
        Assert.Null(SettingsView.HotkeyText(mods, win, vk));

    [Fact]
    public void Every_listed_module_and_live_source_has_labels_in_both_languages()
    {
        var keys = SettingsView.ModuleIds.SelectMany(id => new[] { "mod." + id, "mod." + id + ".hint" })
            .Concat(Modules.LiveEvents.LiveEventsModule.SourceNames.Select(n => "set.live." + n))
            .Concat(SettingsView.Sections.Select(s => "set." + s));
        foreach (var k in keys)
        {
            Assert.True(Strings.English.ContainsKey(k), k);
            Assert.True(Strings.Ukrainian.ContainsKey(k), k);
        }
    }

    [Fact]
    public void Every_section_builds_in_both_languages()
    {
        Exception? error = null;
        var t = new Thread(() =>
        {
            try
            {
                var rebinds = new List<(string, string?)>();
                var v = new SettingsView(new SettingsActions(
                    Hotkeys: () => [new HotkeyInfo("toggle", "hotkey.toggle", "Win+Alt+Space", "Win+Alt+Space", true), new HotkeyInfo("voice", "hotkey.voice", "", "Win+Alt+V", false)],
                    Rebind: (id, g) => { rebinds.Add((id, g)); return true; },
                    SuspendHotkeys: _ => { },
                    StateOf: _ => ModuleState.Running,
                    SyncModules: () => { }, RestartModule: _ => { }, Open: _ => { }, Close: () => { }));
                foreach (var lang in new[] { "en", "uk" })
                {
                    Strings.Language = lang;
                    foreach (var s in SettingsView.Sections)
                    {
                        v.ShowSection(s);
                        Assert.Equal(s, v.Section);
                    }
                }
                v.ShowSection("nonsense");
                Assert.Equal("general", v.Section);
            }
            catch (Exception ex) { error = ex; }
            finally { Strings.Language = "en"; }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
        if (error is not null) throw new Exception("STA test failed", error);
    }
}
