namespace Isle.Modules.Reminders;

public enum RepeatKind { None, Daily, Weekly, Monthly, EveryNDays, EveryNHours, EveryNMinutes }

/// <summary>How a reminder repeats. Times of day come from the reminder's due time.</summary>
public sealed record RepeatRule
{
    public RepeatKind Kind { get; init; }
    public int Interval { get; init; } = 1;
    /// <summary>Weekly: the days (empty = the due date's weekday).</summary>
    public DayOfWeek[] Days { get; init; } = [];
    /// <summary>Monthly: day of month (1–31, clamped to the month length); 0 = the due date's day.</summary>
    public int DayOfMonth { get; init; }

    public static RepeatRule Daily(int every = 1) => new() { Kind = every == 1 ? RepeatKind.Daily : RepeatKind.EveryNDays, Interval = every };
    public static RepeatRule Weekly(params DayOfWeek[] days) => new() { Kind = RepeatKind.Weekly, Days = days };

    /// <summary>The first occurrence strictly after <paramref name="after"/>, keeping <paramref name="due"/>'s time of day (local wall clock of <paramref name="tz"/>).</summary>
    public DateTimeOffset Next(DateTimeOffset due, DateTimeOffset after, TimeZoneInfo tz)
    {
        var n = Math.Max(1, Interval);
        switch (Kind)
        {
            case RepeatKind.EveryNMinutes:
            case RepeatKind.EveryNHours:
            {
                var step = Kind == RepeatKind.EveryNHours ? TimeSpan.FromHours(n) : TimeSpan.FromMinutes(n);
                if (due > after) return due;
                var k = (long)Math.Floor((after - due) / step) + 1;
                return due + step * k;
            }
        }

        var local = TimeZoneInfo.ConvertTime(due, tz).DateTime;
        var afterLocal = TimeZoneInfo.ConvertTime(after, tz).DateTime;
        var time = local.TimeOfDay;
        DateTime candidate;
        switch (Kind)
        {
            case RepeatKind.Daily:
            case RepeatKind.EveryNDays:
            {
                var step = Kind == RepeatKind.Daily ? 1 : n;
                candidate = local;
                if (candidate <= afterLocal)
                {
                    var days = (int)Math.Floor((afterLocal.Date - local.Date).TotalDays / step) * step;
                    candidate = local.Date.AddDays(days) + time;
                    while (candidate <= afterLocal) candidate = candidate.AddDays(step);
                }
                break;
            }
            case RepeatKind.Weekly:
            {
                var days = Days.Length > 0 ? Days : [local.DayOfWeek];
                candidate = (afterLocal.Date > local.Date ? afterLocal.Date : local.Date) + time;
                for (var i = 0; i < 8 * n; i++, candidate = candidate.AddDays(1))
                    if (days.Contains(candidate.DayOfWeek) && candidate > afterLocal && candidate >= local) break;
                break;
            }
            case RepeatKind.Monthly:
            {
                var dom = DayOfMonth > 0 ? DayOfMonth : local.Day;
                var month = new DateTime(local.Year, local.Month, 1);
                candidate = OnDay(month, dom) + time;
                if (candidate < local) { month = month.AddMonths(1); candidate = OnDay(month, dom) + time; }
                while (candidate <= afterLocal) { month = month.AddMonths(n); candidate = OnDay(month, dom) + time; }
                break;
            }
            default:
                return due;
        }
        return ToOffset(candidate, tz);
    }

    static DateTime OnDay(DateTime month, int day) => new(month.Year, month.Month, Math.Min(day, DateTime.DaysInMonth(month.Year, month.Month)));

    public static DateTimeOffset ToOffset(DateTime local, TimeZoneInfo tz)
    {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (tz.IsInvalidTime(local)) local = local.AddHours(1); // skipped by a DST jump
        return new DateTimeOffset(local, tz.GetUtcOffset(local));
    }
}

/// <summary>A reminder. <see cref="Source"/> keeps the phrase the user typed (for editing).</summary>
public sealed class Reminder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..10];
    public string Text { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTimeOffset Due { get; set; }
    public RepeatRule? Repeat { get; set; }
    /// <summary>Original repeat schedule, kept separate from the snoozed due time. Older documents fall back to Due.</summary>
    public DateTimeOffset? RepeatAnchor { get; set; }
    /// <summary>Set while the reminder is showing (fired, waiting for Done/Snooze).</summary>
    public DateTimeOffset? FiredAt { get; set; }
}

public sealed class ReminderDoc
{
    public List<Reminder> Reminders { get; set; } = [];
}

/// <summary>Pure reminder operations with an injectable clock.</summary>
public sealed class ReminderBook(ReminderDoc doc, Func<DateTimeOffset> now, TimeZoneInfo? tz = null)
{
    readonly TimeZoneInfo _tz = tz ?? TimeZoneInfo.Local;
    public ReminderDoc Doc { get; } = doc;
    public IReadOnlyList<Reminder> Reminders => Doc.Reminders;

    public Reminder Add(ParsedReminder p, string source)
    {
        var r = new Reminder { Text = p.Text, Source = source.Trim(), Due = p.Due, Repeat = p.Repeat };
        Doc.Reminders.Add(r);
        return r;
    }

    public Reminder? Find(string id) => Doc.Reminders.FirstOrDefault(r => r.Id == id);

    public bool Replace(string id, ParsedReminder p, string source)
    {
        if (Find(id) is not { } r) return false;
        r.Text = p.Text; r.Source = source.Trim(); r.Due = p.Due; r.Repeat = p.Repeat; r.RepeatAnchor = null; r.FiredAt = null;
        return true;
    }

    public bool Delete(string id) => Doc.Reminders.RemoveAll(r => r.Id == id) > 0;

    /// <summary>Reminders due now (including ones missed while the app was closed), marked as fired.</summary>
    public List<Reminder> CollectDue()
    {
        var n = now();
        var list = new List<Reminder>();
        foreach (var r in Doc.Reminders)
        {
            if (r.FiredAt is null && r.Due <= n)
            {
                r.FiredAt = n;
                list.Add(r);
            }
        }
        return list;
    }

    public IEnumerable<Reminder> Showing => Doc.Reminders.Where(r => r.FiredAt is not null);

    /// <summary>Done: one-off reminders are removed; repeating ones move to their next occurrence.</summary>
    public void Done(string id)
    {
        if (Find(id) is not { } r) return;
        if (r.Repeat is null) { Delete(id); return; }
        r.RepeatAnchor ??= r.Due;
        r.Due = r.Repeat.Next(r.RepeatAnchor.Value, now(), _tz);
        r.FiredAt = null;
    }

    /// <summary>Snooze: shows again after <paramref name="delay"/> (the repeat schedule continues from the original time).</summary>
    public void Snooze(string id, TimeSpan delay)
    {
        if (Find(id) is not { } r) return;
        if (r.Repeat is not null) r.RepeatAnchor ??= r.Due;
        r.Due = now() + delay;
        r.FiredAt = null;
    }

    /// <summary>Snooze until tomorrow at the same time of day.</summary>
    public void SnoozeTomorrow(string id)
    {
        if (Find(id) is not { } r) return;
        if (r.Repeat is not null) r.RepeatAnchor ??= r.Due;
        var local = TimeZoneInfo.ConvertTime(now(), _tz).DateTime;
        var original = TimeZoneInfo.ConvertTime(r.RepeatAnchor ?? r.Due, _tz).DateTime.TimeOfDay;
        r.Due = RepeatRule.ToOffset(local.Date.AddDays(1) + original, _tz);
        r.FiredAt = null;
    }

    public DateTimeOffset? NextDue => Doc.Reminders.Where(r => r.FiredAt is null).Select(r => (DateTimeOffset?)r.Due).Min();

    public IEnumerable<Reminder> Upcoming => Doc.Reminders.OrderBy(r => r.FiredAt is null).ThenBy(r => r.Due);
}
