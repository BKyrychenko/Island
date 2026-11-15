using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Isle.Modules.AiChat;

/// <summary>
/// Turns a file into a <see cref="ChatAttachment"/>: images (png/jpg/gif/webp) as base64, PDFs as base64 plus their
/// extracted text (PdfPig, for providers without document input), .docx as paragraphs, .xlsx as CSV per sheet (both
/// read straight from the Open XML zip), and text/code files as UTF-8 text. Size limits give readable messages.
/// </summary>
public static class Attachments
{
    public const long MaxImageBytes = 5 * 1024 * 1024;   // Anthropic's per-image limit
    public const long MaxPdfBytes = 20 * 1024 * 1024;
    public const long MaxFileBytes = 10 * 1024 * 1024;   // docx/xlsx/text input size
    public const int MaxTextChars = 300_000;             // extracted text per file (~75k tokens)
    public const int MaxPerMessage = 5;

    static readonly HashSet<string> TextExt = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".markdown", ".csv", ".tsv", ".json", ".xml", ".yaml", ".yml", ".toml", ".ini", ".log", ".cfg", ".conf",
        ".cs", ".csproj", ".sln", ".xaml", ".js", ".mjs", ".ts", ".tsx", ".jsx", ".py", ".rb", ".go", ".rs", ".java", ".kt",
        ".c", ".h", ".cpp", ".hpp", ".cc", ".swift", ".php", ".sql", ".sh", ".ps1", ".psm1", ".bat", ".cmd", ".html", ".htm",
        ".css", ".scss", ".vue", ".svelte", ".lua", ".r", ".dart", ".gradle", ".props", ".targets", ".editorconfig", ".gitignore",
    };

    public static string? ImageType(string ext) => ext.ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => null,
    };

    /// <summary>File filter for the open dialog.</summary>
    public static string DialogFilter =>
        "Supported|*.png;*.jpg;*.jpeg;*.gif;*.webp;*.pdf;*.docx;*.xlsx;" + string.Join(";", TextExt.Select(e => "*" + e)) + "|All files|*.*";

    /// <summary>Reads the file (call off the UI thread). Throws <see cref="AiException"/> with an `ai.att.*` key.</summary>
    public static ChatAttachment Read(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists) throw new AiException("ai.att.missing", info.Name);
        var name = info.Name;
        var ext = info.Extension;
        try
        {
            if (ImageType(ext) is { } mt)
            {
                if (info.Length > MaxImageBytes) throw new AiException("ai.att.tooBig", $"{name} > {MaxImageBytes / 1048576} MB");
                return new ChatAttachment { Kind = AttachmentKind.Image, Name = name, MediaType = mt, Data = Convert.ToBase64String(File.ReadAllBytes(path)) };
            }
            if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                if (info.Length > MaxPdfBytes) throw new AiException("ai.att.tooBig", $"{name} > {MaxPdfBytes / 1048576} MB");
                var bytes = File.ReadAllBytes(path);
                return new ChatAttachment { Kind = AttachmentKind.Pdf, Name = name, MediaType = "application/pdf", Data = Convert.ToBase64String(bytes), Text = Cap(PdfText(bytes)) };
            }
            if (info.Length > MaxFileBytes) throw new AiException("ai.att.tooBig", $"{name} > {MaxFileBytes / 1048576} MB");
            string text;
            if (ext.Equals(".docx", StringComparison.OrdinalIgnoreCase)) { using var f = File.OpenRead(path); text = DocxText(f); }
            else if (ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) { using var f = File.OpenRead(path); text = XlsxText(f); }
            else if (TextExt.Contains(ext) || LooksLikeText(path)) text = File.ReadAllText(path);
            else throw new AiException("ai.att.type", name);
            if (text.Trim().Length == 0) throw new AiException("ai.att.empty", name);
            return new ChatAttachment { Kind = AttachmentKind.Text, Name = name, MediaType = "text/plain", Text = Cap(text) };
        }
        catch (AiException) { throw; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or XmlException)
        {
            throw new AiException("ai.att.unreadable", name);
        }
    }

    static string Cap(string s) => s.Length <= MaxTextChars ? s : s[..MaxTextChars] + "\n[…truncated]";

    /// <summary>No NUL bytes in the first 8 KB → treat as text (catches unknown code/config extensions).</summary>
    static bool LooksLikeText(string path)
    {
        using var f = File.OpenRead(path);
        var buf = new byte[8192];
        var n = f.Read(buf, 0, buf.Length);
        return n > 0 && Array.IndexOf(buf, (byte)0, 0, n) < 0;
    }

    public static string PdfText(byte[] pdf)
    {
        try
        {
            using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
            var sb = new StringBuilder();
            foreach (var page in doc.GetPages())
            {
                var t = UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor.ContentOrderTextExtractor.GetText(page);
                if (t.Trim().Length == 0) continue;
                sb.Append("[page ").Append(page.Number).Append("]\n").Append(t.Trim()).Append("\n\n");
                if (sb.Length > MaxTextChars) break;
            }
            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            Core.Logger.Warn($"pdf text: {ex.GetType().Name}");
            return "";
        }
    }

    const string W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    const string S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    /// <summary>Paragraph text of word/document.xml (tabs and breaks kept, list items prefixed with "- ").</summary>
    public static string DocxText(Stream docx)
    {
        using var zip = new ZipArchive(docx, ZipArchiveMode.Read);
        var entry = zip.GetEntry("word/document.xml") ?? throw new InvalidDataException("no document.xml");
        var xml = new XmlDocument { XmlResolver = null };
        using (var s = entry.Open()) xml.Load(XmlReader.Create(s, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }));
        var ns = new XmlNamespaceManager(xml.NameTable);
        ns.AddNamespace("w", W);
        var sb = new StringBuilder();
        foreach (XmlNode p in xml.SelectNodes("//w:body//w:p", ns)!)
        {
            var line = new StringBuilder();
            if (p.SelectSingleNode("w:pPr/w:numPr", ns) is not null) line.Append("- ");
            foreach (XmlNode n in p.SelectNodes(".//w:t|.//w:tab|.//w:br", ns)!)
            {
                if (n.LocalName == "t") line.Append(n.InnerText);
                else if (n.LocalName == "tab") line.Append('\t');
                else line.Append('\n');
            }
            sb.Append(line).Append('\n');
        }
        return sb.ToString().Trim();
    }

    /// <summary>Every sheet as CSV ("## Sheet name" header, shared strings and inline strings resolved, max 5000 rows/sheet).</summary>
    public static string XlsxText(Stream xlsx)
    {
        using var zip = new ZipArchive(xlsx, ZipArchiveMode.Read);
        var rs = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit };

        XmlDocument? Load(string path)
        {
            var e = zip.GetEntry(path);
            if (e is null) return null;
            var d = new XmlDocument { XmlResolver = null };
            using var s = e.Open();
            d.Load(XmlReader.Create(s, rs));
            return d;
        }

        var shared = new List<string>();
        if (Load("xl/sharedStrings.xml") is { } ss)
        {
            var nsm = new XmlNamespaceManager(ss.NameTable);
            nsm.AddNamespace("s", S);
            foreach (XmlNode si in ss.SelectNodes("//s:si", nsm)!)
            {
                var t = new StringBuilder();
                foreach (XmlNode tn in si.SelectNodes(".//s:t", nsm)!) t.Append(tn.InnerText);
                shared.Add(t.ToString());
            }
        }

        // sheet names in workbook order → their part names via the workbook relationships
        var sheets = new List<(string Name, string Part)>();
        var wb = Load("xl/workbook.xml");
        var rels = Load("xl/_rels/workbook.xml.rels");
        if (wb is not null)
        {
            var nsm = new XmlNamespaceManager(wb.NameTable);
            nsm.AddNamespace("s", S);
            var targets = new Dictionary<string, string>();
            if (rels is not null)
                foreach (XmlNode r in rels.DocumentElement!.ChildNodes)
                    if (r.Attributes?["Id"]?.Value is { } id && r.Attributes["Target"]?.Value is { } target)
                        targets[id] = target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target;
            foreach (XmlNode sh in wb.SelectNodes("//s:sheets/s:sheet", nsm)!)
            {
                var rid = sh.Attributes?["id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"]?.Value;
                var name = sh.Attributes?["name"]?.Value ?? "Sheet";
                if (rid is not null && targets.TryGetValue(rid, out var part)) sheets.Add((name, part));
            }
        }
        if (sheets.Count == 0)
            sheets.AddRange(zip.Entries.Where(e => e.FullName.StartsWith("xl/worksheets/sheet", StringComparison.Ordinal)).Select(e => (Path.GetFileNameWithoutExtension(e.Name), e.FullName)));

        var sb = new StringBuilder();
        foreach (var (name, part) in sheets)
        {
            var doc = Load(part);
            if (doc is null) continue;
            var nsm = new XmlNamespaceManager(doc.NameTable);
            nsm.AddNamespace("s", S);
            sb.Append("## ").Append(name).Append('\n');
            var rows = 0;
            foreach (XmlNode row in doc.SelectNodes("//s:sheetData/s:row", nsm)!)
            {
                if (++rows > 5000) { sb.Append("[…more rows]\n"); break; }
                var cells = new List<string>();
                foreach (XmlNode c in row.SelectNodes("s:c", nsm)!)
                {
                    var col = ColumnIndex(c.Attributes?["r"]?.Value);
                    while (col >= 0 && cells.Count < col) cells.Add("");
                    var type = c.Attributes?["t"]?.Value;
                    string v;
                    if (type == "inlineStr") v = c.SelectSingleNode("s:is", nsm)?.InnerText ?? "";
                    else
                    {
                        v = c.SelectSingleNode("s:v", nsm)?.InnerText ?? "";
                        if (type == "s" && int.TryParse(v, out var si) && si >= 0 && si < shared.Count) v = shared[si];
                        else if (type == "b") v = v == "1" ? "TRUE" : "FALSE";
                    }
                    cells.Add(Csv(v));
                }
                while (cells.Count > 0 && cells[^1].Length == 0) cells.RemoveAt(cells.Count - 1);
                if (cells.Count > 0) sb.Append(string.Join(",", cells)).Append('\n');
            }
            sb.Append('\n');
        }
        return sb.ToString().Trim();
    }

    /// <summary>"C7" → 2 (zero-based column), null/invalid → -1.</summary>
    public static int ColumnIndex(string? cellRef)
    {
        if (string.IsNullOrEmpty(cellRef)) return -1;
        var col = 0;
        var i = 0;
        for (; i < cellRef.Length && char.IsAsciiLetter(cellRef[i]); i++) col = col * 26 + (char.ToUpperInvariant(cellRef[i]) - 'A' + 1);
        return i == 0 ? -1 : col - 1;
    }

    static string Csv(string v) => v.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
}
