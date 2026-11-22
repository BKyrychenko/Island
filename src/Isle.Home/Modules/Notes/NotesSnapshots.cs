using Isle.Core;
using Isle.Views.Notes;

namespace Isle.Modules.Notes;

/// <summary>--snapshot states for notes (memory-only store with demo content).</summary>
static class NotesSnapshots
{
    static readonly DateTimeOffset Now = new(2026, 3, 11, 10, 0, 0, TimeSpan.Zero);

    public static void Register(Dictionary<string, Action<IslandWindow>> states)
    {
        states["notes-tab"] = w => Tab(w, Demo());
        states["notes-empty"] = w => Tab(w, new NotesStore(null, () => Now));
        states["notes-toast"] = w =>
        {
            Tab(w, Demo());
            w.ShowToast(Strings.T("notes.deleted"), Strings.T("common.undo"));
        };
    }

    static NotesStore Demo()
    {
        var uk = Strings.Language == "uk";
        var t = Now;
        var s = new NotesStore(null, () => t = t.AddMinutes(1));
        var groceries = s.Create("", uk ? "Список покупок\n☒ Молоко\n☐ Хліб\n☐ Яблука" : "Groceries\n☒ Milk\n☐ Bread\n☐ Apples",
            Xaml(uk ? "Список покупок" : "Groceries", uk ? ["☒ Молоко", "☐ Хліб", "☐ Яблука"] : ["☒ Milk", "☐ Bread", "☐ Apples"]));
        var ideas = s.Create(uk ? "Робота" : "Work", uk ? "Ідеї для презентації\nКоротко про результати кварталу" : "Talk ideas\nShort intro about the quarter");
        s.SetPinned(ideas.Id, true);
        Settings.Current.SetOpt("notes.last", groceries.Id); // in memory only (snapshot)
        s.Create(uk ? "Робота" : "Work", uk ? "Нотатки зустрічі\nОбговорили план релізу" : "Meeting notes\nDiscussed the release plan");
        return s;
    }

    /// <summary>A small formatted document: heading + checkbox lines.</summary>
    static string Xaml(string heading, string[] lines)
    {
        var sb = new System.Text.StringBuilder("<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">");
        sb.Append($"<Paragraph FontSize=\"18\" FontWeight=\"Bold\"><Run>{heading}</Run></Paragraph>");
        foreach (var l in lines) sb.Append($"<Paragraph><Run>{l}</Run></Paragraph>");
        sb.Append("<Paragraph>Bold <Bold>text</Bold>, <Italic>italic</Italic> and plain text.</Paragraph>");
        sb.Append("</Section>");
        return sb.ToString();
    }

    static void Tab(IslandWindow w, NotesStore s)
    {
        w.AddTab(new TabInfo("music", "tab.music", "Icon.Music", () => new System.Windows.Controls.Grid(), 10));
        w.AddTab(new TabInfo("timers", "tab.timers", "Icon.Timer", () => new System.Windows.Controls.Grid(), 20));
        w.AddTab(new TabInfo("notes", "tab.notes", "Icon.Notes", () => new NotesView(s, null), 30));
        w.Expand("notes");
    }
}
