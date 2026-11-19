using System.Windows;
using System.Windows.Media;
using Isle.Core;

namespace Isle;

// Shape animation: hand-written springs stepped on CompositionTarget.Rendering (subscribed only while moving).
public partial class IslandWindow
{
    // expand: ζ≈0.78 (≈2% overshoot, settles in ~400 ms); collapse: ζ≈0.9, stiffer (~260 ms)
    const double ExpandK = 170, ExpandC = 20.5, CollapseK = 290, CollapseC = 30.6;

    Spring _w = new(120), _h = new(32), _e = new(0), _shift = new(0);
    double _pillW = 120, _pillH = 32;
    bool _expanded, _edgeHidden, _animating;
    TimeSpan _lastFrame;

    readonly RectangleGeometry _shape = new();
    readonly RectangleGeometry _shadow1 = new(), _shadow2 = new(), _shadow3 = new();

    public bool IsExpanded => _expanded;

    /// <summary>Raised when the island finishes changing between collapsed and expanded (argument: expanded).</summary>
    public event Action<bool>? ExpandedChanged;

    void InitMotion()
    {
        Body.Clip = _shape;
        Outline.Data = _shape;
        Shadow1.Data = _shadow1;
        Shadow2.Data = _shadow2;
        Shadow3.Data = _shadow3;
        ApplyShape();
    }

    void SetTargets(bool immediate = false)
    {
        _w.Target = _expanded ? ExpandedWidth : _pillW;
        _h.Target = _expanded ? ExpandedHeight : _pillH;
        _e.Target = _expanded ? 1 : 0;
        _shift.Target = _edgeHidden && !_expanded ? -(TopGap + _pillH - 3) : 0;
        if (immediate || _snapshotMode || !SystemParameters.ClientAreaAnimation)
        {
            _w.Snap(); _h.Snap(); _e.Snap(); _shift.Snap();
            StopAnimation();
            ApplyShape();
            return;
        }
        if (!_animating)
        {
            _animating = true;
            _lastFrame = TimeSpan.Zero;
            CompositionTarget.Rendering += OnRendering;
        }
    }

    void StopAnimation()
    {
        if (!_animating) return;
        _animating = false;
        CompositionTarget.Rendering -= OnRendering;
    }

    void OnRendering(object? sender, EventArgs e)
    {
        var now = ((RenderingEventArgs)e).RenderingTime;
        if (now == _lastFrame) return; // WPF can raise Rendering more than once per frame
        var dt = _lastFrame == TimeSpan.Zero ? 1.0 / 60 : (now - _lastFrame).TotalSeconds;
        _lastFrame = now;

        var (k, c) = _expanded ? (ExpandK, ExpandC) : (CollapseK, CollapseC);
        var moving = _w.Step(dt, k, c);
        moving |= _h.Step(dt, k, c);
        moving |= _e.Step(dt, k, c);
        moving |= _shift.Step(dt, CollapseK, CollapseC);
        ApplyShape();
        if (!moving) StopAnimation();
    }

    /// <summary>Pushes the current spring values into the clip, shadow, and content opacities. No layout involved.</summary>
    void ApplyShape()
    {
        double w = Math.Max(8, _w.Value), h = Math.Max(8, _h.Value);
        var r = Math.Min(Math.Min(h / 2, w / 2), MaxRadius);
        var rect = new Rect(BaseWidth / 2 - w / 2, TopGap, w, h);
        _shape.Rect = rect;
        _shape.RadiusX = _shape.RadiusY = r;

        var p = Math.Clamp(_e.Value, 0, 1);
        var spread = 0.7 + 0.9 * p; // bigger, softer shadow when expanded
        SetShadow(_shadow1, rect, r, 1.0 * spread, 1.0 * spread);
        SetShadow(_shadow2, rect, r, 2.5 * spread, 2.0 * spread);
        SetShadow(_shadow3, rect, r, 4.5 * spread, 3.5 * spread);

        StageShift.Y = _shift.Value;

        // content: pill fades out first, panel fades/scales in a little after the shape starts moving
        var pillOpacity = 1 - Math.Clamp(p * 3.2, 0, 1);
        var panelOpacity = Smooth(Math.Clamp((p - 0.25) / 0.6, 0, 1));
        PillLayer.Opacity = pillOpacity;
        PillLayer.Visibility = pillOpacity > 0.01 ? Visibility.Visible : Visibility.Hidden;
        Panel.Opacity = panelOpacity;
        var panelVisible = panelOpacity > 0.01;
        if (panelVisible != (Panel.Visibility == Visibility.Visible))
            Panel.Visibility = panelVisible ? Visibility.Visible : Visibility.Collapsed;
        var scale = 0.95 + 0.05 * panelOpacity;
        PanelScale.ScaleX = PanelScale.ScaleY = scale;
    }

    /// <summary>Attention pulse: the shape bounces <paramref name="times"/> times (finished timer, reminder).</summary>
    public void Pulse(int times = 3)
    {
        if (_snapshotMode || !SystemParameters.ClientAreaAnimation) return;
        for (var i = 0; i < times; i++)
        {
            var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30 + 420 * i) };
            t.Tick += (_, _) =>
            {
                t.Stop();
                _w.Velocity += 260;
                _h.Velocity += 70;
                SetTargets();
            };
            t.Start();
        }
    }

    static void SetShadow(RectangleGeometry g, Rect rect, double r, double inflate, double dy)
    {
        var s = rect;
        s.Inflate(inflate, inflate);
        s.Offset(0, dy);
        g.Rect = s;
        g.RadiusX = g.RadiusY = r + inflate;
    }

    static double Smooth(double t) => t * t * (3 - 2 * t);
}
