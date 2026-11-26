using System.Windows.Threading;
using Skerry.Core;
using Skerry.Modules.Reminders;
using Skerry.Views.Timers;
using Microsoft.Win32;

namespace Skerry.Modules.Timers;

/// <summary>
/// Timers &amp; reminders (module id "timers"): one wake-up timer aimed at the next end/due moment (no polling), a 1-second
/// display tick only while a countdown is in the pill, finished/reminder banners with sound and a pulse, and the tab.
/// </summary>
public sealed class TimersModule(IslandWindow island) : IModule, IDisposable
{
    public const string TabId = "timers";
    const string PillId = "timer";
    static readonly TimeSpan MaxSleep = TimeSpan.FromMinutes(30); // re-check periodically anyway (clock changes)

    TimersController? _ctl;
    DispatcherTimer? _wake, _tick;
    TimerPill? _pill;
    readonly HashSet<string> _banners = [];

    public string Id => "timers";
    public TimersController? Controller => _ctl;

    public void Start()
    {
        _ctl = new TimersController();
        _ctl.Changed += OnChanged;
        _ctl.SaveFailed += OnSaveFailed;
        _wake = new DispatcherTimer(DispatcherPriority.Normal, island.Dispatcher);
        _wake.Tick += (_, _) => Check();
        _tick = new DispatcherTimer(DispatcherPriority.Background, island.Dispatcher) { Interval = TimeSpan.FromSeconds(1) };
        _tick.Tick += (_, _) => _pill?.Refresh();
        island.AddTab(new TabInfo(TabId, "tab.timers", "Icon.Timer", () => new TimersView(_ctl), 20));
        SystemEvents.PowerModeChanged += OnPower;
        SystemEvents.TimeChanged += OnTimeChanged;

        // things that ended while the app was closed show up right away
        foreach (var t in _ctl.Timers.Finished) ShowTimerBanner(t, announce: false);
        foreach (var r in _ctl.Reminders.Showing) ShowReminderBanner(r, announce: false);
        Check();
    }

    void OnPower(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume) island.Dispatcher.BeginInvoke(Check);
    }

    void OnTimeChanged(object? sender, EventArgs e) => island.Dispatcher.BeginInvoke(Check);

    void OnSaveFailed() => island.ShowToast(Strings.T("timers.saveFailed"));

    void OnChanged()
    {
        Reschedule();
        UpdatePill();
        // banners of items that were dismissed/snoozed/cancelled elsewhere (tab) go away
        foreach (var id in _banners.ToList())
        {
            var gone = id.StartsWith("t:", StringComparison.Ordinal)
                ? _ctl?.Timers.Find(id[2..]) is not { FinishedAt: not null }
                : _ctl?.Reminders.Find(id[2..]) is not { FiredAt: not null };
            if (gone) RemoveBanner(id);
        }
    }

    /// <summary>Fires whatever is due, then aims the wake-up timer at the next moment.</summary>
    void Check()
    {
        if (_ctl is null) return;
        try
        {
            var (timers, reminders) = _ctl.CollectDue();
            foreach (var t in timers) ShowTimerBanner(t, announce: true);
            foreach (var r in reminders) ShowReminderBanner(r, announce: true);
        }
        catch (Exception ex) { Logger.Error("timers check", ex); }
        Reschedule();
        UpdatePill();
    }

    void Reschedule()
    {
        if (_ctl is null || _wake is null) return;
        _wake.Stop();
        if (_ctl.NextWake is not { } next) return;
        var delay = next - _ctl.Clock();
        if (_ctl.LastSaveFailed && delay < TimeSpan.FromSeconds(30)) delay = TimeSpan.FromSeconds(30);
        if (delay < TimeSpan.FromMilliseconds(20)) delay = TimeSpan.FromMilliseconds(20);
        if (delay > MaxSleep) delay = MaxSleep;
        _wake.Interval = delay + TimeSpan.FromMilliseconds(15);
        _wake.Start();
    }

    void UpdatePill()
    {
        if (_ctl is null) return;
        var soonest = _ctl.Timers.Soonest;
        if (soonest is null)
        {
            island.Pill.Remove(PillId);
            _tick?.Stop();
            return;
        }
        _pill ??= new TimerPill(_ctl);
        _pill.Show(soonest.Id);
        if (!island.Pill.Contains(PillId))
        {
            island.Pill.Set(new PillItem
            {
                Id = PillId,
                Priority = PillPriority.TimerRunning,
                View = () => _pill,
                Compact = () => _pill.CreateCompact(),
                Width = TimerPill.PillWidth,
                CompactWidth = TimerPill.CompactWidth,
                TabId = TabId,
            });
        }
        if (soonest.IsRunning) _tick?.Start();
        else _tick?.Stop();
    }

    void ShowTimerBanner(TimerItem t, bool announce)
    {
        var key = "t:" + t.Id;
        _banners.Add(key);
        island.Pill.Set(new PillItem
        {
            Id = key,
            Priority = PillPriority.Finished,
            View = () => new TimerDoneBanner(_ctl!, t),
            Width = TimerDoneBanner.BannerWidth,
            Height = TimerDoneBanner.BannerHeight,
            IsBanner = true,
            Peek = true,
            HoverExpands = false,
            TabId = TabId,
        });
        if (announce) Announce(Native.MB_ICONASTERISK);
    }

    void ShowReminderBanner(Reminder r, bool announce)
    {
        var key = "r:" + r.Id;
        _banners.Add(key);
        island.Pill.Set(new PillItem
        {
            Id = key,
            Priority = PillPriority.Finished,
            View = () => new ReminderBanner(_ctl!, r),
            Width = ReminderBanner.BannerWidth,
            Height = ReminderBanner.BannerHeight,
            IsBanner = true,
            Peek = true,
            HoverExpands = false,
            TabId = TabId,
        });
        if (announce) Announce(Native.MB_ICONEXCLAMATION);
    }

    void Announce(uint sound)
    {
        try { Native.MessageBeep(sound); } catch { /* sound is best effort */ }
        island.Pulse(3);
        EventBus.Publish(new TimerFinished());
    }

    void RemoveBanner(string key)
    {
        _banners.Remove(key);
        island.Pill.Remove(key);
    }

    public void Stop()
    {
        SystemEvents.PowerModeChanged -= OnPower;
        SystemEvents.TimeChanged -= OnTimeChanged;
        _wake?.Stop();
        _tick?.Stop();
        foreach (var id in _banners.ToList()) RemoveBanner(id);
        island.Pill.Remove(PillId);
        island.RemoveTab(TabId);
        if (_ctl is not null) { _ctl.Changed -= OnChanged; _ctl.SaveFailed -= OnSaveFailed; }
        _ctl = null;
        _pill = null;
    }

    public void Dispose() { if (_ctl is not null) Stop(); }
}

/// <summary>Published when a timer finishes or a reminder fires (characters react happily).</summary>
public sealed record TimerFinished;
