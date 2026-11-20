using System.Windows;
using System.Windows.Controls;
using Isle.Core;
using Isle.Modules.Commands;
using Isle.Modules.Timers;
using Isle.Modules.Reminders;
using Isle.Views.Timers;

namespace Isle.Tests;

public class ProductivityRegressionTests
{
    [Fact]
    public async Task Timer_commands_disable_unavailable_actions_and_recheck_when_executed()
    {
        TimersController? controller = null;
        var messages = new List<string>();
        var provider = new TimerProvider(() => controller, messages.Add);
        var off = Assert.Single(await provider.QueryAsync("timer 5m", default));
        Assert.Null(off.Run);
        Assert.Equal(Strings.T("cmd.timersOff"), off.Subtitle);

        controller = new TimersController(persist: false);
        var enabled = Assert.Single(await provider.QueryAsync("timer 5m", default));
        for (var i = 0; i < TimerBook.MaxTimers; i++) Assert.True(controller.AddTimer(TimeSpan.FromMinutes(1)));
        var full = Assert.Single(await provider.QueryAsync("timer 5m", default));
        Assert.Null(full.Run);
        Assert.Equal(Strings.T("timers.full"), full.Subtitle);
        enabled.Run!();
        Assert.Equal(Strings.T("timers.full"), Assert.Single(messages));
        controller = null;
        enabled.Run!();
        Assert.Equal(Strings.T("cmd.timersOff"), messages.Last());
    }

    [Fact]
    public void Failed_timer_saves_roll_back_add_pause_stopwatch_and_due_state()
    {
        using var dir = new TempDir();
        var now = DateTimeOffset.UtcNow;
        var ctl = new TimersController(() => now, persist: true, storageDirectory: dir.Path);
        Assert.True(ctl.AddTimer(TimeSpan.FromMinutes(1), "saved"));
        var id = Assert.Single(ctl.Timers.Timers).Id;
        var failures = 0;
        ctl.SaveFailed += () => failures++;
        using (File.Open(dir.File("timers.json"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.False(ctl.AddTimer(TimeSpan.FromMinutes(2), "lost"));
            Assert.Single(ctl.Timers.Timers);
            ctl.Pause(id);
            Assert.True(ctl.Timers.Find(id)!.IsRunning);
            ctl.StopwatchToggle();
            Assert.False(ctl.Timers.Stopwatch.Active);
            now = now.AddMinutes(2);
            Assert.Empty(ctl.CollectDue().Timers);
            Assert.Null(ctl.Timers.Find(id)!.FinishedAt);
            Assert.True(ctl.LastSaveFailed);
        }
        Assert.Equal(4, failures);
        Assert.Single(ctl.CollectDue().Timers);
        Assert.False(ctl.LastSaveFailed);
        var loaded = JsonStore.Load<TimerDoc>(dir.File("timers.json"));
        Assert.NotNull(Assert.Single(loaded.Timers).FinishedAt);
    }

    [Fact]
    public void Failed_reminder_saves_preserve_saved_record_and_report_submit_failure()
    {
        using var dir = new TempDir();
        var ctl = new TimersController(persist: true, storageDirectory: dir.Path);
        var reminder = ctl.AddReminder("tomorrow 9:00 saved")!;
        using (File.Open(dir.File("reminders.json"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Null(ctl.AddReminder("tomorrow 10:00 lost"));
            Assert.Equal(TimersController.SubmitResult.SaveFailed, ctl.Submit("tomorrow 11:00 edited", reminder.Id));
            Assert.Equal("saved", Assert.Single(ctl.Reminders.Reminders).Text);
            ctl.DeleteReminder(reminder.Id);
            Assert.Single(ctl.Reminders.Reminders);
            ctl.Snooze(reminder.Id, TimeSpan.FromHours(1));
            Assert.Equal(reminder.Due, ctl.Reminders.Find(reminder.Id)!.Due);
        }
        Assert.Equal(TimersController.SubmitResult.Reminder, ctl.Submit("tomorrow 11:00 edited", reminder.Id));
        Assert.Equal("edited", Assert.Single(JsonStore.Load<ReminderDoc>(dir.File("reminders.json")).Reminders).Text);
    }

    [Fact]
    public void Index_cache_identity_requires_the_same_normalized_configured_roots()
    {
        var roots = AppIndex.NormalizeRoots([@"C:\Search\One\", @"c:\search\one", @"C:\Search\Two"]);
        Assert.Equal(2, roots.Length);
        Assert.True(AppIndex.SameRoots(roots, AppIndex.NormalizeRoots([@"c:\search\two", @"C:\Search\One"])));
        Assert.False(AppIndex.SameRoots(roots, AppIndex.NormalizeRoots([@"C:\Search\New"])));
        Assert.False(AppIndex.SameRoots(null, [])); // legacy cache has no root identity
        using var dir = new TempDir();
        var path = dir.File("index.json");
        JsonStore.Save(path, new IndexFile { Built = DateTimeOffset.Now, Roots = roots, Entries = [new("cached", "cached", EntryKind.File)] });
        var index = new AppIndex(path, () => roots);
        index.Refresh();
        Assert.Equal("cached", Assert.Single(index.Entries).Name);
    }

    [Fact]
    public void Timer_presets_show_units_and_explicit_submit_keeps_unsaved_draft()
    {
        FakeSink.Run(_ =>
        {
            using var dir = new TempDir();
            var ctl = new TimersController(storageDirectory: dir.Path);
            using var view = new TimersView(ctl);
            var top = (DockPanel)view.Children[0];
            var input = ((Grid)top.Children.OfType<Grid>().Single()).Children.OfType<TextBox>().Single();
            var submit = top.Children.OfType<Button>().Single(b => b.Content is string);
            Assert.Equal(Strings.T("timers.submit"), submit.Content);
            var feedback = view.Children.OfType<StackPanel>().Single(p => Grid.GetRow(p) == 1);
            var presets = ((WrapPanel)feedback.Children[0]).Children.OfType<Button>().ToArray();
            Assert.Equal(5, presets.Length);
            Assert.All(presets, b => Assert.EndsWith(" " + Strings.T("unit.min"), (string)b.Content));
            input.Text = "5m tea";
            submit.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Single(ctl.Timers.Timers);
            Assert.Equal("", input.Text);
            using (File.Open(dir.File("timers.json"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                input.Text = "10m keep this draft";
                submit.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal("10m keep this draft", input.Text);
                Assert.Single(ctl.Timers.Timers);
                Assert.Equal(Strings.T("timers.saveFailed"), ((TextBlock)feedback.Children[1]).Text);
            }
        });
    }
}
