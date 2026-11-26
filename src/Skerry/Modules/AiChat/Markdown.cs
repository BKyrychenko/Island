using System.Text;

namespace Skerry.Modules.AiChat;

public enum MdBlockKind { Paragraph, Heading, Bullet, Numbered, Code, Quote, Rule }

public enum MdSpanKind { Text, Bold, Italic, Code, BoldItalic }

public readonly record struct MdSpan(MdSpanKind Kind, string Text);

/// <summary>One rendered block. <see cref="Level"/>: heading level (1–3) or list indent (0, 1, …).
/// <see cref="Number"/>: the item number of a numbered list. <see cref="Lang"/>: code fence language.</summary>
public sealed record MdBlock(MdBlockKind Kind, string Text, int Level = 0, int Number = 0, string Lang = "");

/// <summary>
/// Tiny Markdown subset for chat answers: paragraphs, `#` headings, `-`/`*`/`+` and `1.` lists (indent levels),
/// ``` fenced code (an unclosed fence while streaming is still a code block), `>` quotes, `---` rules, and inline
/// **bold**, *italic* / _italic_, ***both***, `code`. Everything else stays literal text.
/// </summary>
public static class Markdown
{
    public static List<MdBlock> Parse(string text)
    {
        var blocks = new List<MdBlock>();
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var para = new StringBuilder();

        void Flush()
        {
            if (para.Length > 0) blocks.Add(new MdBlock(MdBlockKind.Paragraph, para.ToString()));
            para.Clear();
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var line = raw.TrimEnd();
            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;

            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                Flush();
                var fence = trimmed[..3];
                var lang = trimmed[3..].Trim();
                var code = new StringBuilder();
                i++;
                for (; i < lines.Length; i++)
                {
                    if (lines[i].TrimStart().StartsWith(fence, StringComparison.Ordinal)) break;
                    if (code.Length > 0) code.Append('\n');
                    code.Append(lines[i].TrimEnd('\r'));
                }
                blocks.Add(new MdBlock(MdBlockKind.Code, code.ToString(), Lang: lang));
                continue;
            }
            if (trimmed.Length == 0) { Flush(); continue; }
            if (IsRule(trimmed)) { Flush(); blocks.Add(new MdBlock(MdBlockKind.Rule, "")); continue; }

            var h = 0;
            while (h < trimmed.Length && h < 6 && trimmed[h] == '#') h++;
            if (h > 0 && h < trimmed.Length && trimmed[h] == ' ')
            {
                Flush();
                blocks.Add(new MdBlock(MdBlockKind.Heading, trimmed[(h + 1)..].Trim().TrimEnd('#').TrimEnd(), Math.Min(h, 3)));
                continue;
            }
            if (trimmed.Length >= 2 && trimmed[0] is '-' or '*' or '+' && trimmed[1] == ' ')
            {
                Flush();
                blocks.Add(new MdBlock(MdBlockKind.Bullet, trimmed[2..].Trim(), indent / 2));
                continue;
            }
            var d = 0;
            while (d < trimmed.Length && d < 4 && char.IsAsciiDigit(trimmed[d])) d++;
            if (d > 0 && d + 1 < trimmed.Length && trimmed[d] is '.' or ')' && trimmed[d + 1] == ' ')
            {
                Flush();
                blocks.Add(new MdBlock(MdBlockKind.Numbered, trimmed[(d + 2)..].Trim(), indent / 2, int.Parse(trimmed[..d], System.Globalization.CultureInfo.InvariantCulture)));
                continue;
            }
            if (trimmed.StartsWith('>'))
            {
                Flush();
                var q = trimmed[1..].TrimStart();
                if (blocks.Count > 0 && blocks[^1].Kind == MdBlockKind.Quote && i > 0 && lines[i - 1].TrimStart().StartsWith('>'))
                    blocks[^1] = blocks[^1] with { Text = blocks[^1].Text + "\n" + q };
                else blocks.Add(new MdBlock(MdBlockKind.Quote, q));
                continue;
            }
            // continuation of a list item (indented text right after it)
            if (para.Length == 0 && indent >= 2 && blocks.Count > 0 && blocks[^1].Kind is MdBlockKind.Bullet or MdBlockKind.Numbered && lines[i - 1].Trim().Length > 0)
            {
                blocks[^1] = blocks[^1] with { Text = blocks[^1].Text + " " + trimmed };
                continue;
            }
            if (para.Length > 0) para.Append('\n');
            para.Append(trimmed);
        }
        Flush();
        return blocks;
    }

    static bool IsRule(string t)
    {
        if (t.Length < 3) return false;
        var c = t[0];
        if (c is not ('-' or '*' or '_')) return false;
        var n = 0;
        foreach (var ch in t)
        {
            if (ch == c) n++;
            else if (ch != ' ') return false;
        }
        return n >= 3;
    }

    /// <summary>Inline spans. Unclosed markers stay literal (so half-streamed text never flickers into bold).</summary>
    public static List<MdSpan> Inline(string text)
    {
        var spans = new List<MdSpan>();
        var sb = new StringBuilder();

        void Add(MdSpanKind k, string s)
        {
            if (sb.Length > 0) { spans.Add(new MdSpan(MdSpanKind.Text, sb.ToString())); sb.Clear(); }
            if (s.Length > 0) spans.Add(new MdSpan(k, s));
        }

        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '\\' && i + 1 < text.Length && "*_`\\#[]".Contains(text[i + 1]))
            {
                sb.Append(text[i + 1]);
                i += 2;
                continue;
            }
            if (c == '`')
            {
                var end = text.IndexOf('`', i + 1);
                if (end > i) { Add(MdSpanKind.Code, text[(i + 1)..end]); i = end + 1; continue; }
            }
            if (c is '*' or '_')
            {
                var run = 1;
                while (i + run < text.Length && text[i + run] == c && run < 3) run++;
                // '_' inside a word (snake_case) is literal
                var wordBefore = i > 0 && char.IsLetterOrDigit(text[i - 1]);
                if (!(c == '_' && wordBefore) && i + run < text.Length && !char.IsWhiteSpace(text[i + run]))
                {
                    var marker = new string(c, run);
                    var end = FindClose(text, marker, i + run);
                    if (end > i + run)
                    {
                        var kind = run switch { 1 => MdSpanKind.Italic, 2 => MdSpanKind.Bold, _ => MdSpanKind.BoldItalic };
                        Add(kind, text[(i + run)..end]);
                        i = end + run;
                        continue;
                    }
                }
                sb.Append(text, i, run);
                i += run;
                continue;
            }
            sb.Append(c);
            i++;
        }
        Add(MdSpanKind.Text, "");
        return spans;
    }

    static int FindClose(string text, string marker, int from)
    {
        var at = from;
        while (true)
        {
            var end = text.IndexOf(marker, at, StringComparison.Ordinal);
            if (end < 0) return -1;
            var after = end + marker.Length;
            var longer = after < text.Length && text[after] == marker[0];
            if (!char.IsWhiteSpace(text[end - 1]) && !longer && !(marker[0] == '_' && after < text.Length && char.IsLetterOrDigit(text[after])))
                return end;
            at = end + 1;
        }
    }
}
