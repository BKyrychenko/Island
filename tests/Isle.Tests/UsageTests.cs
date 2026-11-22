using System.Text;
using Isle.Core;
using Isle.Modules.Usage;

namespace Isle.Tests;

public sealed class UsageTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "isle-usage-" + Guid.NewGuid().ToString("N"));
    string Projects => Path.Combine(_dir, "projects");

    public void Dispose() { try { Directory.Delete(_dir, true); } catch (IOException) { } }

    // ---- fixtures: synthetic transcript lines, no real content ----

    static string Ts(DateTimeOffset t) => t.ToString("o");
    static readonly DateTimeOffset Noon = new(2026, 11, 22, 12, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 11, 22, 12, 0, 0)));

    static string Line(string id, string req, long input = 10, long output = 20, long read = 0, long w5 = 0, long w1 = 0,
        string model = "claude-sonnet-5-5", DateTimeOffset? at = null, string cwd = @"C:\work\demo")
    {
        var cc = $"\"cache_creation\":{{\"ephemeral_5m_input_tokens\":{w5},\"ephemeral_1h_input_tokens\":{w1}}}";
        return "{\"type\":\"assistant\",\"sessionId\":\"x\",\"timestamp\":\"" + Ts(at ?? Noon) + "\",\"cwd\":" + System.Text.Json.JsonSerializer.Serialize(cwd) +
               ",\"requestId\":\"" + req + "\",\"message\":{\"id\":\"" + id + "\",\"model\":\"" + model + "\",\"content\":[{\"type\":\"text\",\"text\":\"hello usage\"}]," +
               $"\"usage\":{{\"input_tokens\":{input},\"output_tokens\":{output},\"cache_read_input_tokens\":{read},\"cache_creation_input_tokens\":{w5 + w1},{cc}}}}}}}";
    }

    string Write(string relative, params string[] lines)
    {
        var path = Path.Combine(Projects, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, string.Concat(lines.Select(l => l + "\n")));
        return path;
    }

    static void Append(string path, string text) => File.AppendAllText(path, text);

    UsageData Scan(UsageData? data = null)
    {
        data ??= new UsageData();
        UsageScanner.Scan(Projects, data);
        return data;
    }

    static long Total(UsageData d) => d.Files.Values.Sum(f => f.Days.Values.Sum(m => m.Values.Sum(t => t.Total)));

    static UsageLine? Parse(string line) => UsageScanner.Parse(Encoding.UTF8.GetBytes(line));

    // ---- parsing ----

    [Fact]
    public void Only_assistant_lines_with_usage_and_a_timestamp_count()
    {
        Assert.Null(Parse("{\"type\":\"user\",\"message\":{\"content\":\"what is usage of assistant tokens\"}}"));
        Assert.Null(Parse("{\"type\":\"assistant\",\"timestamp\":\"" + Ts(Noon) + "\",\"message\":{\"id\":\"m\"}}")); // no usage
        Assert.Null(Parse("{\"type\":\"assistant\",\"message\":{\"usage\":{\"input_tokens\":5}}}")); // no timestamp
        Assert.Null(Parse("{\"type\":\"assistant\",\"usage\": not json at all"));
        Assert.Null(Parse("{\"type\":\"assistant\",\"timestamp\":\"" + Ts(Noon) + "\",\"message\":{\"usage\":{\"input_tokens\":0,\"output_tokens\":0}}}")); // synthetic, empty
        Assert.Null(Parse(""));
        Assert.NotNull(Parse(Line("m1", "r1")));
    }

    [Fact]
    public void Fields_are_read_and_unreadable_ones_become_zero()
    {
        var l = Parse(Line("m1", "r1", input: 1, output: 2, read: 3, w5: 4, w1: 5, cwd: "/home/me/proj/"))!.Value;
        Assert.Equal((1L, 2L, 3L, 4L, 5L), (l.Tokens.In, l.Tokens.Out, l.Tokens.CacheRead, l.Tokens.Write5m, l.Tokens.Write1h));
        Assert.Equal("proj", l.Cwd);
        Assert.Equal("demo", Parse(Line("m", "r"))!.Value.Cwd); // Windows separators

        // no TTL split: the whole cache write is a 5-minute write; strings where numbers belong are ignored
        var plain = Parse("{\"type\":\"assistant\",\"timestamp\":\"" + Ts(Noon) + "\",\"message\":{\"model\":\"m\",\"usage\":{\"input_tokens\":\"7\",\"output_tokens\":2,\"cache_creation_input_tokens\":9}}}")!.Value;
        Assert.Equal((0L, 2L, 9L, 0L), (plain.Tokens.In, plain.Tokens.Out, plain.Tokens.Write5m, plain.Tokens.Write1h));
        Assert.Equal(0UL, plain.Key); // no message.id / requestId: nothing to dedupe on
    }

    // ---- dedupe, subagents ----

    [Fact]
    public void A_message_written_once_per_content_block_counts_once()
    {
        Write("slug/s1.jsonl", Line("m1", "r1"), Line("m1", "r1"), Line("m1", "r1"), Line("m1", "r2"), Line("m2", "r1"));
        Assert.Equal(3 * 30, Total(Scan())); // (m1,r1) (m1,r2) (m2,r1)
    }

    [Fact]
    public void Subagent_usage_belongs_to_the_parent_session()
    {
        Write("slug/s1.jsonl", Line("m1", "r1"));
        Write("slug/s1/subagents/agent-abc.jsonl", Line("m2", "r2", input: 100, output: 0));
        Write("slug/s1/other/agent-x.jsonl", Line("m3", "r3")); // not a transcript location
        var data = Scan();
        Assert.Equal(2, data.Files.Count);
        Assert.All(data.Files.Values, f => Assert.Equal("s1", f.Session));
        var report = UsageReport.Build(data, Noon);
        var row = Assert.Single(report.Sessions);
        Assert.Equal(30 + 100, row.Tokens.Total);
        Assert.Equal("demo", row.Project);
    }

    // ---- incremental reading ----

    [Fact]
    public void A_rescan_reads_only_appended_lines_and_dedupe_survives_it()
    {
        var path = Write("slug/s1.jsonl", Line("m1", "r1"));
        var data = Scan();
        var again = UsageScanner.Scan(Projects, data);
        Assert.Equal(0, again.Bytes);

        Append(path, Line("m1", "r1") + "\n" + Line("m2", "r1") + "\n"); // a duplicate of an old message plus a new one
        var stats = UsageScanner.Scan(Projects, data);
        Assert.Equal(1, stats.Messages);
        Assert.Equal(60, Total(data));
    }

    [Fact]
    public void A_half_written_last_line_waits_for_its_newline()
    {
        var first = Line("m1", "r1");
        var second = Line("m2", "r1");
        var path = Write("slug/s1.jsonl", first);
        Append(path, second[..40]);
        var data = Scan();
        Assert.Equal(30, Total(data));
        Assert.Equal(Encoding.UTF8.GetByteCount(first) + 1, data.Files["slug/s1.jsonl"].Offset);

        Append(path, second[40..] + "\n");
        Scan(data);
        Assert.Equal(60, Total(data));
        Scan(data);
        Assert.Equal(60, Total(data)); // nothing counted twice
    }

    [Fact]
    public void A_shrunk_or_replaced_file_is_scanned_again_from_the_start()
    {
        var path = Write("slug/s1.jsonl", Line("m1", "r1", input: 1000), Line("m2", "r1", input: 1000));
        var data = Scan();
        Assert.Equal(2 * 1020, Total(data));

        File.WriteAllText(path, Line("m9", "r1", input: 1) + "\n"); // truncated and rewritten
        Scan(data);
        Assert.Equal(21, Total(data));

        // same path, now another (bigger) file: the creation time gives it away
        File.WriteAllText(path, Line("m5", "r1", input: 5) + "\n" + Line("m6", "r1", input: 5) + "\n" + Line("m7", "r1", input: 5) + "\n");
        File.SetCreationTimeUtc(path, DateTime.UtcNow.AddHours(-3));
        Scan(data);
        Assert.Equal(3 * 25, Total(data));

        File.Delete(path);
        Scan(data);
        Assert.Empty(data.Files);
    }

    [Fact]
    public async Task The_service_persists_state_and_continues_after_a_restart()
    {
        var path = Write("slug/s1.jsonl", Line("m1", "r1"), Line("m2", "r1"));
        var store = Path.Combine(_dir, "usage.json");
        var report = await new UsageService(Projects, store).RefreshAsync(default);
        Assert.Equal(60, report.Month.Tokens.Total);
        Assert.True(File.Exists(store));

        Append(path, Line("m2", "r1") + "\n" + Line("m3", "r1") + "\n");
        var restarted = await new UsageService(Projects, store).RefreshAsync(default);
        Assert.Equal(90, restarted.Month.Tokens.Total);
        Assert.DoesNotContain("hello usage", File.ReadAllText(store)); // message text is never stored
    }

    [Fact]
    public void A_missing_projects_folder_is_an_empty_report()
    {
        var report = UsageReport.Build(Scan(), Noon);
        Assert.True(report.IsEmpty);
        Assert.Equal(0, report.Month.Tokens.Total);
    }

    // ---- cost ----

    [Fact]
    public void Cost_is_priced_per_token_kind()
    {
        var t = new Tokens { In = 1_000_000, Out = 1_000_000, CacheRead = 1_000_000, Write5m = 1_000_000, Write1h = 1_000_000 };
        Assert.Equal(2m + 10m + 0.10m + 2.5m + 4m, UsagePrices.Cost("claude-sonnet-5-5", t));
        Assert.Equal(2m + 10m + 0.20m + 2.5m + 4m, UsagePrices.Cost("claude-sonnet-5", t));
        Assert.Equal(4m + 20m + 0.20m + 5m + 8m, UsagePrices.Cost("claude-opus-5-5", t));
        Assert.Equal(5m + 25m + 0.50m + 6.25m + 10m, UsagePrices.Cost("claude-opus-4-8", t));
        Assert.Equal(1m + 5m + 0.10m + 1.25m + 2m, UsagePrices.Cost("claude-haiku-4-5-20251001", t)); // dated id
        Assert.Equal(0.5m, UsagePrices.Cost("claude-sonnet-5-5", new Tokens { Out = 50_000 }));
    }

    [Fact]
    public void An_unknown_model_has_tokens_but_no_cost()
    {
        var t = new Tokens { In = 1000 };
        Assert.Null(UsagePrices.Cost("some-other-model", t));
        Assert.Null(UsagePrices.Cost("", t));
        Assert.Equal((null, false), UsagePrices.Cost([new("mystery", t)]));
        Assert.Equal((0m, false), UsagePrices.Cost([new("mystery", new Tokens())])); // no tokens, nothing missing
        Assert.Equal("—", UsageReport.Money(null));

        Write("slug/s1.jsonl", Line("m1", "r1", model: "mystery-1"));
        var report = UsageReport.Build(Scan(), Noon);
        Assert.Equal(30, report.Today.Tokens.Total);
        Assert.Null(report.Today.Cost);
        Assert.Null(report.Sessions[0].Cost);
    }

    // ---- report ----

    [Fact]
    public void Totals_cover_today_seven_and_thirty_local_days_and_sessions_are_newest_first()
    {
        Write("slug/old.jsonl", Line("a", "1", at: Noon.AddDays(-20)));
        Write("slug/mid.jsonl", Line("b", "1", at: Noon.AddDays(-5)), Line("c", "1", at: Noon.AddDays(-5).AddMinutes(90)));
        Write("slug/new.jsonl", Line("d", "1", at: Noon.AddHours(-2), cwd: ""));
        Write("slug/ancient.jsonl", Line("e", "1", at: Noon.AddDays(-40)));
        var report = UsageReport.Build(Scan(), Noon);

        Assert.Equal(30, report.Today.Tokens.Total);
        Assert.Equal(30 + 60, report.Week.Tokens.Total);
        Assert.Equal(30 + 60 + 30, report.Month.Tokens.Total);
        Assert.Equal(["slug", "demo", "demo", "demo"], report.Sessions.Select(s => s.Project)); // cwd missing: the folder slug
        Assert.Equal(TimeSpan.FromMinutes(90), report.Sessions[1].Duration);
        Assert.True(report.Sessions[0].Start > report.Sessions[1].Start);
        Assert.Equal(report.Month.Cost, report.Sessions.Take(3).Sum(s => s.Cost));
    }

    [Fact]
    public void Numbers_and_model_ids_are_shown_short()
    {
        Assert.Equal(["999", "1.2k", "5.6M", "2.5B"], new[] { 999L, 1234, 5_600_000, 2_500_000_000 }.Select(UsageReport.Count));
        Assert.Equal("$1.75", UsageReport.Money(1.7512m));
        Assert.Equal("$327", UsageReport.Money(326.9m));
        Assert.Equal(["Sonnet 5.5", "Opus 4.8", "Haiku 4.5", "gpt-x", "?"],
            new[] { "claude-sonnet-5-5", "claude-opus-4-8", "claude-haiku-4-5-20251001", "gpt-x", "" }.Select(UsageReport.ModelName));
    }

    // ---- fixes: cross-file dedupe, oversize lines, exact prices, partial costs, reset ----

    [Fact]
    public void A_message_copied_into_another_file_counts_once_and_dropping_a_file_is_safe()
    {
        var a = Write("slug/a.jsonl", Line("m1", "r1"));
        var data = Scan();
        var b = Write("slug/b.jsonl", Line("m1", "r1"), Line("m2", "r1")); // a resumed session replays m1
        Scan(data);
        Assert.Equal(60, Total(data));

        File.Delete(a);
        Scan(data);
        Assert.Single(data.Files);
        Assert.Equal(30, Total(data)); // b keeps its own m2
        Append(b, Line("m3", "r1") + "\n");
        Scan(data);
        Assert.Equal(60, Total(data));
    }

    [Fact]
    public void A_rescanned_file_may_count_its_own_keys_again()
    {
        var path = Write("slug/a.jsonl", Line("m1", "r1", input: 1000));
        var data = Scan();
        File.WriteAllText(path, Line("m1", "r1", input: 1) + "\n"); // shorter: reset
        Scan(data);
        Assert.Equal(21, Total(data));
    }

    [Fact]
    public void A_line_longer_than_the_cap_is_skipped_without_losing_the_lines_around_it()
    {
        var huge = Line("big", "r1", cwd: new string('x', 20_000)); // a valid usage line, far over the cap
        var path = Write("slug/s1.jsonl", Line("m1", "r1"), huge, Line("m2", "r1"));
        var data = new UsageData();
        var stats = UsageScanner.Scan(Projects, data, default, maxLine: 4096);
        Assert.Equal(2, stats.Messages);
        Assert.Equal(60, Total(data));
        Assert.Equal(new FileInfo(path).Length, data.Files["slug/s1.jsonl"].Offset);

        Append(path, Line("m3", "r1") + "\n");
        UsageScanner.Scan(Projects, data, default, maxLine: 4096);
        Assert.Equal(90, Total(data));
    }

    [Fact]
    public void Prices_match_exact_ids_with_an_optional_date_suffix_only()
    {
        var t = new Tokens { In = 1_000_000 };
        Assert.Equal(4m, UsagePrices.Cost("claude-opus-5-5", t));
        Assert.Equal(4m, UsagePrices.Cost("claude-opus-5-5-20261101", t));
        Assert.Null(UsagePrices.Cost("claude-opus-5-6", t));      // a future model is unknown, not priced as 5-5
        Assert.Null(UsagePrices.Cost("claude-opus-5-5x", t));
        Assert.Null(UsagePrices.Cost("claude-opus-5-5-2026110", t));
        Assert.Null(UsagePrices.Cost("claude-opus-5-5-2026110a", t));
        Assert.Equal(5m, UsagePrices.Cost("claude-opus-5", t));   // and 5 is not 5-5
    }

    [Fact]
    public void Some_models_unpriced_gives_a_marked_partial_sum_and_nothing_priced_gives_a_dash()
    {
        var t = new Tokens { In = 1_000_000 };
        Assert.Equal((2m, true), UsagePrices.Cost([new("claude-sonnet-5-5", t), new("mystery", t)]));
        Assert.Equal((2m, false), UsagePrices.Cost([new("claude-sonnet-5-5", t)]));
        Assert.Equal("≥ $2.00", UsageReport.Money(2m, partial: true));
        Assert.Equal("$2.00", UsageReport.Money(2m));
        Assert.Equal("—", UsageReport.Money(null, partial: true));

        Write("slug/s1.jsonl", Line("m1", "r1", model: "mystery-1"), Line("m2", "r1", model: "claude-sonnet-5-5", input: 1_000_000, output: 0));
        var report = UsageReport.Build(Scan(), Noon);
        Assert.True(report.Today.Partial);
        Assert.Equal(2m, report.Today.Cost);
        Assert.True(report.Sessions[0].Partial);
    }

    [Fact]
    public async Task A_reset_is_saved_even_when_no_complete_line_is_read_afterwards()
    {
        var path = Write("slug/s1.jsonl", Line("m1", "r1"));
        var store = Path.Combine(_dir, "usage.json");
        var svc = new UsageService(Projects, store);
        await svc.RefreshAsync(default);
        Assert.Equal(30, Total(UsageData.Load(store)));

        File.WriteAllText(path, "{\"type\":\"ass"); // truncated to a partial line
        await svc.RefreshAsync(default);
        Assert.Equal(0, Total(UsageData.Load(store)));
    }
}
