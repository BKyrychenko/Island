using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Isle.Core;
using Isle.Modules.Notes;

namespace Isle.Views.Notes;

/// <summary>
/// Notes tab: searchable list (pinned first, folder filter) on the left; a RichTextBox editor with a small toolbar on
/// the right (bold/italic/underline, bullets, numbers, checkbox items, two heading sizes, pin, move, delete).
/// Edits autosave 500 ms after typing stops. Deleting shows an Undo toast for 5 s.
/// </summary>
public sealed class NotesView : Grid, ITabView, IDisposable
{
    const double BodySize = 13, H1Size = 18, H2Size = 15;
    const char Unchecked = '☐', Checked = '☒'; // ☑ falls back to the colour emoji font; ☒ stays in Segoe UI Symbol

    readonly NotesStore _store;
    readonly IslandWindow? _island;
    readonly TextBox _search = new();
    readonly TextBlock _searchHint = new() { IsHitTestVisible = false, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
    readonly StackPanel _list = new();
    readonly TextBlock _folderLabel = new() { FontSize = 11, Margin = new Thickness(2, 4, 0, 2) };
    readonly RichTextBox _editor = new();
    readonly TextBlock _editorHint = new() { IsHitTestVisible = false, Margin = new Thickness(12, 9, 0, 0), VerticalAlignment = VerticalAlignment.Top };
    readonly StackPanel _toolbar = new() { Orientation = Orientation.Horizontal };
    readonly Border _overlay = new() { Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, MinWidth = 168, MaxHeight = 150, Padding = new Thickness(4), CornerRadius = new CornerRadius(12), BorderThickness = new Thickness(1) };
    readonly StackPanel _overlayList = new();
    readonly TextBlock _empty = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center };
    readonly Button _pinButton;
    readonly DispatcherTimer _autosave;

    string? _folder;          // null = all notes
    string? _currentId;
    bool _loading, _dirty;

    public NotesView(NotesStore store, IslandWindow? island)
    {
        _store = store;
        _island = island;
        _autosave = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Background, (_, _) => SaveNow(), Dispatcher);
        _autosave.Stop();

        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(168) });
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // ---- left: search + folder + list ----
        var left = new Grid();
        left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var searchRow = new DockPanel();
        var add = SmallButton("Icon.Plus", "notes.new", () => NewNote(focus: true));
        var folders = SmallButton("Icon.Folder", "notes.folders", () => ShowFolderOverlay(move: false));
        DockPanel.SetDock(add, Dock.Right);
        DockPanel.SetDock(folders, Dock.Right);
        searchRow.Children.Add(add);
        searchRow.Children.Add(folders);
        var searchHost = new Grid();
        _search.SetResourceReference(StyleProperty, "FlatTextBox");
        _search.MinHeight = 28;
        _search.TextChanged += (_, _) => { _searchHint.Visibility = _search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed; RebuildList(); };
        _searchHint.SetResourceReference(TextBlock.ForegroundProperty, "FgFaintBrush");
        _searchHint.SetBinding(TextBlock.TextProperty, Loc("notes.search"));
        searchHost.Children.Add(_search);
        searchHost.Children.Add(_searchHint);
        searchRow.Children.Add(searchHost);
        left.Children.Add(searchRow);
        _folderLabel.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
        SetRow(_folderLabel, 1);
        left.Children.Add(_folderLabel);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _list };
        SetRow(scroll, 2);
        left.Children.Add(scroll);
        Children.Add(left);

        // ---- right: toolbar + editor ----
        var right = new Grid();
        right.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        SetColumn(right, 2);
        _toolbar.Children.Add(TextToolButton("B", "notes.bold", FontWeights.Bold, FontStyles.Normal, false, () => EditingCommands.ToggleBold.Execute(null, _editor)));
        _toolbar.Children.Add(TextToolButton("I", "notes.italic", FontWeights.Normal, FontStyles.Italic, false, () => EditingCommands.ToggleItalic.Execute(null, _editor)));
        _toolbar.Children.Add(TextToolButton("U", "notes.underline", FontWeights.Normal, FontStyles.Normal, true, () => EditingCommands.ToggleUnderline.Execute(null, _editor)));
        _toolbar.Children.Add(Separator());
        _toolbar.Children.Add(TextToolButton("•", "notes.bullets", FontWeights.Bold, FontStyles.Normal, false, () => EditingCommands.ToggleBullets.Execute(null, _editor)));
        _toolbar.Children.Add(TextToolButton("1.", "notes.numbers", FontWeights.SemiBold, FontStyles.Normal, false, () => EditingCommands.ToggleNumbering.Execute(null, _editor)));
        _toolbar.Children.Add(TextToolButton(Unchecked.ToString(), "notes.checkbox", FontWeights.Normal, FontStyles.Normal, false, ToggleCheckboxes));
        _toolbar.Children.Add(Separator());
        _toolbar.Children.Add(TextToolButton("H1", "notes.h1", FontWeights.Bold, FontStyles.Normal, false, () => ToggleHeading(H1Size)));
        _toolbar.Children.Add(TextToolButton("H2", "notes.h2", FontWeights.SemiBold, FontStyles.Normal, false, () => ToggleHeading(H2Size)));
        var tools = new DockPanel { LastChildFill = false };
        var del = SmallButton("Icon.Trash", "notes.delete", DeleteCurrent);
        var move = SmallButton("Icon.Folder", "notes.move", () => ShowFolderOverlay(move: true));
        _pinButton = SmallButton("Icon.Pin", "notes.pin", TogglePin);
        foreach (var b in new[] { del, move, _pinButton }) { DockPanel.SetDock(b, Dock.Right); tools.Children.Add(b); }
        DockPanel.SetDock(_toolbar, Dock.Left);
        tools.Children.Add(_toolbar);
        right.Children.Add(tools);

        _editor.SetResourceReference(StyleProperty, "FlatTextBox");
        _editor.VerticalContentAlignment = VerticalAlignment.Top;
        _editor.Padding = new Thickness(6, 6, 6, 6);
        _editor.Margin = new Thickness(0, 4, 0, 0);
        _editor.FontSize = BodySize;
        _editor.AcceptsTab = true;
        _editor.IsDocumentEnabled = true;
        _editor.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _editor.Document = NewDocument();
        _editor.TextChanged += (_, _) => OnEdited();
        _editor.PreviewMouseLeftButtonUp += OnEditorClick;
        _editor.PreviewKeyDown += OnEditorKey;
        _editorHint.SetResourceReference(TextBlock.ForegroundProperty, "FgFaintBrush");
        _editorHint.SetBinding(TextBlock.TextProperty, Loc("notes.startTyping"));
        var editorHost = new Grid();
        editorHost.Children.Add(_editor);
        editorHost.Children.Add(_editorHint);
        _editorHint.Margin = new Thickness(13, 13, 0, 0);
        SetRow(editorHost, 1);
        right.Children.Add(editorHost);
        _empty.SetResourceReference(StyleProperty, "TextDim");
        _empty.SetBinding(TextBlock.TextProperty, Loc("notes.none"));
        SetRow(_empty, 1);
        right.Children.Add(_empty);
        Children.Add(right);

        // ---- folder overlay ----
        _overlay.SetResourceReference(Border.BackgroundProperty, "PanelBgBrush");
        _overlay.SetResourceReference(Border.BorderBrushProperty, "StrokeBrush");
        _overlay.Child = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _overlayList };
        _overlay.Margin = new Thickness(0, 32, 0, 0);
        SetColumnSpan(_overlay, 3);
        Children.Add(_overlay);

        _store.Changed += OnStoreChanged;
        Strings.LanguageChanged += RebuildList;
        var last = Settings.Current.Opt("notes.last");
        RebuildList();
        Open(_store.Find(last) ?? _store.Search(null).FirstOrDefault());
    }

    static System.Windows.Data.Binding Loc(string key) => new($"[{key}]") { Source = Strings.Instance };

    static Button SmallButton(string icon, string tip, Action click)
    {
        var b = new Button { Width = 28, Height = 28, Margin = new Thickness(2, 0, 0, 0) };
        b.SetResourceReference(StyleProperty, "IconButton");
        var i = new Icon { Width = 15, Height = 15 };
        i.SetResourceReference(Icon.DataProperty, icon);
        i.SetResourceReference(Icon.ForegroundProperty, "FgDimBrush");
        b.Content = i;
        b.SetBinding(ToolTipProperty, Loc(tip));
        b.Click += (_, _) => { try { click(); } catch (Exception ex) { Logger.Error("notes button", ex); } };
        return b;
    }

    static Button TextToolButton(string text, string tip, FontWeight weight, FontStyle style, bool underline, Action click)
    {
        var t = new TextBlock { Text = text, FontWeight = weight, FontStyle = style, FontSize = 12.5 };
        if (underline) t.TextDecorations = TextDecorations.Underline;
        t.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
        var b = new Button { Width = 26, Height = 26, Content = t };
        b.SetResourceReference(StyleProperty, "IconButton");
        b.SetBinding(ToolTipProperty, Loc(tip));
        b.Click += (_, _) => { try { click(); } catch (Exception ex) { Logger.Error("notes format", ex); } };
        return b;
    }

    static FrameworkElement Separator()
    {
        var s = new Border { Width = 1, Height = 14, Margin = new Thickness(4, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
        s.SetResourceReference(Border.BackgroundProperty, "StrokeBrush");
        return s;
    }

    internal static FlowDocument NewDocument()
    {
        var doc = new FlowDocument { PagePadding = new Thickness(0), FontSize = BodySize };
        // tight paragraphs (WPF's default is a full empty line between paragraphs)
        doc.Resources.Add(typeof(Paragraph), new Style(typeof(Paragraph)) { Setters = { new Setter(Block.MarginProperty, new Thickness(0, 0, 0, 3)) } });
        doc.Resources.Add(typeof(List), new Style(typeof(List)) { Setters = { new Setter(Block.MarginProperty, new Thickness(0, 0, 0, 3)), new Setter(List.PaddingProperty, new Thickness(20, 0, 0, 0)) } });
        return doc;
    }

    // ---- list ----

    void OnStoreChanged() => RebuildList();

    void RebuildList()
    {
        _list.Children.Clear();
        _folderLabel.Text = _folder is null ? Strings.T("notes.allNotes") : _folder.Length == 0 ? Strings.T("notes.noFolder") : _folder;
        foreach (var n in _store.Search(_search.Text, _folder))
            _list.Children.Add(ListItem(n));
        if (_list.Children.Count == 0)
        {
            var t = new TextBlock { Margin = new Thickness(4, 8, 0, 0), TextWrapping = TextWrapping.Wrap };
            t.SetResourceReference(StyleProperty, "TextDim");
            t.Text = _search.Text.Length > 0 ? Strings.T("notes.noMatches") : Strings.T("notes.emptyList");
            _list.Children.Add(t);
        }
        _empty.Visibility = _currentId is null ? Visibility.Visible : Visibility.Collapsed;
        _pinButton.Opacity = _store.Find(_currentId ?? "")?.Pinned == true ? 1 : 0.55;
    }

    FrameworkElement ListItem(Note n)
    {
        var selected = n.Id == _currentId;
        var title = new TextBlock { Text = n.Title.Length > 0 ? n.Title : Strings.T("notes.untitled"), FontWeight = FontWeights.SemiBold, FontSize = 12.5, TextTrimming = TextTrimming.CharacterEllipsis };
        var preview = new TextBlock { Text = n.Preview, FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis };
        preview.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
        var texts = new StackPanel();
        texts.Children.Add(title);
        if (n.Preview.Length > 0) texts.Children.Add(preview);
        var row = new DockPanel();
        if (n.Pinned)
        {
            var pin = new Icon { Width = 11, Height = 11, Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Top };
            pin.SetResourceReference(Icon.DataProperty, "Icon.Pin");
            pin.SetResourceReference(Icon.ForegroundProperty, "AccentBrush");
            DockPanel.SetDock(pin, Dock.Right);
            row.Children.Add(pin);
        }
        row.Children.Add(texts);
        var b = new Button { Content = row, Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(0, 0, 0, 2), MinHeight = 0 };
        b.SetResourceReference(StyleProperty, selected ? "FlatButton" : "GhostButton");
        b.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        var id = n.Id;
        b.Click += (_, _) => Open(_store.Find(id));
        return b;
    }

    // ---- folders overlay ----

    void ShowFolderOverlay(bool move)
    {
        if (_overlay.IsVisible) { _overlay.Visibility = Visibility.Collapsed; return; }
        if (move && _currentId is null) return;
        _overlayList.Children.Clear();
        void Item(string text, Action click, bool dim = false)
        {
            var t = new TextBlock { Text = text };
            if (dim) t.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
            var b = new Button { Content = t, Padding = new Thickness(10, 4, 10, 4) };
            b.SetResourceReference(StyleProperty, "GhostButton");
            b.HorizontalContentAlignment = HorizontalAlignment.Left;
            b.Click += (_, _) => { _overlay.Visibility = Visibility.Collapsed; click(); };
            _overlayList.Children.Add(b);
        }
        if (!move) Item(Strings.T("notes.allNotes"), () => { _folder = null; RebuildList(); });
        Item(Strings.T("notes.noFolder"), () => { if (move) _store.Move(_currentId!, ""); else { _folder = ""; RebuildList(); } });
        foreach (var f in _store.Folders)
        {
            var name = f;
            Item(name, () => { if (move) _store.Move(_currentId!, name); else { _folder = name; RebuildList(); } });
        }
        // new folder: inline box
        var box = new TextBox { Margin = new Thickness(4, 4, 4, 2), MinHeight = 26 };
        box.SetResourceReference(StyleProperty, "FlatTextBox");
        box.ToolTip = Strings.T("notes.newFolderHint");
        box.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            var name = box.Text.Trim();
            if (name.Length == 0) return;
            _store.AddFolder(name);
            if (move) _store.Move(_currentId!, name);
            else { _folder = name; RebuildList(); }
            _overlay.Visibility = Visibility.Collapsed;
        };
        var hint = new TextBlock { Margin = new Thickness(10, 4, 0, 0), FontSize = 11 };
        hint.SetResourceReference(TextBlock.ForegroundProperty, "FgFaintBrush");
        hint.Text = Strings.T("notes.newFolderHint");
        _overlayList.Children.Add(hint);
        _overlayList.Children.Add(box);
        _overlay.HorizontalAlignment = move ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        _overlay.Visibility = Visibility.Visible;
    }

    // ---- open / save ----

    void Open(Note? n)
    {
        SaveNow();
        _currentId = n?.Id;
        _loading = true;
        try
        {
            var doc = NewDocument();
            if (n is not null) Load(doc, n);
            _editor.Document = doc;
        }
        finally { _loading = false; }
        _editor.Visibility = n is null ? Visibility.Collapsed : Visibility.Visible;
        _toolbar.IsEnabled = n is not null;
        UpdateEditorHint();
        if (n is not null && !App.IsSnapshot)
        {
            Settings.Current.SetOpt("notes.last", n.Id);
            JsonStore.Save(Paths.SettingsFile, Settings.Current);
        }
        RebuildList();
    }

    internal static void Load(FlowDocument doc, Note n)
    {
        if (n.Xaml.Length > 0)
        {
            try
            {
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(n.Xaml));
                new TextRange(doc.ContentStart, doc.ContentEnd).Load(ms, DataFormats.Xaml);
                CleanFormatting(doc);
                return;
            }
            catch (Exception ex) { Logger.Warn($"note {n.Id}: formatted text unreadable, showing plain text ({ex.GetType().Name})"); }
        }
        doc.Blocks.Clear();
        foreach (var line in n.Plain.Split('\n'))
            doc.Blocks.Add(new Paragraph(new Run(line.TrimEnd('\r'))));
    }

    /// <summary>TextRange.Save writes the editor's inherited colour/font into the XAML; drop them so theme switches and
    /// font changes apply to old notes. Heading sizes (set on paragraphs) are kept.</summary>
    static void CleanFormatting(FlowDocument doc)
    {
        foreach (var block in doc.Blocks.ToList()) Clean(block);

        static void Clean(TextElement e)
        {
            e.ClearValue(TextElement.ForegroundProperty);
            e.ClearValue(TextElement.FontFamilyProperty);
            e.ClearValue(TextElement.BackgroundProperty);
            if (e is Block blk) blk.ClearValue(Block.MarginProperty);
            if (e is not Paragraph && e.ReadLocalValue(TextElement.FontSizeProperty) is double) e.ClearValue(TextElement.FontSizeProperty);
            if (e is Paragraph para && para.FontSize is not (H1Size or H2Size)) para.ClearValue(TextElement.FontSizeProperty);
            switch (e)
            {
                case Paragraph p: foreach (var i in p.Inlines.ToList()) Clean(i); break;
                case Span s: foreach (var i in s.Inlines.ToList()) Clean(i); break;
                case List l: foreach (var li in l.ListItems.ToList()) foreach (var b in li.Blocks.ToList()) Clean(b); break;
                case Section sec: foreach (var b in sec.Blocks.ToList()) Clean(b); break;
            }
        }
    }

    void OnEdited()
    {
        UpdateEditorHint();
        if (_loading || _currentId is null) return;
        _dirty = true;
        _autosave.Stop();
        _autosave.Start();
    }

    void UpdateEditorHint()
    {
        var empty = _currentId is not null && IsEmpty(_editor.Document);
        _editorHint.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
    }

    static bool IsEmpty(FlowDocument doc) =>
        doc.Blocks.Count == 0 || (doc.Blocks.Count == 1 && doc.Blocks.FirstBlock is Paragraph p && !p.Inlines.Any(i => i is not Run r || r.Text.Length > 0));

    /// <summary>Writes the editor into the store now (autosave target; also on switch/hide).</summary>
    public void SaveNow()
    {
        _autosave.Stop();
        if (!_dirty || _currentId is null) return;
        _dirty = false;
        try
        {
            var (xaml, plain) = Serialize(_editor.Document);
            _store.Update(_currentId, xaml, plain);
        }
        catch (Exception ex) { Logger.Error("note save", ex); }
    }

    internal static (string Xaml, string Plain) Serialize(FlowDocument doc)
    {
        var range = new TextRange(doc.ContentStart, doc.ContentEnd);
        using var ms = new MemoryStream();
        range.Save(ms, DataFormats.Xaml);
        return (Encoding.UTF8.GetString(ms.ToArray()), range.Text.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    public void NewNote(bool focus)
    {
        SaveNow();
        var n = _store.Create(_folder ?? "");
        _search.Text = "";
        Open(n);
        if (focus) FocusEditor();
    }

    void FocusEditor()
    {
        if (App.IsSnapshot) return;
        _island?.AcquireFocus();
        // the panel fades in over a few frames and is collapsed at first: focus once the editor is really visible
        if (_editor.IsVisible) Dispatcher.BeginInvoke(FocusNow, DispatcherPriority.Input);
        else
        {
            DependencyPropertyChangedEventHandler? h = null;
            h = (_, _) =>
            {
                if (!_editor.IsVisible) return;
                _editor.IsVisibleChanged -= h;
                Dispatcher.BeginInvoke(FocusNow, DispatcherPriority.Input);
            };
            _editor.IsVisibleChanged += h;
        }

        void FocusNow()
        {
            _editor.Focus();
            Keyboard.Focus(_editor);
            _editor.CaretPosition = _editor.Document.ContentEnd;
        }
    }

    void TogglePin()
    {
        if (_currentId is null || _store.Find(_currentId) is not { } n) return;
        SaveNow();
        _store.SetPinned(n.Id, !n.Pinned);
    }

    void DeleteCurrent()
    {
        if (_currentId is null) return;
        SaveNow();
        var removed = _store.Delete(_currentId);
        if (removed is not { } r) return;
        _currentId = null;
        Open(_store.Search(_search.Text, _folder).FirstOrDefault());
        _island?.ShowToast(Strings.T("notes.deleted"), Strings.T("common.undo"), () =>
        {
            _store.Restore(r.Note, r.Index);
            Open(_store.Find(r.Note.Id));
        }, seconds: 5);
    }

    // ---- formatting ----

    IEnumerable<Paragraph> SelectedParagraphs()
    {
        var start = _editor.Selection.Start.Paragraph;
        var end = _editor.Selection.End.Paragraph;
        if (start is null) yield break;
        for (Block? b = start; b is not null; b = NextBlock(b))
        {
            if (b is Paragraph p) yield return p;
            if (ReferenceEquals(b, end) || end is null) yield break;
        }
    }

    static Block? NextBlock(Block b)
    {
        if (b.NextBlock is { } next) return next;
        // inside a list item: continue in the following item
        if (b.Parent is ListItem li && li.NextListItem is { } nli) return nli.Blocks.FirstBlock;
        return null;
    }

    void ToggleHeading(double size)
    {
        var paras = SelectedParagraphs().ToList();
        if (paras.Count == 0) return;
        var off = paras.All(p => p.FontSize == size);
        foreach (var p in paras)
        {
            if (off) { p.ClearValue(TextElement.FontSizeProperty); p.ClearValue(TextElement.FontWeightProperty); }
            else { p.FontSize = size; p.FontWeight = size == H1Size ? FontWeights.Bold : FontWeights.SemiBold; }
        }
        OnEdited();
    }

    void ToggleCheckboxes()
    {
        var paras = SelectedParagraphs().ToList();
        if (paras.Count == 0) return;
        var remove = paras.All(StartsWithBox);
        foreach (var p in paras)
        {
            var start = p.ContentStart.GetInsertionPosition(LogicalDirection.Forward);
            if (remove)
            {
                var end = start.GetPositionAtOffset(2) ?? start;
                var r = new TextRange(start, end);
                if (r.Text.Length > 0 && (r.Text[0] == Unchecked || r.Text[0] == Checked || r.Text[0] == '☑')) r.Text = "";
            }
            else if (!StartsWithBox(p)) start.InsertTextInRun(Unchecked + " ");
        }
        OnEdited();
    }

    static bool StartsWithBox(Paragraph p)
    {
        var t = new TextRange(p.ContentStart, p.ContentEnd).Text;
        return t.Length > 0 && (t[0] == Unchecked || t[0] == Checked || t[0] == '☑');
    }

    /// <summary>Clicking a ☐/☑ glyph toggles it.</summary>
    void OnEditorClick(object sender, MouseButtonEventArgs e)
    {
        var pos = _editor.GetPositionFromPoint(e.GetPosition(_editor), snapToText: false);
        if (pos is null) return;
        foreach (var dir in new[] { LogicalDirection.Forward, LogicalDirection.Backward })
        {
            var text = pos.GetTextInRun(dir);
            if (text.Length == 0) continue;
            var ch = dir == LogicalDirection.Forward ? text[0] : text[^1];
            if (ch is not (Unchecked or Checked or '☑')) continue;
            var start = dir == LogicalDirection.Forward ? pos : pos.GetPositionAtOffset(-1);
            var end = dir == LogicalDirection.Forward ? pos.GetPositionAtOffset(1) : pos;
            if (start is null || end is null) return;
            new TextRange(start, end).Text = (ch == Unchecked ? Checked : Unchecked).ToString();
            e.Handled = true;
            OnEdited();
            return;
        }
    }

    /// <summary>Enter on a checkbox line starts the next line with ☐; Enter on an empty checkbox line ends the list.</summary>
    void OnEditorKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { SaveNow(); return; }
        if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None) return;
        if (_editor.CaretPosition.Paragraph is not { } p || !StartsWithBox(p)) return;
        var text = new TextRange(p.ContentStart, p.ContentEnd).Text.Trim();
        if (text.Length <= 1)
        {
            ToggleCheckboxes();
            e.Handled = true;
            return;
        }
        Dispatcher.BeginInvoke(() =>
        {
            if (_editor.CaretPosition.Paragraph is { } np && !StartsWithBox(np))
            {
                _editor.CaretPosition.Paragraph.ContentStart.GetInsertionPosition(LogicalDirection.Forward).InsertTextInRun(Unchecked + " ");
                _editor.CaretPosition = np.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
            }
        }, DispatcherPriority.Input);
    }

    // ---- tab lifecycle ----

    public void OnShown() { }

    public void OnHidden()
    {
        SaveNow();
        _overlay.Visibility = Visibility.Collapsed;
    }

    public void Dispose()
    {
        SaveNow();
        _store.Changed -= OnStoreChanged;
        Strings.LanguageChanged -= RebuildList;
    }
}
