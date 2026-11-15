using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Isle.Core;
using Isle.Modules.Tools;

namespace Isle.Views.Tools;

/// <summary>Tools tab: tool buttons on top; below, the latest result (recognised text or QR content) or the colour history.</summary>
public sealed class ToolsView : Grid
{
    readonly ContentControl _result = new();
    readonly Action<string> _copy;
    readonly Action<string> _open;
    readonly Action<string> _setOcrLanguage;
    readonly Button _recordButton;

    public ToolsView(Action screenshot, Action ocr, Action qr, Action color, Action record, Action<string> copy, Action<string> open, Action<string> setOcrLanguage)
    {
        _copy = copy;
        _open = open;
        _setOcrLanguage = setOcrLanguage;
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var tools = new UniformGrid { Rows = 1, Margin = new Thickness(0, 0, 0, 8) };
        tools.Children.Add(ToolButton("Icon.Screenshot", "tools.screenshot", "Win+Alt+S", screenshot));
        tools.Children.Add(ToolButton("Icon.TextScan", "tools.ocr", "Win+Alt+Q", ocr));
        tools.Children.Add(ToolButton("Icon.Qr", "tools.qr", "Win+Alt+Q", qr));
        tools.Children.Add(ToolButton("Icon.Picker", "tools.color", "Win+Alt+C", color));
        tools.Children.Add(_recordButton = ToolButton("Icon.Record", "tools.record", "Win+Alt+R", record));
        Children.Add(tools);
        SetRow(_result, 1);
        Children.Add(_result);
        ShowColors([]);
    }

    public void SetRecording(bool on)
    {
        if (_recordButton.Content is StackPanel p && p.Children[1] is TextBlock t) t.Text = Strings.T(on ? "tools.stop" : "tools.record");
    }

    /// <summary>Default content: picked colours (click copies) and a hint.</summary>
    public void ShowColors(IReadOnlyList<string> history)
    {
        var colors = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
        foreach (var hex in history)
        {
            Color c;
            try { c = (Color)ColorConverter.ConvertFromString(hex); } catch { continue; }
            var sw = new Border { Width = 22, Height = 22, CornerRadius = new CornerRadius(6), Margin = new Thickness(0, 0, 6, 6), Background = new SolidColorBrush(c), Cursor = System.Windows.Input.Cursors.Hand, ToolTip = hex };
            sw.SetResourceReference(Border.BorderBrushProperty, "StrokeBrush");
            sw.BorderThickness = new Thickness(1);
            var h = hex;
            sw.MouseLeftButtonUp += (_, e) => { e.Handled = true; _copy(h); };
            colors.Children.Add(sw);
        }
        var stack = new StackPanel();
        var title = Caption(history.Count > 0 ? "tools.colorHistory" : "tools.hint");
        stack.Children.Add(title);
        if (history.Count > 0) stack.Children.Add(colors);
        else
        {
            title.TextWrapping = TextWrapping.Wrap;
        }
        _result.Content = stack;
    }

    /// <summary>Recognised text (already on the clipboard) with Copy and the OCR language chips.</summary>
    public void ShowText(string text, IReadOnlyList<(string Tag, string Name)> languages, string? current)
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var head = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
        var copy = SmallAction("tools.copy", () => _copy(text));
        DockPanel.SetDock(copy, Dock.Right);
        head.Children.Add(copy);
        var langs = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        langs.Children.Add(Caption("tools.ocrLanguage"));
        foreach (var (tag, name) in languages)
        {
            var chip = new ToggleButton { Content = name, Margin = new Thickness(6, 0, 0, 0), IsChecked = string.Equals(tag, current, StringComparison.OrdinalIgnoreCase) };
            chip.SetResourceReference(StyleProperty, "Chip");
            var t = tag;
            chip.Click += (_, _) => _setOcrLanguage(t);
            langs.Children.Add(chip);
        }
        head.Children.Add(langs);
        grid.Children.Add(head);
        var box = new TextBox { Text = text.Length > 0 ? text : Strings.T("tools.noText"), IsReadOnly = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, FontSize = 12.5 };
        box.SetResourceReference(StyleProperty, "FlatTextBox");
        box.VerticalContentAlignment = VerticalAlignment.Top;
        SetRow(box, 1);
        grid.Children.Add(box);
        _result.Content = grid;
    }

    /// <summary>QR content: shown first; Open appears only for web links and only opens on click.</summary>
    public void ShowCode(string? content)
    {
        var stack = new StackPanel();
        stack.Children.Add(Caption(content is null ? "tools.qrNone" : "tools.qrFound"));
        if (content is not null)
        {
            var box = new TextBox { Text = content, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, MaxHeight = 70, Margin = new Thickness(0, 4, 0, 6), VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            box.SetResourceReference(StyleProperty, "FlatTextBox");
            stack.Children.Add(box);
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(SmallAction("tools.copy", () => _copy(content)));
            if (QrReader.IsWebUrl(content)) row.Children.Add(SmallAction("tools.open", () => _open(content)));
            stack.Children.Add(row);
        }
        _result.Content = stack;
    }

    static TextBlock Caption(string key)
    {
        var t = new TextBlock { Text = Strings.T(key), VerticalAlignment = VerticalAlignment.Center };
        t.SetResourceReference(StyleProperty, "TextDim");
        return t;
    }

    static Button SmallAction(string key, Action click)
    {
        var b = new Button { Content = Strings.T(key), Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(12, 4, 12, 4) };
        b.SetResourceReference(StyleProperty, "FlatButton");
        b.Click += (_, _) => { try { click(); } catch (Exception ex) { Logger.Error("tools action", ex); } };
        return b;
    }

    static Button ToolButton(string icon, string key, string hotkey, Action click)
    {
        var i = new Icon { Width = 20, Height = 20, HorizontalAlignment = HorizontalAlignment.Center };
        i.SetResourceReference(Icon.DataProperty, icon);
        i.SetResourceReference(Icon.ForegroundProperty, "FgBrush");
        var label = new TextBlock { Text = Strings.T(key), FontSize = 11.5, Margin = new Thickness(0, 5, 0, 0), HorizontalAlignment = HorizontalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
        var stack = new StackPanel { Children = { i, label } };
        var b = new Button { Content = stack, Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(4, 9, 4, 8), ToolTip = hotkey };
        b.SetResourceReference(StyleProperty, "FlatButton");
        b.Click += (_, _) => { try { click(); } catch (Exception ex) { Logger.Error("tool", ex); } };
        return b;
    }
}
