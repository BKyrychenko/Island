using Skerry.Modules.Timers;

namespace Skerry.Tests;

public class TimerInputRoutingTests
{
    static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("21 листопада о 09:00 оплатити рахунок", 21, "оплатити рахунок")]
    [InlineData("21 November 2026 at 09:00 pay fixture", 21, "pay fixture")]
    [InlineData("21 November pay bill", 21, "pay bill")]
    [InlineData("21.11 о 09:00 оплатити рахунок", 21, "оплатити рахунок")]
    [InlineData("09:00 on Monday gym", 23, "gym")]
    [InlineData("09:00 у понеділок зал", 23, "зал")]
    [InlineData("09:00 завтра зарядка", 21, "зарядка")]
    public void Number_or_clock_in_calendar_phrase_creates_a_reminder(string input, int day, string label)
    {
        var controller = new TimersController(() => Now, TimeZoneInfo.Utc, persist: false);
        Assert.Equal(TimersController.SubmitResult.Reminder, controller.Submit(input));
        Assert.Empty(controller.Timers.Timers);
        var reminder = Assert.Single(controller.Reminders.Reminders);
        Assert.Equal(label, reminder.Text);
        Assert.Equal(new DateTimeOffset(2026, 11, day, 9, 0, 0, TimeSpan.Zero), reminder.Due);
    }

    [Theory]
    [InlineData("21", 1260, "")]
    [InlineData("1.5 stretch", 90, "stretch")]
    [InlineData("21 pay bill", 1260, "pay bill")]
    [InlineData("5 — pasta", 300, "pasta")]
    [InlineData("09:00", 540, "")]
    [InlineData("09:00 gym", 540, "gym")]
    [InlineData("1:02:03 gym", 3723, "gym")]
    [InlineData("5 min plan for Monday", 300, "plan for Monday")]
    [InlineData("10 хв зустріч у понеділок", 600, "зустріч у понеділок")]
    [InlineData("15 prepare for tomorrow", 900, "prepare for tomorrow")]
    [InlineData("5 follow up after 999999999999999999 days", 300, "follow up after 999999999999999999 days")]
    [InlineData("09:00 in 9999999 days gym", 540, "in 9999999 days gym")]
    public void Standard_durations_preserve_their_countdown_and_label(string input, int seconds, string label)
    {
        var controller = new TimersController(() => Now, TimeZoneInfo.Utc, persist: false);
        Assert.Equal(TimersController.SubmitResult.Timer, controller.Submit(input));
        Assert.Empty(controller.Reminders.Reminders);
        var timer = Assert.Single(controller.Timers.Timers);
        Assert.Equal(label, timer.Label);
        Assert.Equal(TimeSpan.FromSeconds(seconds), timer.Duration);
    }
}
