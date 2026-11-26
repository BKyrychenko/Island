namespace Skerry.Modules.Commands;

/// <summary>Fuzzy name matching for the command bar: prefix &gt; word prefix / acronym &gt; substring &gt; subsequence.</summary>
public static class Fuzzy
{
    /// <summary>Score (higher is better) or null when <paramref name="query"/> does not match <paramref name="text"/>.</summary>
    public static int? Score(string query, string text)
    {
        query = query.Trim();
        if (query.Length == 0 || text.Length == 0) return null;
        var q = query.ToLowerInvariant();
        var t = text.ToLowerInvariant();
        var lengthPenalty = Math.Min(t.Length, 60);

        if (t.StartsWith(q, StringComparison.Ordinal)) return 1000 - lengthPenalty + (t.Length == q.Length ? 50 : 0);

        var starts = WordStarts(text);
        foreach (var s in starts)
            if (s > 0 && t.AsSpan(s).StartsWith(q, StringComparison.Ordinal)) return 850 - lengthPenalty;

        var acronym = string.Concat(starts.Select(i => t[i]));
        if (q.Length >= 2 && acronym.StartsWith(q, StringComparison.Ordinal)) return 800 - lengthPenalty + (acronym.Length == q.Length ? 20 : 0);

        var idx = t.IndexOf(q, StringComparison.Ordinal);
        if (idx >= 0) return 600 - Math.Min(idx, 50) - lengthPenalty;

        // subsequence: every query char in order; bonuses for word starts and runs
        int ti = 0, score = 300, run = 0;
        var startSet = new HashSet<int>(starts);
        foreach (var c in q)
        {
            if (c == ' ') continue;
            var found = t.IndexOf(c, ti);
            if (found < 0) return null;
            var gap = found - ti;
            score -= Math.Min(gap, 10) * 3;
            if (startSet.Contains(found)) score += 15;
            run = gap == 0 ? run + 1 : 0;
            score += run * 8;
            ti = found + 1;
        }
        return Math.Max(1, score - lengthPenalty);
    }

    /// <summary>Indexes where words begin: after space/punctuation, at digits after letters and at camelCase humps.</summary>
    public static List<int> WordStarts(string text)
    {
        var list = new List<int>();
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (!char.IsLetterOrDigit(c)) continue;
            if (i == 0) { list.Add(i); continue; }
            var p = text[i - 1];
            if (!char.IsLetterOrDigit(p) || (char.IsUpper(c) && char.IsLower(p)) || (char.IsDigit(c) && !char.IsDigit(p))) list.Add(i);
        }
        return list;
    }
}
