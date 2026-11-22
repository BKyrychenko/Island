using Isle.Core;
using Isle.Views.Timers;

namespace Isle.Modules.Timers;

/// <summary>--snapshot states for timers and reminders (fixed clock, no persistence).</summary>
static class TimersSnapshots
{
    static readonly DateTimeOffset Now = new(2026, 3, 11, 10, 0, 0, TimeSpan.FromHours(1));

    public static void Register(Dictionary<string, Action<IslandWindow>> states)
    {
        states["timer-pill"] = w => Pill(w, Demo());
        states["timer-shared"] = w =>
        {
            var c = Demo();
            var pill = new TimerPill(c);
            pill.Show(c.Timers.Soonest!.Id);
            var media = Media.FakeMediaSource.Demo(Now);
            var mp = new Views.Music.MusicPill(new Media.FakeMediaSource { Current = media }, () => null, () => Now);
            mp.Update(media);
            Theme.SetAccent(media.Accent);
            w.Pill.Set(new PillItem { Id = "music", Priority = PillPriority.Music, View = () => mp, Compact = () => mp.CreateCompact(), CompactWidth = 40, Width = 168 });
            w.Pill.Set(new PillItem { Id = "timer", Priority = PillPriority.TimerRunning, View = () => pill, Width = TimerPill.PillWidth });
        };
        states["timer-done"] = w =>
        {
            var c = Demo();
            var t = c.Timers.Timers[1];
            w.Pill.Set(new PillItem { Id = "d", Priority = PillPriority.Finished, View = () => new TimerDoneBanner(c, t), Width = TimerDoneBanner.BannerWidth, Height = TimerDoneBanner.BannerHeight, IsBanner = true });
        };
        states["reminder-banner"] = w =>
        {
            var c = Demo();
            var r = c.Reminders.Reminders[0];
            w.Pill.Set(new PillItem { Id = "r", Priority = PillPriority.Finished, View = () => new ReminderBanner(c, r), Width = ReminderBanner.BannerWidth, Height = ReminderBanner.BannerHeight, IsBanner = true });
        };
        states["timers-tab"] = w => Tab(w, Demo());
        states["timers-empty"] = w => Tab(w, new TimersController(() => Now, persist: false));
        states["timers-invalid"] = w =>
        {
            Tab(w, new TimersController(() => Now, persist: false));
            if (w.TabViewFor("timers") is TimersView v) v.ShowInvalidForSnapshot("buy milk");
        };
    }

    static TimersController Demo()
    {
        var c = new TimersController(() => Now, TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"), persist: false);
        var uk = Strings.Language == "uk";
        c.AddTimer(TimeSpan.FromMinutes(25), uk ? "Фокус" : "Focus");
        c.AddTimer(TimeSpan.FromMinutes(12) + TimeSpan.FromSeconds(30), uk ? "Паста" : "Pasta");
        c.Pause(c.Timers.Timers[0].Id);
        c.Timers.StopwatchStart();
        c.AddReminder(uk ? "завтра о 9:00 здати звіт" : "tomorrow 9:00 submit report");
        c.AddReminder(uk ? "щопонеділка о 8:30 спортзал" : "every monday 08:30 gym");
        c.AddReminder(uk ? "через 2 години подзвонити мамі" : "in 2 hours call mom");
        return c;
    }

    static void Pill(IslandWindow w, TimersController c)
    {
        var pill = new TimerPill(c);
        pill.Show(c.Timers.Soonest!.Id);
        w.Pill.Set(new PillItem { Id = "timer", Priority = PillPriority.TimerRunning, View = () => pill, Width = TimerPill.PillWidth });
    }

    static void Tab(IslandWindow w, TimersController c)
    {
        w.AddTab(new TabInfo("music", "tab.music", "Icon.Music", () => new System.Windows.Controls.Grid(), 10));
        w.AddTab(new TabInfo("timers", "tab.timers", "Icon.Timer", () => new TimersView(c), 20));
        w.AddTab(new TabInfo("notes", "tab.notes", "Icon.Notes", () => new System.Windows.Controls.Grid(), 30));
        w.Expand("timers");
    }
}
