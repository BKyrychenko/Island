using Skerry.Views.Commands;

namespace Skerry.Modules.Commands;

/// <summary>--snapshot states for the command bar (fixed results; nothing is indexed or fetched).</summary>
static class CommandSnapshots
{
    public static void Register(Dictionary<string, Action<IslandWindow>> states)
    {
        states["command-calc"] = w => Show(w, "(1 250 + 15% of 80) / 3", Calculate("(1 250 + 15% of 80) / 3"));
        states["command-units"] = w => Show(w, "10 km to mi", Calculate("10 km to mi"));
        states["command-apps"] = w =>
        {
            var win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            Show(w, "note", [
                new CommandItem { Title = "Notepad", Subtitle = Core.Strings.T("cmd.app"), IconPath = Path.Combine(win, "notepad.exe"), Score = 990 },
                new CommandItem { Title = "Notes — weekly plan.docx", Subtitle = Core.Strings.T("cmd.recent"), IconPath = Path.Combine(win, "win.ini"), Score = 800 },
                new CommandItem { Title = Core.Strings.T("cmd.searchWeb", "note"), Subtitle = "google.com", IconKey = "Icon.Search", Score = 5 },
            ]);
        };
        states["command-currency"] = w => Show(w, "100 usd to uah", [
            new CommandItem { Title = "100 USD = 4,150.00 UAH", Subtitle = Core.Strings.T("cmd.rateOf", "14 Nov 2026") + " · " + Core.Strings.T("cmd.copyResult"), IconKey = "Icon.Currency", Score = 4900 },
        ]);
        states["command-system"] = w => Show(w, "shut", new SystemProvider().QueryAsync("shut", default).Result);
        states["command-empty"] = w => Show(w, "", []);
    }

    static IEnumerable<CommandItem> Calculate(string text) => new CalculatorProvider(_ => { }).QueryAsync(text, default).Result;

    static void Show(IslandWindow w, string text, IEnumerable<CommandItem> items)
    {
        var v = new CommandBarView([], _ => { }, w);
        v.SetResultsForSnapshot(text, items);
        w.AddTab(new Core.TabInfo("music", "tab.music", "Icon.Music", () => new System.Windows.Controls.Grid(), 10));
        w.ShowOverlay(v);
    }
}
