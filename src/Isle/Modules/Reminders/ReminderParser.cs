using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Isle.Modules.Reminders;

public sealed record ParsedReminder(string Text, DateTimeOffset Due, RepeatRule? Repeat);

/// <summary>
/// Natural-language reminder phrases in English and Ukrainian, e.g. "remind me in 2 hours to call mom",
/// "tomorrow 9:00 submit report", "every monday 08:30 gym", "нагадай через 2 години подзвонити",
/// "щодня о 22:00 пити воду". Table-driven: each rule recognises one kind of fragment (relative time, repeat,
/// time of day, date, weekday, part of day) and consumes its span; what is left over is the reminder text.
/// </summary>
public static class ReminderParser
{
    // ---- vocabulary ----

    const string L = @"(?<![\p{L}\d'])";   // left word boundary (apostrophe counts as a letter: п'ятниця)
    const string R = @"(?![\p{L}'])";        // right word boundary

    static readonly (string Pattern, int Value)[] NumberWords =
    [
        ("an?|one|один|одна|одну|одне", 1), ("two|два|дві|couple(?:\\s+of)?", 2), ("three|три", 3), ("four|чотири", 4),
        ("five|п'ять", 5), ("six|шість", 6), ("seven|сім", 7), ("eight|вісім", 8), ("nine|дев'ять", 9), ("ten|десять", 10),
        ("fifteen|п'ятнадцять", 15), ("twenty|двадцять", 20), ("thirty|тридцять", 30), ("forty|сорок", 40),
    ];

    static readonly string NumberWord = string.Join("|", NumberWords.Select(n => n.Pattern));
    static readonly string Num = $@"(?:\d+(?:[.,]\d+)?|{NumberWord})";

    // unit name → kind
    const string UMin = @"minutes?|mins?|m|хвилин[аиу]?|хв";
    const string UHour = @"hours?|hrs?|h|годин[аиу]?|год";
    const string USec = @"seconds?|secs?|s|секунд[аиу]?|сек";
    const string UDay = @"days?|d|день|дн(?:і|ів|я)|доб[аиу]";
    const string UWeek = @"weeks?|w|тиждень|тижн(?:і|ів|я)";
    const string UMonth = @"months?|місяць|місяці|місяців|місяця";
    static readonly string AnyUnit = $"{UMin}|{UHour}|{USec}|{UDay}|{UWeek}";

    static readonly (string Pattern, DayOfWeek Day)[] Weekdays =
    [
        (@"mon(?:day)?s?|понеділ(?:ок|ка|ки|ках)", DayOfWeek.Monday),
        (@"tue(?:s|sday)?s?|вівтор(?:ок|ка|ки|ках)", DayOfWeek.Tuesday),
        (@"wed(?:nesday)?s?|серед[аиуі]|середах", DayOfWeek.Wednesday),
        (@"thu(?:r|rs|rsday)?s?|четвер(?:га|ги|гах)?", DayOfWeek.Thursday),
        (@"fri(?:day)?s?|п'ятниц(?:я|ю|і|ях)", DayOfWeek.Friday),
        (@"sat(?:urday)?s?|субот(?:а|у|и|ах)", DayOfWeek.Saturday),
        (@"sun(?:day)?s?|неділ(?:я|ю|і|ях)", DayOfWeek.Sunday),
    ];
    static readonly string Weekday = string.Join("|", Weekdays.Select(w => w.Pattern));

    // "щопонеділка", "щоп'ятниці" ...
    static readonly (string Pattern, DayOfWeek Day)[] EveryWeekdayUk =
    [
        ("щопонеділка", DayOfWeek.Monday), ("щовівторка", DayOfWeek.Tuesday), ("щосереди", DayOfWeek.Wednesday),
        ("щочетверга", DayOfWeek.Thursday), ("щоп'ятниці", DayOfWeek.Friday), ("щосуботи", DayOfWeek.Saturday), ("щонеділі", DayOfWeek.Sunday),
    ];

    static readonly (string Pattern, int Month)[] Months =
    [
        (@"jan(?:uary)?|січ(?:ень|ня)", 1), (@"feb(?:ruary)?|лют(?:ий|ого)", 2), (@"mar(?:ch)?|берез(?:ень|ня)", 3),
        (@"apr(?:il)?|квіт(?:ень|ня)", 4), (@"may|трав(?:ень|ня)", 5), (@"june?|черв(?:ень|ня)", 6), (@"july?|лип(?:ень|ня)", 7),
        (@"aug(?:ust)?|серп(?:ень|ня)", 8), (@"sep(?:t|tember)?|верес(?:ень|ня)", 9), (@"oct(?:ober)?|жовт(?:ень|ня)", 10),
        (@"nov(?:ember)?|листопад(?:а)?", 11), (@"dec(?:ember)?|груд(?:ень|ня)", 12),
    ];
    static readonly string Month = string.Join("|", Months.Select(m => m.Pattern));

    const string Morning = @"in\s+the\s+morning|morning|вранці|зранку|ранку|ранок";
    const string Afternoon = @"in\s+the\s+afternoon|afternoon|вдень|дня|після\s+обіду";
    const string Evening = @"in\s+the\s+evening|evening|tonight|ввечері|увечері|вечора|вечір";
    const string Night = @"at\s+night|night|вночі|уночі|ночі";

    sealed class State
    {
        public TimeSpan Relative;
        public bool HasRelative;
        public int? Hour, Minute;
        public bool AmbiguousHour; // "at 5" without am/pm
        public int DayOffset = -1; // today=0, tomorrow=1...
        public int DateYear, DateMonth, DateDay; // DateMonth > 0 when a calendar date was given
        public bool HasDate => DateMonth > 0;
        public int? MonthDay; // "on the 5th" without a month
        public DayOfWeek? Weekday;
        public bool NextWeek;
        public int? PartOfDay; // default hour for morning/evening...
        public RepeatRule? Repeat;
        /// <summary>Span and amount of a relative time introduced by Ukrainian "за", which also means "for"
        /// ("звіт за тиждень"); dropped again when the phrase has an explicit time.</summary>
        public (int Index, int Length, TimeSpan Amount)? ZaRelative;
    }

    sealed class Rule(string pattern, Func<Match, State, bool> apply)
    {
        public readonly Regex Regex = new(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        public readonly Func<Match, State, bool> Apply = apply;
    }

    static readonly Rule[] Rules = BuildRules();

    static Rule[] BuildRules()
    {
        var list = new List<Rule>
        {
            // ---- repeats ----
            new($@"{L}(?:every|each)\s+(other|{Num})?\s*({UMin}|{UHour}|{UDay}|{UWeek}|{UMonth}){R}", (m, s) =>
            {
                var n = m.Groups[1].Value.Equals("other", StringComparison.OrdinalIgnoreCase) ? 2 : m.Groups[1].Success ? (int)Math.Round(NumberOf(m.Groups[1].Value)) : 1;
                s.Repeat = RepeatFor(m.Groups[2].Value, n);
                return s.Repeat is not null;
            }),
            new($@"{L}кожн(?:і|их)\s+({Num})\s*({UMin}|{UHour}|{UDay}|{UWeek}|{UMonth}){R}", (m, s) =>
            {
                s.Repeat = RepeatFor(m.Groups[2].Value, (int)Math.Round(NumberOf(m.Groups[1].Value)));
                return s.Repeat is not null;
            }),
            new($@"{L}(?:daily|every\s*day|щодня|щоденно|кож(?:ен|ного)\s+дня?|кожен\s+день|кожного\s+дня|кожний\s+день){R}", (_, s) => { s.Repeat = RepeatRule.Daily(); return true; }),
            new($@"{L}(?:every\s+morning|щоранку|кожного\s+ранку){R}", (_, s) => { s.Repeat = RepeatRule.Daily(); s.PartOfDay ??= 9; return true; }),
            new($@"{L}(?:every\s+(?:evening|night)|щовечора|кожного\s+вечора){R}", (_, s) => { s.Repeat = RepeatRule.Daily(); s.PartOfDay ??= 19; return true; }),
            new($@"{L}(?:hourly|щогодини|кожну\s+годину|кожної\s+години){R}", (_, s) => { s.Repeat = new RepeatRule { Kind = RepeatKind.EveryNHours, Interval = 1 }; return true; }),
            new($@"{L}(?:weekly|щотижня|кожного\s+тижня|кожен\s+тиждень){R}", (_, s) => { s.Repeat = RepeatRule.Weekly(); return true; }),
            new($@"{L}(?:monthly|щомісяця|кожного\s+місяця|кожен\s+місяць){R}", (_, s) => { s.Repeat = new RepeatRule { Kind = RepeatKind.Monthly }; return true; }),
            new($@"{L}(?:(?:every|on)\s+(?:weekday|workday|business\s+day)s?|(?:по|у|в)\s+будн(?:ях|і)|щобудня|кожного\s+буднього\s+дня){R}", (_, s) =>
            {
                s.Repeat = RepeatRule.Weekly(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);
                return true;
            }),
            new($@"{L}(?:every\s+weekend|(?:on\s+)?weekends|(?:по|на)\s+вихідних|щовихідних){R}", (_, s) =>
            {
                s.Repeat = RepeatRule.Weekly(DayOfWeek.Saturday, DayOfWeek.Sunday);
                return true;
            }),
            // "every monday and thursday", "кожного понеділка та четверга", "по понеділках"
            new($@"{L}(?:every|each|кож(?:ен|ного|ну|ної|ний)|по)\s+((?:{Weekday})(?:\s*(?:,|and|&|і|й|та)\s*(?:{Weekday}))*){R}", (m, s) =>
            {
                s.Repeat = RepeatRule.Weekly([.. DaysIn(m.Groups[1].Value)]);
                return s.Repeat.Days.Length > 0;
            }),
            // "щопонеділка і щоп'ятниці"
            new($@"{L}((?:{string.Join("|", EveryWeekdayUk.Select(e => e.Pattern))})(?:\s*(?:,|і|й|та)\s*(?:{string.Join("|", EveryWeekdayUk.Select(e => e.Pattern))}))*){R}", (m, s) =>
            {
                var days = EveryWeekdayUk.Where(e => m.Groups[1].Value.Contains(e.Pattern, StringComparison.OrdinalIgnoreCase)).Select(e => e.Day).ToArray();
                s.Repeat = RepeatRule.Weekly(days);
                return days.Length > 0;
            }),
            // monthly day: "on the 5th", "5-го числа", "5 числа"
            new($@"{L}(?:on\s+the\s+)?(\d{{1,2}})(?:st|nd|rd|th){R}(?!\s*(?:{Month}))", (m, s) =>
            {
                var d = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                return SetMonthDay(s, d);
            }),
            new($@"{L}(\d{{1,2}})(?:-?го)?\s+числа{R}", (m, s) =>
            {
                var d = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                return SetMonthDay(s, d);
            }),

            // ---- relative ("in 2 hours", "через 30 хвилин", "in 1h30m", "in half an hour", "через півтори години") ----
            new($@"{L}(?:in|after|через|за)\s+(?:half\s+an?\s+hour|пів\s*години|півгодини)({R})", (_, s) => AddRelative(s, TimeSpan.FromMinutes(30))),
            new($@"{L}(?:через|за)\s+півтори\s+години{R}", (_, s) => AddRelative(s, TimeSpan.FromMinutes(90))),
            new($@"{L}(?:in|after|через|за)\s+((?:(?:{Num})?\s*(?:{AnyUnit}){R}\s*(?:and\s+|і\s+|й\s+)?)+)", (m, s) =>
            {
                var total = TimeSpan.Zero;
                var units = Regex.Replace(m.Groups[1].Value, $@"{L}(?:and|і|й){R}", " "); // "and" must not read as "an d(ay)"
                foreach (Match g in Regex.Matches(units, $@"({Num})?\s*({AnyUnit}){R}", RegexOptions.IgnoreCase))
                {
                    var n = g.Groups[1].Success && g.Groups[1].Length > 0 ? NumberOf(g.Groups[1].Value) : 1;
                    total += UnitSpan(g.Groups[2].Value) * n;
                }
                if (total <= TimeSpan.Zero) return false;
                if (m.Value.StartsWith("за", StringComparison.Ordinal)) s.ZaRelative = (m.Index, m.Length, total);
                return AddRelative(s, total);
            }),

            // ---- time of day ----
            new($@"{L}(?:(?:at\s+)?(?:noon|midday)|(?:о\s+|в\s+)?(?:полудні|опівдні)){R}", (_, s) => SetTime(s, 12, 0, ambiguous: false)),
            new($@"{L}(?:(?:at\s+)?midnight|опівночі){R}", (_, s) => SetTime(s, 0, 0, ambiguous: false)),
            new($@"{L}(?:at|by|@|о|об|в|у|на)\s*(\d{{1,2}})(?:[:.](\d{{2}}))?\s*(a\.?m\.?|p\.?m\.?)?(?:\s+({Morning}|{Afternoon}|{Evening}|{Night}))?(?![\p{{L}}\d'.:])(?!\s*(?:{AnyUnit}|{UMonth}|{Month}|числа|-?го){R})", (m, s) => ApplyClock(m, s)),
            new($@"{L}(\d{{1,2}}):(\d{{2}})\s*(a\.?m\.?|p\.?m\.?)?(?![\d])", (m, s) => ApplyClock(m, s)),
            new($@"{L}(\d{{1,2}})\s*(a\.?m\.?|p\.?m\.?){R}", (m, s) =>
            {
                var h = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                return h is >= 1 and <= 12 && SetTime(s, To24(h, m.Groups[2].Value), 0, ambiguous: false);
            }),

            // ---- dates ----
            new($@"{L}(\d{{4}})-(\d{{1,2}})-(\d{{1,2}})(?!\d)", (m, s) => SetDate(s, Int(m, 1), Int(m, 2), Int(m, 3), hasYear: true)),
            new($@"{L}(?:on\s+)?(\d{{1,2}})[./](\d{{1,2}})(?:[./](\d{{2,4}}))?(?![\d:])", (m, s) =>
                SetDate(s, m.Groups[3].Success ? FullYear(Int(m, 3)) : 0, Int(m, 2), Int(m, 1), m.Groups[3].Success)),
            new($@"{L}(?:on\s+)?(\d{{1,2}})(?:st|nd|rd|th)?\s+(?:of\s+)?({Month})(?:\s+(\d{{4}}))?{R}", (m, s) =>
                SetDate(s, m.Groups[3].Success ? Int(m, 3) : 0, MonthOf(m.Groups[2].Value), Int(m, 1), m.Groups[3].Success)),
            new($@"{L}(?:on\s+)?({Month})\s+(\d{{1,2}})(?:st|nd|rd|th)?(?:,?\s+(\d{{4}}))?{R}", (m, s) =>
                SetDate(s, m.Groups[3].Success ? Int(m, 3) : 0, MonthOf(m.Groups[1].Value), Int(m, 2), m.Groups[3].Success)),

            // ---- day words ----
            new($@"{L}(?:(?:the\s+)?day\s+after\s+tomorrow|післязавтра){R}", (_, s) => SetDay(s, 2)),
            new($@"{L}(?:tomorrow|tmrw|завтра){R}", (_, s) => SetDay(s, 1)),
            new($@"{L}(?:today|сьогодні){R}", (_, s) => SetDay(s, 0)),
            new($@"{L}tonight{R}", (_, s) => { SetDay(s, 0); s.PartOfDay ??= 20; return true; }),

            // ---- single weekday ("on friday", "next monday", "у п'ятницю", "в наступний вівторок") ----
            new($@"{L}(?:(?:on|this)\s+|(next)\s+|(?:в|у|на)\s+(?:(наступн(?:ий|у|ого|ої))\s+)?|(наступн(?:ий|у|ого|ої))\s+)?({Weekday}){R}", (m, s) =>
            {
                var days = DaysIn(m.Groups[4].Value);
                if (days.Count != 1) return false;
                s.Weekday = days[0];
                s.NextWeek = m.Groups[1].Success || m.Groups[2].Success || m.Groups[3].Success;
                return true;
            }),

            // ---- part of day alone ----
            new($@"{L}(?:{Morning}){R}", (_, s) => { s.PartOfDay = 9; return true; }),
            new($@"{L}(?:{Afternoon}){R}", (_, s) => { s.PartOfDay = 14; return true; }),
            new($@"{L}(?:{Evening}){R}", (_, s) => { s.PartOfDay = 19; return true; }),
            new($@"{L}(?:{Night}){R}", (_, s) => { s.PartOfDay = 21; return true; }),
        };
        return [.. list];
    }

    // ---- entry point ----

    /// <summary>Parses a phrase. Returns null when it contains no recognisable time.</summary>
    public static ParsedReminder? Parse(string? phrase, DateTimeOffset now, TimeZoneInfo? tz = null)
    {
        if (string.IsNullOrWhiteSpace(phrase)) return null;
        tz ??= TimeZoneInfo.Local;
        var original = phrase.Trim();
        var text = Normalize(original);
        var consumed = new bool[text.Length];
        var state = new State();
        var any = false;

        foreach (var rule in Rules)
        {
            for (var m = rule.Regex.Match(text); m.Success; m = m.NextMatch())
            {
                if (m.Length == 0 || Overlaps(consumed, m.Index, m.Length)) continue;
                if (!rule.Apply(m, state)) continue;
                for (var i = m.Index; i < m.Index + m.Length; i++) consumed[i] = true;
                any = true;
                break; // each rule once
            }
        }
        if (!any) return null;

        if (state.ZaRelative is { } za && (state.Hour is not null || state.HasDate || state.DayOffset >= 0 || state.Weekday is not null || state.Repeat is not null))
        {
            state.Relative -= za.Amount;
            state.HasRelative = state.Relative != TimeSpan.Zero;
            for (var i = za.Index; i < za.Index + za.Length; i++) consumed[i] = false;
        }

        var due = Resolve(state, now, tz);
        if (due is null) return null;
        return new ParsedReminder(CleanText(original, consumed), due.Value, state.Repeat);
    }

    static DateTimeOffset? Resolve(State s, DateTimeOffset now, TimeZoneInfo tz)
    {
        var local = TimeZoneInfo.ConvertTime(now, tz).DateTime;

        // purely relative ("in 2 hours", "every 30 minutes")
        if (s.HasRelative && s.Hour is null && !s.HasDate && s.MonthDay is null && s.DayOffset < 0 && s.Weekday is null)
            return now + s.Relative;
        if (s.Repeat is { Kind: RepeatKind.EveryNHours or RepeatKind.EveryNMinutes } rep && s.Hour is null && s.PartOfDay is null)
            return now + (rep.Kind == RepeatKind.EveryNHours ? TimeSpan.FromHours(rep.Interval) : TimeSpan.FromMinutes(rep.Interval));

        int hour, minute = s.Minute ?? 0;
        if (s.Hour is { } h)
        {
            hour = h;
            if (s.PartOfDay is { } pod && h < 12 && pod >= 14) hour = h + 12;        // "at 7 in the evening"
            else if (s.PartOfDay is 9 && h == 12) hour = 0;
        }
        else hour = s.PartOfDay ?? 9;

        DateTime candidate;
        if (s.HasDate)
        {
            var y = s.DateYear > 0 ? s.DateYear : local.Year;
            DateTime? Build(int year) => s.DateDay <= DateTime.DaysInMonth(year, s.DateMonth)
                ? new DateTime(year, s.DateMonth, s.DateDay) + new TimeSpan(hour, minute, 0) : null;
            var built = Build(y);
            if (s.DateYear == 0 && (built is null || built <= local)) built = Build(y + 1) ?? Build(y + 4);
            if (built is null) return null;
            candidate = built.Value;
        }
        else if (s.DayOffset >= 0)
        {
            candidate = local.Date.AddDays(s.DayOffset) + new TimeSpan(hour, minute, 0);
            if (s.AmbiguousHour && s.Hour is <= 7) candidate = candidate.AddHours(12); // "tomorrow at 5" → 17:00
            else if (s.AmbiguousHour && s.DayOffset == 0 && candidate <= local && candidate.AddHours(12) > local) candidate = candidate.AddHours(12);
        }
        else if (s.Weekday is { } wd)
        {
            var days = ((int)wd - (int)local.DayOfWeek + 7) % 7;
            if (s.NextWeek && days == 0) days = 7;
            candidate = local.Date.AddDays(days) + new TimeSpan(hour, minute, 0);
            if (s.AmbiguousHour && s.Hour is <= 7) candidate = candidate.AddHours(12);
            if (candidate <= local) candidate = candidate.AddDays(7);
        }
        else if (s.Repeat is { Kind: RepeatKind.Monthly } || s.MonthDay is not null)
        {
            var dom = s.MonthDay ?? (s.Repeat!.DayOfMonth > 0 ? s.Repeat.DayOfMonth : local.Day);
            var month = new DateTime(local.Year, local.Month, 1);
            candidate = month.AddDays(Math.Min(dom, DateTime.DaysInMonth(month.Year, month.Month)) - 1) + new TimeSpan(hour, minute, 0);
            if (candidate <= local)
            {
                month = month.AddMonths(1);
                candidate = month.AddDays(Math.Min(dom, DateTime.DaysInMonth(month.Year, month.Month)) - 1) + new TimeSpan(hour, minute, 0);
            }
        }
        else if (s.Hour is not null || s.PartOfDay is not null || s.Repeat is not null)
        {
            candidate = local.Date + new TimeSpan(hour, minute, 0);
            if (s.AmbiguousHour && candidate <= local && candidate.AddHours(12) > local) candidate = candidate.AddHours(12);
            if (candidate <= local) candidate = candidate.AddDays(1);
        }
        else return null;

        if (s.HasRelative) candidate += s.Relative; // "tomorrow in 2 hours" — rare, but keep the information
        var due = RepeatRule.ToOffset(candidate, tz);

        // a weekly rule starts on its first matching day
        if (s.Repeat is { Kind: RepeatKind.Weekly, Days.Length: > 0 } weekly && !weekly.Days.Contains(candidate.DayOfWeek))
            due = weekly.Next(due, now, tz);
        else if (s.Repeat is { Kind: RepeatKind.Weekly, Days.Length: 0 } && s.Weekday is { } d)
            s.Repeat = RepeatRule.Weekly(d);
        if (s.Repeat is not null && due <= now) due = s.Repeat.Next(due, now, tz);
        return due;
    }

    // ---- rule helpers ----

    static bool AddRelative(State s, TimeSpan t)
    {
        s.Relative += t;
        s.HasRelative = true;
        return true;
    }

    static bool SetTime(State s, int hour, int minute, bool ambiguous)
    {
        if (s.Hour is not null || hour is < 0 or > 23 || minute is < 0 or > 59) return false;
        s.Hour = hour;
        s.Minute = minute;
        s.AmbiguousHour = ambiguous;
        return true;
    }

    static bool ApplyClock(Match m, State s)
    {
        var h = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var min = m.Groups[2].Success && m.Groups[2].Length > 0 ? int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) : 0;
        var ampm = m.Groups.Count > 3 && m.Groups[3].Success ? m.Groups[3].Value : "";
        var part = m.Groups.Count > 4 && m.Groups[4].Success ? m.Groups[4].Value : "";
        if (ampm.Length > 0)
        {
            if (h is < 1 or > 12) return false;
            return SetTime(s, To24(h, ampm), min, ambiguous: false);
        }
        if (part.Length > 0)
        {
            var pod = PartHour(part);
            s.PartOfDay = pod;
            if (pod >= 14 && h < 12) h += 12;
            else if (pod == 9 && h == 12) h = 0;
            return SetTime(s, h, min, ambiguous: false);
        }
        var written = m.Groups[1].Value;
        var ambiguous = h is >= 1 and <= 12 && !written.StartsWith('0') && !(m.Groups[2].Success && m.Groups[2].Length > 0 && h >= 10);
        // "о 9:30" or "at 9" are ambiguous only without minutes or for small hours; 24-hour style stays as written
        if (m.Groups[2].Success && m.Groups[2].Length > 0) ambiguous = false;
        return SetTime(s, h, min, ambiguous);
    }

    static int PartHour(string part)
    {
        var p = part.ToLowerInvariant();
        if (Regex.IsMatch(p, Morning)) return 9;
        if (Regex.IsMatch(p, Evening)) return 19;
        if (Regex.IsMatch(p, Night)) return 21;
        return 14;
    }

    static int To24(int h, string ampm)
    {
        var pm = ampm.StartsWith('p') || ampm.StartsWith('P');
        return pm ? (h % 12) + 12 : h % 12;
    }

    static bool SetDay(State s, int offset)
    {
        if (s.DayOffset >= 0 || s.HasDate) return false;
        s.DayOffset = offset;
        return true;
    }

    static bool SetDate(State s, int year, int month, int day, bool hasYear)
    {
        if (s.HasDate || month is < 1 or > 12 || day is < 1 or > 31) return false;
        if (hasYear && (year < 2000 || year > 2100 || day > DateTime.DaysInMonth(year, month))) return false;
        s.DateYear = hasYear ? year : 0;
        s.DateMonth = month;
        s.DateDay = day;
        return true;
    }

    /// <summary>"on the 5th": the day of a monthly repeat, or the next 5th as a one-off.</summary>
    static bool SetMonthDay(State s, int day)
    {
        if (day is < 1 or > 31 || s.MonthDay is not null || s.HasDate) return false;
        if (s.Repeat is { Kind: RepeatKind.Monthly } r) s.Repeat = r with { DayOfMonth = day };
        s.MonthDay = day;
        return true;
    }

    static int FullYear(int y) => y < 100 ? 2000 + y : y;

    static int Int(Match m, int group) => int.Parse(m.Groups[group].Value, CultureInfo.InvariantCulture);

    static int MonthOf(string word)
    {
        foreach (var (p, n) in Months)
            if (Regex.IsMatch(word, $"^(?:{p})$", RegexOptions.IgnoreCase)) return n;
        return 0;
    }

    static List<DayOfWeek> DaysIn(string text)
    {
        var days = new List<DayOfWeek>();
        foreach (Match m in Regex.Matches(text, $"{L}(?:{Weekday}){R}", RegexOptions.IgnoreCase))
        {
            foreach (var (p, d) in Weekdays)
            {
                if (Regex.IsMatch(m.Value, $"^(?:{p})$", RegexOptions.IgnoreCase))
                {
                    if (!days.Contains(d)) days.Add(d);
                    break;
                }
            }
        }
        return days;
    }

    static double NumberOf(string s)
    {
        s = s.Trim();
        if (double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var n)) return n;
        foreach (var (p, v) in NumberWords)
            if (Regex.IsMatch(s, $"^(?:{p})$", RegexOptions.IgnoreCase)) return v;
        return 1;
    }

    static TimeSpan UnitSpan(string unit)
    {
        var u = unit.ToLowerInvariant();
        if (Regex.IsMatch(u, $"^(?:{UHour})$")) return TimeSpan.FromHours(1);
        if (Regex.IsMatch(u, $"^(?:{USec})$")) return TimeSpan.FromSeconds(1);
        if (Regex.IsMatch(u, $"^(?:{UDay})$")) return TimeSpan.FromDays(1);
        if (Regex.IsMatch(u, $"^(?:{UWeek})$")) return TimeSpan.FromDays(7);
        return TimeSpan.FromMinutes(1);
    }

    static RepeatRule? RepeatFor(string unit, int n)
    {
        n = Math.Max(1, n);
        var u = unit.ToLowerInvariant();
        if (Regex.IsMatch(u, $"^(?:{UMonth})$")) return new RepeatRule { Kind = RepeatKind.Monthly, Interval = n };
        if (Regex.IsMatch(u, $"^(?:{UWeek})$")) return n == 1 ? RepeatRule.Weekly() : RepeatRule.Daily(7 * n);
        if (Regex.IsMatch(u, $"^(?:{UDay})$")) return RepeatRule.Daily(n);
        if (Regex.IsMatch(u, $"^(?:{UHour})$")) return new RepeatRule { Kind = RepeatKind.EveryNHours, Interval = n };
        if (Regex.IsMatch(u, $"^(?:{UMin})$")) return new RepeatRule { Kind = RepeatKind.EveryNMinutes, Interval = n };
        return null;
    }

    static bool Overlaps(bool[] consumed, int start, int length)
    {
        for (var i = start; i < start + length; i++)
            if (consumed[i]) return true;
        return false;
    }

    // ---- text ----

    /// <summary>Lower case and one apostrophe form; keeps the string length so spans map back to the original.</summary>
    static string Normalize(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            var c = char.ToLowerInvariant(ch);
            sb.Append(c is '’' or 'ʼ' or '`' or '‘' ? '\'' : c);
        }
        return sb.ToString();
    }

    static readonly Regex LeadingFiller = new(
        @"^(?:(?:please|будь\s+ласка)\s+)?(?:remind(?:\s+me)?|set\s+(?:a\s+)?reminder|reminder|нагадай(?:те)?|нагадати|нагадування|нагадуй)(?:\s+(?:me|мені|мене))?(?:\s*[:,\-—])?\s*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    static readonly Regex EdgeWords = new(@"^(?:to|that|about|of|at|on|in|and|for|що|щоб|про|о|об|в|у|на|і|й|та|,|:|-|—)(?:\s+|$)|(?:\s+|^)(?:to|at|on|in|and|for|о|об|в|у|на|і|й|та|,|:|-|—)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    static string CleanText(string original, bool[] consumed)
    {
        var sb = new StringBuilder(original.Length);
        for (var i = 0; i < original.Length; i++) sb.Append(consumed[i] ? ' ' : original[i]);
        var t = Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        t = Regex.Replace(t, @"\s+([,.!?;:])", "$1");
        for (var guard = 0; guard < 8; guard++)
        {
            var before = t;
            t = LeadingFiller.Replace(t, "").Trim();
            t = EdgeWords.Replace(t, "").Trim();
            t = t.Trim(',', ';', ':', '-', '—', ' ');
            if (t == before) break;
        }
        return t;
    }
}
