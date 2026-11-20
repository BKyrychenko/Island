using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Isle.Core;
using Isle.Modules.Reminders;
using Isle.Modules.Timers;

namespace Isle.Views.Timers;

/// <summary>
/// Timers &amp; Reminders tab: one input (a duration starts a timer, a phrase sets a reminder), preset chips, a stopwatch
/// button, and the list of timers, the stopwatch and reminders. Ticks once a second only while shown and something runs.
/// </summary>
public sealed class TimersView : Grid, ITabView, IDisposable
{
    static readonly int[] Presets = [5, 10, 15, 25, 45];

    readonly TimersController _ctl;
    readonly TextBox _input = new();
    readonly TextBlock _placeholder = new() { IsHitTestVisible = false, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
    readonly TextBlock _hint = new() { Visibility = Visibility.Collapsed, Margin = new Thickness(2, 4, 0, 0), FontSize = 11 };
    readonly StackPanel _list = new();
    readonly List<(Button Button, int Minutes)> _presets = [];
    readonly StackPanel _empty = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    readonly List<Action> _tickers = [];
    DispatcherTimer? _timer;
    bool _shown;
    string? _editing;

    public TimersView(TimersController ctl)
    {
        _ctl = ctl;
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // input row
        var top = new DockPanel { LastChildFill = true };
        var sw = new Button { Width = 30, Height = 30, Margin = new Thickness(4, 0, 0, 0) };
        sw.SetResourceReference(StyleProperty, "IconButton");
        sw.Content = MakeIcon("Icon.Stopwatch", 16, "FgDimBrush");
        sw.SetBinding(ToolTipProperty, Loc("timers.stopwatch"));
        sw.Click += (_, _) => { if (!_ctl.Timers.Stopwatch.Active) _ctl.StopwatchToggle(); };
        DockPanel.SetDock(sw, Dock.Right);
        top.Children.Add(sw);
        var submit = new Button { MinHeight = 30, Margin = new Thickness(4, 0, 0, 0), Padding = new Thickness(8, 0, 8, 0) };
        submit.SetResourceReference(StyleProperty, "FlatButton");
        submit.SetBinding(ContentControl.ContentProperty, Loc("timers.submit"));
        submit.Click += (_, _) => Submit();
        DockPanel.SetDock(submit, Dock.Right);
        top.Children.Add(submit);
        var presets = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
        foreach (var m in Presets)
        {
            var b = new Button { Content = $"{m} {Strings.T("unit.min")}", Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(8, 0, 8, 0), MinHeight = 30 };
            b.SetResourceReference(StyleProperty, "FlatButton");
            b.ToolTip = $"{m} {Strings.T("unit.min")}";
            var minutes = m;
            b.Click += (_, _) => { if (!_ctl.AddTimer(TimeSpan.FromMinutes(minutes))) ShowHint(Strings.T(_ctl.LastSaveFailed ? "timers.saveFailed" : "timers.full")); };
            _presets.Add((b, minutes));
            presets.Children.Add(b);
        }
        var inputHost = new Grid();
        _input.SetResourceReference(StyleProperty, "FlatTextBox");
        _input.MinHeight = 30;
        _input.KeyDown += OnInputKey;
        _input.TextChanged += (_, _) => { _placeholder.Visibility = _input.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed; _hint.Visibility = Visibility.Collapsed; };
        _placeholder.SetResourceReference(TextBlock.ForegroundProperty, "FgFaintBrush");
        _placeholder.SetBinding(TextBlock.TextProperty, Loc("timers.placeholder"));
        _placeholder.TextTrimming = TextTrimming.CharacterEllipsis;
        inputHost.Children.Add(_input);
        inputHost.Children.Add(_placeholder);
        top.Children.Add(inputHost);
        Children.Add(top);

        _hint.SetResourceReference(TextBlock.ForegroundProperty, "WarnBrush");
        var feedback = new StackPanel();
        feedback.Children.Add(presets);
        feedback.Children.Add(_hint);
        SetRow(feedback, 1);
        Children.Add(feedback);

        // list
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 6, 0, 0), Content = _list };
        SetRow(scroll, 2);
        Children.Add(scroll);
        var emptyIcon = MakeIcon("Icon.Timer", 22, "FgFaintBrush");
        emptyIcon.Margin = new Thickness(0, 0, 0, 6);
        emptyIcon.HorizontalAlignment = HorizontalAlignment.Center;
        var emptyText = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center };
        emptyText.SetResourceReference(StyleProperty, "TextDim");
        emptyText.SetBinding(TextBlock.TextProperty, Loc("timers.empty"));
        _empty.Children.Add(emptyIcon);
        _empty.Children.Add(emptyText);
        SetRow(_empty, 2);
        Children.Add(_empty);

        _ctl.Changed += Rebuild;
        Strings.LanguageChanged += Rebuild;
        Rebuild();
    }

    static System.Windows.Data.Binding Loc(string key) => new($"[{key}]") { Source = Strings.Instance };

    static Icon MakeIcon(string key, double size, string brush, bool filled = false)
    {
        var i = new Icon { Width = size, Height = size, Filled = filled };
        i.SetResourceReference(Icon.DataProperty, key);
        i.SetResourceReference(Icon.ForegroundProperty, brush);
        return i;
    }

    void OnInputKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && (_input.Text.Length > 0 || _editing is not null))
        {
            _input.Clear();
            _editing = null;
            e.Handled = true;
            return;
        }
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        Submit();
    }

    void Submit()
    {
        var result = _ctl.Submit(_input.Text, _editing);
        switch (result)
        {
            case TimersController.SubmitResult.Invalid:
                ShowHint(Strings.T("timers.invalid"));
                return;
            case TimersController.SubmitResult.SaveFailed:
                ShowHint(Strings.T("timers.saveFailed"));
                return;
            case TimersController.SubmitResult.Full:
                ShowHint(Strings.T("timers.full"));
                return;
        }
        _input.Clear();
        _editing = null;
    }

    void ShowHint(string text)
    {
        _hint.Text = text;
        _hint.Visibility = Visibility.Visible;
    }

    /// <summary>Puts text into the input with the caret at the end (command bar / edit).</summary>
    public void Prefill(string text, string? editReminderId = null)
    {
        _editing = editReminderId;
        _input.Text = text;
        _input.CaretIndex = text.Length;
        App.Island?.AcquireFocus();
        _input.Focus();
    }

    internal void ShowInvalidForSnapshot(string text)
    {
        _input.Text = text;
        ShowHint(Strings.T("timers.invalid"));
    }

    // ---- list ----

    void Rebuild()
    {
        foreach (var (button, minutes) in _presets)
            button.Content = button.ToolTip = $"{minutes} {Strings.T("unit.min")}";
        if (_ctl.LastSaveFailed) ShowHint(Strings.T("timers.saveFailed"));
        _list.Children.Clear();
        _tickers.Clear();
        var now = _ctl.Clock();

        foreach (var t in _ctl.Timers.Timers.OrderBy(t => t.FinishedAt is null).ThenBy(t => t.Remaining(now)))
            _list.Children.Add(TimerRow(t));
        if (_ctl.Timers.Stopwatch.Active) _list.Children.Add(StopwatchRow());
        foreach (var r in _ctl.Reminders.Upcoming)
            _list.Children.Add(ReminderRow(r));

        _empty.Visibility = _list.Children.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateTimer();
    }

    Grid Row(FrameworkElement lead, string title, out TextBlock subtitle, params Button[] actions)
    {
        var row = new Grid { Height = 40, Margin = new Thickness(0, 0, 0, 2), Background = Brushes.Transparent };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var bg = new Border { CornerRadius = new CornerRadius(10) };
        bg.SetResourceReference(Border.BackgroundProperty, "SurfaceBrush");
        SetColumnSpan(bg, 3);
        row.Children.Add(bg);
        lead.HorizontalAlignment = HorizontalAlignment.Center;
        lead.VerticalAlignment = VerticalAlignment.Center;
        row.Children.Add(lead);

        var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var t = new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
        t.Typography.NumeralAlignment = FontNumeralAlignment.Tabular;
        subtitle = new TextBlock { FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis };
        subtitle.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
        texts.Children.Add(t);
        texts.Children.Add(subtitle);
        SetColumn(texts, 1);
        row.Children.Add(texts);

        var acts = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) };
        foreach (var a in actions) acts.Children.Add(a);
        SetColumn(acts, 2);
        row.Children.Add(acts);
        return row;
    }

    static Button Action(string icon, string tip, Action click, bool filled = false)
    {
        var b = new Button { Width = 28, Height = 28 };
        b.SetResourceReference(StyleProperty, "IconButton");
        b.Content = MakeIcon(icon, filled ? 12 : 14, "FgDimBrush", filled);
        b.SetBinding(ToolTipProperty, Loc(tip));
        b.Click += (_, _) => { try { click(); } catch (Exception ex) { Logger.Error("timers action", ex); } };
        return b;
    }

    FrameworkElement TimerRow(TimerItem t)
    {
        var ring = new Ring(20);
        var id = t.Id;
        Grid row;
        TextBlock sub;
        if (t.FinishedAt is not null)
        {
            row = Row(ring, Strings.T("timers.timesUp"), out sub,
                Action("Icon.Plus", "timers.plus1", () => _ctl.AddMinute(id)),
                Action("Icon.Check", "timers.dismiss", () => _ctl.Dismiss(id)));
            ring.Progress = 1;
            sub.Text = Describe(t);
            return row;
        }
        row = Row(ring, "", out sub,
            t.IsPaused ? Action("Icon.Play", "timers.resume", () => _ctl.Resume(id), filled: true) : Action("Icon.Pause", "timers.pause", () => _ctl.Pause(id), filled: true),
            Action("Icon.Close", "timers.cancel", () => _ctl.Cancel(id)));
        var title = (TextBlock)((StackPanel)row.Children[2]).Children[0];
        void Tick()
        {
            var now = _ctl.Clock();
            title.Text = DurationParser.Format(t.Remaining(now));
            ring.Progress = t.Progress(now);
        }
        Tick();
        sub.Text = Describe(t) + (t.IsPaused ? " · " + Strings.T("timers.paused") : "");
        if (t.IsRunning) _tickers.Add(Tick);
        return row;
    }

    static string Describe(TimerItem t) =>
        t.Label.Length > 0 ? $"{t.Label} · {DurationParser.Describe(t.Duration, TimerDoneBanner.UnitName)}" : DurationParser.Describe(t.Duration, TimerDoneBanner.UnitName);

    FrameworkElement StopwatchRow()
    {
        var s = _ctl.Timers.Stopwatch;
        var row = Row(MakeIcon("Icon.Stopwatch", 18, "AccentTextBrush"), "", out var sub,
            s.IsRunning ? Action("Icon.Pause", "timers.pause", _ctl.StopwatchToggle, filled: true) : Action("Icon.Play", "timers.resume", _ctl.StopwatchToggle, filled: true),
            Action("Icon.Reset", "timers.reset", _ctl.StopwatchReset));
        var title = (TextBlock)((StackPanel)row.Children[2]).Children[0];
        void Tick() => title.Text = DurationParser.Format(s.Elapsed(_ctl.Clock()) - TimeSpan.FromSeconds(0.999));
        Tick();
        sub.Text = Strings.T("timers.stopwatch") + (s.IsRunning ? "" : " · " + Strings.T("timers.paused"));
        if (s.IsRunning) _tickers.Add(Tick);
        return row;
    }

    FrameworkElement ReminderRow(Reminder r)
    {
        var id = r.Id;
        var row = Row(MakeIcon(r.Repeat is null ? "Icon.Bell" : "Icon.Repeat", 16, r.FiredAt is null ? "FgDimBrush" : "AccentTextBrush"),
            r.Text.Length > 0 ? r.Text : Strings.T("rem.title"), out var sub,
            r.FiredAt is not null ? Action("Icon.Check", "rem.done", () => _ctl.ReminderDone(id)) : Action("Icon.Edit", "rem.edit", () => Prefill(r.Source, id)),
            Action("Icon.Trash", "rem.delete", () => _ctl.DeleteReminder(id)));
        var repeat = TimersController.FormatRepeat(r.Repeat);
        sub.Text = _ctl.FormatDue(r.Due) + (repeat.Length > 0 ? " · " + repeat : "");
        return row;
    }

    // ---- ticking ----

    void UpdateTimer()
    {
        var needed = _shown && _tickers.Count > 0;
        if (!needed) { _timer?.Stop(); return; }
        _timer ??= new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => { foreach (var t in _tickers) t(); }, Dispatcher);
        _timer.Start();
    }

    public void OnShown()
    {
        _shown = true;
        foreach (var t in _tickers) t();
        UpdateTimer();
    }

    public void OnHidden()
    {
        _shown = false;
        UpdateTimer();
        _hint.Visibility = Visibility.Collapsed;
    }

    public void Dispose()
    {
        _timer?.Stop();
        _ctl.Changed -= Rebuild;
        Strings.LanguageChanged -= Rebuild;
    }
}
