using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Isle.Core;
using Isle.Modules.Weather;

namespace Isle.Views.Weather;

/// <summary>Callbacks from the Weather tab to its module.</summary>
public sealed record WeatherActions(
    Func<string, Task<List<Place>>> Search, Action<Place> Pick, Action UseIp, Action Refresh,
    Action<bool> SetFahrenheit, Action<bool> SetInPill, Action<bool> SetRainAlerts);

/// <summary>
/// Weather tab. Report: current conditions on top, then a 24-hour strip or the 7-day forecast (chips switch).
/// Setup: city search (Open-Meteo geocoding), approximate-location chip, unit / pill / rain-alert chips.
/// </summary>
public sealed class WeatherView : Grid
{
    readonly WeatherActions _a;
    readonly ContentControl _body = new();
    WeatherReport? _report;
    bool _failed, _fahrenheit, _days;

    public WeatherView(WeatherActions actions)
    {
        _a = actions;
        Children.Add(_body);
    }

    // ---- report ----

    public void ShowReport(WeatherReport r, bool failed, bool fahrenheit)
    {
        _report = r;
        _failed = failed;
        _fahrenheit = fahrenheit;
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var c = r.Current;
        var head = new DockPanel { Margin = new Thickness(2, 0, 0, 8), LastChildFill = true };
        var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top };
        right.Children.Add(Radio("wx.hours", !_days, () => { _days = false; ShowReport(r, _failed, _fahrenheit); }));
        right.Children.Add(Radio("wx.days", _days, () => { _days = true; ShowReport(r, _failed, _fahrenheit); }));
        right.Children.Add(IconAction("Icon.Edit", "wx.change", () => ShowSetup(Settings.Current.OptBool("weather.ip"))));
        DockPanel.SetDock(right, Dock.Right);
        head.Children.Add(right);

        var icon = WxIcon(WeatherCodes.IconKey(c.Code, c.IsDay), 34);
        icon.Margin = new Thickness(0, 0, 10, 0);
        head.Children.Add(icon);
        var temp = new TextBlock { Text = WeatherFormat.Temp(c.Temp, fahrenheit), FontSize = 30, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, -4, 12, 0), VerticalAlignment = VerticalAlignment.Center };
        temp.SetResourceReference(TextBlock.FontFamilyProperty, "UiFontDisplay");
        head.Children.Add(temp);
        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var cond = new TextBlock { Text = Strings.T(WeatherCodes.TextKey(c.Code)) + " · " + r.Place };
        cond.SetResourceReference(StyleProperty, "TextTitle");
        cond.FontSize = 13;
        info.Children.Add(cond);
        var details = Strings.T("wx.feels", WeatherFormat.Temp(c.Feels, fahrenheit)) + " · " + c.Humidity + "% · " + WeatherFormat.Wind(c.Wind, fahrenheit);
        info.Children.Add(Dim(details));
        var updated = Dim((failed ? Strings.T("wx.offline") + " · " : "") + Strings.T("wx.updated", r.Fetched.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture)));
        updated.FontSize = 10.5;
        if (failed) updated.SetResourceReference(TextBlock.ForegroundProperty, "WarnBrush");
        info.Children.Add(updated);
        head.Children.Add(info);
        grid.Children.Add(head);

        FrameworkElement body = _days ? Days(r) : Hours(r);
        SetRow(body, 1);
        grid.Children.Add(body);
        _body.Content = grid;
    }

    FrameworkElement Hours(WeatherReport r)
    {
        var strip = new StackPanel { Orientation = Orientation.Horizontal };
        var nowS = DateTimeOffset.Now.ToUnixTimeSeconds();
        var hours = r.Hours.Where(h => h.Time > nowS - 3600).Take(24).ToList();
        if (hours.Count == 0) hours = [.. r.Hours.Take(24)];
        for (var i = 0; i < hours.Count; i++)
        {
            var h = hours[i];
            var label = i == 0 && h.Time <= nowS ? Strings.T("wx.now") : r.Local(h.Time).ToString("HH", CultureInfo.InvariantCulture);
            strip.Children.Add(Column(label, WeatherCodes.IconKey(h.Code, h.IsDay), WeatherFormat.Temp(h.Temp, _fahrenheit), null, h.PrecipProb, 40, i == 0));
        }
        var scroll = new ScrollViewer
        {
            Content = strip, HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        scroll.PreviewMouseWheel += (_, e) => { scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset - e.Delta / 2.0); e.Handled = true; };
        return scroll;
    }

    FrameworkElement Days(WeatherReport r)
    {
        var grid = new UniformGrid { Rows = 1 };
        var culture = CultureInfo.GetCultureInfo(Strings.Language == "uk" ? "uk-UA" : "en-GB");
        var today = r.Local(DateTimeOffset.Now.ToUnixTimeSeconds()).Date;
        foreach (var d in r.Days.Take(7))
        {
            var date = r.Local(d.Time).Date;
            var label = date == today ? Strings.T("wx.today") : culture.TextInfo.ToTitleCase(date.ToString("ddd", culture));
            grid.Children.Add(Column(label, WeatherCodes.IconKey(d.Code, true), WeatherFormat.Temp(d.Max, _fahrenheit), WeatherFormat.Temp(d.Min, _fahrenheit), d.PrecipProb, double.NaN, date == today));
        }
        return grid;
    }

    static FrameworkElement Column(string label, string icon, string temp, string? low, int precip, double width, bool highlight)
    {
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var l = Dim(label);
        l.HorizontalAlignment = HorizontalAlignment.Center;
        if (highlight) { l.SetResourceReference(TextBlock.ForegroundProperty, "FgBrush"); l.FontWeight = FontWeights.SemiBold; }
        stack.Children.Add(l);
        var i = WxIcon(icon, 20);
        i.Margin = new Thickness(0, 6, 0, 6);
        i.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Children.Add(i);
        var t = new TextBlock { Text = temp, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, FontSize = 13 };
        stack.Children.Add(t);
        if (low is not null)
        {
            var lo = Dim(low);
            lo.HorizontalAlignment = HorizontalAlignment.Center;
            stack.Children.Add(lo);
        }
        var p = Dim(precip >= 20 ? precip + "%" : " ");
        p.FontSize = 10.5;
        p.HorizontalAlignment = HorizontalAlignment.Center;
        p.SetResourceReference(TextBlock.ForegroundProperty, "AccentBrush");
        stack.Children.Add(p);
        var b = new Border { Child = stack, Width = width, CornerRadius = new CornerRadius(10), Padding = new Thickness(0, 6, 0, 4), Margin = new Thickness(0, 0, 2, 0) };
        if (highlight) b.SetResourceReference(Border.BackgroundProperty, "SurfaceBrush");
        return b;
    }

    // ---- other states ----

    public void ShowLoading() => _body.Content = Centered(Strings.T("wx.loading"), null);

    public void ShowError() => _body.Content = Centered(Strings.T("wx.error"), ("wx.retry", _a.Refresh));

    FrameworkElement Centered(string text, (string Key, Action Click)? action)
    {
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var i = WxIcon("Wx.Cloud", 24, "FgFaintBrush");
        i.HorizontalAlignment = HorizontalAlignment.Center;
        i.Margin = new Thickness(0, 0, 0, 6);
        stack.Children.Add(i);
        var t = Dim(text);
        t.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Children.Add(t);
        if (action is { } a)
        {
            var b = Small(a.Key, a.Click);
            b.Margin = new Thickness(0, 8, 0, 0);
            b.HorizontalAlignment = HorizontalAlignment.Center;
            stack.Children.Add(b);
        }
        return stack;
    }

    /// <summary>Place search and options. With a report already shown, a Back button returns to it.</summary>
    public void ShowSetup(bool ipOn)
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var top = new DockPanel { LastChildFill = true };
        var results = new StackPanel();
        var status = Dim(Strings.T(_report is null ? "wx.setupHint" : "wx.current", Settings.Current.Opt("weather.city")));
        status.TextWrapping = TextWrapping.Wrap;
        status.Margin = new Thickness(2, 6, 0, 0);
        var box = new TextBox { MinHeight = 30 };
        box.SetResourceReference(StyleProperty, "FlatTextBox");
        var placeholder = new TextBlock { Text = Strings.T("wx.cityPlaceholder"), IsHitTestVisible = false, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        placeholder.SetResourceReference(TextBlock.ForegroundProperty, "FgFaintBrush");
        box.TextChanged += (_, _) => placeholder.Visibility = box.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

        async void DoSearch()
        {
            var q = box.Text.Trim();
            if (q.Length < 2) return;
            status.Text = Strings.T("wx.searching");
            results.Children.Clear();
            var found = await _a.Search(q);
            ShowResults(results, status, found);
        }
        box.KeyDown += (_, e) => { if (e.Key == Key.Enter) { e.Handled = true; DoSearch(); } };
        if (_report is not null)
        {
            var back = IconAction("Icon.ChevronLeft", "wx.back", () => ShowReport(_report, _failed, _fahrenheit));
            back.Margin = new Thickness(0, 0, 4, 0);
            DockPanel.SetDock(back, Dock.Left);
            top.Children.Add(back);
        }
        var search = Small("wx.search", DoSearch);
        search.Margin = new Thickness(6, 0, 0, 0);
        DockPanel.SetDock(search, Dock.Right);
        top.Children.Add(search);
        top.Children.Add(new Grid { Children = { box, placeholder } });
        grid.Children.Add(top);

        var chips = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
        var ip = Toggle("wx.useIp", ipOn, on => { if (on) _a.UseIp(); });
        chips.Children.Add(ip);
        chips.Children.Add(Toggle("wx.fahrenheit", Settings.Current.Opt("weather.units") == "F", _a.SetFahrenheit));
        chips.Children.Add(Toggle("wx.inPill", Settings.Current.OptBool("weather.inPill", true), _a.SetInPill));
        chips.Children.Add(Toggle("wx.rainAlerts", Settings.Current.OptBool("weather.rainAlerts", true), _a.SetRainAlerts));
        SetRow(chips, 1);
        grid.Children.Add(chips);

        var lower = new StackPanel { Children = { status, results } };
        var scroll = new ScrollViewer { Content = lower, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        SetRow(scroll, 2);
        grid.Children.Add(scroll);
        _body.Content = grid;
    }

    void ShowResults(StackPanel results, TextBlock status, List<Place> found)
    {
        results.Children.Clear();
        status.Text = Strings.T(found.Count == 0 ? "wx.noResults" : "wx.pick");
        foreach (var p in found.Take(5))
        {
            var row = new Button { HorizontalContentAlignment = HorizontalAlignment.Stretch, Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(0, 2, 0, 0) };
            row.SetResourceReference(StyleProperty, "GhostButton");
            var dock = new DockPanel();
            var d = Dim(p.Detail);
            d.Margin = new Thickness(8, 0, 0, 0);
            DockPanel.SetDock(d, Dock.Right);
            dock.Children.Add(d);
            var n = new TextBlock { Text = p.Name };
            n.SetResourceReference(StyleProperty, "Text");
            dock.Children.Add(n);
            row.Content = dock;
            var place = p;
            row.Click += (_, _) => _a.Pick(place);
            results.Children.Add(row);
        }
    }

    /// <summary>Snapshot helper: the 7-day view of the current report.</summary>
    internal void SwitchToDays()
    {
        _days = true;
        if (_report is not null) ShowReport(_report, _failed, _fahrenheit);
    }

    /// <summary>Snapshot helper: setup screen with fixed search results.</summary>
    internal void ShowSetupWithResults(List<Place> found)
    {
        ShowSetup(false);
        if (_body.Content is Grid g && g.Children[^1] is ScrollViewer { Content: StackPanel lower } && lower.Children[0] is TextBlock status && lower.Children[1] is StackPanel results)
            ShowResults(results, status, found);
    }

    // ---- small builders ----

    static Icon WxIcon(string key, double size, string brush = "FgBrush")
    {
        var i = new Icon { Width = size, Height = size, Stroke = 1.6 };
        i.SetResourceReference(Icon.DataProperty, key);
        i.SetResourceReference(Icon.ForegroundProperty, brush);
        return i;
    }

    static TextBlock Dim(string text)
    {
        var t = new TextBlock { Text = text };
        t.SetResourceReference(StyleProperty, "TextDim");
        return t;
    }

    static RadioButton Radio(string key, bool on, Action click)
    {
        var r = new RadioButton { Content = Strings.T(key), IsChecked = on, Margin = new Thickness(0, 0, 4, 0), GroupName = "wxview" + Guid.NewGuid().ToString("N") };
        r.SetResourceReference(StyleProperty, "ChipRadio");
        r.Checked += (_, _) => click();
        return r;
    }

    static ToggleButton Toggle(string key, bool on, Action<bool> changed)
    {
        var chip = new ToggleButton { Content = Strings.T(key), IsChecked = on, Margin = new Thickness(0, 0, 6, 4) };
        chip.SetResourceReference(StyleProperty, "Chip");
        chip.Click += (_, _) => { try { changed(chip.IsChecked == true); } catch (Exception ex) { Logger.Error("weather option", ex); } };
        return chip;
    }

    static Button Small(string key, Action click)
    {
        var b = new Button { Content = Strings.T(key), Padding = new Thickness(12, 4, 12, 4), MinHeight = 30 };
        b.SetResourceReference(StyleProperty, "FlatButton");
        b.Click += (_, _) => { try { click(); } catch (Exception ex) { Logger.Error("weather action", ex); } };
        return b;
    }

    static Button IconAction(string icon, string tipKey, Action click)
    {
        var b = new Button { Width = 28, Height = 28, ToolTip = Strings.T(tipKey), Content = WxIcon(icon, 15, "FgDimBrush") };
        b.SetResourceReference(StyleProperty, "IconButton");
        b.Click += (_, _) => { try { click(); } catch (Exception ex) { Logger.Error("weather action", ex); } };
        return b;
    }
}

/// <summary>Icon + temperature for the idle pill's right slot.</summary>
public sealed class WeatherPill : StackPanel
{
    readonly Icon _icon = new() { Width = 14, Height = 14, Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
    readonly TextBlock _temp = new() { FontSize = 12, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };

    public WeatherPill()
    {
        Orientation = Orientation.Horizontal;
        _icon.SetResourceReference(Icon.ForegroundProperty, "FgBrush");
        Children.Add(_icon);
        Children.Add(_temp);
    }

    public void Set(string iconKey, string temp)
    {
        _icon.SetResourceReference(Icon.DataProperty, iconKey);
        _temp.Text = temp;
    }
}
