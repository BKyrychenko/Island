using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Isle.Core;
using Isle.Modules.Shelf;

namespace Isle.Views.Shelf;

/// <summary>
/// Shelf tab: parked files as tiles (shell icon or image thumbnail + name). Click selects, double-click opens, drag a tile
/// out into any window (FileDrop). Toolbar: open, copy path, remove, clear, "remove after drag-out".
/// </summary>
public sealed class ShelfView : Grid, ITabView
{
    static readonly HashSet<string> ImageExt = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tif", ".tiff" };

    readonly ShelfStore _store;
    readonly IslandWindow? _island;
    readonly WrapPanel _tiles = new();
    readonly TextBlock _empty = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center };
    readonly Border _dropHint = new() { CornerRadius = new CornerRadius(14), BorderThickness = new Thickness(1.5), Visibility = Visibility.Collapsed, IsHitTestVisible = false };
    readonly Border _dropHintHost = new() { CornerRadius = new CornerRadius(14), Visibility = Visibility.Collapsed, IsHitTestVisible = false };
    readonly StackPanel _actions = new() { Orientation = Orientation.Horizontal };
    readonly ToggleButton _removeAfter = new();
    readonly Dictionary<string, ImageSource?> _icons = new(StringComparer.OrdinalIgnoreCase);
    string? _selected;
    Point _dragStart;
    string? _dragId;
    bool _visible, _stale = true;

    public ShelfView(ShelfStore store, IslandWindow? island)
    {
        _store = store;
        _island = island;
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var bar = new DockPanel { Margin = new Thickness(0, 0, 0, 6), LastChildFill = false };
        _removeAfter.SetResourceReference(StyleProperty, "Chip");
        _removeAfter.SetBinding(ContentControl.ContentProperty, Loc("shelf.removeAfterDrop"));
        _removeAfter.IsChecked = Settings.Current.OptBool("shelf.removeAfterDrop");
        _removeAfter.Click += (_, _) => { Settings.Current.SetOpt("shelf.removeAfterDrop", _removeAfter.IsChecked == true); Settings.Current.Save("Options"); };
        DockPanel.SetDock(_removeAfter, Dock.Left);
        bar.Children.Add(_removeAfter);
        var clear = SmallButton("Icon.Trash", "shelf.clear", () => { _selected = null; _store.Clear(); });
        DockPanel.SetDock(clear, Dock.Right);
        bar.Children.Add(clear);
        _actions.Children.Add(SmallButton("Icon.Folder", "shelf.open", () => Open(_selected)));
        _actions.Children.Add(SmallButton("Icon.Copy", "shelf.copyPath", CopyPath));
        _actions.Children.Add(SmallButton("Icon.Close", "shelf.remove", () => { if (_selected is { } id) { _selected = null; _store.Remove(id); } }));
        DockPanel.SetDock(_actions, Dock.Right);
        bar.Children.Add(_actions);
        Children.Add(bar);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _tiles };
        SetRow(scroll, 1);
        Children.Add(scroll);
        _empty.SetResourceReference(StyleProperty, "TextDim");
        _empty.SetBinding(TextBlock.TextProperty, Loc("shelf.empty"));
        SetRow(_empty, 1);
        Children.Add(_empty);
        _dropHint.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");
        _dropHint.SetResourceReference(Border.BackgroundProperty, "AccentSoftBrush");
        var hint = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold };
        hint.SetBinding(TextBlock.TextProperty, Loc("shelf.drop"));
        _dropHint.Child = hint;
        // opaque underlay so the tiles don't show through the translucent accent
        _dropHintHost.SetResourceReference(Border.BackgroundProperty, "BgBrush");
        _dropHintHost.Child = _dropHint;
        SetRowSpan(_dropHintHost, 2);
        Children.Add(_dropHintHost);

        _store.Changed += () => { if (_visible) Rebuild(); else _stale = true; };
        Rebuild();
    }

    public void OnShown()
    {
        _visible = true;
        if (_stale) Rebuild();
    }

    public void OnHidden()
    {
        _visible = false;
        ShowDropHint(false);
    }

    public void ShowDropHint(bool on) => _dropHintHost.Visibility = _dropHint.Visibility = on ? Visibility.Visible : Visibility.Collapsed;

    public void Rebuild()
    {
        _stale = false;
        _tiles.Children.Clear();
        if (_selected is not null && _store.Find(_selected) is null) _selected = null;
        foreach (var item in _store.Items) _tiles.Children.Add(Tile(item));
        _empty.Visibility = _store.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        _actions.Visibility = _selected is null ? Visibility.Collapsed : Visibility.Visible;
    }

    FrameworkElement Tile(ShelfItem item)
    {
        var missing = _store.IsMissing(item);
        var name = System.IO.Path.GetFileName(item.Path);
        if (name.Length == 0) name = item.Path;
        var stack = new StackPanel { Width = 76 };
        var img = new Image { Width = 40, Height = 40, Stretch = Stretch.Uniform, Margin = new Thickness(0, 2, 0, 4), Source = missing ? null : IconFor(item.Path), Opacity = missing ? 0.35 : 1 };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
        stack.Children.Add(img);
        var label = new TextBlock { Text = name, FontSize = 11, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxHeight = 30, TextTrimming = TextTrimming.CharacterEllipsis };
        label.SetResourceReference(TextBlock.ForegroundProperty, missing ? "FgFaintBrush" : "FgBrush");
        stack.Children.Add(label);
        if (missing)
        {
            var m = new TextBlock { Text = Strings.T("shelf.missing"), FontSize = 10, TextAlignment = TextAlignment.Center };
            m.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
            stack.Children.Add(m);
        }
        var border = new Border { Child = stack, Padding = new Thickness(4, 6, 4, 6), Margin = new Thickness(0, 0, 4, 4), CornerRadius = new CornerRadius(12), Background = Brushes.Transparent, ToolTip = item.Path };
        if (item.Id == _selected) border.SetResourceReference(Border.BackgroundProperty, "SurfaceHoverBrush");
        var id = item.Id;
        border.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2) { Open(id); e.Handled = true; return; }
            _dragStart = e.GetPosition(this);
            _dragId = missing ? null : id;
            if (_selected != id) { _selected = id; Rebuild(); }
        };
        border.MouseMove += (_, e) =>
        {
            if (_dragId != id || e.LeftButton != MouseButtonState.Pressed) return;
            var d = e.GetPosition(this) - _dragStart;
            if (Math.Abs(d.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(d.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            _dragId = null;
            DragOut(id);
        };
        border.MouseLeftButtonUp += (_, _) => _dragId = null;
        return border;
    }

    void DragOut(string id)
    {
        if (_store.Find(id) is not { } item) return;
        var data = new DataObject(DataFormats.FileDrop, new[] { item.Path });
        _island?.HoldOpen("shelf-drag");
        try
        {
            var result = DragDrop.DoDragDrop(this, data, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);
            if (result != DragDropEffects.None && Settings.Current.OptBool("shelf.removeAfterDrop")) _store.Remove(id);
        }
        catch (Exception ex) { Logger.Error("shelf drag", ex); }
        finally { _island?.ReleaseOpen("shelf-drag"); }
    }

    void Open(string? id)
    {
        if (id is null || _store.Find(id) is not { } item) return;
        try
        {
            if (_store.IsMissing(item))
            {
                var dir = System.IO.Path.GetDirectoryName(item.Path);
                if (dir is not null && Directory.Exists(dir)) Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
                return;
            }
            Process.Start(new ProcessStartInfo(item.Path) { UseShellExecute = true });
        }
        catch (Exception ex) { Logger.Error("shelf open", ex); }
    }

    void CopyPath()
    {
        if (_selected is null || _store.Find(_selected) is not { } item) return;
        try { System.Windows.Clipboard.SetText(item.Path); } catch (Exception ex) { Logger.Error("shelf copy path", ex); }
    }

    ImageSource? IconFor(string path)
    {
        if (_icons.TryGetValue(path, out var cached)) return cached;
        ImageSource? src = null;
        try
        {
            if (ImageExt.Contains(System.IO.Path.GetExtension(path)) && new FileInfo(path).Length < 50_000_000)
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.DecodePixelWidth = 80;
                bmp.UriSource = new Uri(path);
                bmp.EndInit();
                bmp.Freeze();
                src = bmp;
            }
        }
        catch { src = null; }
        src ??= ShellIcons.For(path);
        _icons[path] = src;
        return src;
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
        b.Click += (_, _) => { try { click(); } catch (Exception ex) { Logger.Error("shelf button", ex); } };
        return b;
    }
}
