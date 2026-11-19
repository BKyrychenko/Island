using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Isle.Core;
using Isle.Modules.Commands;

namespace Isle.Views.Commands;

/// <summary>
/// Command bar overlay: one focused input, results below (best first). Typing re-queries after 80 ms; every provider runs
/// off the UI thread and its results are merged as they arrive. ↑/↓ select, Enter runs (system commands need a second
/// Enter), Esc closes (the island's Esc handling).
/// </summary>
public sealed class CommandBarView : Grid, ITabView
{
    const int MaxRows = 8;
    readonly IReadOnlyList<ICommandProvider> _providers;
    readonly Action<CommandItem> _run;
    readonly IslandWindow? _island;
    readonly TextBox _input = new();
    readonly TextBlock _hint = new() { IsHitTestVisible = false, Margin = new Thickness(40, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 15 };
    readonly StackPanel _rows = new();
    readonly ScrollViewer _scroll;
    readonly DispatcherTimer _debounce;
    readonly List<CommandItem> _results = [];
    CancellationTokenSource? _cts;
    int _selected, _queryId;
    bool _userMoved; // arrow keys used: keep that choice while late results arrive
    CommandItem? _armed;
    long _armedAt;
    bool _querying, _executeWhenReady;
    readonly DispatcherTimer _confirmationExpiry;

    public CommandBarView(IReadOnlyList<ICommandProvider> providers, Action<CommandItem> run, IslandWindow? island)
    {
        _providers = providers;
        _run = run;
        _island = island;
        SetResourceReference(BackgroundProperty, "BgBrush");
        Margin = new Thickness(0);
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var box = new Grid { Margin = new Thickness(12, 12, 12, 6) };
        _input.SetResourceReference(StyleProperty, "FlatTextBox");
        _input.FontSize = 15;
        _input.MinHeight = 40;
        _input.Padding = new Thickness(19, 0, 8, 0);
        _input.VerticalContentAlignment = VerticalAlignment.Center;
        _input.TextChanged += (_, _) => OnTextChanged();
        _input.PreviewKeyDown += OnKey;
        var icon = new Icon { Width = 17, Height = 17, Margin = new Thickness(12, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        icon.SetResourceReference(Icon.DataProperty, "Icon.Search");
        icon.SetResourceReference(Icon.ForegroundProperty, "FgDimBrush");
        _hint.SetResourceReference(TextBlock.ForegroundProperty, "FgFaintBrush");
        _hint.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("[cmd.placeholder]") { Source = Strings.Instance });
        box.Children.Add(_input);
        box.Children.Add(icon);
        box.Children.Add(_hint);
        Children.Add(box);

        _scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _rows, Margin = new Thickness(8, 0, 8, 8) };
        SetRow(_scroll, 1);
        Children.Add(_scroll);
        _debounce = new DispatcherTimer(TimeSpan.FromMilliseconds(80), DispatcherPriority.Input, (_, _) => { _debounce!.Stop(); Query(); }, Dispatcher);
        _debounce.Stop();
        _confirmationExpiry = new DispatcherTimer(TimeSpan.FromSeconds(3), DispatcherPriority.Input, (_, _) => { _confirmationExpiry!.Stop(); _armed = null; Render(); }, Dispatcher);
        _confirmationExpiry.Stop();
    }

    /// <summary>Re-runs the current query (e.g. the app index finished building while the bar is open).</summary>
    public void Requery()
    {
        if (IsVisible && _input.Text.Trim().Length > 0) Query();
    }

    public string Text { get => _input.Text; set { _input.Text = value; _input.CaretIndex = value.Length; } }

    public void OnShown()
    {
        if (App.IsSnapshot) return;
        _island?.AcquireFocus();
        if (_input.IsVisible) Dispatcher.BeginInvoke(FocusInput, DispatcherPriority.Input);
        else
        {
            DependencyPropertyChangedEventHandler? h = null;
            h = (_, _) => { if (!_input.IsVisible) return; _input.IsVisibleChanged -= h; Dispatcher.BeginInvoke(FocusInput, DispatcherPriority.Input); };
            _input.IsVisibleChanged += h;
        }
    }

    void FocusInput()
    {
        _input.Focus();
        Keyboard.Focus(_input);
        _input.SelectAll();
    }

    public void OnHidden()
    {
        _cts?.Cancel();
        _debounce.Stop();
        _armed = null;
        _confirmationExpiry.Stop();
        _executeWhenReady = false;
        _querying = false;
        _queryId++;
    }

    /// <summary>Clears the input and results (when the bar opens).</summary>
    public void Reset()
    {
        _input.Text = "";
        _results.Clear();
        Render();
    }

    void OnTextChanged()
    {
        _queryId++;
        _cts?.Cancel();
        _executeWhenReady = false;
        _querying = false;
        _confirmationExpiry.Stop();
        _hint.Visibility = _input.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        _armed = null;
        _debounce.Stop();
        if (_input.Text.Trim().Length == 0) { _cts?.Cancel(); _results.Clear(); Render(); return; }
        _debounce.Start();
    }

    /// <summary>Runs every provider in the background; results merge in as each one finishes.</summary>
    public void Query()
    {
        _debounce.Stop();
        _armed = null;
        _confirmationExpiry.Stop();
        _querying = _providers.Count > 0;
        _cts?.Cancel();
        var cts = _cts = new CancellationTokenSource();
        var id = ++_queryId;
        var text = _input.Text;
        _results.Clear();
        _selected = 0;
        _userMoved = false;
        var pending = _providers.Count;
        foreach (var p in _providers)
        {
            Task.Run(() => p.QueryAsync(text, cts.Token), cts.Token).ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    List<CommandItem> items;
                    try { items = t.Result.ToList(); } catch { items = []; }
                    Dispatcher.BeginInvoke(() => Merge(id, items, --pending == 0));
                }
                else
                {
                    if (t.Exception is not null) Logger.Error("command provider", t.Exception.GetBaseException());
                    Dispatcher.BeginInvoke(() => Merge(id, [], --pending == 0));
                }
            }, TaskScheduler.Default);
        }
    }

    void Merge(int id, List<CommandItem> items, bool last)
    {
        if (id != _queryId) return;
        if (items.Count > 0) { _armed = null; _confirmationExpiry.Stop(); }
        var keep = _userMoved && _selected < _results.Count ? _results[_selected] : null;
        _results.AddRange(items);
        _results.Sort((a, b) => b.Score.CompareTo(a.Score));
        if (_results.Count > MaxRows) _results.RemoveRange(MaxRows, _results.Count - MaxRows);
        // the best result stays selected until the user moves; then their choice is kept (if still listed)
        _selected = keep is null ? 0 : Math.Max(0, _results.IndexOf(keep));
        if (items.Count > 0 || last) Render();
        if (last)
        {
            _querying = false;
            if (_executeWhenReady) { _executeWhenReady = false; Execute(); }
        }
    }

    internal int SelectedIndex => _selected;
    internal IReadOnlyList<CommandItem> Results => _results;

    internal void SetResultsForSnapshot(string text, IEnumerable<CommandItem> items)
    {
        _input.Text = text;
        _debounce.Stop();
        _results.Clear();
        _results.AddRange(items.OrderByDescending(i => i.Score).Take(MaxRows));
        _selected = 0;
        Render();
    }

    void Render()
    {
        _rows.Children.Clear();
        for (var i = 0; i < _results.Count; i++) _rows.Children.Add(Row(_results[i], i));
        if (_results.Count == 0 && _input.Text.Trim().Length > 0 && _queryId > 0)
        {
            var t = new TextBlock { Margin = new Thickness(10, 6, 0, 0) };
            t.SetResourceReference(StyleProperty, "TextDim");
            t.Text = Strings.T("cmd.noResults");
            _rows.Children.Add(t);
        }
        if (_selected < _rows.Children.Count && _rows.Children[_selected] is FrameworkElement fe) fe.BringIntoView();
    }

    FrameworkElement Row(CommandItem item, int index)
    {
        var selected = index == _selected;
        var row = new DockPanel();
        FrameworkElement glyph;
        if (item.IconPath is not null && ShellIcons.For(item.IconPath) is { } img)
            glyph = new Image { Source = img, Width = 22, Height = 22 };
        else
        {
            var ic = new Icon { Width = 17, Height = 17 };
            ic.SetResourceReference(Icon.DataProperty, item.IconKey ?? "Icon.Search");
            ic.SetResourceReference(Icon.ForegroundProperty, selected ? "AccentTextBrush" : "FgDimBrush");
            glyph = ic;
        }
        glyph.HorizontalAlignment = HorizontalAlignment.Center;
        glyph.VerticalAlignment = VerticalAlignment.Center;
        var slot = new Grid { Width = 24, Margin = new Thickness(2, 0, 12, 0), Children = { glyph } };
        DockPanel.SetDock(slot, Dock.Left);
        row.Children.Add(slot);
        if (selected)
        {
            var enter = new TextBlock { Text = "↵", FontSize = 13, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 4, 0) };
            enter.SetResourceReference(TextBlock.ForegroundProperty, "FgFaintBrush");
            DockPanel.SetDock(enter, Dock.Right);
            row.Children.Add(enter);
        }
        var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var title = new TextBlock { Text = item.Title, FontSize = 13.5, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
        title.SetResourceReference(TextBlock.ForegroundProperty, "FgBrush");
        texts.Children.Add(title);
        var subText = ReferenceEquals(item, _armed) ? (item.Confirmation.Length > 0 ? item.Confirmation + "\n" : "") + Strings.T("cmd.confirmTimed") : item.Subtitle;
        if (subText.Length > 0)
        {
            var sub = new TextBlock { Text = subText, FontSize = 11, TextWrapping = TextWrapping.Wrap };
            sub.SetResourceReference(TextBlock.ForegroundProperty, ReferenceEquals(item, _armed) ? "DangerBrush" : "FgDimBrush");
            texts.Children.Add(sub);
        }
        row.Children.Add(texts);
        var b = new Border { Child = row, Padding = new Thickness(8, 5, 8, 5), CornerRadius = new CornerRadius(10), Margin = new Thickness(0, 0, 0, 1), Background = Brushes.Transparent, Cursor = Cursors.Hand };
        if (selected) b.SetResourceReference(Border.BackgroundProperty, "SurfaceHoverBrush");
        b.MouseLeftButtonUp += (_, e) => { e.Handled = true; _selected = index; Execute(); };
        return b;
    }

    void OnKey(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                if (_results.Count > 0) { _selected = (_selected + 1) % _results.Count; _userMoved = true; _armed = null; Render(); }
                e.Handled = true;
                break;
            case Key.Up:
                if (_results.Count > 0) { _selected = (_selected - 1 + _results.Count) % _results.Count; _userMoved = true; _armed = null; Render(); }
                e.Handled = true;
                break;
            case Key.Enter:
                e.Handled = true;
                if (e.IsRepeat) return;
                Execute();
                break;
            case Key.Escape:
                e.Handled = true;
                _island?.Collapse();
                break;
        }
    }

    internal void Execute()
    {
        if (_debounce.IsEnabled) { Query(); _executeWhenReady = true; return; }
        if (_querying) { _executeWhenReady = true; return; }
        if (_selected >= _results.Count) return;
        var item = _results[_selected];
        if (item.Run is null) return;
        var now = Environment.TickCount64;
        if (item.Confirm && (!ReferenceEquals(_armed, item) || now - _armedAt >= 3000))
        {
            _armed = item;
            _armedAt = now;
            _confirmationExpiry.Start();
            Render();
            return;
        }
        if (item.Confirm && now - _armedAt < 400) return;
        _armed = null;
        _confirmationExpiry.Stop();
        _run(item);
    }
}
