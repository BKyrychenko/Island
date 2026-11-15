using System.IO.Compression;
using System.Text;
using Isle.Modules.AiChat;

namespace Isle.Tests;

public class MarkdownTests
{
    [Fact]
    public void ParsesBlocks()
    {
        var b = Markdown.Parse("# Title\nIntro line\nsecond line\n\n- one\n  - nested\n2. two\n\n```cs\nvar x = 1;\n\nx++;\n```\n> quote\n> more\n---\nend");
        Assert.Equal(
            [MdBlockKind.Heading, MdBlockKind.Paragraph, MdBlockKind.Bullet, MdBlockKind.Bullet, MdBlockKind.Numbered, MdBlockKind.Code, MdBlockKind.Quote, MdBlockKind.Rule, MdBlockKind.Paragraph],
            b.Select(x => x.Kind));
        Assert.Equal("Title", b[0].Text);
        Assert.Equal(1, b[0].Level);
        Assert.Equal("Intro line\nsecond line", b[1].Text);
        Assert.Equal(1, b[3].Level);
        Assert.Equal(2, b[4].Number);
        Assert.Equal("var x = 1;\n\nx++;", b[5].Text);
        Assert.Equal("cs", b[5].Lang);
        Assert.Equal("quote\nmore", b[6].Text);
    }

    [Fact]
    public void UnclosedFenceWhileStreamingIsCode()
    {
        var b = Markdown.Parse("Look:\n```\nline 1\nline 2");
        Assert.Equal(MdBlockKind.Code, b[^1].Kind);
        Assert.Equal("line 1\nline 2", b[^1].Text);
    }

    [Fact]
    public void ListContinuationJoinsItem()
    {
        var b = Markdown.Parse("- first\n  continued\n- second");
        Assert.Equal(2, b.Count);
        Assert.Equal("first continued", b[0].Text);
    }

    [Fact]
    public void HashWithoutSpaceIsText()
    {
        var b = Markdown.Parse("#hashtag and C# code");
        Assert.Single(b);
        Assert.Equal(MdBlockKind.Paragraph, b[0].Kind);
    }

    static string Show(string s) => string.Join("|", Markdown.Inline(s).Select(x => x.Kind + ":" + x.Text));

    [Theory]
    [InlineData("plain", "Text:plain")]
    [InlineData("a **bold** b", "Text:a |Bold:bold|Text: b")]
    [InlineData("*it* and _it_", "Italic:it|Text: and |Italic:it")]
    [InlineData("***both***", "BoldItalic:both")]
    [InlineData("use `x * y` here", "Text:use |Code:x * y|Text: here")]
    [InlineData("snake_case_name", "Text:snake_case_name")]
    [InlineData("2 * 3 * 4", "Text:2 * 3 * 4")]
    [InlineData("half **open", "Text:half **open")]
    [InlineData("tick ` alone", "Text:tick ` alone")]
    [InlineData(@"\*not italic\*", "Text:*not italic*")]
    public void ParsesInline(string input, string expected) => Assert.Equal(expected, Show(input));
}

public class AttachmentTests
{
    static MemoryStream Zip(params (string Path, string Content)[] entries)
    {
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var (p, c) in entries)
            {
                using var w = new StreamWriter(zip.CreateEntry(p).Open(), Encoding.UTF8);
                w.Write(c);
            }
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void ReadsDocxParagraphs()
    {
        const string doc = """
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>
              <w:p><w:r><w:t>Hello </w:t></w:r><w:r><w:t>world</w:t></w:r></w:p>
              <w:p><w:pPr><w:numPr><w:ilvl w:val="0"/></w:numPr></w:pPr><w:r><w:t>item</w:t><w:tab/><w:t>x</w:t></w:r></w:p>
              <w:tbl><w:tr><w:tc><w:p><w:r><w:t>cell</w:t></w:r></w:p></w:tc></w:tr></w:tbl>
            </w:body></w:document>
            """;
        var text = Attachments.DocxText(Zip(("word/document.xml", doc)));
        Assert.Equal("Hello world\n- item\tx\ncell", text);
    }

    [Fact]
    public void ReadsXlsxAsCsv()
    {
        const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var wb = $"""<workbook xmlns="{ns}" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Budget" sheetId="1" r:id="rId1"/></sheets></workbook>""";
        const string rels = """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="x" Target="worksheets/sheet1.xml"/></Relationships>""";
        var shared = $"""<sst xmlns="{ns}"><si><t>Item</t></si><si><t>Cost, EUR</t></si><si><r><t>Rent</t></r></si></sst>""";
        var sheet = $"""
            <worksheet xmlns="{ns}"><sheetData>
              <row r="1"><c r="A1" t="s"><v>0</v></c><c r="B1" t="s"><v>1</v></c></row>
              <row r="2"><c r="A2" t="s"><v>2</v></c><c r="C2"><v>950.5</v></c></row>
              <row r="3"><c r="A3" t="inlineStr"><is><t>Paid</t></is></c><c r="B3" t="b"><v>1</v></c></row>
            </sheetData></worksheet>
            """;
        var text = Attachments.XlsxText(Zip(("xl/workbook.xml", wb), ("xl/_rels/workbook.xml.rels", rels), ("xl/sharedStrings.xml", shared), ("xl/worksheets/sheet1.xml", sheet)));
        Assert.Equal("## Budget\nItem,\"Cost, EUR\"\nRent,,950.5\nPaid,TRUE", text);
    }

    [Theory]
    [InlineData("A1", 0)]
    [InlineData("C7", 2)]
    [InlineData("AA10", 26)]
    [InlineData(null, -1)]
    [InlineData("12", -1)]
    public void ColumnIndex(string? cell, int expected) => Assert.Equal(expected, Attachments.ColumnIndex(cell));

    [Fact]
    public void ReadsFilesByKind()
    {
        var dir = Directory.CreateTempSubdirectory("isle-att").FullName;
        try
        {
            var code = Path.Combine(dir, "a.cs");
            File.WriteAllText(code, "class A {}");
            var a = Attachments.Read(code);
            Assert.Equal(AttachmentKind.Text, a.Kind);
            Assert.Equal("class A {}", a.Text);

            var png = Path.Combine(dir, "p.png");
            File.WriteAllBytes(png, [0x89, 0x50, 0x4E, 0x47]);
            var img = Attachments.Read(png);
            Assert.Equal(AttachmentKind.Image, img.Kind);
            Assert.Equal("image/png", img.MediaType);
            Assert.Equal("iVBORw==", img.Data);

            var unknownText = Path.Combine(dir, "notes.weird");
            File.WriteAllText(unknownText, "just text");
            Assert.Equal("just text", Attachments.Read(unknownText).Text);

            var bin = Path.Combine(dir, "x.bin");
            File.WriteAllBytes(bin, [1, 0, 2]);
            Assert.Equal("ai.att.type", Assert.Throws<AiException>(() => Attachments.Read(bin)).Key);

            var empty = Path.Combine(dir, "e.txt");
            File.WriteAllText(empty, "  ");
            Assert.Equal("ai.att.empty", Assert.Throws<AiException>(() => Attachments.Read(empty)).Key);

            var big = Path.Combine(dir, "big.jpg");
            using (var f = File.Create(big)) f.SetLength(Attachments.MaxImageBytes + 1);
            Assert.Equal("ai.att.tooBig", Assert.Throws<AiException>(() => Attachments.Read(big)).Key);

            var broken = Path.Combine(dir, "broken.docx");
            File.WriteAllText(broken, "not a zip");
            Assert.Equal("ai.att.unreadable", Assert.Throws<AiException>(() => Attachments.Read(broken)).Key);

            Assert.Equal("ai.att.missing", Assert.Throws<AiException>(() => Attachments.Read(Path.Combine(dir, "nope.txt"))).Key);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void ExtractsPdfText()
    {
        var b = new UglyToad.PdfPig.Writer.PdfDocumentBuilder();
        var font = b.AddStandard14Font(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14Font.Helvetica);
        b.AddPage(UglyToad.PdfPig.Content.PageSize.A4).AddText("Invoice total 42 EUR", 12, new UglyToad.PdfPig.Core.PdfPoint(50, 700), font);
        var text = Attachments.PdfText(b.Build());
        Assert.Contains("Invoice total 42 EUR", text);
        Assert.StartsWith("[page 1]", text);
    }

    [Fact]
    public void BrokenPdfGivesNoTextButStillAttaches()
    {
        Assert.Equal("", Attachments.PdfText(Encoding.ASCII.GetBytes("%PDF-1.4 garbage")));
    }
}

public class TextActionTests
{
    [Fact]
    public void EveryActionHasPromptAndTitle()
    {
        foreach (var a in TextActions.All)
        {
            Assert.StartsWith("ta.", TextActions.TitleKey(a));
            Assert.False(string.IsNullOrWhiteSpace(TextActions.SystemPrompt(a, "en")));
        }
        Assert.Equal(7, TextActions.All.Length);
    }

    [Fact]
    public void BuildsOneOffConversation()
    {
        var c = TextActions.Build(TextAction.TranslateNl, "Привіт", "uk");
        Assert.Contains("Dutch", c.SystemPrompt);
        var m = Assert.Single(c.ForRequest());
        Assert.Equal("user", m.Role);
        Assert.Equal("Привіт", m.Text);
    }

    [Fact]
    public void SummaryFollowsUiLanguageAndLongSelectionIsCut()
    {
        Assert.Contains("Ukrainian", TextActions.SystemPrompt(TextAction.Summarise, "uk"));
        Assert.Contains("English", TextActions.SystemPrompt(TextAction.Explain, "en"));
        var c = TextActions.Build(TextAction.Fix, new string('a', TextActions.MaxChars + 50), "en");
        Assert.Equal(TextActions.MaxChars, c.Messages[0].Text.Length);
    }

    [Fact]
    public void ReplacingActions()
    {
        Assert.True(TextActions.Replaces(TextAction.Fix));
        Assert.True(TextActions.Replaces(TextAction.TranslateUk));
        Assert.False(TextActions.Replaces(TextAction.Explain));
        Assert.False(TextActions.Replaces(TextAction.Summarise));
    }

    [Fact]
    public void QuestionAsksForShortAnswer()
    {
        var c = TextActions.Question("why is the sky blue?", "en");
        Assert.Contains("briefly", c.SystemPrompt);
        Assert.Equal("why is the sky blue?", c.Messages[0].Text);
    }

    [Fact]
    public void ClipboardQuietWindow()
    {
        Isle.Core.ClipboardOps.Quiet(TimeSpan.FromSeconds(5));
        Assert.True(Isle.Core.ClipboardOps.IsQuiet);
        Isle.Core.ClipboardOps.Quiet(TimeSpan.Zero);
        Assert.False(Isle.Core.ClipboardOps.IsQuiet);
    }
}
