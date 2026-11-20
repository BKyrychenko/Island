using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Isle.Core;
using Isle.Modules.Tools;
using Path = System.Windows.Shapes.Path;
using IconView = Isle.Views.Icon;
using TextElement = System.Windows.Documents.TextElement;

namespace Isle.Views.Tools;

/// <summary>
/// Screenshot markup: the captured region at 1:1 (scaled down only if it does not fit), a toolbar with pen, highlighter,
/// arrow, rectangle, ellipse, text and pixelate, colours, three thicknesses, undo/redo, and Copy / Save / Copy path.
/// Shortcuts: Ctrl+Z/Y, Ctrl+C, Ctrl+S, Esc. Annotations live in image pixels so the export is exact.
/// </summary>
public sealed class MarkupWindow : Window
{
    enum Tool { Pen, Highlighter, Arrow, Rect, Ellipse, Text, Pixelate }
    enum ExportAction { Copy, Save, CopyPath }

    static readonly Color[] Palette = [Color.FromRgb(0xFF, 0x4D, 0x4D), Color.FromRgb(0xFF, 0xC8, 0x1F), Color.FromRgb(0x34, 0xC7, 0x59), Color.FromRgb(0x3D, 0x8B, 0xFF), Colors.White, Colors.Black];
    static readonly double[] Widths = [2, 4, 8];

    readonly Selection _sel;
    readonly Action<string> _copyText;
    readonly Action<BitmapSource> _copyImage;
    readonly Func<BitmapSource, string> _savePng;
    readonly TextBlock _error = new() { Margin = new Thickness(4, 6, 4, 0), Visibility = Visibility.Collapsed };
    readonly Canvas _canvas = new() { ClipToBounds = true, Background = Brushes.Transparent };
    readonly List<UIElement> _undo = [], _redo = [];
    readonly Dictionary<Tool, PickButton> _toolButtons = [];
    readonly List<PickButton> _colorButtons = [], _widthButtons = [];
    readonly double _s;
    Tool _tool = Tool.Arrow;
    Color _color = Palette[0];
    double _width = Widths[1];
    Point _start;
    UIElement? _drawing;
    TextBox? _typing;
    string? _savedPath;

    public static void Open(Selection sel, Action<string> copyText)
    {
        var w = new MarkupWindow(sel, copyText, System.Windows.Clipboard.SetImage, ScreenGrab.SavePng);
        w.Show();
        w.Activate();
    }

    internal MarkupWindow(Selection sel, Action<string> copyText, Action<BitmapSource> copyImage, Func<BitmapSource, string> savePng)
    {
        _sel = sel;
        _copyText = copyText;
        _copyImage = copyImage;
        _savePng = savePng;
        _s = Math.Max(1, sel.Monitor.DpiScale);
        Title = "Isle — " + Strings.T("tools.screenshot");
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        ShowInTaskbar = true;
        SizeToContent = SizeToContent.WidthAndHeight;
        UseLayoutRounding = true;
        SetResourceReference(TextElement.FontFamilyProperty, "UiFont");

        var img = sel.Image;
        _canvas.Width = img.PixelWidth;
        _canvas.Height = img.PixelHeight;
        var bg = new Image { Source = img, Width = img.PixelWidth, Height = img.PixelHeight };
        RenderOptions.SetBitmapScalingMode(bg, BitmapScalingMode.HighQuality);
        _canvas.Children.Add(bg);
        _canvas.MouseLeftButtonDown += OnDown;
        _canvas.MouseMove += OnMove;
        _canvas.MouseLeftButtonUp += OnUp;
        _canvas.Cursor = Cursors.Cross;

        // fit into ~85 % of the monitor (in DIPs of that monitor); 1:1 physical pixels when it fits
        var maxW = sel.Monitor.Width / _s * 0.85;
        var maxH = sel.Monitor.Height / _s * 0.85 - 70;
        var fit = Math.Min(1, Math.Min(maxW / (img.PixelWidth / _s), maxH / (img.PixelHeight / _s)));
        var view = new Viewbox { Child = _canvas, Stretch = Stretch.Uniform, Width = Math.Max(120, img.PixelWidth / _s * fit), Height = Math.Max(60, img.PixelHeight / _s * fit) };
        var imageFrame = new Border { Child = view, CornerRadius = new CornerRadius(6), ClipToBounds = true, HorizontalAlignment = HorizontalAlignment.Center };

        var toolbar = BuildToolbar();
        var root = new StackPanel();
        root.Children.Add(imageFrame);
        root.Children.Add(toolbar);
        _error.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
        root.Children.Add(_error);
        var chrome = new Border { Child = root, Padding = new Thickness(8), CornerRadius = new CornerRadius(14), BorderThickness = new Thickness(1) };
        chrome.SetResourceReference(Border.BackgroundProperty, "PanelBgBrush");
        chrome.SetResourceReference(Border.BorderBrushProperty, "StrokeBrush");
        chrome.SetResourceReference(TextElement.ForegroundProperty, "FgBrush");
        Content = chrome;

        SourceInitialized += (_, _) => PlaceOnMonitor();
        PreviewKeyDown += (_, e) => { if (HandleKey(e.Key, Keyboard.Modifiers)) e.Handled = true; };
        SelectTool(_tool);
        SelectColor(0);
        SelectWidth(1);
    }

    void PlaceOnMonitor()
    {
        // centre on the monitor of the selection, in physical pixels
        UpdateLayout();
        var m = _sel.Monitor;
        var w = (int)(ActualWidth * _s);
        var h = (int)(ActualHeight * _s);
        var hwnd = new WindowInteropHelper(this).Handle;
        Native.SetWindowPos(hwnd, IntPtr.Zero, m.Left + (m.Width - w) / 2, m.Top + Math.Max(0, (m.Height - h) / 2), 0, 0, Native.SWP_NOSIZE | Native.SWP_NOZORDER);
    }

    FrameworkElement BuildToolbar()
    {
        var bar = new DockPanel { Margin = new Thickness(0, 8, 0, 0), LastChildFill = false, Background = Brushes.Transparent };
        bar.MouseLeftButtonDown += (_, e) => { if (e.OriginalSource == bar) DragMove(); };
        var left = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var (tool, icon, tip) in new[]
        {
            (Tool.Pen, "Icon.Pen", "markup.pen"), (Tool.Highlighter, "Icon.Highlighter", "markup.highlighter"), (Tool.Arrow, "Icon.Arrow", "markup.arrow"),
            (Tool.Rect, "Icon.Rect", "markup.rect"), (Tool.Ellipse, "Icon.Ellipse", "markup.ellipse"), (Tool.Text, "Icon.TextTool", "markup.text"),
            (Tool.Pixelate, "Icon.Pixelate", "markup.pixelate"),
        })
        {
            var b = IconToggle(icon, tip);
            var t = tool;
            b.Click += (_, _) => SelectTool(t);
            _toolButtons[tool] = b;
            left.Children.Add(b);
        }
        left.Children.Add(Gap());
        for (var i = 0; i < Palette.Length; i++)
        {
            var dot = new Ellipse { Width = 14, Height = 14, Fill = new SolidColorBrush(Palette[i]), Stroke = new SolidColorBrush(Color.FromArgb(0x60, 0x80, 0x80, 0x80)), StrokeThickness = 1 };
            var b = new PickButton(dot, 26);
            var index = i;
            b.Click += (_, _) => SelectColor(index);
            _colorButtons.Add(b);
            left.Children.Add(b);
        }
        left.Children.Add(Gap());
        for (var i = 0; i < Widths.Length; i++)
        {
            var line = new Rectangle { Width = 14, Height = 1.5 + i * 2, RadiusX = 1, RadiusY = 1 };
            line.SetResourceReference(Shape.FillProperty, "FgBrush");
            var b = new PickButton(line, 28);
            var index = i;
            b.Click += (_, _) => SelectWidth(index);
            _widthButtons.Add(b);
            left.Children.Add(b);
        }
        left.Children.Add(Gap());
        left.Children.Add(IconButton("Icon.Undo", "markup.undo", Undo));
        left.Children.Add(IconButton("Icon.Redo", "markup.redo", Redo));
        DockPanel.SetDock(left, Dock.Left);
        bar.Children.Add(left);

        var right = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(16, 0, 0, 0) };
        right.Children.Add(TextButton("markup.copy", "AccentButton", () => Export(ExportAction.Copy)));
        right.Children.Add(TextButton("markup.save", "FlatButton", () => Export(ExportAction.Save)));
        right.Children.Add(TextButton("markup.copyPath", "FlatButton", () => Export(ExportAction.CopyPath)));
        right.Children.Add(IconButton("Icon.Close", "markup.close", Close));
        DockPanel.SetDock(right, Dock.Right);
        bar.Children.Add(right);
        return bar;
    }

    static FrameworkElement Gap() => new Border { Width = 1, Height = 18, Margin = new Thickness(6, 0, 6, 0), Background = new SolidColorBrush(Color.FromArgb(0x40, 0x80, 0x80, 0x80)) };

    static PickButton IconToggle(string icon, string tip)
    {
        var i = new IconView { Width = 16, Height = 16 };
        i.SetResourceReference(IconView.DataProperty, icon);
        i.SetResourceReference(IconView.ForegroundProperty, "FgBrush");
        return new PickButton(i, 30) { ToolTip = Strings.T(tip) };
    }

    static Button IconButton(string icon, string tip, Action click)
    {
        var i = new IconView { Width = 16, Height = 16 };
        i.SetResourceReference(IconView.DataProperty, icon);
        i.SetResourceReference(IconView.ForegroundProperty, "FgDimBrush");
        var b = new Button { Content = i, Width = 30, Height = 28, ToolTip = Strings.T(tip) };
        b.SetResourceReference(StyleProperty, "IconButton");
        b.Click += (_, _) => click();
        return b;
    }

    static Button TextButton(string key, string style, Action click)
    {
        var b = new Button { Content = Strings.T(key), Margin = new Thickness(4, 0, 0, 0), Padding = new Thickness(12, 4, 12, 4) };
        b.SetResourceReference(StyleProperty, style);
        b.Click += (_, _) => click();
        return b;
    }

    void SelectTool(Tool t)
    {
        CommitText();
        _tool = t;
        foreach (var (k, b) in _toolButtons) b.IsChecked = k == t;
        _canvas.Cursor = t == Tool.Text ? Cursors.IBeam : Cursors.Cross;
    }

    void SelectColor(int i)
    {
        _color = Palette[i];
        for (var k = 0; k < _colorButtons.Count; k++) _colorButtons[k].IsChecked = k == i;
    }

    void SelectWidth(int i)
    {
        _width = Widths[i];
        for (var k = 0; k < _widthButtons.Count; k++) _widthButtons[k].IsChecked = k == i;
    }

    double Stroke => _width * _s;

    // ---- drawing ----

    void OnDown(object sender, MouseButtonEventArgs e)
    {
        var p = e.GetPosition(_canvas);
        if (_tool == Tool.Text) { CommitText(); StartText(p); e.Handled = true; return; }
        CommitText();
        _start = p;
        var brush = new SolidColorBrush(_color);
        _drawing = _tool switch
        {
            Tool.Pen => new Polyline { Stroke = brush, StrokeThickness = Stroke, StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, Points = { p } },
            Tool.Highlighter => new Polyline { Stroke = new SolidColorBrush(Color.FromArgb(0x66, _color.R, _color.G, _color.B)), StrokeThickness = Stroke * 4, StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Flat, StrokeEndLineCap = PenLineCap.Flat, Points = { p } },
            Tool.Arrow => new Path { Stroke = brush, Fill = brush, StrokeThickness = Stroke, StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round },
            Tool.Rect => new Rectangle { Stroke = brush, StrokeThickness = Stroke, RadiusX = 2 * _s, RadiusY = 2 * _s },
            Tool.Ellipse => new Ellipse { Stroke = brush, StrokeThickness = Stroke },
            Tool.Pixelate => new Rectangle { Stroke = new SolidColorBrush(Color.FromArgb(0xA0, 0x80, 0x80, 0x80)), StrokeThickness = 1 * _s, StrokeDashArray = [4, 3] },
            _ => null,
        };
        if (_drawing is null) return;
        _canvas.Children.Add(_drawing);
        _canvas.CaptureMouse();
        e.Handled = true;
    }

    void OnMove(object sender, MouseEventArgs e)
    {
        if (_drawing is null || e.LeftButton != MouseButtonState.Pressed) return;
        var p = Clamp(e.GetPosition(_canvas));
        switch (_drawing)
        {
            case Polyline line: line.Points.Add(p); break;
            case Path arrow: arrow.Data = ArrowGeometry(_start, p, Stroke); break;
            case Shape box: PlaceBox(box, _start, p); break;
        }
    }

    void OnUp(object sender, MouseButtonEventArgs e)
    {
        if (_drawing is null) return;
        _canvas.ReleaseMouseCapture();
        var p = Clamp(e.GetPosition(_canvas));
        var tooSmall = (p - _start).Length < 3 * _s && _drawing is not Polyline;
        if (tooSmall) _canvas.Children.Remove(_drawing);
        else if (_tool == Tool.Pixelate)
        {
            _canvas.Children.Remove(_drawing);
            var px = Pixelate(_start, p);
            if (px is not null) Push(px);
        }
        else Push(_drawing);
        _drawing = null;
    }

    Point Clamp(Point p) => new(Math.Clamp(p.X, 0, _canvas.Width), Math.Clamp(p.Y, 0, _canvas.Height));

    static void PlaceBox(FrameworkElement box, Point a, Point b)
    {
        Canvas.SetLeft(box, Math.Min(a.X, b.X));
        Canvas.SetTop(box, Math.Min(a.Y, b.Y));
        box.Width = Math.Abs(b.X - a.X);
        box.Height = Math.Abs(b.Y - a.Y);
    }

    /// <summary>Line from <paramref name="a"/> to <paramref name="b"/> with a filled head scaled to the stroke.</summary>
    internal static Geometry ArrowGeometry(Point a, Point b, double stroke)
    {
        var g = new StreamGeometry();
        var v = b - a;
        var len = v.Length;
        using (var ctx = g.Open())
        {
            if (len < 0.5) { ctx.BeginFigure(a, false, false); ctx.LineTo(b, true, false); return g; }
            v /= len;
            var head = Math.Min(len * 0.6, Math.Max(10, stroke * 4.5));
            var baseP = b - v * head;
            var n = new Vector(-v.Y, v.X) * head * 0.55;
            ctx.BeginFigure(a, false, false);
            ctx.LineTo(baseP, true, false);
            ctx.BeginFigure(b, true, true);
            ctx.LineTo(baseP + n, true, true);
            ctx.LineTo(baseP - n, true, true);
        }
        g.Freeze();
        return g;
    }

    /// <summary>A pixelated copy of the screenshot region (blocks of ~12 px) placed over it.</summary>
    UIElement? Pixelate(Point a, Point b)
    {
        int x = (int)Math.Min(a.X, b.X), y = (int)Math.Min(a.Y, b.Y);
        int w = (int)Math.Abs(b.X - a.X), h = (int)Math.Abs(b.Y - a.Y);
        w = Math.Min(w, _sel.Image.PixelWidth - x);
        h = Math.Min(h, _sel.Image.PixelHeight - y);
        if (w < 2 || h < 2) return null;
        var block = Math.Max(6, 12 * _s);
        var crop = new CroppedBitmap(_sel.Image, new Int32Rect(x, y, w, h));
        var small = new TransformedBitmap(crop, new ScaleTransform(Math.Max(1, w / block) / w, Math.Max(1, h / block) / h));
        small.Freeze();
        var img = new Image { Source = small, Width = w, Height = h, Stretch = Stretch.Fill };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
        Canvas.SetLeft(img, x);
        Canvas.SetTop(img, y);
        return img;
    }

    void StartText(Point p)
    {
        var box = new TextBox
        {
            FontSize = 14 * _s + _width * 2 * _s, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(_color),
            Background = Brushes.Transparent, BorderThickness = new Thickness(1 * _s), BorderBrush = new SolidColorBrush(Color.FromArgb(0x80, 0x80, 0x80, 0x80)),
            MinWidth = 60 * _s, AcceptsReturn = true, Padding = new Thickness(2 * _s, 0, 2 * _s, 0), CaretBrush = new SolidColorBrush(_color),
        };
        Canvas.SetLeft(box, p.X);
        Canvas.SetTop(box, p.Y - box.FontSize * 0.7);
        _canvas.Children.Add(box);
        _typing = box;
        Dispatcher.BeginInvoke(() => { box.Focus(); Keyboard.Focus(box); }, System.Windows.Threading.DispatcherPriority.Input);
    }

    void CommitText()
    {
        if (_typing is not { } box) return;
        _typing = null;
        _canvas.Children.Remove(box);
        if (box.Text.Trim().Length == 0) return;
        var t = new TextBlock { Text = box.Text, FontSize = box.FontSize, FontWeight = box.FontWeight, Foreground = box.Foreground, Padding = new Thickness(box.Padding.Left + box.BorderThickness.Left + 2, box.BorderThickness.Top, 0, 0) };
        Canvas.SetLeft(t, Canvas.GetLeft(box));
        Canvas.SetTop(t, Canvas.GetTop(box));
        _canvas.Children.Add(t);
        Push(t);
    }

    void Push(UIElement e)
    {
        _savedPath = null;
        if (!_canvas.Children.Contains(e)) _canvas.Children.Add(e);
        _undo.Add(e);
        _redo.Clear();
    }

    void Undo()
    {
        CommitText();
        if (_undo.Count == 0) return;
        _savedPath = null;
        var e = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _canvas.Children.Remove(e);
        _redo.Add(e);
    }

    void Redo()
    {
        if (_redo.Count == 0) return;
        _savedPath = null;
        var e = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _canvas.Children.Add(e);
        _undo.Add(e);
    }

    internal bool HandleKey(Key key, ModifierKeys modifiers)
    {
        var ctrl = (modifiers & ModifierKeys.Control) != 0;
        if (ctrl && key == Key.S) Export(ExportAction.Save);
        else if (_typing is not null)
        {
            if (key != Key.Escape && (key != Key.Enter || modifiers.HasFlag(ModifierKeys.Shift))) return false;
            CommitText();
        }
        else if (key == Key.Escape) Close();
        else if (ctrl && key == Key.Z) Undo();
        else if (ctrl && key == Key.Y) Redo();
        else if (ctrl && key == Key.C) Export(ExportAction.Copy);
        else return false;
        return true;
    }

    // ---- export ----

    BitmapSource Render()
    {
        _canvas.Measure(new Size(_canvas.Width, _canvas.Height));
        _canvas.Arrange(new Rect(0, 0, _canvas.Width, _canvas.Height));
        var rtb = new RenderTargetBitmap((int)_canvas.Width, (int)_canvas.Height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(_canvas);
        rtb.Freeze();
        return rtb;
    }

    void Export(ExportAction action)
    {
        var error = action == ExportAction.Copy ? "markup.copyFailed" : "markup.saveFailed";
        try
        {
            CommitText();
            if (action == ExportAction.Copy) _copyImage(Render());
            else
            {
                if (_drawing is not null || !File.Exists(_savedPath)) _savedPath = _savePng(Render());
                if (action == ExportAction.CopyPath)
                {
                    error = "markup.copyPathFailed";
                    _copyText(_savedPath);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(error, ex);
            _error.Text = Strings.T(error);
            _error.Visibility = Visibility.Visible;
            return;
        }
        Close();
    }
}

/// <summary>Small selectable toolbar cell (tool / colour / width): hover tint, accent tint when selected.</summary>
sealed class PickButton : Border
{
    bool _checked, _hover;
    public event RoutedEventHandler? Click;

    public PickButton(UIElement content, double width)
    {
        Child = content;
        Width = width;
        Height = 28;
        Margin = new Thickness(1, 0, 1, 0);
        CornerRadius = new CornerRadius(8);
        Background = Brushes.Transparent;
        Cursor = Cursors.Hand;
        if (content is FrameworkElement fe) { fe.HorizontalAlignment = HorizontalAlignment.Center; fe.VerticalAlignment = VerticalAlignment.Center; }
        MouseEnter += (_, _) => { _hover = true; Update(); };
        MouseLeave += (_, _) => { _hover = false; Update(); };
        MouseLeftButtonUp += (_, e) => { e.Handled = true; Click?.Invoke(this, new RoutedEventArgs()); };
    }

    public bool? IsChecked
    {
        get => _checked;
        set { _checked = value == true; Update(); }
    }

    void Update()
    {
        if (_checked) SetResourceReference(BackgroundProperty, "AccentSoftBrush");
        else if (_hover) SetResourceReference(BackgroundProperty, "SurfaceHoverBrush");
        else Background = Brushes.Transparent;
    }
}
