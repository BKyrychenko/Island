using Isle.Modules.Commands;

namespace Isle.Tests;

public class CalculatorTests
{
    [Theory]
    [InlineData("2+2", 4)]
    [InlineData("2 + 3 * 4", 14)]
    [InlineData("(2 + 3) * 4", 20)]
    [InlineData("2^10", 1024)]
    [InlineData("2**3", 8)]
    [InlineData("2^3^2", 512)]
    [InlineData("-2^2", -4)]
    [InlineData("10 / 4", 2.5)]
    [InlineData("7 × 6", 42)]
    [InlineData("9 ÷ 3", 3)]
    [InlineData("10 − 3", 7)]
    [InlineData("50%", 0.5)]
    [InlineData("200 * 15%", 30)]
    [InlineData("15% of 80", 12)]
    [InlineData("15 % of 80", 12)]
    [InlineData("100 + 15% of 80", 112)]
    [InlineData("20% від 50", 10)]
    [InlineData("(1 250 + 15% of 80) / 3", 420.6666666666667)]
    [InlineData("(1 250 + 15% від 80) / 3", 420.6666666666667)]
    [InlineData("(15% of 80) * 2", 24)]
    [InlineData("15% of (80 + 20)", 15)]
    [InlineData("sqrt(16)", 4)]
    [InlineData("sqrt 16 + 0", double.NaN)]
    [InlineData("abs(-3)", 3)]
    [InlineData("round(2.5)", 3)]
    [InlineData("round(3.14159, 2)", 3.14)]
    [InlineData("round(1.2345,2)", 1.23)]
    [InlineData("round((1,2345),2)", 1.23)]
    [InlineData("abs(-3,5)", 3.5)]
    [InlineData("log(1000)", 3)]
    [InlineData("ln(e)", 1)]
    [InlineData("sin(0) + cos(0)", 1)]
    [InlineData("2pi", 6.283185307179586)]
    [InlineData("3(4+1)", 15)]
    [InlineData("1,000,000 / 4", 250000)]
    [InlineData("1 000 + 1", 1001)]
    [InlineData("3,5 * 2", 7)]
    [InlineData("1.5e3 + 1", 1501)]
    [InlineData("5!", 120)]
    [InlineData("=2*21", 42)]
    [InlineData("(1+2", 3)]
    [InlineData("max(3, 9, 4)", 9)]
    [InlineData("max(2,3)", 3)]
    [InlineData("max(2,300)", 300)]
    [InlineData("min(3,2)", 2)]
    [InlineData("log(8,2)", 3)]
    public void Evaluates(string input, double expected)
    {
        var ok = Calculator.TryEvaluate(input, out var r);
        if (double.IsNaN(expected)) { Assert.False(ok); return; }
        Assert.True(ok, input);
        Assert.Equal(expected, r.Value, 9);
    }

    [Theory]
    [InlineData("")]
    [InlineData("42")]
    [InlineData("-5")]
    [InlineData("pi")]
    [InlineData("hello")]
    [InlineData("notepad")]
    [InlineData("2 +")]
    [InlineData("1/0")]
    [InlineData("foo(2)")]
    [InlineData("Process.Start(1)")]
    [InlineData("2 2")]
    public void Rejects(string input) => Assert.False(Calculator.TryEvaluate(input, out _));

    [Theory]
    [InlineData(1234567.891, "1,234,567.891")]
    [InlineData(0.1 + 0.2, "0.3")]
    [InlineData(1e20, "1E+20")]
    [InlineData(-2.5, "-2.5")]
    [InlineData(1.0 / 3, "0.3333333333")]
    public void Formats(double v, string text) => Assert.Equal(text, Calculator.Format(v));
}

public class UnitTests
{
    [Theory]
    [InlineData("10 km to mi", 6.2137119224, "mi")]
    [InlineData("1 mi in km", 1.609344, "km")]
    [InlineData("12 in to cm", 30.48, "cm")]
    [InlineData("6 ft to m", 1.8288, "m")]
    [InlineData("100 c to f", 212, "°F")]
    [InlineData("32 °F to °C", 0, "°C")]
    [InlineData("0 k in c", -273.15, "°C")]
    [InlineData("1 kg to lb", 2.2046226218, "lb")]
    [InlineData("16 oz to g", 453.59237, "g")]
    [InlineData("5 GB to MB", 5000, "MB")]
    [InlineData("1 GiB to MiB", 1024, "MiB")]
    [InlineData("3 кг в фунти", 6.6138678655, "lb")]
    [InlineData("5 км у милі", 3.1068559612, "mi")]
    [InlineData("1 000 m to km", 1, "km")]
    [InlineData("2,5 km to m", 2500, "m")]
    public void Converts(string input, double expected, string unit)
    {
        Assert.True(Calculator.TryEvaluate(input, out var r), input);
        Assert.Equal(expected, r.Value, 6);
        Assert.Equal(unit, r.Unit);
    }

    [Theory]
    [InlineData("10 km to kg")]
    [InlineData("10 parsecs to km")]
    [InlineData("km to mi")]
    public void Rejects_mismatched_or_unknown(string input) => Assert.False(Units.TryConvert(input, out _, out _));
}

public class FuzzyTests
{
    [Fact]
    public void Ranking_prefix_beats_word_start_beats_acronym_beats_substring_beats_subsequence()
    {
        var prefix = Fuzzy.Score("vis", "Visual Studio Code")!.Value;
        var word = Fuzzy.Score("stu", "Visual Studio Code")!.Value;
        var acronym = Fuzzy.Score("vsc", "Visual Studio Code")!.Value;
        var substring = Fuzzy.Score("udi", "Visual Studio Code")!.Value;
        var subsequence = Fuzzy.Score("vsco", "Visual Studio Code")!.Value;
        Assert.True(prefix > word, $"{prefix} > {word}");
        Assert.True(word > acronym, $"{word} > {acronym}");
        Assert.True(acronym > substring, $"{acronym} > {substring}");
        Assert.True(substring > subsequence, $"{substring} > {subsequence}");
    }

    [Theory]
    [InlineData("xyz", "Visual Studio Code")]
    [InlineData("codev", "Visual Studio Code")]
    [InlineData("", "anything")]
    public void No_match(string q, string text) => Assert.Null(Fuzzy.Score(q, text));

    [Fact]
    public void Case_insensitive_and_shorter_names_win()
    {
        Assert.NotNull(Fuzzy.Score("NOTE", "notepad"));
        Assert.True(Fuzzy.Score("note", "Notepad") > Fuzzy.Score("note", "Notepad++ Portable Edition Launcher"));
        Assert.True(Fuzzy.Score("chrome", "Chrome") > Fuzzy.Score("chrome", "Chrome Remote Desktop"));
    }

    [Fact]
    public void Camel_case_and_digits_are_word_starts()
    {
        Assert.Equal([0, 3, 9], Fuzzy.WordStarts("OneDrive 365"));
        Assert.NotNull(Fuzzy.Score("od", "OneDrive"));
        Assert.True(Fuzzy.Score("od", "OneDrive") > Fuzzy.Score("od", "Outlook Desktop Helper Thing Long"));
    }

    [Fact]
    public void Ukrainian_text()
    {
        Assert.NotNull(Fuzzy.Score("кальк", "Калькулятор"));
        Assert.NotNull(Fuzzy.Score("клк", "Калькулятор"));
    }
}

public class CurrencyTests
{
    [Theory]
    [InlineData("100 usd to eur", 100, "USD", "EUR")]
    [InlineData("100 USD in UAH", 100, "USD", "UAH")]
    [InlineData("$20 in uah", 20, "USD", "UAH")]
    [InlineData("€ 5 to грн", 5, "EUR", "UAH")]
    [InlineData("100 євро в гривню", 100, "EUR", "UAH")]
    [InlineData("1 000 гривень у долари", 1000, "UAH", "USD")]
    [InlineData("2,5 eur to pln", 2.5, "EUR", "PLN")]
    [InlineData("usd to pln", 1, "USD", "PLN")]
    [InlineData("50 злотих на євро", 50, "PLN", "EUR")]
    [InlineData("10 dollars into yen", 10, "USD", "JPY")]
    public void Parses(string input, double amount, string from, string to)
    {
        var q = CurrencyParser.Parse(input);
        Assert.NotNull(q);
        Assert.Equal((decimal)amount, q.Amount);
        Assert.Equal(from, q.From);
        Assert.Equal(to, q.To);
    }

    [Theory]
    [InlineData("10 km to mi")]
    [InlineData("100 to eur")]
    [InlineData("100 usd")]
    [InlineData("hello there")]
    [InlineData("100 xyz to eur")]
    public void Rejects(string input) => Assert.Null(CurrencyParser.Parse(input));

    const string NbuJson = """
        [{"r030":840,"txt":"Долар США","rate":41.5,"cc":"USD","exchangedate":"14.11.2026"},
         {"r030":978,"txt":"Євро","rate":48.0,"cc":"EUR","exchangedate":"14.11.2026"},
         {"r030":985,"txt":"Злотий","rate":11.2,"cc":"PLN","exchangedate":"14.11.2026"}]
        """;

    [Fact]
    public void Parses_nbu_and_converts_through_uah()
    {
        var s = NbuRatesSource.Parse(NbuJson, DateTimeOffset.Now)!;
        Assert.Equal(new DateOnly(2026, 11, 14), s.Date);
        Assert.Equal(4150m, s.Convert(100, "USD", "UAH"));
        Assert.Equal(1m, s.Convert(48, "UAH", "EUR"));
        Assert.Equal(111.036m, Math.Round(s.Convert(96, "EUR", "USD")!.Value, 3));
        Assert.Null(s.Convert(1, "USD", "XYZ"));
        Assert.Null(NbuRatesSource.Parse("not json", DateTimeOffset.Now));
        Assert.Null(NbuRatesSource.Parse("[]", DateTimeOffset.Now));
    }

    sealed class FakeRates : IRatesSource
    {
        public int Calls;
        public bool Fail;
        public Task<RatesSnapshot?> FetchAsync(CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(Fail ? null : NbuRatesSource.Parse(NbuJson, Now));
        }
        public DateTimeOffset Now = new(2026, 11, 15, 8, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public async Task Cache_for_12h_then_refresh_and_offline_fallback()
    {
        var file = Path.Combine(Path.GetTempPath(), "isle-rates-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var src = new FakeRates();
            var now = src.Now;
            var svc = new CurrencyService(src, file, () => now);
            Assert.NotNull(await svc.GetAsync(default));
            now = now.AddHours(11);
            Assert.NotNull(await svc.GetAsync(default));
            Assert.Equal(1, src.Calls);

            now = now.AddHours(2); // older than 12 h → refetch
            src.Now = now;
            await svc.GetAsync(default);
            Assert.Equal(2, src.Calls);

            // a new process, offline, cache stale: still answers from the cache
            src.Fail = true;
            var later = now.AddDays(3);
            var offline = new CurrencyService(src, file, () => later);
            var snap = await offline.GetAsync(default);
            Assert.NotNull(snap);
            Assert.Equal(41.5m, snap.Rates["USD"]);
            Assert.Equal(3, src.Calls);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task Nothing_cached_and_offline_gives_null()
    {
        var svc = new CurrencyService(new FakeRates { Fail = true }, Path.Combine(Path.GetTempPath(), "isle-none-" + Guid.NewGuid().ToString("N") + ".json"));
        Assert.Null(await svc.GetAsync(default));
    }
}

public class CommandBarTests
{
    static void ClickRow(Isle.Views.Commands.CommandBarView view, int index)
    {
        var rows = (System.Windows.Controls.StackPanel)((System.Windows.Controls.ScrollViewer)view.Children[1]).Content;
        rows.Children[index].RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0,
            System.Windows.Input.MouseButton.Left) { RoutedEvent = System.Windows.UIElement.MouseLeftButtonUpEvent });
    }

    sealed class Delayed(int delayMs, params CommandItem[] items) : ICommandProvider
    {
        public async Task<IEnumerable<CommandItem>> QueryAsync(string text, CancellationToken ct)
        {
            await Task.Delay(delayMs, ct);
            return items;
        }
    }

    sealed class Pending : ICommandProvider
    {
        public readonly TaskCompletionSource<IEnumerable<CommandItem>> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IEnumerable<CommandItem>> QueryAsync(string text, CancellationToken ct) => Completion.Task.WaitAsync(ct);
    }

    [Fact]
    public void Clicking_an_early_row_runs_that_item_even_when_a_better_result_arrives_later()
    {
        FakeSink.Run(sink =>
        {
            var runs = 0;
            var early = new CommandItem { Title = "early", Score = 5, Run = () => runs++ };
            var late = new CommandItem { Title = "late", Score = 1000, Run = () => runs += 100 };
            var pending = new Pending();
            var view = new Isle.Views.Commands.CommandBarView([new Delayed(0, early), pending], i => i.Run!(), null);
            view.Text = "query";
            view.Query();
            Assert.True(sink.PumpUntil(() => view.Results.Count == 1));
            ClickRow(view, 0);
            Assert.Equal(1, runs);
            pending.Completion.SetResult([late]);
            Assert.True(sink.PumpUntil(() => view.Results.Count == 2));
            Assert.Equal(1, runs);
            Assert.Same(early, view.Results[view.SelectedIndex]);
        });
    }

    [Fact]
    public void Clicking_an_early_system_row_still_requires_confirmation_and_late_results_disarm_it()
    {
        FakeSink.Run(sink =>
        {
            var runs = 0;
            var system = new CommandItem { Title = "system", Score = 5, Confirm = true, Run = () => runs++ };
            var late = new CommandItem { Title = "late", Score = 1000, Run = () => runs += 100 };
            var pending = new Pending();
            var view = new Isle.Views.Commands.CommandBarView([new Delayed(0, system), pending], i => i.Run!(), null);
            view.Text = "query";
            view.Query();
            Assert.True(sink.PumpUntil(() => view.Results.Count == 1));
            ClickRow(view, 0);
            ClickRow(view, 0);
            Assert.Equal(0, runs);
            pending.Completion.SetResult([late]);
            Assert.True(sink.PumpUntil(() => view.Results.Count == 2));
            var wait = System.Diagnostics.Stopwatch.StartNew();
            Assert.True(sink.PumpUntil(() => wait.ElapsedMilliseconds >= 450));
            ClickRow(view, view.SelectedIndex);
            Assert.Equal(0, runs);
        });
    }

    [Fact]
    public void Late_better_result_takes_the_selection_and_fast_double_enter_does_not_confirm()
    {
        FakeSink.Run(sink =>
        {
            var runs = 0;
            var web = new CommandItem { Title = "web", Score = 5, Run = () => runs += 100 };
            var shutdown = new CommandItem { Title = "Shut down", Score = 1000, Confirm = true, Run = () => runs++ };
            var view = new Isle.Views.Commands.CommandBarView([new Delayed(0, web), new Delayed(300, shutdown)], i => i.Run!(), null);
            view.Text = "shutdown";
            view.Query();
            Assert.True(sink.PumpUntil(() => view.Results.Count == 2));
            Assert.Same(shutdown, view.Results[view.SelectedIndex]);
            view.Execute();
            Assert.Equal(0, runs); // first Enter only arms
            view.Execute();
            Assert.Equal(0, runs); // a fast double Enter must not confirm
            var wait = System.Diagnostics.Stopwatch.StartNew();
            Assert.True(sink.PumpUntil(() => wait.ElapsedMilliseconds >= 450));
            view.Execute();
            Assert.Equal(1, runs);
        });
    }

    [Fact]
    public void Enter_during_debounce_waits_for_all_results_and_runs_the_best_once()
    {
        FakeSink.Run(sink =>
        {
            var runs = 0;
            var web = new CommandItem { Title = "web", Score = 5, Run = () => runs += 100 };
            var best = new CommandItem { Title = "app", Score = 1000, Run = () => runs++ };
            var view = new Isle.Views.Commands.CommandBarView([new Delayed(0, web), new Delayed(160, best)], i => i.Run!(), null);
            view.Text = "app";
            view.Execute();
            Assert.True(sink.PumpUntil(() => runs != 0));
            Assert.Equal(1, runs);
        });
    }

    [Fact]
    public void A_new_query_disarms_confirmation()
    {
        FakeSink.Run(sink =>
        {
            var runs = 0;
            var item = new CommandItem { Title = "system", Score = 100, Confirm = true, Run = () => runs++ };
            var view = new Isle.Views.Commands.CommandBarView([new Delayed(0, item)], i => i.Run!(), null);
            view.Text = "system";
            view.Query();
            Assert.True(sink.PumpUntil(() => view.Results.Count == 1));
            view.Execute();
            var wait = System.Diagnostics.Stopwatch.StartNew();
            Assert.True(sink.PumpUntil(() => wait.ElapsedMilliseconds >= 450));
            view.Query();
            Assert.True(sink.PumpUntil(() => view.Results.Count == 1));
            view.Execute();
            Assert.Equal(0, runs);
        });
    }
}
