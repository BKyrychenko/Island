using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Isle.Core;
using Isle.Modules.Clipboard;

namespace Isle.Views.Clipboard;

/// <summary>
/// Clipboard tab: search + clear on top, history below (pinned first). Clicking an entry pastes it into the window
/// that was active before; the pin / copy / delete buttons sit on the right of each row.
/// </summary>
public sealed class ClipboardView : Grid, ITabView
{
    readonly ClipStore _store;
    readonly Action<ClipEntry> _paste, _copy;
    readonly TextBox _search = new();
    readonly TextBlock _searchHint = new() { IsHitTestVisible = false, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
    readonly StackPanel _list = new();
    readonly ScrollViewer _scroll;
    bool _visible, _stale = true;

    public ClipboardView(ClipStore store, Action<ClipEntry> paste, Action<ClipEntry> copy)
    {
        _store = store;
        _paste = paste;
        _copy = copy;
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var top = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
        var clear = SmallButton("Icon.Trash", "clip.clear", () => { _store.ClearUnpinned(); });
        DockPanel.SetDock(clear, Dock.Right);
        top.Children.Add(clear);
        var searchHost = new Grid();
        _search.SetResourceReference(StyleProperty, "FlatTextBox");
        _search.MinHeight = 28;
        _search.TextChanged += (_, _) => { _searchHint.Visibility = _search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed; Rebuild(); };
        _searchHint.SetResourceReference(TextBlock.ForegroundProperty, "FgFaintBrush");
        _searchHint.SetBinding(TextBlock.TextProperty, Loc("clip.search"));
        searchHost.Children.Add(_search);
        searchHost.Children.Add(_searchHint);
        top.Children.Add(searchHost);
        Children.Add(top);

        _scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _list };
        SetRow(_scroll, 1);
        Children.Add(_scroll);
        _store.Changed += OnStoreChanged;
        Rebuild();
    }

    void OnStoreChanged()
    {
        if (_visible) Rebuild();
        else _stale = true; // rebuild when shown: no UI work while collapsed
    }

    public void OnShown()
    {
        _visible = true;
        if (_stale) Rebuild();
    }

    public void OnHidden()
    {
        _visible = false;
        if (_search.Text.Length > 0) _search.Text = "";
    }

    public void Rebuild()
    {
        _stale = false;
        _list.Children.Clear();
        var count = 0;
        foreach (var e in _store.Search(_search.Text))
        {
            _list.Children.Add(Row(e));
            if (++count >= 60) break; // keep the visual tree small; search finds older ones
        }
        if (count == 0)
        {
            var t = new TextBlock { Margin = new Thickness(4, 10, 0, 0), TextWrapping = TextWrapping.Wrap };
            t.SetResourceReference(StyleProperty, "TextDim");
            t.Text = Strings.T(_search.Text.Length > 0 ? "clip.noMatches" : "clip.empty");
            _list.Children.Add(t);
        }
    }

    FrameworkElement Row(ClipEntry e)
    {
        var row = new DockPanel();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var id = e.Id;
        var pin = SmallButton("Icon.Pin", e.Pinned ? "clip.unpin" : "clip.pin", () => _store.SetPinned(id, !(_store.Find(id)?.Pinned ?? false)), e.Pinned ? "AccentBrush" : null);
        buttons.Children.Add(pin);
        buttons.Children.Add(SmallButton("Icon.Copy", "clip.copy", () => { if (_store.Find(id) is { } x) _copy(x); }));
        buttons.Children.Add(SmallButton("Icon.Close", "clip.delete", () => _store.Delete(id)));
        DockPanel.SetDock(buttons, Dock.Right);
        row.Children.Add(buttons);

        var time = new TextBlock { FontSize = 10.5, Text = Ago(e.Time), Margin = new Thickness(8, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
        time.SetResourceReference(TextBlock.ForegroundProperty, "FgFaintBrush");
        DockPanel.SetDock(time, Dock.Right);
        row.Children.Add(time);

        if (e.Kind == ClipKind.Text)
        {
            var text = new TextBlock { Text = ClipStore.Preview(e.Text), FontSize = 12, TextWrapping = TextWrapping.Wrap, MaxHeight = 34, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
            text.SetResourceReference(TextBlock.ForegroundProperty, "FgBrush");
            row.Children.Add(text);
        }
        else
        {
            var p = new StackPanel { Orientation = Orientation.Horizontal };
            var img = new Image { Height = 40, MaxWidth = 120, Stretch = Stretch.Uniform, Source = Thumb(_store.ImagePath(e)) };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
            p.Children.Add(new Border { CornerRadius = new CornerRadius(6), ClipToBounds = true, Child = img });
            var dims = new TextBlock { Text = $"{e.ImageWidth} × {e.ImageHeight}", FontSize = 11, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            dims.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
            p.Children.Add(dims);
            row.Children.Add(p);
        }

        var b = new Button { Content = row, Padding = new Thickness(8, 4, 4, 4), Margin = new Thickness(0, 0, 0, 2), MinHeight = 0 };
        b.SetResourceReference(StyleProperty, "GhostButton");
        b.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        b.SetBinding(ToolTipProperty, Loc("clip.clickToPaste"));
        b.Click += (_, _) => { if (_store.Find(id) is { } x) _paste(x); };
        return b;
    }

    static BitmapSource? Thumb(string path)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad; // don't keep the file open
            bmp.DecodePixelHeight = 80;
            bmp.UriSource = new Uri(path);
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch { return null; }
    }

    internal static string Ago(DateTimeOffset t)
    {
        var d = DateTimeOffset.Now - t;
        if (d.TotalMinutes < 1) return Strings.T("clip.now");
        if (d.TotalHours < 1) return $"{(int)d.TotalMinutes} {Strings.T("unit.min")}";
        if (d.TotalDays < 1) return $"{(int)d.TotalHours} {Strings.T("unit.h")}";
        return t.ToLocalTime().ToString("d MMM", System.Globalization.CultureInfo.GetCultureInfo(Strings.Language == "uk" ? "uk-UA" : "en-US"));
    }

    static System.Windows.Data.Binding Loc(string key) => new($"[{key}]") { Source = Strings.Instance };

    static Button SmallButton(string icon, string tip, Action click, string? brush = null)
    {
        var b = new Button { Width = 26, Height = 26, Margin = new Thickness(1, 0, 0, 0) };
        b.SetResourceReference(StyleProperty, "IconButton");
        var i = new Icon { Width = 14, Height = 14 };
        i.SetResourceReference(Icon.DataProperty, icon);
        i.SetResourceReference(Icon.ForegroundProperty, brush ?? "FgDimBrush");
        b.Content = i;
        b.SetBinding(ToolTipProperty, Loc(tip));
        b.Click += (_, e) => { e.Handled = true; try { click(); } catch (Exception ex) { Logger.Error("clipboard button", ex); } };
        return b;
    }
}
