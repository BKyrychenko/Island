using System.Globalization;
using System.Text.RegularExpressions;

namespace Skerry.Modules.Lyrics;

public sealed record LrcLine(TimeSpan Time, string Text);

/// <summary>Parser for LRC synced lyrics: "[mm:ss.xx]text", several stamps per line, [offset:±ms], metadata tags ignored.</summary>
public static partial class Lrc
{
    [GeneratedRegex(@"\[(\d{1,3}):(\d{1,2})(?:[.:](\d{1,3}))?\]")]
    private static partial Regex StampRegex();

    [GeneratedRegex(@"^\s*\[offset:\s*([+-]?\d+)\s*\]", RegexOptions.IgnoreCase)]
    private static partial Regex OffsetRegex();

    public static List<LrcLine> Parse(string? text)
    {
        var lines = new List<LrcLine>();
        if (string.IsNullOrWhiteSpace(text)) return lines;
        var offset = TimeSpan.Zero;
        foreach (var raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var om = OffsetRegex().Match(raw);
            if (om.Success)
            {
                // positive offset = lyrics come earlier (LRC convention)
                if (int.TryParse(om.Groups[1].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var ms))
                    offset = TimeSpan.FromMilliseconds(-ms);
                continue;
            }
            var pos = 0;
            var stamps = new List<TimeSpan>(2);
            while (true)
            {
                var m = StampRegex().Match(raw, pos);
                if (!m.Success || m.Index != SkipSpaces(raw, pos)) break;
                stamps.Add(ToTime(m));
                pos = m.Index + m.Length;
            }
            if (stamps.Count == 0) continue; // metadata ([ar:...]) or garbage
            var lyric = raw[pos..].Trim();
            // strip word-level "<mm:ss.xx>" stamps of enhanced LRC
            if (lyric.Contains('<')) lyric = WordStamp().Replace(lyric, "").Trim();
            foreach (var t in stamps)
            {
                var at = t + offset;
                lines.Add(new LrcLine(at < TimeSpan.Zero ? TimeSpan.Zero : at, lyric));
            }
        }
        lines.Sort((a, b) => a.Time.CompareTo(b.Time)); // List.Sort is unstable; ties are rare and harmless here
        return lines;
    }

    [GeneratedRegex(@"<\d{1,3}:\d{1,2}(?:[.:]\d{1,3})?>")]
    private static partial Regex WordStamp();

    static int SkipSpaces(string s, int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        return i;
    }

    static TimeSpan ToTime(Match m)
    {
        var min = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var sec = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        var frac = m.Groups[3].Success ? m.Groups[3].Value : "0";
        var ms = frac.Length switch
        {
            1 => int.Parse(frac, CultureInfo.InvariantCulture) * 100,
            2 => int.Parse(frac, CultureInfo.InvariantCulture) * 10,
            _ => int.Parse(frac[..3], CultureInfo.InvariantCulture),
        };
        return new TimeSpan(0, 0, min, sec, ms);
    }

    /// <summary>Index of the line being sung at <paramref name="position"/>, or -1 before the first line.</summary>
    public static int IndexAt(IReadOnlyList<LrcLine> lines, TimeSpan position)
    {
        int lo = 0, hi = lines.Count - 1, found = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) / 2;
            if (lines[mid].Time <= position) { found = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        return found;
    }
}
