using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Isle.Core;
using Isle.Modules.AiChat;

namespace Isle.Views.AiChat;

/// <summary>
/// AI tab: header (chat picker · system prompt · new chat · provider settings), the conversation (user bubbles right,
/// Markdown answers left with Copy), pending attachment chips, and the input row (attach · text · send/stop).
/// Chat list, system prompt and settings are in-panel overlays (no popup windows on the non-activating island).
/// While an answer streams, its bubble is re-rendered at most every 80 ms.
/// </summary>
public sealed class AiChatView : Grid, ITabView
{
    readonly AiModule _m;
    readonly Button _title = new() { HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(6, 1, 6, 1), MinHeight = 24, Margin = new Thickness(-6, 0, 0, 0) };
    readonly TextBlock _titleText = new() { FontSize = 12.5, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
    readonly StackPanel _messages = new() { Margin = new Thickness(0, 0, 6, 0) };
    readonly ScrollViewer _scroll = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    readonly WrapPanel _chips = new() { Margin = new Thickness(0, 4, 0, 0) };
    readonly TextBox _input = new() { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MaxHeight = 64, MinHeight = 30, VerticalContentAlignment = VerticalAlignment.Center };
    readonly TextBlock _placeholder = new() { IsHitTestVisible = false, Margin = new Thickness(19, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
    readonly Button _send = new() { Width = 30, Height = 30, Margin = new Thickness(6, 0, 0, 0) };
    readonly Border _overlay = new() { Visibility = Visibility.Collapsed };
    readonly List<ChatAttachment> _pending = [];
    readonly DispatcherTimer _render = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(80) };
    ChatMessage? _streaming;
    Border? _streamingBubble;
    Conversation? _chat;

    public AiChatView(AiModule module)
    {
        _m = module;
        ClipToBounds = true;
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // header
        var head = new DockPanel { Margin = new Thickness(0, -2, 0, 2) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        buttons.Children.Add(IconAction("Icon.Edit", "ai.systemPrompt", ShowSystemPrompt));
        buttons.Children.Add(IconAction("Icon.Plus", "ai.newChat", () => { _m.NewChat(); FocusInput(); }));
        buttons.Children.Add(IconAction("Icon.Key", "ai.settings", ShowSettings));
        DockPanel.SetDock(buttons, Dock.Right);
        head.Children.Add(buttons);
        _title.SetResourceReference(StyleProperty, "GhostButton");
        _titleText.SetResourceReference(TextBlock.ForegroundProperty, "FgBrush");
        var chevron = MarkdownView.SmallIcon("Icon.ChevronDown", 12);
        chevron.Margin = new Thickness(4, 0, 0, 0);
        var titleRow = new DockPanel { Children = { chevron, _titleText } };
        DockPanel.SetDock(chevron, Dock.Right);
        _title.Content = titleRow;
        _title.HorizontalAlignment = HorizontalAlignment.Left;
        _title.MaxWidth = 340;
        _title.Click += (_, _) => ShowChatList();
        head.Children.Add(_title);
        Children.Add(head);

        // conversation
        _scroll.Content = _messages;
        SetRow(_scroll, 1);
        Children.Add(_scroll);

        SetRow(_chips, 2);
        Children.Add(_chips);

        // input
        var row = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };
        var attach = IconAction("Icon.Attach", "ai.attach", PickFiles);
        attach.Margin = new Thickness(0, 0, 4, 0);
        attach.VerticalAlignment = VerticalAlignment.Bottom;
        DockPanel.SetDock(attach, Dock.Left);
        row.Children.Add(attach);
        _send.SetResourceReference(StyleProperty, "AccentButton");
        _send.Padding = new Thickness(0);
        _send.VerticalAlignment = VerticalAlignment.Bottom;
        _send.Click += (_, _) => SendOrStop();
        DockPanel.SetDock(_send, Dock.Right);
        row.Children.Add(_send);
        _input.SetResourceReference(StyleProperty, "FlatTextBox");
        _input.SetBinding(System.Windows.Automation.AutomationProperties.NameProperty, new System.Windows.Data.Binding("[ai.inputName]") { Source = Strings.Instance });
        _input.PreviewKeyDown += OnInputKey;
        _input.TextChanged += (_, _) => UpdatePlaceholder();
        _placeholder.SetResourceReference(TextBlock.ForegroundProperty, "FgFaintBrush");
        row.Children.Add(new Grid { Children = { _input, _placeholder } });
        SetRow(row, 3);
        Children.Add(row);

        _overlay.SetResourceReference(Border.BackgroundProperty, "BgBrush");
        SetRowSpan(_overlay, 4);
        Children.Add(_overlay);

        _render.Tick += (_, _) => { _render.Stop(); RenderStreaming(); };
        Loaded += (_, _) => { Settings.Changed -= OnSettingsChanged; Settings.Changed += OnSettingsChanged; };
        Unloaded += (_, _) => Settings.Changed -= OnSettingsChanged;
        UpdateSendButton();
        UpdatePlaceholder();
    }

    void OnSettingsChanged(string name)
    {
        if (name == "Language") { ShowChat(_chat); UpdatePlaceholder(); }
    }

    public void OnShown() { }
    public void OnHidden() => HideOverlay();

    // ---- conversation ----

    public void ShowChat(Conversation? c)
    {
        _chat = c;
        _streaming = null;
        _streamingBubble = null;
        HideOverlay();
        _titleText.Text = c is { Title.Length: > 0 } ? c.Title : Strings.T("ai.newChatTitle");
        _messages.Children.Clear();
        if (c is null || c.Messages.Count == 0)
        {
            _messages.Children.Add(EmptyState());
        }
        else
        {
            foreach (var m in c.Messages)
            {
                var b = Bubble(m);
                _messages.Children.Add(b);
                if (m.Role == "assistant" && _m.Busy && ReferenceEquals(m, c.Messages[^1])) { _streaming = m; _streamingBubble = b; }
            }
        }
        UpdateSendButton();
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () => _scroll.ScrollToEnd());
    }

    FrameworkElement EmptyState()
    {
        var ready = AiConfig.FromSettings(Settings.Current).IsReady;
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
        var t = Dim(Strings.T(ready ? "ai.emptyHint" : "ai.setupHint"));
        t.HorizontalAlignment = HorizontalAlignment.Center;
        t.TextAlignment = TextAlignment.Center;
        t.TextWrapping = TextWrapping.Wrap;
        t.MaxWidth = 360;
        stack.Children.Add(t);
        if (!ready)
        {
            var b = Small("ai.openSettings", ShowSettings);
            b.HorizontalAlignment = HorizontalAlignment.Center;
            b.Margin = new Thickness(0, 6, 0, 0);
            stack.Children.Add(b);
        }
        return stack;
    }

    Border Bubble(ChatMessage m)
    {
        var user = m.Role == "user";
        var content = new StackPanel();
        if (m.Attachments.Count > 0)
        {
            var files = new WrapPanel { Margin = new Thickness(0, 0, 0, m.Text.Length > 0 ? 4 : 0) };
            foreach (var a in m.Attachments) files.Children.Add(Chip(a, null));
            content.Children.Add(files);
        }
        if (user)
        {
            if (m.Text.Length > 0) content.Children.Add(MarkdownView.Para(m.Text, 12.5, FontWeights.Normal));
        }
        else FillAnswer(content, m);

        var b = new Border
        {
            Child = content, CornerRadius = new CornerRadius(12), Padding = user ? new Thickness(10, 6, 10, 6) : new Thickness(2, 2, 2, 2),
            Margin = new Thickness(user ? 60 : 0, 6, 0, 0), HorizontalAlignment = user ? HorizontalAlignment.Right : HorizontalAlignment.Stretch,
        };
        if (user) b.SetResourceReference(Border.BackgroundProperty, "SurfaceBrush");
        return b;
    }

    void FillAnswer(StackPanel content, ChatMessage m)
    {
        content.Children.Clear();
        var streaming = ReferenceEquals(m, _streaming) || (_m.Busy && _chat is not null && ReferenceEquals(m, _chat.Messages[^1]));
        if (m.Error)
        {
            var e = MarkdownView.Para(m.Text, 12, FontWeights.Normal);
            e.SetResourceReference(TextBlock.ForegroundProperty, "WarnBrush");
            content.Children.Add(e);
            return;
        }
        if (m.Text.Length == 0 && streaming)
        {
            content.Children.Add(Dim(Strings.T("ai.thinking")));
            return;
        }
        content.Children.Add(MarkdownView.Render(m.Text));
        if (m.Note.Length > 0 || m.Stopped)
        {
            var n = Dim(m.Note.Length > 0 ? m.Note : Strings.T("ai.stopped"));
            n.FontSize = 11;
            n.Margin = new Thickness(0, 4, 0, 0);
            n.TextWrapping = TextWrapping.Wrap;
            if (m.Note.Length > 0) n.SetResourceReference(TextBlock.ForegroundProperty, "WarnBrush");
            content.Children.Add(n);
        }
        if (!streaming && m.Text.Length > 0)
        {
            var copy = IconAction("Icon.Copy", "ai.copy", () => CopyText(m.Text));
            copy.Width = copy.Height = 24;
            copy.HorizontalAlignment = HorizontalAlignment.Left;
            copy.Margin = new Thickness(-4, 2, 0, 0);
            content.Children.Add(copy);
        }
    }

    /// <summary>Called by the module for every streamed piece; rendering is coalesced.</summary>
    public void Streamed(ChatMessage m)
    {
        if (!ReferenceEquals(m, _streaming))
        {
            _streaming = m;
            _streamingBubble = _messages.Children.OfType<Border>().LastOrDefault();
        }
        if (!_render.IsEnabled) _render.Start();
    }

    void RenderStreaming()
    {
        if (_streaming is null || _streamingBubble?.Child is not StackPanel content) return;
        var atEnd = _scroll.VerticalOffset >= _scroll.ScrollableHeight - 24;
        FillAnswer(content, _streaming);
        if (atEnd) Dispatcher.BeginInvoke(DispatcherPriority.Background, () => _scroll.ScrollToEnd());
    }

    public void Finished(ChatMessage m)
    {
        _render.Stop();
        var bubble = _streamingBubble;
        _streaming = null;
        if (bubble?.Child is StackPanel content) FillAnswer(content, m);
        else ShowChat(_chat);
        _titleText.Text = _chat is { Title.Length: > 0 } ? _chat.Title : Strings.T("ai.newChatTitle");
        UpdateSendButton();
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () => _scroll.ScrollToEnd());
    }

    // ---- input ----

    void OnInputKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            if (!_m.Busy) SendOrStop();
        }
        else if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) != 0 && System.Windows.Clipboard.ContainsImage())
        {
            e.Handled = true;
            PasteImage();
        }
    }

    async void SendOrStop()
    {
        if (_m.Busy) { _m.Cancel(); return; }
        var text = _input.Text;
        if (text.Trim().Length == 0 && _pending.Count == 0) return;
        var files = _pending.ToList();
        _input.Text = "";
        _pending.Clear();
        RefreshChips();
        try
        {
            var send = _m.Send(text, files);
            UpdateSendButton();
            await send;
        }
        catch (Exception ex) { Logger.Error("ai send", ex); }
        UpdateSendButton();
    }

    void UpdateSendButton()
    {
        var busy = _m.Busy;
        _send.Content = MarkdownView.SmallIcon(busy ? "Icon.Stop" : "Icon.Send", 15, "AccentFgBrush");
        _send.ToolTip = Strings.T(busy ? "ai.stop" : "ai.send");
        System.Windows.Automation.AutomationProperties.SetName(_send, Strings.T(busy ? "ai.stop" : "ai.send"));
    }

    void UpdatePlaceholder()
    {
        _placeholder.Text = Strings.T("ai.inputPlaceholder");
        _placeholder.Visibility = _input.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public void FocusInput()
    {
        App.Island?.AcquireFocus();
        Dispatcher.BeginInvoke(DispatcherPriority.Input, () => { _input.Focus(); Keyboard.Focus(_input); });
    }

    /// <summary>Puts text in the input (command bar `?` hand-over, snapshots).</summary>
    public void SetInput(string text) { _input.Text = text; _input.CaretIndex = text.Length; }

    // ---- attachments ----

    async void PickFiles()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Multiselect = true, Filter = Attachments.DialogFilter, Title = Strings.T("ai.attach") };
        App.Island?.HoldOpen("ai-dialog");
        bool? ok;
        try { ok = dlg.ShowDialog(); }
        finally { App.Island?.ReleaseOpen("ai-dialog"); }
        if (ok != true) return;
        foreach (var path in dlg.FileNames) await AddFile(path);
    }

    async Task AddFile(string path)
    {
        if (_pending.Count >= Attachments.MaxPerMessage) { App.Island?.ShowToast(Strings.T("ai.att.tooMany", Attachments.MaxPerMessage)); return; }
        try
        {
            var a = await Task.Run(() => Attachments.Read(path));
            _pending.Add(a);
            RefreshChips();
        }
        catch (AiException ex) { App.Island?.ShowToast(Strings.T(ex.Key) + (ex.Detail.Length > 0 ? " · " + ex.Detail : "")); }
        catch (Exception ex) { Logger.Error("ai attach", ex); }
    }

    void PasteImage()
    {
        try
        {
            var img = System.Windows.Clipboard.GetImage();
            if (img is null) return;
            var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
            enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(img));
            using var ms = new MemoryStream();
            enc.Save(ms);
            if (ms.Length > Attachments.MaxImageBytes) { App.Island?.ShowToast(Strings.T("ai.att.tooBig")); return; }
            if (_pending.Count >= Attachments.MaxPerMessage) return;
            _pending.Add(new ChatAttachment { Kind = AttachmentKind.Image, Name = "image.png", MediaType = "image/png", Data = Convert.ToBase64String(ms.ToArray()) });
            RefreshChips();
        }
        catch (Exception ex) { Logger.Error("ai paste image", ex); }
    }

    /// <summary>Snapshot helper: renders the last answer as if it were still streaming.</summary>
    internal void ShowStreamingDemo(Conversation c)
    {
        ShowChat(c);
        _streaming = c.Messages[^1];
        _streamingBubble = _messages.Children.OfType<Border>().LastOrDefault();
        if (_streamingBubble?.Child is StackPanel content) FillAnswer(content, _streaming);
        _send.Content = MarkdownView.SmallIcon("Icon.Stop", 15, "AccentFgBrush");
    }

    internal void ScrollToEndNow() { UpdateLayout(); _scroll.ScrollToEnd(); }

    /// <summary>Snapshot helper.</summary>
    internal void AddPending(ChatAttachment a) { _pending.Add(a); RefreshChips(); }

    void RefreshChips()
    {
        _chips.Children.Clear();
        foreach (var a in _pending) _chips.Children.Add(Chip(a, () => { _pending.Remove(a); RefreshChips(); }));
        _chips.Visibility = _pending.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    static FrameworkElement Chip(ChatAttachment a, Action? remove)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        var icon = MarkdownView.SmallIcon(a.Kind == AttachmentKind.Image ? "Icon.Screenshot" : "Icon.Notes", 12);
        icon.Margin = new Thickness(0, 0, 5, 0);
        row.Children.Add(icon);
        var name = new TextBlock { Text = a.Name, FontSize = 11, MaxWidth = 150, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        name.SetResourceReference(TextBlock.ForegroundProperty, "FgBrush");
        row.Children.Add(name);
        if (remove is not null)
        {
            var x = new Button { Width = 18, Height = 18, Margin = new Thickness(4, 0, -4, 0), Content = MarkdownView.SmallIcon("Icon.Close", 10) };
            x.SetResourceReference(StyleProperty, "IconButton");
            x.Click += (_, _) => remove();
            row.Children.Add(x);
        }
        var b = new Border { Child = row, CornerRadius = new CornerRadius(8), Padding = new Thickness(7, 3, 7, 3), Margin = new Thickness(0, 0, 4, 2) };
        b.SetResourceReference(Border.BackgroundProperty, remove is null ? "SurfaceHoverBrush" : "SurfaceBrush");
        return b;
    }

    // ---- overlays ----

    void ShowOverlay(string titleKey, FrameworkElement body, FrameworkElement? extra = null)
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var head = new DockPanel { Margin = new Thickness(0, -2, 0, 4) };
        var close = IconAction("Icon.Close", "ai.close", HideOverlay);
        DockPanel.SetDock(close, Dock.Right);
        head.Children.Add(close);
        if (extra is not null) { DockPanel.SetDock(extra, Dock.Right); head.Children.Add(extra); }
        var t = new TextBlock { Text = Strings.T(titleKey), VerticalAlignment = VerticalAlignment.Center };
        t.SetResourceReference(StyleProperty, "TextTitle");
        head.Children.Add(t);
        grid.Children.Add(head);
        SetRow(body, 1);
        grid.Children.Add(body);
        _overlay.Child = grid;
        _overlay.Visibility = Visibility.Visible;
    }

    void HideOverlay()
    {
        _overlay.Visibility = Visibility.Collapsed;
        _overlay.Child = null;
    }

    internal void ShowChatList()
    {
        var list = new StackPanel();
        var chats = _m.Store.All;
        if (chats.Count == 0) list.Children.Add(Dim(Strings.T("ai.noChats")));
        foreach (var c in chats.Take(60))
        {
            var row = new DockPanel();
            var del = IconAction("Icon.Trash", "ai.deleteChat", () => { _m.Delete(c); ShowChatList(); });
            del.Width = del.Height = 24;
            DockPanel.SetDock(del, Dock.Right);
            row.Children.Add(del);
            var when = Dim(c.Updated.ToLocalTime().ToString(c.Updated.Date == DateTimeOffset.Now.Date ? "HH:mm" : "dd.MM", System.Globalization.CultureInfo.InvariantCulture));
            when.FontSize = 10.5;
            when.Margin = new Thickness(8, 0, 4, 0);
            when.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(when, Dock.Right);
            row.Children.Add(when);
            var name = new TextBlock { Text = c.Title.Length > 0 ? c.Title : Strings.T("ai.newChatTitle"), TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
            name.SetResourceReference(StyleProperty, "Text");
            if (ReferenceEquals(c, _chat)) name.FontWeight = FontWeights.SemiBold;
            row.Children.Add(name);
            var btn = new Button { Content = row, HorizontalContentAlignment = HorizontalAlignment.Stretch, Padding = new Thickness(8, 2, 4, 2), Margin = new Thickness(0, 0, 0, 1) };
            btn.SetResourceReference(StyleProperty, "GhostButton");
            btn.Click += (_, _) => _m.Select(c);
            list.Children.Add(btn);
        }
        ShowOverlay("ai.chats", new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }

    internal void ShowSystemPrompt()
    {
        var box = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Text = _chat?.SystemPrompt ?? "", VerticalContentAlignment = VerticalAlignment.Top, Padding = new Thickness(8, 6, 8, 6) };
        box.SetResourceReference(StyleProperty, "FlatTextBox");
        var hint = new TextBlock { Text = Strings.T("ai.systemHint"), IsHitTestVisible = false, Margin = new Thickness(10, 7, 0, 0), VerticalAlignment = VerticalAlignment.Top, TextWrapping = TextWrapping.Wrap };
        hint.SetResourceReference(TextBlock.ForegroundProperty, "FgFaintBrush");
        hint.Visibility = box.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        box.TextChanged += (_, _) => hint.Visibility = box.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        var save = Small("ai.save", () => { _m.SetSystemPrompt(box.Text); HideOverlay(); }, accent: true);
        save.HorizontalAlignment = HorizontalAlignment.Right;
        save.Margin = new Thickness(0, 6, 0, 0);
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.Children.Add(new Grid { Children = { box, hint } });
        SetRow(save, 1);
        grid.Children.Add(save);
        ShowOverlay("ai.systemPrompt", grid);
    }

    internal void ShowSettings()
    {
        var s = Settings.Current;
        var cfg = AiConfig.FromSettings(s);
        var kind = cfg.Provider;
        var stack = new StackPanel();

        var providers = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 8, 0) };
        var group = "aiprov" + Guid.NewGuid().ToString("N");
        var anth = new RadioButton { Content = "Anthropic", IsChecked = kind == AiProviderKind.Anthropic, GroupName = group, Margin = new Thickness(0, 0, 4, 0) };
        var oai = new RadioButton { Content = Strings.T("ai.openaiCompat"), IsChecked = kind == AiProviderKind.OpenAi, GroupName = group };
        anth.SetResourceReference(StyleProperty, "ChipRadio");
        oai.SetResourceReference(StyleProperty, "ChipRadio");
        providers.Children.Add(anth);
        providers.Children.Add(oai);

        var model = Field();
        var baseUrl = Field();
        var key = new PasswordBox { MinHeight = 26, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
        var keyHint = new TextBlock { IsHitTestVisible = false, Margin = new Thickness(11, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 11.5 };
        keyHint.SetResourceReference(TextBlock.ForegroundProperty, "FgFaintBrush");
        key.PasswordChanged += (_, _) => keyHint.Visibility = key.Password.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        var baseRow = Row("ai.baseUrl", baseUrl);
        stack.Children.Add(Row("ai.model", model));
        stack.Children.Add(baseRow);
        stack.Children.Add(Row("ai.key", new Grid { Children = { key, keyHint } }));

        var status = Dim("");
        status.TextWrapping = TextWrapping.Wrap;
        status.FontSize = 11;
        status.Margin = new Thickness(0, 4, 0, 0);

        AiProviderKind Kind() => oai.IsChecked == true ? AiProviderKind.OpenAi : AiProviderKind.Anthropic;
        void Load()
        {
            var k = Kind();
            var p = k == AiProviderKind.OpenAi ? "ai.openai" : "ai.anthropic";
            model.Text = s.Opt(p + ".model", k == AiProviderKind.Anthropic ? AiConfig.DefaultAnthropicModel : "");
            baseUrl.Text = s.Opt("ai.openai.base", AiConfig.DefaultOpenAiBase);
            baseRow.Visibility = k == AiProviderKind.OpenAi ? Visibility.Visible : Visibility.Collapsed;
            key.Password = "";
            keyHint.Text = Strings.T(s.Opt(p + ".key").Length > 0 ? "ai.keySaved" : "ai.keyEmpty");
            keyHint.Visibility = Visibility.Visible;
            status.Text = "";
        }
        anth.Checked += (_, _) => Load();
        oai.Checked += (_, _) => Load();
        Load();

        void Save()
        {
            AiModule.SaveConfig(Kind(), model.Text, baseUrl.Text, key.Password);
            Load();
        }

        var actions = new WrapPanel { Margin = new Thickness(72, 2, 0, 0) };
        actions.Children.Add(Small("ai.save", () => { Save(); status.Text = Strings.T("ai.saved"); ShowEmptyIfNeeded(); }, accent: true));
        var test = Small("ai.test", () => { });
        test.Margin = new Thickness(6, 0, 0, 0);
        test.Click += async (_, _) =>
        {
            if (_m.Busy) return;
            Save();
            status.Text = Strings.T("ai.testing");
            status.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
            var err = await _m.TestConnection();
            status.Text = err ?? Strings.T("ai.testOk");
            status.SetResourceReference(TextBlock.ForegroundProperty, err is null ? "OkBrush" : "WarnBrush");
        };
        actions.Children.Add(test);
        var forget = Small("ai.forgetKey", () => { AiModule.ForgetKey(Kind()); Load(); ShowEmptyIfNeeded(); });
        forget.Margin = new Thickness(6, 0, 0, 0);
        actions.Children.Add(forget);
        stack.Children.Add(actions);
        stack.Children.Add(status);
        ShowOverlay("ai.settings", new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }, providers);
    }

    void ShowEmptyIfNeeded()
    {
        if (_chat is not null && _chat.Messages.Count > 0) return;
        _messages.Children.Clear();
        _messages.Children.Add(EmptyState());
    }

    static TextBox Field()
    {
        var t = new TextBox { MinHeight = 26, Padding = new Thickness(4.5, 0, 4.5, 0), VerticalContentAlignment = VerticalAlignment.Center };
        t.SetResourceReference(StyleProperty, "FlatTextBox");
        return t;
    }

    static FrameworkElement Row(string labelKey, FrameworkElement field)
    {
        var dock = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
        var l = Dim(Strings.T(labelKey));
        l.Width = 72;
        l.VerticalAlignment = VerticalAlignment.Center;
        DockPanel.SetDock(l, Dock.Left);
        dock.Children.Add(l);
        dock.Children.Add(field);
        return dock;
    }

    // ---- helpers ----

    public static void CopyText(string text)
    {
        try { System.Windows.Clipboard.SetText(text); App.Island?.ShowToast(Strings.T("ai.copied"), seconds: 2); }
        catch (Exception ex) { Logger.Error("ai copy", ex); }
    }

    static TextBlock Dim(string text)
    {
        var t = new TextBlock { Text = text };
        t.SetResourceReference(StyleProperty, "TextDim");
        return t;
    }

    static Button Small(string key, Action click, bool accent = false)
    {
        var b = new Button { Content = Strings.T(key), Padding = new Thickness(12, 3, 12, 3), MinHeight = 28 };
        b.SetResourceReference(StyleProperty, accent ? "AccentButton" : "FlatButton");
        b.Click += (_, _) => { try { click(); } catch (Exception ex) { Logger.Error("ai action", ex); } };
        return b;
    }

    static Button IconAction(string icon, string tipKey, Action click)
    {
        var b = new Button { Width = 28, Height = 28, ToolTip = Strings.T(tipKey), Content = MarkdownView.SmallIcon(icon, 15) };
        System.Windows.Automation.AutomationProperties.SetName(b, Strings.T(tipKey));
        b.SetResourceReference(StyleProperty, "IconButton");
        b.Click += (_, _) => { try { click(); } catch (Exception ex) { Logger.Error("ai action", ex); } };
        return b;
    }
}
