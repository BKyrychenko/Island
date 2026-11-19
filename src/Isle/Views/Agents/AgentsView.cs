using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using Isle.Core;
using Isle.Modules.Agents;

namespace Isle.Views.Agents;

/// <summary>What the Agents tab can ask its module to do.</summary>
public sealed record AgentsActions(
    Action<AgentRequest> Allow,
    Action<AgentRequest> Deny,
    Action<AgentRequest> Terminal,
    Action<AgentRequest, IReadOnlyList<IReadOnlyCollection<string>>> Answer,
    Action<AgentSession> Dismiss,
    Func<IReadOnlyList<HookTarget>> Targets,
    Func<HookTarget, string> Install,
    Func<HookTarget, string> Remove,
    Func<int> TimeoutSeconds);

/// <summary>
/// Agents tab: one card per session (project, agent, state, last action). Waiting cards show the tool call with
/// Allow · Deny · In terminal, or the agent's question with one button per option. A full-cover overlay installs /
/// removes the hooks (with a preview of exactly what changes before anything is written).
/// </summary>
public sealed class AgentsView : Grid
{
    readonly AgentsActions _a;
    readonly StackPanel _list = new();
    readonly TextBlock _count = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
    readonly Border _overlay = new() { Visibility = Visibility.Collapsed };
    /// <summary>Progress through multi-question requests (survives re-renders): request id → (question index, picks).</summary>
    readonly Dictionary<long, (int Index, List<HashSet<string>> Picks)> _progress = [];
    IReadOnlyList<AgentSession> _sessions = [];
    bool _anyHooks = true;

    public AgentsView(AgentsActions actions)
    {
        _a = actions;
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var head = new DockPanel { Margin = new Thickness(0, -2, 0, 6) };
        var hooks = IconAction("Icon.Plug", "agents.hooksTip", () => ShowHooks());
        DockPanel.SetDock(hooks, Dock.Right);
        head.Children.Add(hooks);
        var title = new TextBlock { Text = Strings.T("tab.agents"), VerticalAlignment = VerticalAlignment.Center };
        title.SetResourceReference(StyleProperty, "TextTitle");
        _count.SetResourceReference(StyleProperty, "TextDim");
        head.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { title, _count } });
        Children.Add(head);

        var scroll = new ScrollViewer { Content = _list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        SetRow(scroll, 1);
        Children.Add(scroll);

        _overlay.SetResourceReference(Border.BackgroundProperty, "BgBrush");
        SetRowSpan(_overlay, 2);
        Children.Add(_overlay);
    }

    public bool OverlayOpen => _overlay.Visibility == Visibility.Visible;

    public void Show(IReadOnlyList<AgentSession> sessions, bool anyHooks)
    {
        _sessions = sessions;
        _anyHooks = anyHooks;
        var live = sessions.SelectMany(s => s.Pending).Select(r => r.Id).ToHashSet();
        foreach (var id in _progress.Keys.Where(k => !live.Contains(k)).ToList()) _progress.Remove(id);

        _count.Text = sessions.Count > 0 ? sessions.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
        _list.Children.Clear();
        if (sessions.Count == 0)
        {
            _list.Children.Add(Wrap(Strings.T(anyHooks ? "agents.empty" : "agents.emptySetup")));
            if (!anyHooks)
            {
                var setup = Small("agents.setup", () => ShowHooks(), accent: true);
                setup.HorizontalAlignment = HorizontalAlignment.Left;
                setup.Margin = new Thickness(0, 8, 0, 0);
                _list.Children.Add(setup);
            }
            return;
        }
        foreach (var s in sessions) _list.Children.Add(Card(s));
    }

    void Refresh() => Show(_sessions, _anyHooks);

    // ---- cards ----

    FrameworkElement Card(AgentSession s)
    {
        var stack = new StackPanel();

        var top = new DockPanel();
        var dot = new Ellipse { Width = 8, Height = 8, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
        dot.SetResourceReference(Shape.FillProperty, StateBrush(s.State));
        DockPanel.SetDock(dot, Dock.Left);
        top.Children.Add(dot);
        if (s.State is AgentState.Done or AgentState.Idle)
        {
            var hide = IconAction("Icon.Close", "agents.dismiss", () => _a.Dismiss(s));
            hide.Width = hide.Height = 20;
            hide.Margin = new Thickness(4, -2, -4, -2);
            DockPanel.SetDock(hide, Dock.Right);
            top.Children.Add(hide);
        }
        var state = new TextBlock { Text = Strings.T("agents.state." + s.State.ToString().ToLowerInvariant()), FontSize = 11.5, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        state.SetResourceReference(TextBlock.ForegroundProperty, s.State == AgentState.Waiting ? "WarnBrush" : "FgDimBrush");
        DockPanel.SetDock(state, Dock.Right);
        top.Children.Add(state);
        var name = new TextBlock { FontSize = 12.5, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        name.Inlines.Add(s.Project.Length > 0 ? s.Project : AgentText.AgentName(s.Agent));
        var agent = new System.Windows.Documents.Run("  " + AgentText.AgentName(s.Agent)) { FontWeight = FontWeights.Normal, FontSize = 11 };
        agent.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "FgDimBrush");
        name.Inlines.Add(agent);
        name.SetResourceReference(TextBlock.ForegroundProperty, "FgBrush");
        top.Children.Add(name);
        stack.Children.Add(top);

        var request = s.Pending.FirstOrDefault();
        if (request is null)
        {
            if (s.LastAction.Length > 0)
            {
                var last = new TextBlock { Text = s.LastAction, FontSize = 11.5, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(16, 2, 0, 0) };
                last.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
                stack.Children.Add(last);
            }
        }
        else if (request.IsQuestion) stack.Children.Add(QuestionBody(request));
        else stack.Children.Add(PermissionBody(request));
        if (s.Pending.Count > 1)
        {
            var more = new TextBlock { Text = Strings.T("agents.more", s.Pending.Count - 1), FontSize = 11, Margin = new Thickness(16, 4, 0, 0) };
            more.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
            stack.Children.Add(more);
        }

        var card = new Border { Child = stack, Margin = new Thickness(0, 0, 0, 6) };
        card.SetResourceReference(StyleProperty, "Card");
        return card;
    }

    FrameworkElement PermissionBody(AgentRequest r)
    {
        var stack = new StackPanel { Margin = new Thickness(16, 4, 0, 0) };
        var what = new TextBlock { Text = Strings.T("agents.wantsTool", r.Tool), FontSize = 11.5 };
        what.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
        stack.Children.Add(what);
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
        if (r.ApprovalInput is { } input)
        {
            var allow = Small("agents.allow", () => _a.Allow(r), accent: true);
            allow.IsDefault = false;
            allow.IsEnabled = false;
            var detail = new TextBlock { Text = input, FontSize = 11.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(7, 3, 7, 3) };
            detail.SetResourceReference(TextBlock.FontFamilyProperty, "MonoFont");
            detail.SetResourceReference(TextBlock.ForegroundProperty, "FgBrush");
            var scroll = new ScrollViewer
            {
                Content = detail, MaxHeight = 44, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Focusable = true,
            };
            System.Windows.Automation.AutomationProperties.SetName(scroll, Strings.T("agents.reviewInput"));
            scroll.ScrollChanged += (_, _) =>
                allow.IsEnabled = scroll.ViewportHeight > 0 && scroll.VerticalOffset >= scroll.ScrollableHeight - 0.5;
            var box = new Border { Child = scroll, CornerRadius = new CornerRadius(6), Margin = new Thickness(0, 3, 0, 0) };
            box.SetResourceReference(Border.BackgroundProperty, "SurfaceHoverBrush");
            stack.Children.Add(box);
            row.Children.Add(allow);
        }
        else
        {
            stack.Children.Add(Wrap(Strings.T("agents.reviewInTerminal")));
        }
        row.Children.Add(Small("agents.deny", () => _a.Deny(r)));
        row.Children.Add(Small("agents.terminal", () => _a.Terminal(r), ghost: true, tipKey: "agents.terminalTip"));
        stack.Children.Add(row);
        return stack;
    }

    FrameworkElement QuestionBody(AgentRequest r)
    {
        if (!_progress.TryGetValue(r.Id, out var p))
        {
            p = (0, [.. r.Questions.Select(_ => new HashSet<string>())]);
            _progress[r.Id] = p;
        }
        var q = r.Questions[Math.Min(p.Index, r.Questions.Count - 1)];
        var stack = new StackPanel { Margin = new Thickness(16, 4, 0, 0) };
        var text = new TextBlock { Text = q.Text, FontSize = 12, TextWrapping = TextWrapping.Wrap };
        text.SetResourceReference(TextBlock.ForegroundProperty, "FgBrush");
        if (r.Questions.Count > 1 || q.Multi)
        {
            var hint = new TextBlock { FontSize = 11, Margin = new Thickness(0, 0, 0, 1),
                Text = string.Join(" · ", new[] { r.Questions.Count > 1 ? Strings.T("agents.question", p.Index + 1, r.Questions.Count) : "", q.Multi ? Strings.T("agents.multi") : "" }.Where(x => x.Length > 0)) };
            hint.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
            stack.Children.Add(hint);
        }
        stack.Children.Add(text);

        var options = new WrapPanel { Margin = new Thickness(0, 5, 0, 0) };
        var picks = p.Picks[p.Index];
        foreach (var (label, description) in q.Options)
        {
            var l = label;
            if (q.Multi)
            {
                var chip = new ToggleButton { Content = label, IsChecked = picks.Contains(label), Margin = new Thickness(0, 0, 6, 4), ToolTip = description.Length > 0 ? description : null };
                chip.SetResourceReference(StyleProperty, "Chip");
                chip.Click += (_, _) => { if (chip.IsChecked == true) picks.Add(l); else picks.Remove(l); };
                options.Children.Add(chip);
            }
            else
            {
                var b = Small(null, () => { picks.Clear(); picks.Add(l); Advance(r); }, text: label);
                b.ToolTip = description.Length > 0 ? description : null;
                b.Margin = new Thickness(0, 0, 6, 4);
                options.Children.Add(b);
            }
        }
        stack.Children.Add(options);
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
        if (q.Multi) row.Children.Add(Small(p.Index + 1 < r.Questions.Count ? "agents.next" : "agents.send", () => { if (picks.Count > 0) Advance(r); }, accent: true));
        row.Children.Add(Small("agents.terminal", () => _a.Terminal(r), ghost: true, tipKey: "agents.terminalTip"));
        stack.Children.Add(row);
        return stack;
    }

    void Advance(AgentRequest r)
    {
        if (!_progress.TryGetValue(r.Id, out var p)) return;
        if (p.Index + 1 < r.Questions.Count)
        {
            _progress[r.Id] = (p.Index + 1, p.Picks);
            Refresh();
            return;
        }
        _progress.Remove(r.Id);
        _a.Answer(r, [.. p.Picks]);
    }

    static string StateBrush(AgentState s) => s switch
    {
        AgentState.Waiting => "WarnBrush",
        AgentState.Working => "AccentTextBrush",
        AgentState.Done => "OkBrush",
        _ => "FgFaintBrush",
    };

    // ---- hooks overlay ----

    public void ShowHooks(string? message = null)
    {
        var stack = new StackPanel();
        foreach (var t in _a.Targets())
        {
            var installed = HookInstaller.IsInstalled(t);
            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            if (installed || t.Detected)
            {
                var b = Small(installed ? "agents.remove" : "agents.install", () => Confirm(t, install: !installed), accent: !installed);
                b.VerticalAlignment = VerticalAlignment.Center;
                DockPanel.SetDock(b, Dock.Right);
                row.Children.Add(b);
                if (installed)
                {
                    var re = Small("agents.reinstall", () => Confirm(t, install: true), ghost: true);
                    re.VerticalAlignment = VerticalAlignment.Center;
                    DockPanel.SetDock(re, Dock.Right);
                    row.Children.Add(re);
                }
            }
            var info = new StackPanel();
            var name = new TextBlock { Text = t.Name, FontSize = 12.5, FontWeight = FontWeights.SemiBold };
            name.SetResourceReference(TextBlock.ForegroundProperty, "FgBrush");
            info.Children.Add(name);
            var status = new TextBlock { FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis,
                Text = Strings.T(installed ? "agents.installed" : t.Detected ? "agents.notInstalled" : "agents.notFound") + " · " + t.ConfigPath };
            status.SetResourceReference(TextBlock.ForegroundProperty, installed ? "OkBrush" : "FgDimBrush");
            info.Children.Add(status);
            row.Children.Add(info);
            var card = new Border { Child = row, Margin = new Thickness(0, 0, 0, 6), Padding = new Thickness(12, 7, 10, 3) };
            card.SetResourceReference(StyleProperty, "Card");
            stack.Children.Add(card);
        }
        stack.Children.Add(Wrap(message ?? Strings.T("agents.timeoutHint", _a.TimeoutSeconds()), message is null ? "FgDimBrush" : "FgBrush"));
        ShowOverlay(Strings.T("agents.hooks"), stack);
    }

    internal void Confirm(HookTarget t, bool install)
    {
        var plan = HookInstaller.Plan(t, DateTime.Now);
        var lines = new List<string>();
        if (plan.FileExists) lines.Add(Strings.T("agents.planBackup", plan.BackupPath!));
        else if (install) lines.Add(Strings.T("agents.planNew", t.ConfigPath));
        if (install)
        {
            lines.Add(Strings.T("agents.planCopy", plan.HookExe));
            lines.Add(Strings.T("agents.planEvents", string.Join(", ", plan.Events)));
            lines.Add(Strings.T(plan.AlreadyInstalled ? "agents.planReplace" : "agents.planKeep"));
        }
        else lines.Add(Strings.T("agents.planRemove"));
        lines.Add(Strings.T("agents.planRestart"));

        var stack = new StackPanel();
        foreach (var l in lines)
        {
            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 3) };
            var bullet = Wrap("•");
            bullet.Width = 14;
            DockPanel.SetDock(bullet, Dock.Left);
            row.Children.Add(bullet);
            var tb = Wrap(l);
            tb.FontSize = 11.5;
            row.Children.Add(tb);
            stack.Children.Add(row);
        }
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        buttons.Children.Add(Small("agents.cancel", () => ShowHooks()));
        buttons.Children.Add(Small("agents.confirm", () => ShowHooks(install ? _a.Install(t) : _a.Remove(t)), accent: true));
        ShowOverlay(Strings.T(install ? "agents.confirmInstall" : "agents.confirmRemove", t.Name),
            new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }, buttons, closable: false);
    }

    void ShowOverlay(string title, FrameworkElement body, FrameworkElement? extra = null, bool closable = true)
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var head = new DockPanel { Margin = new Thickness(0, -2, 0, 6) };
        if (closable)
        {
            var close = IconAction("Icon.Close", "ai.close", HideOverlay);
            DockPanel.SetDock(close, Dock.Right);
            head.Children.Add(close);
        }
        if (extra is not null) { DockPanel.SetDock(extra, Dock.Right); head.Children.Add(extra); }
        var t = new TextBlock { Text = title, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
        t.SetResourceReference(StyleProperty, "TextTitle");
        head.Children.Add(t);
        grid.Children.Add(head);
        SetRow(body, 1);
        grid.Children.Add(body);
        _overlay.Child = grid;
        _overlay.Visibility = Visibility.Visible;
    }

    public void HideOverlay()
    {
        _overlay.Visibility = Visibility.Collapsed;
        _overlay.Child = null;
    }

    // ---- small helpers ----

    static TextBlock Wrap(string text, string brush = "FgDimBrush")
    {
        var t = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 12 };
        t.SetResourceReference(TextBlock.ForegroundProperty, brush);
        return t;
    }

    static Button Small(string? key, Action click, bool accent = false, bool ghost = false, string? tipKey = null, string? text = null)
    {
        var b = new Button { Content = text ?? Strings.T(key!), Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(12, 3, 12, 4), FontSize = 12 };
        if (tipKey is not null) b.ToolTip = Strings.T(tipKey);
        b.SetResourceReference(StyleProperty, accent ? "AccentButton" : ghost ? "GhostButton" : "FlatButton");
        b.Click += (_, _) => { try { click(); } catch (Exception ex) { Logger.Error("agents action", ex); } };
        return b;
    }

    static Button IconAction(string icon, string tipKey, Action click)
    {
        var i = new Icon { Width = 15, Height = 15 };
        i.SetResourceReference(Icon.DataProperty, icon);
        i.SetResourceReference(Icon.ForegroundProperty, "FgBrush");
        var b = new Button { Width = 28, Height = 28, ToolTip = Strings.T(tipKey), Content = i };
        System.Windows.Automation.AutomationProperties.SetName(b, Strings.T(tipKey));
        b.SetResourceReference(StyleProperty, "IconButton");
        b.Click += (_, _) => { try { click(); } catch (Exception ex) { Logger.Error("agents action", ex); } };
        return b;
    }
}
