using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Isle.Core;

namespace Isle.Views;

/// <summary>
/// Icon tab strip with a sliding indicator. The selected tab also shows its label. Tabs that do not fit go to
/// the "more" button (the island shows them in an overlay list). A gear button on the right opens settings.
/// </summary>
public sealed class TabBar : Grid
{
    const double TabWidth = 34, LabelRoom = 118, SideButton = 32;

    readonly StackPanel _strip = new() { Orientation = Orientation.Horizontal };
    readonly Border _indicator = new() { Height = 28, CornerRadius = new CornerRadius(14), HorizontalAlignment = HorizontalAlignment.Left, Width = 0 };
    readonly TranslateTransform _indicatorX = new();
    readonly Button _more, _gear;
    readonly Dictionary<string, RadioButton> _buttons = [];
    List<TabInfo> _tabs = [];
    List<TabInfo> _hidden = [];
    string? _selected;
    bool _animateNext;

    public event Action<string>? TabSelected;
    public event Action<IReadOnlyList<TabInfo>>? MoreRequested;
    public event Action? SettingsRequested;

    public string? SelectedId => _selected;
    /// <summary>Tabs that did not fit and live in the "more" list.</summary>
    public IReadOnlyList<TabInfo> Hidden => _hidden;
    public double AvailableWidth { get; set; } = 496;

    public TabBar()
    {
        Height = 28;
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _indicator.SetResourceReference(Border.BackgroundProperty, "SurfaceBrush");
        _indicator.RenderTransform = _indicatorX;
        var host = new Grid();
        host.Children.Add(_indicator);
        host.Children.Add(_strip);
        Children.Add(host);

        _more = SideIcon("Icon.More", "tabs.more");
        _more.Click += (_, _) => MoreRequested?.Invoke(_hidden);
        _more.Visibility = Visibility.Collapsed;
        SetColumn(_more, 1);
        Children.Add(_more);

        _gear = SideIcon("Icon.Settings", "tray.settings");
        _gear.Click += (_, _) => SettingsRequested?.Invoke();
        SetColumn(_gear, 2);
        Children.Add(_gear);

        Strings.LanguageChanged += Rebuild;
        Unloaded += (_, _) => Strings.LanguageChanged -= Rebuild;
        Loaded += (_, _) => { Strings.LanguageChanged -= Rebuild; Strings.LanguageChanged += Rebuild; };
        _strip.LayoutUpdated += (_, _) => MoveIndicator();
    }

    Button SideIcon(string iconKey, string tipKey)
    {
        var b = new Button { Margin = new Thickness(4, 0, 0, 0) };
        b.SetResourceReference(StyleProperty, "IconButton");
        var icon = new Icon { Width = 16, Height = 16 };
        icon.SetResourceReference(Icon.DataProperty, iconKey);
        icon.SetResourceReference(Icon.ForegroundProperty, "FgDimBrush");
        b.Content = icon;
        b.SetBinding(ToolTipProperty, new System.Windows.Data.Binding($"[{tipKey}]") { Source = Strings.Instance });
        return b;
    }

    public void SetTabs(IEnumerable<TabInfo> tabs, string? selected)
    {
        _tabs = [.. tabs.OrderBy(t => t.Order)];
        _selected = selected is not null && _tabs.Any(t => t.Id == selected) ? selected : _tabs.FirstOrDefault()?.Id;
        Rebuild();
    }

    public void Select(string id, bool animate = true)
    {
        if (_tabs.All(t => t.Id != id)) return;
        var wasHidden = _hidden.Any(t => t.Id == id);
        _selected = id;
        _animateNext = animate && !wasHidden;
        if (wasHidden) Rebuild();
        else UpdateButtons();
    }

    void Rebuild()
    {
        _strip.Children.Clear();
        _buttons.Clear();

        // how many fit?
        var avail = AvailableWidth - SideButton;
        var visible = _tabs;
        _hidden = [];
        if (_tabs.Count * TabWidth + LabelRoom > avail)
        {
            var n = Math.Max(1, (int)((avail - SideButton - LabelRoom) / TabWidth));
            visible = [.. _tabs.Take(n)];
            _hidden = [.. _tabs.Skip(n)];
            var sel = _hidden.FirstOrDefault(t => t.Id == _selected);
            if (sel is not null)
            {
                // the selected tab takes the last visible slot
                _hidden.Remove(sel);
                _hidden.Insert(0, visible[^1]);
                visible[^1] = sel;
            }
        }
        _more.Visibility = _hidden.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (var tab in visible)
        {
            var icon = new Icon { Width = 16, Height = 16 };
            icon.SetResourceReference(Icon.DataProperty, tab.IconKey);
            var label = new TextBlock
            {
                Text = Strings.T(tab.TitleKey), FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(6, 0, 1, 0),
                VerticalAlignment = VerticalAlignment.Center, MaxWidth = LabelRoom - 28, TextTrimming = TextTrimming.CharacterEllipsis,
            };
            var content = new StackPanel { Orientation = Orientation.Horizontal };
            content.Children.Add(icon);
            content.Children.Add(label);
            var rb = new RadioButton { Content = content, Tag = label, GroupName = "IsleTabs" + GetHashCode(), ToolTip = Strings.T(tab.TitleKey) };
            rb.SetResourceReference(StyleProperty, "TabButton");
            var id = tab.Id;
            rb.Click += (_, _) =>
            {
                if (_selected == id) return;
                _selected = id;
                _animateNext = true;
                UpdateButtons();
                TabSelected?.Invoke(id);
            };
            _buttons[tab.Id] = rb;
            _strip.Children.Add(rb);
        }
        _animateNext = false;
        UpdateButtons();
    }

    void UpdateButtons()
    {
        foreach (var (id, rb) in _buttons)
        {
            var on = id == _selected;
            rb.IsChecked = on;
            ((TextBlock)rb.Tag).Visibility = on ? Visibility.Visible : Visibility.Collapsed;
            ToolTipService.SetIsEnabled(rb, !on);
        }
        _indicatorDirty = true;
        _strip.InvalidateArrange();
    }

    bool _indicatorDirty;

    void MoveIndicator()
    {
        if (!_indicatorDirty) return;
        if (_selected is null || !_buttons.TryGetValue(_selected, out var rb))
        {
            _indicatorDirty = false;
            _indicator.BeginAnimation(WidthProperty, null);
            _indicator.Width = 0;
            return;
        }
        if (rb.ActualWidth <= 0) return; // not laid out yet
        _indicatorDirty = false;
        var x = rb.TranslatePoint(new Point(0, 0), _strip).X;
        var w = rb.ActualWidth;
        if (_animateNext && _indicator.Width > 0)
        {
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var d = TimeSpan.FromMilliseconds(220);
            _indicatorX.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(x, d) { EasingFunction = ease });
            _indicator.BeginAnimation(WidthProperty, new DoubleAnimation(w, d) { EasingFunction = ease });
        }
        else
        {
            _indicatorX.BeginAnimation(TranslateTransform.XProperty, null);
            _indicator.BeginAnimation(WidthProperty, null);
            _indicatorX.X = x;
            _indicator.Width = w;
        }
        _animateNext = false;
    }
}
