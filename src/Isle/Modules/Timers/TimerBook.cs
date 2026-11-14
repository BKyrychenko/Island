namespace Isle.Modules.Timers;

/// <summary>One countdown. Times are absolute so timers survive collapse and restarts.</summary>
public sealed class TimerItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..10];
    public string Label { get; set; } = "";
    public TimeSpan Duration { get; set; }
    /// <summary>When it ends, while running. Null while paused.</summary>
    public DateTimeOffset? EndsAt { get; set; }
    /// <summary>Remaining time while paused.</summary>
    public TimeSpan? PausedRemaining { get; set; }
    /// <summary>Set when it reached zero (and not yet dismissed).</summary>
    public DateTimeOffset? FinishedAt { get; set; }

    public bool IsPaused => EndsAt is null && PausedRemaining is not null && FinishedAt is null;
    public bool IsRunning => EndsAt is not null && FinishedAt is null;

    public TimeSpan Remaining(DateTimeOffset now) =>
        FinishedAt is not null ? TimeSpan.Zero
        : EndsAt is { } end ? (end - now < TimeSpan.Zero ? TimeSpan.Zero : end - now)
        : PausedRemaining ?? TimeSpan.Zero;

    /// <summary>0 → 1 as time passes.</summary>
    public double Progress(DateTimeOffset now) =>
        Duration <= TimeSpan.Zero ? 1 : Math.Clamp(1 - Remaining(now) / Duration, 0, 1);
}

/// <summary>The single stopwatch: accumulated time + start stamp while running.</summary>
public sealed class StopwatchState
{
    public bool Active { get; set; }
    public TimeSpan Accumulated { get; set; }
    public DateTimeOffset? RunningSince { get; set; }
    public bool IsRunning => RunningSince is not null;
    public TimeSpan Elapsed(DateTimeOffset now) => Accumulated + (RunningSince is { } s && now > s ? now - s : TimeSpan.Zero);
}

/// <summary>Stored document (timers.json).</summary>
public sealed class TimerDoc
{
    public List<TimerItem> Timers { get; set; } = [];
    public StopwatchState Stopwatch { get; set; } = new();
}

/// <summary>Pure timer operations on a <see cref="TimerDoc"/> with an injectable clock. No UI, no threads.</summary>
public sealed class TimerBook(TimerDoc doc, Func<DateTimeOffset> now)
{
    public const int MaxTimers = 20;
    public TimerDoc Doc { get; } = doc;
    public IReadOnlyList<TimerItem> Timers => Doc.Timers;
    public StopwatchState Stopwatch => Doc.Stopwatch;
    public DateTimeOffset Now => now();

    public TimerItem? Add(TimeSpan duration, string label = "")
    {
        if (duration <= TimeSpan.Zero || Doc.Timers.Count >= MaxTimers) return null;
        var t = new TimerItem { Duration = duration, Label = label.Trim(), EndsAt = now() + duration };
        Doc.Timers.Add(t);
        return t;
    }

    public TimerItem? Find(string id) => Doc.Timers.FirstOrDefault(t => t.Id == id);

    public bool Pause(string id)
    {
        if (Find(id) is not { IsRunning: true } t) return false;
        t.PausedRemaining = t.Remaining(now());
        t.EndsAt = null;
        return true;
    }

    public bool Resume(string id)
    {
        if (Find(id) is not { IsPaused: true } t) return false;
        t.EndsAt = now() + t.PausedRemaining!.Value;
        t.PausedRemaining = null;
        return true;
    }

    public bool Cancel(string id) => Doc.Timers.RemoveAll(t => t.Id == id) > 0;

    /// <summary>"+1 min": on a finished timer restarts it for one minute; on a running/paused one extends it.</summary>
    public bool AddTime(string id, TimeSpan extra)
    {
        if (Find(id) is not { } t) return false;
        if (t.FinishedAt is not null)
        {
            t.FinishedAt = null;
            t.EndsAt = now() + extra;
            t.Duration = extra;
        }
        else if (t.EndsAt is { } end) { t.EndsAt = end + extra; t.Duration += extra; }
        else if (t.PausedRemaining is { } r) { t.PausedRemaining = r + extra; t.Duration += extra; }
        return true;
    }

    /// <summary>Marks every timer whose end has passed as finished. Returns the newly finished ones.</summary>
    public List<TimerItem> CollectFinished()
    {
        var n = now();
        var done = new List<TimerItem>();
        foreach (var t in Doc.Timers)
        {
            if (t is { FinishedAt: null, EndsAt: { } end } && end <= n)
            {
                t.FinishedAt = end;
                done.Add(t);
            }
        }
        return done;
    }

    /// <summary>Removes a finished timer (Dismiss).</summary>
    public bool Dismiss(string id) => Doc.Timers.RemoveAll(t => t.Id == id && t.FinishedAt is not null) > 0;

    public IEnumerable<TimerItem> Finished => Doc.Timers.Where(t => t.FinishedAt is not null);

    /// <summary>The running timer that ends first (what the pill shows).</summary>
    public TimerItem? Soonest => Doc.Timers.Where(t => t.IsRunning).OrderBy(t => t.EndsAt).FirstOrDefault()
                                 ?? Doc.Timers.Where(t => t.IsPaused).OrderBy(t => t.PausedRemaining).FirstOrDefault();

    public DateTimeOffset? NextEnd => Doc.Timers.Where(t => t.IsRunning).Select(t => t.EndsAt).Min();

    // ---- stopwatch ----

    public void StopwatchStart()
    {
        var s = Doc.Stopwatch;
        s.Active = true;
        s.RunningSince ??= now();
    }

    public void StopwatchPause()
    {
        var s = Doc.Stopwatch;
        if (s.RunningSince is null) return;
        s.Accumulated = s.Elapsed(now());
        s.RunningSince = null;
    }

    public void StopwatchReset() => Doc.Stopwatch = new StopwatchState();
}
