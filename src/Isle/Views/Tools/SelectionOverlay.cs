using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using Isle.Core;
using Isle.Modules.Tools;

namespace Isle.Views.Tools;

public enum SelectMode { Region, Color }

/// <summary>Result of a selection: the cropped frozen pixels and where they were (physical px), or a picked colour.</summary>
public sealed record Selection(BitmapSource Image, PxRect Rect, ScreenInfo Monitor, Color? Picked = null);

/// <summary>
/// Full-screen selection over a frozen capture: one window per monitor (each sized in that monitor's physical pixels, so
/// mixed DPI is exact). Region mode: drag a rectangle, or click a window, Enter = whole monitor. Colour mode: magnifier
/// loupe with HEX readout, click picks. Esc cancels.
/// </summary>
public static class SelectionSession
{
    public static Task<Selection?> RunAsync(SelectMode mode, Action<bool>? hideIsland = null)
    {
        var tcs = new TaskCompletionSource<Selection?>();
        var monitors = Screens.All();
        var windows = ScreenGrab.WindowRects();
        hideIsland?.Invoke(true);
        List<(ScreenInfo M, BitmapSource Img)> shots;
        try { shots = monitors.Select(m => (m, ScreenGrab.Capture(new PxRect(m.Left, m.Top, m.Width, m.Height)))).ToList(); }
        finally { hideIsland?.Invoke(false); }

        var overlays = new List<OverlayWindow>();
        void Finish(Selection? s)
        {
            foreach (var o in overlays) o.CloseQuietly();
            overlays.Clear();
            tcs.TrySetResult(s);
        }
        foreach (var (m, img) in shots) overlays.Add(new OverlayWindow(m, img, mode, windows, Finish));
        foreach (var o in overlays) o.Show();
        // the window under the cursor gets the keyboard
        Native.GetCursorPos(out var pt);
        (overlays.FirstOrDefault(o => o.Monitor.Left <= pt.X && pt.X < o.Monitor.Left + o.Monitor.Width && o.Monitor.Top <= pt.Y && pt.Y < o.Monitor.Top + o.Monitor.Height) ?? overlays.FirstOrDefault())?.Grab();
        return tcs.Task;
    }

    sealed class OverlayWindow : Window
    {
        public ScreenInfo Monitor { get; }
        readonly BitmapSource _img;
        readonly SelectMode _mode;
        readonly List<PxRect> _windows;
        readonly Action<Selection?> _done;
        readonly Canvas _canvas = new();
        readonly Path _dim = new() { Fill = new SolidColorBrush(Color.FromArgb(0x78, 0, 0, 0)), IsHitTestVisible = false };
        readonly Rectangle _frame = new() { IsHitTestVisible = false, Visibility = Visibility.Collapsed };
        readonly Border _label = new() { IsHitTestVisible = false, Visibility = Visibility.Collapsed, Background = new SolidColorBrush(Color.FromArgb(0xD8, 0, 0, 0)) };
        readonly TextBlock _labelText = new() { Foreground = Brushes.White };
        readonly Border _loupe = new() { IsHitTestVisible = false, Visibility = Visibility.Collapsed, BorderBrush = Brushes.White, Background = Brushes.Black };
        readonly Image _loupeImage = new() { Stretch = Stretch.Fill };
        readonly TextBlock _loupeText = new() { Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, FontFamily = new FontFamily("Consolas") };
        readonly byte[] _pixels;
        readonly double _s; // monitor DPI scale: canvas units are physical pixels
        Point? _start;
        PxRect _current;
        bool _closing;

        public OverlayWindow(ScreenInfo monitor, BitmapSource img, SelectMode mode, List<PxRect> windows, Action<Selection?> done)
        {
            Monitor = monitor;
            _img = img;
            _mode = mode;
            _windows = windows;
            _done = done;
            _s = Math.Max(1, monitor.DpiScale);
            _pixels = new byte[img.PixelWidth * img.PixelHeight * 4];
            img.CopyPixels(_pixels, img.PixelWidth * 4, 0);

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            Background = Brushes.Black;
            Cursor = Cursors.Cross;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = -32000; // placed in physical pixels once the handle exists
            Top = -32000;
            Width = 100;
            Height = 100;

            _canvas.Width = img.PixelWidth;
            _canvas.Height = img.PixelHeight;
            var bg = new Image { Source = img, Width = img.PixelWidth, Height = img.PixelHeight, Stretch = Stretch.Fill };
            RenderOptions.SetBitmapScalingMode(bg, BitmapScalingMode.NearestNeighbor);
            _canvas.Children.Add(bg);
            _dim.Data = new RectangleGeometry(new Rect(0, 0, img.PixelWidth, img.PixelHeight));
            if (mode == SelectMode.Region) _canvas.Children.Add(_dim);
            _frame.Stroke = new SolidColorBrush(Color.FromRgb(0x4D, 0x9F, 0xFF));
            _frame.StrokeThickness = 1.5 * _s;
            _canvas.Children.Add(_frame);
            _labelText.FontSize = 12 * _s;
            _label.Padding = new Thickness(6 * _s, 2 * _s, 6 * _s, 2 * _s);
            _label.CornerRadius = new CornerRadius(6 * _s);
            _label.Child = _labelText;
            _canvas.Children.Add(_label);

            var loupeStack = new StackPanel();
            var lens = new Grid { Width = 132 * _s, Height = 132 * _s };
            RenderOptions.SetBitmapScalingMode(_loupeImage, BitmapScalingMode.NearestNeighbor);
            lens.Children.Add(_loupeImage);
            lens.Children.Add(new Rectangle { Width = 12 * _s, Height = 12 * _s, Stroke = Brushes.White, StrokeThickness = 1.5 * _s, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            loupeStack.Children.Add(lens);
            _loupeText.FontSize = 13 * _s;
            _loupeText.Margin = new Thickness(0, 3 * _s, 0, 3 * _s);
            loupeStack.Children.Add(_loupeText);
            _loupe.Child = loupeStack;
            _loupe.BorderThickness = new Thickness(1.5 * _s);
            _loupe.CornerRadius = new CornerRadius(8 * _s);
            _canvas.Children.Add(_loupe);

            Content = new Viewbox { Stretch = Stretch.Fill, Child = _canvas };
            SourceInitialized += (_, _) => Place();
            DpiChanged += (_, _) => Dispatcher.BeginInvoke(Place);
            MouseLeftButtonDown += OnDown;
            MouseMove += OnMove;
            MouseLeftButtonUp += OnUp;
            MouseRightButtonUp += (_, _) => Cancel();
            KeyDown += OnKey;
            Closed += (_, _) => { if (!_closing) Cancel(); };
            Deactivated += (_, _) => { /* other monitors' overlays may take focus; nothing to do */ };
        }

        void Place()
        {
            var h = new WindowInteropHelper(this).Handle;
            Native.SetWindowPos(h, Native.HWND_TOPMOST, Monitor.Left, Monitor.Top, Monitor.Width, Monitor.Height, Native.SWP_SHOWWINDOW);
        }

        public void Grab()
        {
            Activate();
            Focus();
            Keyboard.Focus(this);
            if (_mode == SelectMode.Color) UpdateLoupe(Mouse.GetPosition(_canvas));
            else HoverWindow(Mouse.GetPosition(_canvas));
        }

        void OnDown(object sender, MouseButtonEventArgs e)
        {
            if (_mode == SelectMode.Color) { PickAt(e.GetPosition(_canvas)); return; }
            _start = e.GetPosition(_canvas);
            CaptureMouse();
        }

        void OnMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(_canvas);
            if (_mode == SelectMode.Color) { UpdateLoupe(p); return; }
            if (_start is not { } s) { HoverWindow(p); return; }
            SetSelection(PxRect.FromPoints((int)s.X, (int)s.Y, (int)p.X, (int)p.Y));
        }

        void OnUp(object sender, MouseButtonEventArgs e)
        {
            if (_mode != SelectMode.Region || _start is not { } s) return;
            ReleaseMouseCapture();
            var p = e.GetPosition(_canvas);
            var dragged = Math.Abs(p.X - s.X) > 4 * _s || Math.Abs(p.Y - s.Y) > 4 * _s;
            _start = null;
            if (!dragged) HoverWindow(p); // a click picks the window under the cursor (or the whole monitor)
            Complete(_current);
        }

        void OnKey(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Cancel();
            else if (e.Key == Key.Enter && _mode == SelectMode.Region) Complete(new PxRect(0, 0, _img.PixelWidth, _img.PixelHeight));
        }

        /// <summary>Highlights the top-most window under the cursor (clipped to this monitor), else the whole monitor.</summary>
        void HoverWindow(Point p)
        {
            int gx = Monitor.Left + (int)p.X, gy = Monitor.Top + (int)p.Y;
            var mon = new PxRect(Monitor.Left, Monitor.Top, Monitor.Width, Monitor.Height);
            var hit = _windows.FirstOrDefault(w => w.Contains(gx, gy));
            var r = hit.IsEmpty ? mon : hit.Intersect(mon);
            if (r.IsEmpty) r = mon;
            SetSelection(new PxRect(r.X - Monitor.Left, r.Y - Monitor.Top, r.Width, r.Height));
        }

        void SetSelection(PxRect r)
        {
            r = r.Intersect(new PxRect(0, 0, _img.PixelWidth, _img.PixelHeight));
            _current = r;
            var full = new RectangleGeometry(new Rect(0, 0, _img.PixelWidth, _img.PixelHeight));
            var hole = new RectangleGeometry(new Rect(r.X, r.Y, r.Width, r.Height));
            _dim.Data = new CombinedGeometry(GeometryCombineMode.Exclude, full, hole);
            _frame.Visibility = r.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
            Canvas.SetLeft(_frame, r.X);
            Canvas.SetTop(_frame, r.Y);
            _frame.Width = Math.Max(1, r.Width);
            _frame.Height = Math.Max(1, r.Height);
            _labelText.Text = $"{r.Width} × {r.Height}";
            _label.Visibility = r.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
            Canvas.SetLeft(_label, r.X);
            Canvas.SetTop(_label, r.Y > 28 * _s ? r.Y - 26 * _s : r.Y + 4 * _s);
        }

        void UpdateLoupe(Point p)
        {
            int x = (int)p.X, y = (int)p.Y;
            if (x < 0 || y < 0 || x >= _img.PixelWidth || y >= _img.PixelHeight) { _loupe.Visibility = Visibility.Collapsed; return; }
            const int R = 5; // 11 × 11 pixels magnified
            int cx = Math.Clamp(x - R, 0, Math.Max(0, _img.PixelWidth - (2 * R + 1))), cy = Math.Clamp(y - R, 0, Math.Max(0, _img.PixelHeight - (2 * R + 1)));
            _loupeImage.Source = new CroppedBitmap(_img, new Int32Rect(cx, cy, Math.Min(2 * R + 1, _img.PixelWidth), Math.Min(2 * R + 1, _img.PixelHeight)));
            var c = ColorAt(x, y);
            _loupeText.Text = ColorText.Hex(c) + "  " + $"{c.R}, {c.G}, {c.B}";
            _loupe.Visibility = Visibility.Visible;
            var lx = x + 24 * _s;
            var ly = y + 24 * _s;
            if (lx + 140 * _s > _img.PixelWidth) lx = x - 160 * _s;
            if (ly + 170 * _s > _img.PixelHeight) ly = y - 190 * _s;
            Canvas.SetLeft(_loupe, lx);
            Canvas.SetTop(_loupe, ly);
        }

        Color ColorAt(int x, int y)
        {
            var i = (y * _img.PixelWidth + x) * 4;
            return Color.FromRgb(_pixels[i + 2], _pixels[i + 1], _pixels[i]);
        }

        void PickAt(Point p)
        {
            int x = (int)p.X, y = (int)p.Y;
            if (x < 0 || y < 0 || x >= _img.PixelWidth || y >= _img.PixelHeight) return;
            var c = ColorAt(x, y);
            _done(new Selection(_img, new PxRect(Monitor.Left + x, Monitor.Top + y, 1, 1), Monitor, c));
        }

        void Complete(PxRect r)
        {
            r = r.Intersect(new PxRect(0, 0, _img.PixelWidth, _img.PixelHeight));
            if (r.Width < 2 || r.Height < 2) { Cancel(); return; }
            var crop = ScreenGrab.Crop(_img, new Int32Rect(r.X, r.Y, r.Width, r.Height));
            _done(new Selection(crop, new PxRect(Monitor.Left + r.X, Monitor.Top + r.Y, r.Width, r.Height), Monitor));
        }

        void Cancel() => _done(null);

        public void CloseQuietly()
        {
            if (_closing) return;
            _closing = true;
            try { Close(); } catch { }
        }
    }
}

public static class ColorText
{
    public static string Hex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
}
