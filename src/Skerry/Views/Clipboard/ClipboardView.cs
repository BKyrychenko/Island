using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Skerry.Core;
using Skerry.Modules.Clipboard;

namespace Skerry.Views.Clipboard;

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
    int _shown = 60;
    bool _visible, _stale = true;

    public ClipboardView(ClipStore store, Action<ClipEntry> paste, Action<ClipEntry> copy, IslandWindow? island = null)
    {
        _store = store;
        _paste = paste;
        _copy = copy;
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var top = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
        var clear = SmallButton("Icon.Trash", "clip.clear", () =>
            island?.ShowToast(Strings.T("clip.confirmClear"), Strings.T("common.confirmClear"), _store.ClearUnpinned));
        DockPanel.SetDock(clear, Dock.Right);
        top.Children.Add(clear);
        var searchHost = new Grid();
        _search.SetResourceReference(StyleProperty, "FlatTextBox");
        _search.MinHeight = 28;
        _search.TextChanged += (_, _) => { _shown = 60; _searchHint.Visibility = _search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed; Rebuild(); };
        _searchHint.SetResourceReference(TextBlock.ForegroundProperty, "FgFaintBrush");
        _searchHint.SetBinding(TextBlock.TextProperty, Ui.Loc("clip.search"));
        searchHost.Children.Add(_search);
        searchHost.Children.Add(_searchHint);
        top.Children.Add(searchHost);
        Children.Add(top);

        _scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _list };
        var pasteHint = new TextBlock { FontSize = 11, Margin = new Thickness(4, 0, 4, 6), TextWrapping = TextWrapping.Wrap };
        pasteHint.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
        pasteHint.SetBinding(TextBlock.TextProperty, Ui.Loc("clip.clickToPaste"));
        SetRow(pasteHint, 1);
        Children.Add(pasteHint);
        SetRow(_scroll, 2);
        Children.Add(_scroll);
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 2, 4, 2) };
        error.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
        SetRow(error, 3);
        Children.Add(error);
        void UpdateError()
        {
            if (_store.SaveErrorKey is { } key)
            {
                error.SetBinding(TextBlock.TextProperty, Ui.Loc(key));
                error.Visibility = Visibility.Visible;
            }
            else error.Visibility = Visibility.Collapsed;
        }
        _store.SaveStatusChanged += UpdateError;
        UpdateError();
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
        foreach (var e in _store.Search(_search.Text, Strings.T("clip.image")))
        {
            if (count++ == _shown)
            {
                var more = new Button { Margin = new Thickness(4), HorizontalAlignment = HorizontalAlignment.Stretch };
                more.SetResourceReference(StyleProperty, "GhostButton");
                more.SetBinding(ContentControl.ContentProperty, Ui.Loc("clip.showMore"));
                more.Click += (_, _) =>
                {
                    var next = _shown;
                    _shown += 60;
                    Rebuild();
                    if (_list.Children.Count > next) ((FrameworkElement)_list.Children[next]).Focus();
                };
                _list.Children.Add(more);
                break;
            }
            _list.Children.Add(Row(e));
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
        var pin = SmallButton("Icon.Pin", e.Pinned ? "clip.unpin" : "clip.pin", () => _store.SetPinned(id, !(_store.Find(id)?.Pinned ?? false)), e.Pinned ? "AccentTextBrush" : null);
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
        b.SetBinding(ToolTipProperty, Ui.Loc("clip.clickToPaste"));
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

    static Button SmallButton(string icon, string tip, Action click, string? brush = null)
    {
        var b = Ui.IconButton(Ui.MakeIcon(icon, 14, brush ?? "FgDimBrush"), 26, click, "clipboard button", handled: true);
        b.Margin = new Thickness(1, 0, 0, 0);
        b.SetBinding(ToolTipProperty, Ui.Loc(tip));
        return b;
    }
}
