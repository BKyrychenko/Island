using Isle.Core;
using Isle.Views.Tools;

namespace Isle.Modules.Tools;

/// <summary>--snapshot states for the Tools tab (fixed results).</summary>
static class ToolsSnapshots
{
    public static void Register(Dictionary<string, Action<IslandWindow>> states)
    {
        states["tools-tab"] = w => Tab(w, v => v.ShowColors(["#129AE3", "#FF4D4D", "#34C759", "#FFC81F", "#1F2020", "#F2F2F7"]));
        states["tools-tab-empty"] = w => Tab(w, v => v.ShowColors([]));
        states["tools-ocr"] = w => Tab(w, v => v.ShowText("Island OCR check 4721\nSecond line of recognised text", [("en-GB", "English (United Kingdom)"), ("uk-UA", "українська")], "en-GB"));
        states["tools-qr"] = w => Tab(w, v => v.ShowCode("https://example.com/isle-qr"));
        states["tools-qr-none"] = w => Tab(w, v => v.ShowCode(null));
    }

    static void Tab(IslandWindow w, Action<ToolsView> setup)
    {
        w.AddTab(new TabInfo("music", "tab.music", "Icon.Music", () => new System.Windows.Controls.Grid(), 10));
        w.AddTab(new TabInfo("tools", "tab.tools", "Icon.Tools", () =>
        {
            var v = new ToolsView(() => { }, () => { }, () => { }, () => { }, () => { }, _ => { }, _ => { }, _ => { });
            setup(v);
            return v;
        }, 70));
        w.Expand("tools");
    }
}
