using System.Text.Json;
using Skerry.Modules.Reminders;

namespace Skerry.Tests;

public class ReminderRecurrenceParsingTests
{
    static readonly DateTimeOffset Now = new(2026, 11, 20, 13, 15, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("every 1.5 hours stretch", 90, "stretch")]
    [InlineData("кожні 1,5 години розминка", 90, "розминка")]
    [InlineData("every 2.05 hours stretch", 123, "stretch")]
    [InlineData("every 0.031250 weeks stretch", 315, "stretch")]
    [InlineData("кожні 0,5 дня розминка", 720, "розминка")]
    public void Fractional_intervals_keep_their_cadence(string phrase, int minutes, string label)
    {
        var reminder = Assert.IsType<ParsedReminder>(ReminderParser.Parse(phrase, Now, TimeZoneInfo.Utc));
        Assert.Equal(label, reminder.Text);
        var first = Now.AddMinutes(minutes);
        Assert.Equal(first, reminder.Due);
        Assert.Equal(RepeatKind.EveryNMinutes, reminder.Repeat!.Kind);
        Assert.Equal(minutes, reminder.Repeat.Interval);
        Assert.Equal(first.AddMinutes(3 * minutes), reminder.Repeat.Next(first, first.AddMinutes(2 * minutes + 1), TimeZoneInfo.Utc));
    }

    [Theory]
    [InlineData("every 1.5 months at 10:00 stretch")]
    [InlineData("every 1.00000000000000000000000000001 hours stretch")]
    [InlineData("кожні 0,5 хвилини о 10:00 розминка")]
    public void Unsupported_fractions_cannot_become_one_off_reminders(string phrase) =>
        Assert.Null(ReminderParser.Parse(phrase, Now, TimeZoneInfo.Utc));

    [Theory]
    [InlineData("February 29 check", 2026, 2028)]
    [InlineData("February 29th check", 2026, 2028)]
    [InlineData("29th of February check", 2026, 2028)]
    [InlineData("29 лютого check", 2026, 2028)]
    [InlineData("29/02 check", 2028, 2032)]
    [InlineData("February 29 check", 2096, 2104)]
    public void Yearless_leap_dates_select_the_next_valid_year(string phrase, int startYear, int dueYear)
    {
        var reminder = Assert.IsType<ParsedReminder>(ReminderParser.Parse(phrase, Now.AddYears(startYear - Now.Year), TimeZoneInfo.Utc));
        Assert.Equal(new DateTimeOffset(dueYear, 2, 29, 9, 0, 0, TimeSpan.Zero), reminder.Due);
        Assert.Equal("check", reminder.Text);
    }

    [Fact]
    public void Existing_hourly_rule_advances_from_its_saved_due_time()
    {
        const string json = """{"Reminders":[{"Id":"old","Due":"2026-11-20T10:00:00Z","Repeat":{"Kind":5,"Interval":2}}]}""";
        var book = new ReminderBook(JsonSerializer.Deserialize<ReminderDoc>(json)!, () => Now, TimeZoneInfo.Utc);
        book.Done("old");
        Assert.Equal(new DateTimeOffset(2026, 11, 20, 14, 0, 0, TimeSpan.Zero), book.Find("old")!.Due);
    }
}
