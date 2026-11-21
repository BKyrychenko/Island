using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Isle.Core;
using Isle.Modules.AiChat;

namespace Isle.Views.AiChat;

/// <summary>Renders <see cref="Markdown"/> blocks as plain WPF text elements (TextBlocks, code cards with Copy).</summary>
public static class MarkdownView
{
    public static StackPanel Render(string text, double fontSize = 12.5)
    {
        var stack = new StackPanel();
        foreach (var b in Markdown.Parse(text))
        {
            FrameworkElement el = b.Kind switch
            {
                MdBlockKind.Code => Code(b),
                MdBlockKind.Rule => Rule(),
                MdBlockKind.Bullet or MdBlockKind.Numbered => ListItem(b, fontSize),
                MdBlockKind.Quote => Quote(b, fontSize),
                MdBlockKind.Heading => Para(b.Text, fontSize + (b.Level == 1 ? 3 : b.Level == 2 ? 1.5 : 0.5), FontWeights.SemiBold),
                _ => Para(b.Text, fontSize, FontWeights.Normal),
            };
            el.Margin = new Thickness(el.Margin.Left, stack.Children.Count == 0 ? 0 : b.Kind is MdBlockKind.Bullet or MdBlockKind.Numbered ? 2 : 6, 0, 0);
            stack.Children.Add(el);
        }
        return stack;
    }

    public static TextBlock Para(string text, double size, FontWeight weight)
    {
        var t = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = size, FontWeight = weight };
        t.SetResourceReference(TextBlock.ForegroundProperty, "FgBrush");
        AddInlines(t.Inlines, text);
        return t;
    }

    static void AddInlines(InlineCollection inlines, string text)
    {
        foreach (var s in Markdown.Inline(text))
        {
            Inline run = s.Kind switch
            {
                MdSpanKind.Bold => new Bold(new Run(s.Text)),
                MdSpanKind.Italic => new Italic(new Run(s.Text)),
                MdSpanKind.BoldItalic => new Bold(new Italic(new Run(s.Text))),
                MdSpanKind.Code => CodeRun(s.Text),
                _ => new Run(s.Text),
            };
            inlines.Add(run);
        }
    }

    static Run CodeRun(string text)
    {
        var r = new Run(text);
        r.SetResourceReference(TextElement.FontFamilyProperty, "MonoFont");
        r.SetResourceReference(TextElement.BackgroundProperty, "SurfaceBrush");
        return r;
    }

    static FrameworkElement ListItem(MdBlock b, double size)
    {
        var dock = new DockPanel { Margin = new Thickness(4 + b.Level * 14, 0, 0, 0) };
        var mark = new TextBlock
        {
            Text = b.Kind == MdBlockKind.Numbered ? b.Number + "." : b.Level == 0 ? "•" : "◦",
            FontSize = size, MinWidth = b.Kind == MdBlockKind.Numbered ? 18 : 12, Margin = new Thickness(0, 0, 4, 0),
        };
        mark.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
        DockPanel.SetDock(mark, Dock.Left);
        dock.Children.Add(mark);
        dock.Children.Add(Para(b.Text, size, FontWeights.Normal));
        return dock;
    }

    static FrameworkElement Quote(MdBlock b, double size)
    {
        var p = Para(b.Text, size, FontWeights.Normal);
        p.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
        var border = new Border { BorderThickness = new Thickness(2, 0, 0, 0), Padding = new Thickness(8, 0, 0, 0), Child = p };
        border.SetResourceReference(Border.BorderBrushProperty, "StrokeBrush");
        return border;
    }

    static FrameworkElement Rule()
    {
        var r = new Border { Height = 1, Margin = new Thickness(0, 2, 0, 2) };
        r.SetResourceReference(Border.BackgroundProperty, "StrokeBrush");
        return r;
    }

    static FrameworkElement Code(MdBlock b)
    {
        var code = new TextBlock { Text = b.Text, FontSize = 11.5, TextWrapping = TextWrapping.NoWrap, Margin = new Thickness(10, 8, 10, 8) };
        code.SetResourceReference(TextBlock.FontFamilyProperty, "MonoFont");
        code.SetResourceReference(TextBlock.ForegroundProperty, "FgBrush");
        var scroll = new ScrollViewer { Content = code, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var grid = new Grid { Children = { scroll } };
        var copy = new Button
        {
            Width = 22, Height = 22, Margin = new Thickness(0, 3, 3, 0), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top,
            ToolTip = Strings.T("ai.copy"), Content = SmallIcon("Icon.Copy"),
        };
        copy.SetResourceReference(FrameworkElement.StyleProperty, "IconButton");
        var text = b.Text;
        copy.Click += (_, _) => AiChatView.CopyText(text);
        grid.Children.Add(copy);
        if (b.Lang.Length > 0)
        {
            var lang = new TextBlock { Text = b.Lang, FontSize = 9.5, Margin = new Thickness(0, 6, 30, 0), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
            lang.SetResourceReference(TextBlock.ForegroundProperty, "FgFaintBrush");
            grid.Children.Add(lang);
        }
        var border = new Border { CornerRadius = new CornerRadius(8), Child = grid };
        border.SetResourceReference(Border.BackgroundProperty, "SurfaceBrush");
        return border;
    }

    public static Icon SmallIcon(string key, double size = 13, string brush = "FgDimBrush") => Ui.MakeIcon(key, size, brush);
}
