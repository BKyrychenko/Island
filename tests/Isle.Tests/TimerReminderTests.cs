using Isle.Modules.Reminders;
using Isle.Modules.Timers;

namespace Isle.Tests;

public class ReminderParserTests
{
    // Wednesday 11 March 2026, 10:00 (UTC keeps the tests independent of the machine's time zone)
    static readonly DateTimeOffset Now = new(2026, 3, 11, 10, 0, 0, TimeSpan.Zero);
    static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;

    static ParsedReminder P(string phrase) => ReminderParser.Parse(phrase, Now, Tz) ?? throw new Xunit.Sdk.XunitException($"not parsed: {phrase}");

    [Theory]
    // ---- English ----
    [InlineData("remind me in 2 hours to call mom", "2026-03-11 12:00", "call mom")]
    [InlineData("in 30 minutes check oven", "2026-03-11 10:30", "check oven")]
    [InlineData("remind me in 1h30m to stretch", "2026-03-11 11:30", "stretch")]
    [InlineData("in an hour take pills", "2026-03-11 11:00", "take pills")]
    [InlineData("in half an hour call back", "2026-03-11 10:30", "call back")]
    [InlineData("in 45 seconds tea", "2026-03-11 10:00:45", "tea")]
    [InlineData("in 3 days renew passport", "2026-03-14 10:00", "renew passport")]
    [InlineData("in 2 hours and 15 minutes leave", "2026-03-11 12:15", "leave")]
    [InlineData("tomorrow 9:00 submit report", "2026-03-12 09:00", "submit report")]
    [InlineData("tomorrow at 5pm dentist", "2026-03-12 17:00", "dentist")]
    [InlineData("at 3pm meeting with Anna", "2026-03-11 15:00", "meeting with Anna")]
    [InlineData("at 9 call bank", "2026-03-11 21:00", "call bank")]
    [InlineData("on friday at 18:00 buy flowers", "2026-03-13 18:00", "buy flowers")]
    [InlineData("next monday standup", "2026-03-16 09:00", "standup")]
    [InlineData("on 25.12 gifts", "2026-12-25 09:00", "gifts")]
    [InlineData("december 31 at 23:00 party", "2026-12-31 23:00", "party")]
    [InlineData("2026-04-01 10:30 release", "2026-04-01 10:30", "release")]
    [InlineData("tonight watch the game", "2026-03-11 20:00", "watch the game")]
    [InlineData("tomorrow morning run", "2026-03-12 09:00", "run")]
    [InlineData("remind me to call John at noon", "2026-03-11 12:00", "call John")]
    [InlineData("remind me at 7 in the evening to cook", "2026-03-11 19:00", "cook")]
    [InlineData("the day after tomorrow at 10 haircut", "2026-03-13 10:00", "haircut")]
    [InlineData("friday at 5 drinks", "2026-03-13 17:00", "drinks")]
    [InlineData("on the 20th pay taxes", "2026-03-20 09:00", "pay taxes")]
    [InlineData("call grandma on March 2", "2027-03-02 09:00", "call grandma")]
    // ---- Ukrainian ----
    [InlineData("нагадай через 2 години подзвонити", "2026-03-11 12:00", "подзвонити")]
    [InlineData("через 15 хвилин вимкнути духовку", "2026-03-11 10:15", "вимкнути духовку")]
    [InlineData("через годину забрати посилку", "2026-03-11 11:00", "забрати посилку")]
    [InlineData("через пів години зателефонувати", "2026-03-11 10:30", "зателефонувати")]
    [InlineData("через 1 год 30 хв розминка", "2026-03-11 11:30", "розминка")]
    [InlineData("за 10 хвилин вийти з дому", "2026-03-11 10:10", "вийти з дому")]
    [InlineData("завтра о 9:00 здати звіт", "2026-03-12 09:00", "здати звіт")]
    [InlineData("нагадай мені завтра о 18:30 купити хліб", "2026-03-12 18:30", "купити хліб")]
    [InlineData("у п'ятницю о 19:00 кіно", "2026-03-13 19:00", "кіно")]
    [InlineData("у пʼятницю о 19:00 кіно", "2026-03-13 19:00", "кіно")]
    [InlineData("25 грудня привітати маму", "2026-12-25 09:00", "привітати маму")]
    [InlineData("післязавтра о 10 перукар", "2026-03-13 10:00", "перукар")]
    [InlineData("сьогодні ввечері прибрати", "2026-03-11 19:00", "прибрати")]
    [InlineData("о 7 вечора вечеря", "2026-03-11 19:00", "вечеря")]
    [InlineData("нагадай о 23:15 вимкнути світло", "2026-03-11 23:15", "вимкнути світло")]
    [InlineData("в наступний понеділок нарада", "2026-03-16 09:00", "нарада")]
    [InlineData("завтра вранці пробіжка", "2026-03-12 09:00", "пробіжка")]
    [InlineData("1 квітня о 12:00 жарт", "2026-04-01 12:00", "жарт")]
    public void One_off(string phrase, string due, string text)
    {
        var p = P(phrase);
        Assert.Equal(due.Length == 16 ? due + ":00" : due, p.Due.ToString("yyyy-MM-dd HH:mm:ss"));
        Assert.Equal(text, p.Text);
        Assert.Null(p.Repeat);
    }

    public static TheoryData<string, string, string, RepeatRule> Repeats => new()
    {
        { "every monday 08:30 gym", "2026-03-16 08:30", "gym", RepeatRule.Weekly(DayOfWeek.Monday) },
        { "every day at 22:00 drink water", "2026-03-11 22:00", "drink water", RepeatRule.Daily() },
        { "daily 7am vitamins", "2026-03-12 07:00", "vitamins", RepeatRule.Daily() },
        { "every 2 hours stand up", "2026-03-11 12:00", "stand up", new RepeatRule { Kind = RepeatKind.EveryNHours, Interval = 2 } },
        { "every weekday at 9:30 standup", "2026-03-12 09:30", "standup", RepeatRule.Weekly(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday) },
        { "every month on the 1st pay rent", "2026-04-01 09:00", "pay rent", new RepeatRule { Kind = RepeatKind.Monthly, DayOfMonth = 1 } },
        { "every monday and thursday at 19:00 football", "2026-03-12 19:00", "football", RepeatRule.Weekly(DayOfWeek.Monday, DayOfWeek.Thursday) },
        { "every other day water plants", "2026-03-12 09:00", "water plants", RepeatRule.Daily(2) },
        { "every 30 minutes look away from the screen", "2026-03-11 10:30", "look away from the screen", new RepeatRule { Kind = RepeatKind.EveryNMinutes, Interval = 30 } },
        { "щодня о 22:00 пити воду", "2026-03-11 22:00", "пити воду", RepeatRule.Daily() },
        { "щопонеділка о 8:30 спортзал", "2026-03-16 08:30", "спортзал", RepeatRule.Weekly(DayOfWeek.Monday) },
        { "кожного вівторка та четверга о 18:00 англійська", "2026-03-12 18:00", "англійська", RepeatRule.Weekly(DayOfWeek.Tuesday, DayOfWeek.Thursday) },
        { "кожні 3 дні полити квіти", "2026-03-12 09:00", "полити квіти", RepeatRule.Daily(3) },
        { "щомісяця 5 числа сплатити рахунки", "2026-04-05 09:00", "сплатити рахунки", new RepeatRule { Kind = RepeatKind.Monthly, DayOfMonth = 5 } },
        { "по буднях о 9:30 стендап", "2026-03-12 09:30", "стендап", RepeatRule.Weekly(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday) },
        { "кожні 2 години пити воду", "2026-03-11 12:00", "пити воду", new RepeatRule { Kind = RepeatKind.EveryNHours, Interval = 2 } },
        { "щоранку зарядка", "2026-03-12 09:00", "зарядка", RepeatRule.Daily() },
        { "кожен день о 21:00 читати", "2026-03-11 21:00", "читати", RepeatRule.Daily() },
        { "щоп'ятниці о 17:00 звіт за тиждень", "2026-03-13 17:00", "звіт за тиждень", RepeatRule.Weekly(DayOfWeek.Friday) },
    };

    [Theory]
    [MemberData(nameof(Repeats))]
    public void Repeating(string phrase, string due, string text, RepeatRule repeat)
    {
        var p = P(phrase);
        Assert.Equal(due, p.Due.ToString("yyyy-MM-dd HH:mm"));
        Assert.Equal(text, p.Text);
        Assert.NotNull(p.Repeat);
        Assert.Equal(repeat.Kind, p.Repeat!.Kind);
        Assert.Equal(repeat.Interval, p.Repeat.Interval);
        Assert.Equal(repeat.DayOfMonth, p.Repeat.DayOfMonth);
        Assert.Equal(repeat.Days.OrderBy(d => d), p.Repeat.Days.OrderBy(d => d));
    }

    [Theory]
    [InlineData("")]
    [InlineData("buy milk")]
    [InlineData("купити молоко")]
    [InlineData("meeting notes")]
    public void Not_a_reminder(string phrase) => Assert.Null(ReminderParser.Parse(phrase, Now, Tz));

    [Fact]
    public void Empty_text_is_allowed() => Assert.Equal("", P("in 5 minutes").Text);
}

public class RepeatRuleTests
{
    static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;
    static DateTimeOffset D(string s) => DateTimeOffset.Parse(s + "Z", System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void Daily_and_every_n_days()
    {
        Assert.Equal(D("2026-03-12T08:00"), RepeatRule.Daily().Next(D("2026-03-11T08:00"), D("2026-03-11T08:00"), Tz));
        Assert.Equal(D("2026-03-20T08:00"), RepeatRule.Daily().Next(D("2026-03-11T08:00"), D("2026-03-19T09:00"), Tz));
        Assert.Equal(D("2026-03-17T08:00"), RepeatRule.Daily(3).Next(D("2026-03-11T08:00"), D("2026-03-15T00:00"), Tz));
    }

    [Fact]
    public void Weekly_days()
    {
        var r = RepeatRule.Weekly(DayOfWeek.Monday, DayOfWeek.Thursday);
        Assert.Equal(D("2026-03-12T19:00"), r.Next(D("2026-03-09T19:00"), D("2026-03-09T19:00"), Tz)); // Mon → Thu
        Assert.Equal(D("2026-03-16T19:00"), r.Next(D("2026-03-12T19:00"), D("2026-03-12T19:00"), Tz)); // Thu → Mon
    }

    [Fact]
    public void Monthly_clamps_to_month_length()
    {
        var r = new RepeatRule { Kind = RepeatKind.Monthly, DayOfMonth = 31 };
        Assert.Equal(D("2026-02-28T09:00"), r.Next(D("2026-01-31T09:00"), D("2026-01-31T09:00"), Tz));
        Assert.Equal(D("2026-03-31T09:00"), r.Next(D("2026-02-28T09:00"), D("2026-02-28T09:00"), Tz));
    }

    [Fact]
    public void Every_n_hours_skips_missed_slots()
    {
        var r = new RepeatRule { Kind = RepeatKind.EveryNHours, Interval = 2 };
        Assert.Equal(D("2026-03-11T16:00"), r.Next(D("2026-03-11T10:00"), D("2026-03-11T15:30"), Tz));
    }
}

public class ReminderBookTests
{
    [Fact]
    public void Due_done_snooze_and_missed()
    {
        var now = new DateTimeOffset(2026, 3, 11, 10, 0, 0, TimeSpan.Zero);
        var book = new ReminderBook(new ReminderDoc(), () => now, TimeZoneInfo.Utc);
        var once = book.Add(ReminderParser.Parse("in 1 minute tea", now, TimeZoneInfo.Utc)!, "in 1 minute tea");
        var daily = book.Add(ReminderParser.Parse("every day at 9:00 standup", now, TimeZoneInfo.Utc)!, "every day at 9:00 standup");
        Assert.Empty(book.CollectDue());

        now = now.AddHours(30); // app was closed: both are missed and fire at once
        var due = book.CollectDue();
        Assert.Equal(2, due.Count);
        Assert.Empty(book.CollectDue()); // not twice

        book.Done(once.Id);
        Assert.Null(book.Find(once.Id));
        book.Done(daily.Id);
        Assert.Equal(new DateTimeOffset(2026, 3, 13, 9, 0, 0, TimeSpan.Zero), book.Find(daily.Id)!.Due);

        var r = book.Add(ReminderParser.Parse("in 5 minutes x", now, TimeZoneInfo.Utc)!, "x");
        now = now.AddMinutes(6);
        book.CollectDue();
        book.Snooze(r.Id, TimeSpan.FromMinutes(5));
        Assert.Equal(now.AddMinutes(5), r.Due);
        Assert.Null(r.FiredAt);
        book.SnoozeTomorrow(r.Id);
        Assert.Equal(now.Date.AddDays(1), r.Due.Date);
    }
}

public class DurationParserTests
{
    [Theory]
    [InlineData("90", 90 * 60, "")]
    [InlineData("1:30", 90, "")]
    [InlineData("1:02:03", 3723, "")]
    [InlineData("1h30m", 5400, "")]
    [InlineData("45s", 45, "")]
    [InlineData("2m 30s", 150, "")]
    [InlineData("1.5h", 5400, "")]
    [InlineData("25 min focus", 1500, "focus")]
    [InlineData("10 хв чай", 600, "чай")]
    [InlineData("1 год 30 хв", 5400, "")]
    [InlineData("5 — pasta", 300, "pasta")]
    [InlineData(" 3 minutes ", 180, "")]
    public void Parses(string text, int seconds, string label)
    {
        Assert.True(DurationParser.TryParse(text, out var d, out var l), text);
        Assert.Equal(TimeSpan.FromSeconds(seconds), d);
        Assert.Equal(label, l);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("1:75")]
    [InlineData("200h")]
    [InlineData("tomorrow 9:00 report")]
    public void Rejects(string text) => Assert.False(DurationParser.TryParse(text, out _));

    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(59.2, "1:00")]
    [InlineData(65, "1:05")]
    [InlineData(3723, "1:02:03")]
    public void Formats(double seconds, string expected) => Assert.Equal(expected, DurationParser.Format(TimeSpan.FromSeconds(seconds)));
}

public class TimerBookTests
{
    [Fact]
    public void Run_pause_resume_finish_restart()
    {
        var now = new DateTimeOffset(2026, 3, 11, 10, 0, 0, TimeSpan.Zero);
        var book = new TimerBook(new TimerDoc(), () => now);
        var t = book.Add(TimeSpan.FromMinutes(10), "tea")!;
        var u = book.Add(TimeSpan.FromMinutes(5))!;
        Assert.Equal(u, book.Soonest);

        now = now.AddMinutes(2);
        Assert.Equal(TimeSpan.FromMinutes(8), t.Remaining(now));
        Assert.True(book.Pause(t.Id));
        now = now.AddMinutes(30);
        Assert.Equal(TimeSpan.FromMinutes(8), t.Remaining(now)); // frozen while paused
        Assert.Equal([u], book.CollectFinished());
        Assert.True(book.Resume(t.Id));
        Assert.Equal(now.AddMinutes(8), t.EndsAt);

        Assert.True(book.AddTime(u.Id, TimeSpan.FromMinutes(1))); // +1 min on a finished timer restarts it
        Assert.True(u.IsRunning);
        Assert.Equal(now.AddMinutes(1), u.EndsAt);
        Assert.Equal(u, book.Soonest);

        now = now.AddMinutes(10);
        Assert.Equal(2, book.CollectFinished().Count);
        Assert.True(book.Dismiss(t.Id));
        Assert.False(book.Cancel(t.Id));
    }

    [Fact]
    public void Survives_restart_through_json()
    {
        var now = new DateTimeOffset(2026, 3, 11, 10, 0, 0, TimeSpan.Zero);
        using var dir = new TempDir();
        var path = dir.File("timers.json");
        var book = new TimerBook(new TimerDoc(), () => now);
        book.Add(TimeSpan.FromMinutes(1), "eggs");
        book.StopwatchStart();
        Assert.True(Isle.Core.JsonStore.Save(path, book.Doc));

        now = now.AddMinutes(5); // closed for 5 minutes
        var again = new TimerBook(Isle.Core.JsonStore.Load<TimerDoc>(path), () => now);
        var finished = again.CollectFinished();
        Assert.Single(finished);
        Assert.Equal("eggs", finished[0].Label);
        Assert.Equal(TimeSpan.FromMinutes(5), again.Stopwatch.Elapsed(now));
        again.StopwatchPause();
        now = now.AddMinutes(1);
        Assert.Equal(TimeSpan.FromMinutes(5), again.Stopwatch.Elapsed(now));
    }
}
