using Isle.Core;
using Isle.Modules.Commands;
using Isle.Modules.Timers;
using Isle.Views.Commands;

namespace Isle.Tests;

public class ReminderCommandPrivacyTests
{
    [Theory]
    [InlineData("remind me tomorrow at 09:00 to call fixture")]
    [InlineData("нагадай мені завтра о 09:00 подзвонити")]
    [InlineData("  Please set a reminder tomorrow at 09:00 to call fixture")]
    [InlineData("Будь ласка нагадайте мені завтра о 09:00 подзвонити")]
    public async Task Reminder_commands_stay_local_when_timers_are_disabled(string text)
    {
        var timer = new TimerProvider(() => null, _ => Assert.Fail("Disabled reminder ran"));
        var web = new WebProvider();
        Assert.Empty(await web.QueryAsync(text, default));
        var row = Assert.Single(await timer.QueryAsync(text, default));
        Assert.Equal(Strings.T("cmd.timersOff"), row.Subtitle);
        Assert.Null(row.Run);

        FakeSink.Run(sink =>
        {
            var dispatched = new List<CommandItem>();
            var view = new CommandBarView([timer, web], dispatched.Add, null) { Text = text };
            view.Execute();
            Assert.True(sink.PumpUntil(() => view.Results.Count > 0));
            Assert.Equal(Strings.T("cmd.timersOff"), view.Results[view.SelectedIndex].Subtitle);
            view.Execute();
            Assert.Empty(dispatched);
            view.OnHidden();
        });
    }

    [Theory]
    [InlineData("remind me tomorrow at 09:00 to call fixture", "call fixture")]
    [InlineData("Нагадування: завтра о 09:00 подзвонити", "подзвонити")]
    public async Task Enabled_reminder_runs_locally(string text, string title)
    {
        var ctl = new TimersController(persist: false);
        var timer = new TimerProvider(() => ctl, _ => { });
        var row = Assert.Single(await timer.QueryAsync(text, default));
        Assert.Equal(title, row.Title);
        row.Run!();
        Assert.Equal(title, Assert.Single(ctl.Reminders.Reminders).Text);
    }

    [Theory]
    [InlineData("g remind me tomorrow at 09:00 to call fixture")]
    [InlineData("найкращі нагадування")]
    [InlineData("reminders app")]
    [InlineData("reminder.example")]
    [InlineData("reminder-board.example")]
    public async Task Web_search_remains_available_for_ordinary_and_explicit_queries(string text)
    {
        var timer = new TimerProvider(() => null, _ => { });
        Assert.Empty(await timer.QueryAsync(text, default));
        var web = await new WebProvider().QueryAsync(text, default);
        var search = Assert.Single(web, item => item.IconKey == "Icon.Search");
        var term = text.StartsWith("g ") ? text[2..] : text;
        Assert.Equal(Strings.T("cmd.searchWeb", term), search.Title);
        Assert.NotNull(search.Run);
    }
}
