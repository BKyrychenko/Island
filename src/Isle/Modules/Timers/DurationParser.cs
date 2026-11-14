using System.Globalization;
using System.Text.RegularExpressions;

namespace Isle.Modules.Timers;

/// <summary>
/// Timer input: "90" (minutes), "1:30" (m:ss), "1:02:03" (h:mm:ss), "1h30m", "45s", "2m 30s", "1.5h", "25 min focus",
/// Ukrainian units ("10 хв", "1 год 30 хв", "45 с"). Anything after the duration becomes the label.
/// </summary>
public static partial class DurationParser
{
    [GeneratedRegex(@"^\s*(\d{1,3}):(\d{1,2})(?::(\d{1,2}))?(?=\s|$)")]
    private static partial Regex Clock();

    // one unit group: number + unit
    [GeneratedRegex(@"^\s*(\d+(?:[.,]\d+)?)\s*(hours?|hrs?|h|minutes?|mins?|m|seconds?|secs?|s|години|годин|година|годину|год|г|хвилини|хвилин|хвилина|хвилину|хв|секунди|секунд|секунда|секунду|сек|с)(?![\p{L}])", RegexOptions.IgnoreCase)]
    private static partial Regex UnitGroup();

    [GeneratedRegex(@"^\s*(\d+(?:[.,]\d+)?)(?=\s|$)")]
    private static partial Regex BareNumber();

    public static readonly TimeSpan Max = TimeSpan.FromHours(99);

    public static bool TryParse(string? text, out TimeSpan duration) => TryParse(text, out duration, out _);

    public static bool TryParse(string? text, out TimeSpan duration, out string label)
    {
        duration = TimeSpan.Zero;
        label = "";
        if (string.IsNullOrWhiteSpace(text)) return false;
        var s = text.Trim();
        var rest = s;

        var clock = Clock().Match(s);
        if (clock.Success)
        {
            int a = int.Parse(clock.Groups[1].Value, CultureInfo.InvariantCulture), b = int.Parse(clock.Groups[2].Value, CultureInfo.InvariantCulture);
            if (clock.Groups[3].Success)
            {
                var c = int.Parse(clock.Groups[3].Value, CultureInfo.InvariantCulture);
                if (b >= 60 || c >= 60) return false;
                duration = new TimeSpan(a, b, c);
            }
            else
            {
                if (b >= 60) return false;
                duration = new TimeSpan(0, a, b);
            }
            rest = s[clock.Length..];
        }
        else
        {
            var any = false;
            while (true)
            {
                var m = UnitGroup().Match(rest);
                if (!m.Success) break;
                if (!TryNumber(m.Groups[1].Value, out var n)) return false;
                duration += Unit(m.Groups[2].Value.ToLowerInvariant()) * n;
                rest = rest[m.Length..];
                any = true;
            }
            if (!any)
            {
                var bare = BareNumber().Match(rest);
                if (!bare.Success || !TryNumber(bare.Groups[1].Value, out var minutes)) return false;
                duration = TimeSpan.FromMinutes(minutes);
                rest = rest[bare.Length..];
            }
        }

        duration = TimeSpan.FromSeconds(Math.Round(duration.TotalSeconds));
        if (duration <= TimeSpan.Zero || duration > Max) return false;
        label = rest.Trim().TrimStart('-', '—', ':', ',').Trim();
        return true;
    }

    static bool TryNumber(string s, out double n) =>
        double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out n) && n >= 0 && n < 100000;

    static TimeSpan Unit(string u) => u switch
    {
        "h" or "hr" or "hrs" or "hour" or "hours" or "г" or "год" or "година" or "години" or "годин" or "годину" => TimeSpan.FromHours(1),
        "s" or "sec" or "secs" or "second" or "seconds" or "с" or "сек" or "секунда" or "секунди" or "секунд" or "секунду" => TimeSpan.FromSeconds(1),
        _ => TimeSpan.FromMinutes(1),
    };

    /// <summary>Compact remaining-time text: "4:05", "1:02:03", "0:09".</summary>
    public static string Format(TimeSpan t)
    {
        if (t < TimeSpan.Zero) t = TimeSpan.Zero;
        var total = (long)Math.Ceiling(t.TotalSeconds - 1e-9);
        long h = total / 3600, m = total / 60 % 60, sec = total % 60;
        return h > 0 ? $"{h}:{m:00}:{sec:00}" : $"{m}:{sec:00}";
    }

    /// <summary>Human duration for labels: "25 min", "1 h 30 min", "45 s".</summary>
    public static string Describe(TimeSpan t, Func<string, string> unit)
    {
        var parts = new List<string>(3);
        if (t.Hours + t.Days * 24 > 0) parts.Add($"{t.Hours + t.Days * 24} {unit("h")}");
        if (t.Minutes > 0) parts.Add($"{t.Minutes} {unit("min")}");
        if (t.Seconds > 0 || parts.Count == 0) parts.Add($"{t.Seconds} {unit("s")}");
        return string.Join(" ", parts);
    }
}
