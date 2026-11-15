using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Isle.Core;
using Isle.Modules.AiChat;

namespace Isle.Views.AiChat;

/// <summary>
/// Full-panel overlay for quick AI results: text actions on a selection (Win+Alt+A: action chips, streamed result,
/// Copy / Replace selection) and command-bar questions (`? …`: streamed short answer, Copy / Open in chat).
/// </summary>
public sealed class AiPopupView : Grid, ITabView
{
    readonly AiModule _m;
    readonly Icon _icon = MarkdownView.SmallIcon("Icon.Ai", 16, "AccentBrush");
    readonly TextBlock _title = new() { FontSize = 13, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
    readonly TextBlock _preview = new() { FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 2, 0, 0) };
    readonly WrapPanel _chips = new() { Margin = new Thickness(0, 6, 0, 0) };
    readonly ContentControl _result = new();
    readonly ScrollViewer _scroll = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 6, 0, 0) };
    readonly StackPanel _footer = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
    readonly DispatcherTimer _render = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(80) };
    string _selection = "";
    string _question = "";
    IntPtr _target;
    TextAction? _action;
    ChatMessage? _answer;
    bool _streaming;
    int _run;

    public AiPopupView(AiModule module)
    {
        _m = module;
        ClipToBounds = true;
        SetResourceReference(BackgroundProperty, "BgBrush");
        Margin = new Thickness(0);
        var inner = new Grid { Margin = new Thickness(16, 12, 14, 10) };
        for (var i = 0; i < 5; i++)
            inner.RowDefinitions.Add(new RowDefinition { Height = i == 3 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });

        var head = new DockPanel();
        var close = IconAction("Icon.Close", "ai.close", () => App.Island?.Collapse());
        close.Margin = new Thickness(0, -4, -4, -4);
        DockPanel.SetDock(close, Dock.Right);
        head.Children.Add(close);
        _icon.Margin = new Thickness(0, 0, 8, 0);
        head.Children.Add(_icon);
        _title.SetResourceReference(TextBlock.ForegroundProperty, "FgBrush");
        head.Children.Add(_title);
        inner.Children.Add(head);

        _preview.SetResourceReference(TextBlock.ForegroundProperty, "FgFaintBrush");
        SetRow(_preview, 1);
        inner.Children.Add(_preview);
        SetRow(_chips, 2);
        inner.Children.Add(_chips);
        _scroll.Content = _result;
        SetRow(_scroll, 3);
        inner.Children.Add(_scroll);
        SetRow(_footer, 4);
        inner.Children.Add(_footer);
        Children.Add(inner);
        _render.Tick += (_, _) => { _render.Stop(); RenderResult(); };
    }

    public void OnShown() { }

    public void OnHidden()
    {
        // closing the popup stops a running answer (nobody would see it)
        if (_streaming) _m.Cancel();
    }

    // ---- text actions ----

    public void ShowSelection(string text, IntPtr target)
    {
        _selection = text;
        _question = "";
        _target = target;
        _answer = null;
        _action = null;
        _icon.SetResourceReference(Icon.DataProperty, "Icon.Ai");
        _title.Text = Strings.T("ta.title");
        _preview.Text = "“" + OneLine(text) + "”";
        _preview.Visibility = Visibility.Visible;
        BuildChips();
        _chips.Visibility = Visibility.Visible;
        _result.Content = Hint(Strings.T("ta.pick"));
        BuildFooter();
    }

    void BuildChips()
    {
        _chips.Children.Clear();
        var group = "ta" + Guid.NewGuid().ToString("N");
        foreach (var a in TextActions.All)
        {
            var r = new RadioButton { Content = Strings.T(TextActions.TitleKey(a)), GroupName = group, IsChecked = _action == a, Margin = new Thickness(0, 0, 3, 3), Padding = new Thickness(9, 3, 9, 3), FontSize = 12 };
            if (a is TextAction.TranslateEn or TextAction.TranslateUk or TextAction.TranslateNl) r.ToolTip = Strings.T("ta.translate");
            r.SetResourceReference(StyleProperty, "ChipRadio");
            var action = a;
            r.Click += (_, _) => _ = Run(action);
            _chips.Children.Add(r);
        }
    }

    public async Task Run(TextAction a)
    {
        _action = a;
        await Stream(TextActions.Build(a, _selection, Strings.Language));
    }

    // ---- questions ----

    public void ShowQuestion(string question)
    {
        _question = question;
        _selection = "";
        _action = null;
        _icon.SetResourceReference(Icon.DataProperty, "Icon.Ai");
        _title.Text = question;
        _preview.Visibility = Visibility.Collapsed;
        _chips.Visibility = Visibility.Collapsed;
        _ = Stream(TextActions.Question(question, Strings.Language));
    }

    // ---- streaming ----

    async Task Stream(Conversation c)
    {
        if (_streaming) { _m.Cancel(); await ClipboardOps.WaitFor(() => !_m.Busy, TimeSpan.FromSeconds(2)); }
        if (_m.Busy) { _result.Content = Hint(Strings.T("ta.busy")); return; }
        var run = ++_run;
        var answer = new ChatMessage { Role = "assistant" };
        _answer = answer;
        _streaming = true;
        _result.Content = Hint(Strings.T("ai.thinking"));
        BuildFooter();
        App.Island?.HoldOpen("ai-popup");
        try
        {
            await _m.StreamInto(c, answer, _ => { if (run == _run && !_render.IsEnabled) _render.Start(); });
        }
        finally
        {
            App.Island?.ReleaseOpen("ai-popup");
            if (run == _run)
            {
                _streaming = false;
                _render.Stop();
                RenderResult();
                BuildFooter();
            }
        }
    }

    void RenderResult()
    {
        if (_answer is not { } a) return;
        if (a.Error)
        {
            var e = MarkdownView.Para(a.Text, 12, FontWeights.Normal);
            e.SetResourceReference(TextBlock.ForegroundProperty, "WarnBrush");
            _result.Content = e;
            return;
        }
        if (a.Text.Length == 0) { _result.Content = Hint(Strings.T(_streaming ? "ai.thinking" : a.Stopped ? "ai.stopped" : "ai.err.empty")); return; }
        var atEnd = _scroll.VerticalOffset >= _scroll.ScrollableHeight - 24;
        var body = MarkdownView.Render(a.Text);
        if (a.Note.Length > 0 || a.Stopped)
        {
            var n = MarkdownView.Para(a.Note.Length > 0 ? a.Note : Strings.T("ai.stopped"), 11, FontWeights.Normal);
            n.SetResourceReference(TextBlock.ForegroundProperty, a.Note.Length > 0 ? "WarnBrush" : "FgDimBrush");
            n.Margin = new Thickness(0, 4, 0, 0);
            body.Children.Add(n);
        }
        _result.Content = body;
        if (atEnd && _streaming) Dispatcher.BeginInvoke(DispatcherPriority.Background, () => _scroll.ScrollToEnd());
    }

    void BuildFooter()
    {
        _footer.Children.Clear();
        var has = _answer is { Error: false, Text.Length: > 0 };
        if (_streaming)
        {
            _footer.Children.Add(Small("ai.stop", () => _m.Cancel()));
            return;
        }
        if (!has)
        {
            if (_answer is { Error: true } && !AiConfig.FromSettings(Settings.Current).IsReady)
                _footer.Children.Add(Small("ai.openSettings", _m.OpenSettings, accent: true));
            return;
        }
        var text = _answer!.Text;
        if (_question.Length == 0)
        {
            var replace = Small("ta.replace", () => Replace(text), accent: _action is { } a && TextActions.Replaces(a));
            _footer.Children.Add(replace);
            _footer.Children.Add(Spaced(Small("ai.copy", () => AiChatView.CopyText(text), accent: _action is { } b && !TextActions.Replaces(b))));
        }
        else
        {
            _footer.Children.Add(Small("ai.copy", () => AiChatView.CopyText(text), accent: true));
            _footer.Children.Add(Spaced(Small("ta.openChat", () => _m.OpenInChat(_question, _answer!))));
        }
    }

    async void Replace(string text)
    {
        App.Island?.Collapse();
        try { await ClipboardOps.PasteTextAsync(text, _target); }
        catch (Exception ex) { Logger.Error("ai replace", ex); }
    }

    // ---- snapshot helpers ----

    internal void ShowDemoResult(TextAction? action, string text, bool streaming)
    {
        _action = action;
        if (action is not null) BuildChips();
        _answer = new ChatMessage { Role = "assistant", Text = text };
        _streaming = streaming;
        RenderResult();
        BuildFooter();
    }

    // ---- helpers ----

    static string OneLine(string s)
    {
        var t = string.Join(' ', s.Split((char[])['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return t.Length > 160 ? t[..160] + "…" : t;
    }

    static TextBlock Hint(string text)
    {
        var t = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        t.SetResourceReference(StyleProperty, "TextDim");
        return t;
    }

    static FrameworkElement Spaced(FrameworkElement e) { e.Margin = new Thickness(6, 0, 0, 0); return e; }

    static Button Small(string key, Action click, bool accent = false)
    {
        var b = new Button { Content = Strings.T(key), Padding = new Thickness(12, 3, 12, 3), MinHeight = 28 };
        b.SetResourceReference(StyleProperty, accent ? "AccentButton" : "FlatButton");
        b.Click += (_, _) => { try { click(); } catch (Exception ex) { Logger.Error("ai popup", ex); } };
        return b;
    }

    static Button IconAction(string icon, string tipKey, Action click)
    {
        var b = new Button { Width = 28, Height = 28, ToolTip = Strings.T(tipKey), Content = MarkdownView.SmallIcon(icon, 15) };
        System.Windows.Automation.AutomationProperties.SetName(b, Strings.T(tipKey));
        b.SetResourceReference(StyleProperty, "IconButton");
        b.Click += (_, _) => { try { click(); } catch (Exception ex) { Logger.Error("ai popup", ex); } };
        return b;
    }
}
