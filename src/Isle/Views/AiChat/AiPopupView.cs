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
    readonly Icon _icon = MarkdownView.SmallIcon("Icon.Ai", 16, "AccentTextBrush");
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
        var clipped = TextActions.IsClipped(text);
        _preview.Text = clipped ? Strings.T("ta.selectionClipped") : "“" + OneLine(text) + "”";
        _preview.TextWrapping = clipped ? TextWrapping.Wrap : TextWrapping.NoWrap;
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

    public void ShowQuestion(string question, bool startRequest = true)
    {
        _question = question;
        _selection = "";
        _action = null;
        _icon.SetResourceReference(Icon.DataProperty, "Icon.Ai");
        _title.Text = question;
        _preview.Visibility = Visibility.Collapsed;
        _chips.Visibility = Visibility.Collapsed;
        if (startRequest) _ = Stream(TextActions.Question(question, Strings.Language));
    }

    // ---- streaming ----

    static string WaitingKey => AiProviders.FromSettings(Settings.Current) == AiProviderKind.Codex ? "ai.codexWaiting" : "ai.thinking";

    async Task Stream(Conversation c)
    {
        var answer = new ChatMessage { Role = "assistant" };
        _answer = answer;
        if (_streaming) { _m.Cancel(); await ClipboardOps.WaitFor(() => !_m.Busy, TimeSpan.FromSeconds(2)); }
        if (!ReferenceEquals(_answer, answer)) return;
        if (_m.Busy) { _result.Content = Hint(Strings.T("ta.busy")); BuildFooter(); return; }
        var run = ++_run;
        _streaming = true;
        _result.Content = Hint(Strings.T(WaitingKey));
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
        if (a.Text.Length == 0) { _result.Content = Hint(Strings.T(_streaming ? WaitingKey : a.Stopped ? "ai.stopped" : "ai.err.empty")); return; }
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
            if (_answer is { Error: true } && !_m.IsProviderInstalled(AiProviders.FromSettings(Settings.Current)))
                _footer.Children.Add(Small("ai.openSettings", _m.OpenSettings, accent: true));
            return;
        }
        var answer = _answer!;
        var text = answer.Text;
        if (_question.Length == 0)
        {
            var canReplace = TextActions.CanReplace(_selection, answer, _streaming);
            var preferReplace = canReplace && _action is { } a && TextActions.Replaces(a);
            if (canReplace) _footer.Children.Add(Small("ta.replace", () => Replace(answer), accent: preferReplace));
            var copy = Small("ai.copy", () => AiChatView.CopyText(text), accent: !preferReplace);
            _footer.Children.Add(canReplace ? Spaced(copy) : copy);
        }
        else
        {
            _footer.Children.Add(Small("ai.copy", () => AiChatView.CopyText(text), accent: true));
            _footer.Children.Add(Spaced(Small("ta.openChat", () => _m.OpenInChat(_question, _answer!))));
        }
    }

    async void Replace(ChatMessage answer)
    {
        if (!ReferenceEquals(answer, _answer) || !TextActions.CanReplace(_selection, answer, _streaming)) return;
        var target = _target;
        App.Island?.Collapse();
        try
        {
            var result = await ClipboardOps.PasteTextAsync(answer.Text, target);
            if (ClipboardOps.PasteNotice(result) is { } notice) App.Island?.ShowToast(notice);
        }
        catch (Exception ex) { Logger.Error("ai replace", ex); App.Island?.ShowToast(Strings.T("clipboard.pasteFailed")); }
    }

    // ---- snapshot helpers ----

    internal void ShowDemoResult(TextAction? action, string text, bool streaming, bool error = false)
    {
        _action = action;
        if (action is not null) BuildChips();
        _answer = new ChatMessage { Role = "assistant", Text = text, Error = error };
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

    static TextBlock Hint(string text) => Ui.Dim(text, wrap: true);

    static FrameworkElement Spaced(FrameworkElement e) { e.Margin = new Thickness(6, 0, 0, 0); return e; }

    static Button Small(string key, Action click, bool accent = false) =>
        Ui.TextButton(Strings.T(key), accent ? "AccentButton" : "FlatButton", new Thickness(12, 3, 12, 3), click, "ai popup", minHeight: 28);

    static Button IconAction(string icon, string tipKey, Action click) => Ui.IconAction(icon, tipKey, click, "ai popup");
}
