using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Isle.Core;
using Isle.Modules.Reminders;
using Isle.Modules.Timers;

namespace Isle.Modules.Commands;

/// <summary>One row in the command bar.</summary>
public sealed class CommandItem
{
    public required string Title { get; init; }
    public string Subtitle { get; init; } = "";
    /// <summary>Icon resource key ("Icon.Search") or null when <see cref="IconPath"/> gives a shell icon.</summary>
    public string? IconKey { get; init; }
    public string? IconPath { get; init; }
    public int Score { get; init; }
    public Action? Run { get; init; }
    /// <summary>Ask "press Enter again" before <see cref="Run"/> (system commands).</summary>
    public bool Confirm { get; init; }
    public string Confirmation { get; init; } = "";
    public Action? Undo { get; init; }
}

/// <summary>Asks the AI chat (M11) a question from the command bar.</summary>
public sealed record AskAi(string Question);

public interface ICommandProvider
{
    /// <summary>Runs off the UI thread. Must be quick or honour <paramref name="ct"/>.</summary>
    Task<IEnumerable<CommandItem>> QueryAsync(string text, CancellationToken ct);
}

public sealed class CalculatorProvider(Action<string> copy) : ICommandProvider
{
    public Task<IEnumerable<CommandItem>> QueryAsync(string text, CancellationToken ct)
    {
        if (!Calculator.TryEvaluate(text, out var r)) return Task.FromResult(Enumerable.Empty<CommandItem>());
        var shown = r.Unit is null ? Calculator.Format(r.Value, Culture()) : Calculator.Format(r.Value, Culture()) + " " + r.Unit;
        var plain = Calculator.Format(r.Value).Replace(",", "");
        IEnumerable<CommandItem> list = [new CommandItem { Title = "= " + shown, Subtitle = Strings.T("cmd.copyResult"), IconKey = "Icon.Calc", Score = 5000, Run = () => copy(plain) }];
        return Task.FromResult(list);
    }

    internal static CultureInfo Culture() => Strings.Language == "uk" ? CultureInfo.GetCultureInfo("uk-UA") : CultureInfo.InvariantCulture;
}

public sealed class CurrencyProvider(CurrencyService rates, Action<string> copy) : ICommandProvider
{
    public async Task<IEnumerable<CommandItem>> QueryAsync(string text, CancellationToken ct)
    {
        var q = CurrencyParser.Parse(text);
        if (q is null) return [];
        var snap = await rates.GetAsync(ct).ConfigureAwait(false);
        if (snap is null) return [new CommandItem { Title = $"{q.Amount} {q.From} → {q.To}", Subtitle = Strings.T("cmd.ratesUnavailable"), IconKey = "Icon.Currency", Score = 4800 }];
        var v = snap.Convert(q.Amount, q.From, q.To);
        if (v is null) return [];
        var value = Math.Round(v.Value, v.Value >= 100 ? 2 : 4);
        var shown = value.ToString("#,##0.####", CalculatorProvider.Culture());
        var date = snap.Date == default ? "" : snap.Date.ToString("d MMM yyyy", CalculatorProvider.Culture());
        return [new CommandItem
        {
            Title = $"{q.Amount.ToString("#,##0.##", CalculatorProvider.Culture())} {q.From} = {shown} {q.To}",
            Subtitle = Strings.T("cmd.rateOf", date) + " · " + Strings.T("cmd.copyResult"),
            IconKey = "Icon.Currency", Score = 4900,
            Run = () => copy(value.ToString(CultureInfo.InvariantCulture)),
        }];
    }
}

/// <summary>"timer 10m", "таймер 25 хв", "remind me …", "нагадай …" → the timers module.</summary>
public sealed partial class TimerProvider(Func<TimersController?> controller, Action<string> notify) : ICommandProvider
{
    [GeneratedRegex(@"^(?:timer|таймер)\s+(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex TimerPrefix();

    public Task<IEnumerable<CommandItem>> QueryAsync(string text, CancellationToken ct)
    {
        var list = new List<CommandItem>();
        var t = TimerPrefix().Match(text.Trim());
        if (t.Success && DurationParser.TryParse(t.Groups[1].Value, out var d, out var label))
        {
            var ctl = controller();
            var unavailable = ctl is null ? Strings.T("cmd.timersOff") : ctl.Timers.Timers.Count >= TimerBook.MaxTimers ? Strings.T("timers.full") : null;
            list.Add(new CommandItem
            {
                Title = Strings.T("cmd.startTimer", DurationParser.Describe(d, k => Strings.T("unit." + k))) + (label.Length > 0 ? $" · {label}" : ""),
                Subtitle = unavailable ?? Strings.T("tab.timers"), IconKey = "Icon.Timer", Score = 4700,
                Run = unavailable is not null ? null : () =>
                {
                    var current = controller();
                    notify(Strings.T(current is null ? "cmd.timersOff" : current.AddTimer(d, label) ? "cmd.timerStarted" : current.LastSaveFailed ? "timers.saveFailed" : "timers.full"));
                },
            });
        }
        else if (ReminderParser.HasCommandPrefix(text))
        {
            var ctl = controller();
            if (ctl is not null && ReminderParser.Parse(text, ctl.Clock(), ctl.Zone) is { } parsed)
            {
                var due = TimeZoneInfo.ConvertTime(parsed.Due, ctl.Zone);
                list.Add(new CommandItem
                {
                    Title = parsed.Text.Length > 0 ? parsed.Text : Strings.T("cmd.reminder"),
                    Subtitle = Strings.T("cmd.remindAt", due.ToString("ddd d MMM HH:mm", CalculatorProvider.Culture())),
                    IconKey = "Icon.Bell", Score = 4700,
                    Run = () =>
                    {
                        var current = controller();
                        notify(Strings.T(current is null ? "cmd.timersOff" : current.AddReminder(text) is not null ? "cmd.reminderSet" : current.LastSaveFailed ? "timers.saveFailed" : "timers.invalid"));
                    },
                });
            }
            else
                list.Add(new CommandItem { Title = Strings.T("cmd.reminder"), Subtitle = Strings.T(ctl is null ? "cmd.timersOff" : "timers.invalid"), IconKey = "Icon.Bell", Score = 4700 });
        }
        return Task.FromResult<IEnumerable<CommandItem>>(list);
    }
}

/// <summary>Lock, sleep, restart, shut down (all confirmed), Settings pages, empty recycle bin.</summary>
public sealed class SystemProvider : ICommandProvider
{
    sealed record Cmd(string Key, string[] Words, string Icon, bool Confirm, Action Run, string? Consequence = null, Action? Undo = null);

    static readonly Cmd[] Commands =
    [
        new("cmd.sys.lock", ["lock", "заблокувати", "блокування"], "Icon.Lock", true, () => LockWorkStation()),
        new("cmd.sys.sleep", ["sleep", "suspend", "сон", "сплячий режим"], "Icon.Moon", true, () => SetSuspendState(false, false, false)),
        new("cmd.sys.restart", ["restart", "reboot", "перезавантажити", "перезапуск"], "Icon.Power", true, () => Run("shutdown.exe", "/r /t 10"), "cmd.restartWarning", () => Run("shutdown.exe", "/a")),
        new("cmd.sys.shutdown", ["shutdown", "shut down", "power off", "вимкнути", "завершити роботу"], "Icon.Power", true, () => Run("shutdown.exe", "/s /t 10"), "cmd.shutdownWarning", () => Run("shutdown.exe", "/a")),
        new("cmd.sys.abort", ["cancel shutdown", "abort shutdown", "скасувати вимкнення", "скасувати перезапуск"], "Icon.Close", false, () => Run("shutdown.exe", "/a")),
        new("cmd.sys.recycle", ["empty recycle bin", "recycle bin", "trash", "кошик", "очистити кошик"], "Icon.Trash", true, () => SHEmptyRecycleBin(IntPtr.Zero, null, 0x6), "cmd.recycleWarning"),
        new("cmd.set.display", ["settings display", "display", "налаштування дисплей", "дисплей", "екран"], "Icon.Settings", false, () => Open("ms-settings:display")),
        new("cmd.set.sound", ["settings sound", "sound", "налаштування звук", "звук"], "Icon.Settings", false, () => Open("ms-settings:sound")),
        new("cmd.set.bluetooth", ["settings bluetooth", "bluetooth", "налаштування bluetooth", "блютуз"], "Icon.Settings", false, () => Open("ms-settings:bluetooth")),
        new("cmd.set.network", ["settings network", "network", "wi-fi", "wifi", "налаштування мережа", "мережа"], "Icon.Settings", false, () => Open("ms-settings:network")),
        new("cmd.set.personalization", ["settings personalization", "personalization", "wallpaper", "персоналізація", "шпалери"], "Icon.Settings", false, () => Open("ms-settings:personalization")),
        new("cmd.set.apps", ["settings apps", "apps", "uninstall", "програми", "видалити програму"], "Icon.Settings", false, () => Open("ms-settings:appsfeatures")),
        new("cmd.set.update", ["windows update", "update", "оновлення"], "Icon.Settings", false, () => Open("ms-settings:windowsupdate")),
        new("cmd.set.power", ["settings power", "battery", "power", "батарея", "живлення"], "Icon.Settings", false, () => Open("ms-settings:powersleep")),
        new("cmd.set.settings", ["settings", "налаштування", "параметри"], "Icon.Settings", false, () => Open("ms-settings:")),
    ];

    public Task<IEnumerable<CommandItem>> QueryAsync(string text, CancellationToken ct)
    {
        var q = text.Trim();
        var list = new List<CommandItem>();
        if (q.Length < 2) return Task.FromResult<IEnumerable<CommandItem>>(list);
        foreach (var c in Commands)
        {
            var best = c.Words.Append(Strings.T(c.Key)).Select(w => Fuzzy.Score(q, w)).Max();
            if (best is not { } s || s < 500) continue; // system commands need a close match
            list.Add(new CommandItem { Title = Strings.T(c.Key), Subtitle = c.Consequence is { } key ? Strings.T(key) : c.Confirm ? Strings.T("cmd.needsConfirm") : Strings.T("cmd.system"), Confirmation = c.Consequence is { } consequence ? Strings.T(consequence) : "", IconKey = c.Icon, Score = s + 100, Confirm = c.Confirm, Run = c.Run, Undo = c.Undo });
        }
        return Task.FromResult<IEnumerable<CommandItem>>(list);
    }

    static void Run(string exe, string args) => Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), exe), args) { UseShellExecute = false, CreateNoWindow = true });
    internal static void Open(string target) => Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });

    [DllImport("user32.dll")]
    static extern bool LockWorkStation();

    [DllImport("powrprof.dll")]
    static extern bool SetSuspendState(bool hibernate, bool force, bool wakeupEventsDisabled);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHEmptyRecycleBin(IntPtr hwnd, string? rootPath, uint flags);
}

/// <summary>URLs open directly, "g …" searches the web, "? …" asks the AI chat; otherwise a low-ranked "search the web" fallback.</summary>
public sealed partial class WebProvider : ICommandProvider
{
    [GeneratedRegex(@"^(?:https?://)?(?:[a-z0-9-]+\.)+[a-z]{2,}(?::\d+)?(?:/\S*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex Url();

    public Task<IEnumerable<CommandItem>> QueryAsync(string text, CancellationToken ct)
    {
        var q = text.Trim();
        var list = new List<CommandItem>();
        if (q.Length == 0 || ReminderParser.HasCommandPrefix(q)) return Task.FromResult<IEnumerable<CommandItem>>(list);
        if (q.StartsWith("? ") || q.StartsWith('?') && q.Length > 1)
        {
            var question = q[1..].Trim();
            var ai = EventBus.HasSubscribers<AskAi>();
            list.Add(new CommandItem
            {
                Title = Strings.T("cmd.askAi", question), Subtitle = ai ? Strings.T("tab.ai") : Strings.T("cmd.aiOff"), IconKey = "Icon.Ai", Score = 4600,
                Run = ai ? () => EventBus.Publish(new AskAi(question)) : null,
            });
            return Task.FromResult<IEnumerable<CommandItem>>(list);
        }
        if (q.StartsWith("g ", StringComparison.OrdinalIgnoreCase) && q.Length > 2)
        {
            var term = q[2..].Trim();
            list.Add(Search(term, 4600));
            return Task.FromResult<IEnumerable<CommandItem>>(list);
        }
        if (!q.Contains(' ') && Url().IsMatch(q) && !q.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            var url = q.Contains("://") ? q : "https://" + q;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
                list.Add(new CommandItem { Title = Strings.T("cmd.openUrl", uri.Host), Subtitle = uri.ToString(), IconKey = "Icon.Globe", Score = 4500, Run = () => SystemProvider.Open(uri.ToString()) });
        }
        list.Add(Search(q, 5));
        return Task.FromResult<IEnumerable<CommandItem>>(list);
    }

    static CommandItem Search(string term, int score) => new()
    {
        Title = Strings.T("cmd.searchWeb", term), Subtitle = "google.com", IconKey = "Icon.Search", Score = score,
        Run = () => SystemProvider.Open("https://www.google.com/search?q=" + Uri.EscapeDataString(term)),
    };
}

/// <summary>Start-menu apps, recent files and the configured folders, matched with <see cref="Fuzzy"/>.</summary>
public sealed class AppsProvider(AppIndex index) : ICommandProvider
{
    public Task<IEnumerable<CommandItem>> QueryAsync(string text, CancellationToken ct)
    {
        var q = text.Trim();
        if (q.Length == 0) return Task.FromResult(Enumerable.Empty<CommandItem>());
        var results = new List<(int Score, IndexEntry E)>();
        foreach (var e in index.Entries)
        {
            if (ct.IsCancellationRequested) break;
            if (Fuzzy.Score(q, e.Name) is not { } s) continue;
            s += e.Kind switch { EntryKind.App => 60, EntryKind.Recent => -40, _ => -120 };
            results.Add((s, e));
        }
        var items = results.OrderByDescending(r => r.Score).Take(12).Select(r => new CommandItem
        {
            Title = r.E.Name,
            Subtitle = r.E.Kind switch { EntryKind.App => Strings.T("cmd.app"), EntryKind.Recent => Strings.T("cmd.recent"), _ => r.E.Path },
            IconPath = r.E.Path, Score = r.Score,
            Run = () => SystemProvider.Open(r.E.Path),
        });
        return Task.FromResult(items);
    }
}
