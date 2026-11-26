using System.Text.Json;
using Skerry.Core;
using Skerry.Modules.Reminders;

namespace Skerry.Modules.Timers;

/// <summary>
/// Timers + reminders state and every user operation, with persistence (timers.json, reminders.json).
/// No UI and no timers of its own; <see cref="TimersModule"/> schedules, views call the operations.
/// </summary>
public sealed class TimersController
{
    readonly bool _persist;
    readonly string _timersPath, _remindersPath;
    string _savedTimers, _savedReminders;
    public bool LastSaveFailed { get; private set; }
    public event Action? SaveFailed;

    public Func<DateTimeOffset> Clock { get; }
    public TimeZoneInfo Zone { get; }
    public TimerBook Timers { get; }
    public ReminderBook Reminders { get; }

    /// <summary>Raised after any change (UI thread).</summary>
    public event Action? Changed;

    public TimersController(Func<DateTimeOffset>? clock = null, TimeZoneInfo? zone = null, bool persist = true, TimerDoc? timers = null, ReminderDoc? reminders = null, string? storageDirectory = null)
    {
        Clock = clock ?? (() => DateTimeOffset.Now);
        Zone = zone ?? TimeZoneInfo.Local;
        _persist = persist;
        _timersPath = storageDirectory is null ? Paths.File("timers.json") : Path.Combine(storageDirectory, "timers.json");
        _remindersPath = storageDirectory is null ? Paths.File("reminders.json") : Path.Combine(storageDirectory, "reminders.json");
        Timers = new TimerBook(timers ?? (persist ? JsonStore.Load<TimerDoc>(_timersPath) : new TimerDoc()), Clock);
        Reminders = new ReminderBook(reminders ?? (persist ? JsonStore.Load<ReminderDoc>(_remindersPath) : new ReminderDoc()), Clock, Zone);
        _savedTimers = JsonSerializer.Serialize(Timers.Doc, JsonStore.Options);
        _savedReminders = JsonSerializer.Serialize(Reminders.Doc, JsonStore.Options);
    }

    public enum SubmitResult { Timer, Reminder, Invalid, Full, SaveFailed }

    /// <summary>Text from the input box: a duration starts a timer, a phrase with a time sets (or edits) a reminder.</summary>
    public SubmitResult Submit(string input, string? editReminderId = null)
    {
        input = input.Trim();
        if (input.Length == 0) return SubmitResult.Invalid;
        ParsedReminder? parsed;
        if (editReminderId is null && DurationParser.TryParse(input, out var d, out var label, out var ambiguousPrefixLength))
        {
            parsed = ambiguousPrefixLength > 0 && label.Length > 0
                ? ReminderParser.Parse(input, Clock(), Zone, ambiguousPrefixLength)
                : null;
            if (parsed is null)
                return AddTimer(d, label) ? SubmitResult.Timer : LastSaveFailed ? SubmitResult.SaveFailed : SubmitResult.Full;
        }
        else parsed = ReminderParser.Parse(input, Clock(), Zone);
        if (parsed is null) return SubmitResult.Invalid;
        if (editReminderId is not null && Reminders.Replace(editReminderId, parsed, input)) { }
        else Reminders.Add(parsed, input);
        return Commit(reminders: true) ? SubmitResult.Reminder : SubmitResult.SaveFailed;
    }

    /// <summary>Adds a reminder from a phrase (command bar). Returns the reminder or null when not understood.</summary>
    public Reminder? AddReminder(string phrase)
    {
        var parsed = ReminderParser.Parse(phrase, Clock(), Zone);
        if (parsed is null) return null;
        var r = Reminders.Add(parsed, phrase);
        return Commit(reminders: true) ? r : null;
    }

    public bool AddTimer(TimeSpan duration, string label = "")
    {
        LastSaveFailed = false;
        if (Timers.Add(duration, label) is null) return false;
        return Commit(timers: true);
    }

    public void Pause(string id) { if (Timers.Pause(id)) Commit(timers: true); }
    public void Resume(string id) { if (Timers.Resume(id)) Commit(timers: true); }
    public void Cancel(string id) { if (Timers.Cancel(id)) Commit(timers: true); }
    public void AddMinute(string id) { if (Timers.AddTime(id, TimeSpan.FromMinutes(1))) Commit(timers: true); }
    public void Dismiss(string id) { if (Timers.Dismiss(id)) Commit(timers: true); }

    public void StopwatchToggle()
    {
        if (Timers.Stopwatch.IsRunning) Timers.StopwatchPause();
        else Timers.StopwatchStart();
        Commit(timers: true);
    }

    public void StopwatchReset()
    {
        Timers.StopwatchReset();
        Commit(timers: true);
    }

    public void ReminderDone(string id) { Reminders.Done(id); Commit(reminders: true); }
    public void Snooze(string id, TimeSpan delay) { Reminders.Snooze(id, delay); Commit(reminders: true); }
    public void SnoozeTomorrow(string id) { Reminders.SnoozeTomorrow(id); Commit(reminders: true); }
    public void DeleteReminder(string id) { if (Reminders.Delete(id)) Commit(reminders: true); }

    /// <summary>Marks finished timers and due reminders. Returns what became due now.</summary>
    public (List<TimerItem> Timers, List<Reminder> Reminders) CollectDue()
    {
        var t = Timers.CollectFinished();
        var r = Reminders.CollectDue();
        if (t.Count > 0 && !Commit(timers: true)) t.Clear();
        if (r.Count > 0 && !Commit(reminders: true)) r.Clear();
        return (t, r);
    }

    /// <summary>Earliest moment something needs attention.</summary>
    public DateTimeOffset? NextWake
    {
        get
        {
            var a = Timers.NextEnd;
            var b = Reminders.NextDue;
            return a is null ? b : b is null ? a : (a < b ? a : b);
        }
    }

    bool Commit(bool timers = false, bool reminders = false)
    {
        var saved = true;
        if (timers)
        {
            if (!_persist || JsonStore.Save(_timersPath, Timers.Doc))
                _savedTimers = JsonSerializer.Serialize(Timers.Doc, JsonStore.Options);
            else
            {
                var previous = JsonSerializer.Deserialize<TimerDoc>(_savedTimers, JsonStore.Options)!;
                Timers.Doc.Timers = previous.Timers;
                Timers.Doc.Stopwatch = previous.Stopwatch;
                saved = false;
            }
        }
        if (reminders)
        {
            if (!_persist || JsonStore.Save(_remindersPath, Reminders.Doc))
                _savedReminders = JsonSerializer.Serialize(Reminders.Doc, JsonStore.Options);
            else
            {
                Reminders.Doc.Reminders = JsonSerializer.Deserialize<ReminderDoc>(_savedReminders, JsonStore.Options)!.Reminders;
                saved = false;
            }
        }
        LastSaveFailed = !saved;
        try { Changed?.Invoke(); }
        catch (Exception ex) { Logger.Error("timers changed handler", ex); }
        if (!saved) SaveFailed?.Invoke();
        return saved;
    }

    // ---- display helpers ----

    /// <summary>"Today 18:30", "Tomorrow 09:00", "Fri 13 Mar 09:00".</summary>
    public string FormatDue(DateTimeOffset due)
    {
        var local = TimeZoneInfo.ConvertTime(due, Zone).DateTime;
        var today = TimeZoneInfo.ConvertTime(Clock(), Zone).DateTime.Date;
        var time = local.ToString("HH:mm");
        if (local.Date == today) return $"{Strings.T("day.today")} {time}";
        if (local.Date == today.AddDays(1)) return $"{Strings.T("day.tomorrow")} {time}";
        var culture = Strings.Language == "uk" ? new System.Globalization.CultureInfo("uk-UA") : new System.Globalization.CultureInfo("en-GB");
        var fmt = local.Year == today.Year ? "ddd d MMM" : "d MMM yyyy";
        return $"{local.ToString(fmt, culture)} {time}";
    }

    public static string FormatRepeat(RepeatRule? r)
    {
        if (r is null) return "";
        var culture = Strings.Language == "uk" ? new System.Globalization.CultureInfo("uk-UA") : new System.Globalization.CultureInfo("en-GB");
        return r.Kind switch
        {
            RepeatKind.Daily => Strings.T("rep.daily"),
            RepeatKind.EveryNDays => Strings.T("rep.days", r.Interval),
            RepeatKind.EveryNHours => r.Interval == 1 ? Strings.T("rep.hourly") : Strings.T("rep.hours", r.Interval),
            RepeatKind.EveryNMinutes => Strings.T("rep.minutes", r.Interval),
            RepeatKind.Monthly => r.DayOfMonth > 0 ? Strings.T("rep.monthlyOn", r.DayOfMonth) : Strings.T("rep.monthly"),
            RepeatKind.Weekly when r.Days.Length == 5 && !r.Days.Contains(DayOfWeek.Saturday) && !r.Days.Contains(DayOfWeek.Sunday) => Strings.T("rep.weekdays"),
            RepeatKind.Weekly when r.Days.Length > 0 => Strings.T("rep.weekly", string.Join(", ", r.Days.OrderBy(d => ((int)d + 6) % 7).Select(d => culture.DateTimeFormat.GetAbbreviatedDayName(d)))),
            _ => Strings.T("rep.weeklyPlain"),
        };
    }
}
