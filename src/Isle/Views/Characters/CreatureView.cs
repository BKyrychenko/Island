using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Isle.Modules.Characters;

namespace Isle.Views.Characters;

/// <summary>
/// Draws one character with frozen shapes (no element tree, no storyboards). Its own clock runs only while the view
/// is visible: 30 fps while dancing / hopping / blinking / eyes moving, 8 fps while only breathing, 4 fps asleep.
/// </summary>
public sealed class CreatureView : FrameworkElement
{
    static readonly Stopwatch Clock = Stopwatch.StartNew();
    static readonly Random Rng = new();
    static readonly Dictionary<CreatureDesign, Faces> FaceCache = [];
    const double MaxGaze = 1.5; // design units

    readonly CreatureBrain? _brain;
    readonly DispatcherTimer _timer;
    CreatureDesign _design;
    Mood _mood = Mood.Awake;
    Vector _gaze, _gazeTarget;
    double _t, _blinkStart = -10, _nextBlink = 2, _moodSince;
    bool _still;

    public CreatureView(CreatureDesign design, CreatureBrain? brain)
    {
        _design = design;
        _brain = brain;
        IsHitTestVisible = false;
        _timer = new DispatcherTimer(DispatcherPriority.Render, Dispatcher) { Interval = TimeSpan.FromMilliseconds(125) };
        _timer.Tick += (_, _) => Tick();
        IsVisibleChanged += (_, _) => UpdateClock();
        Loaded += (_, _) => { if (_brain is not null) { _brain.Poked -= Poke; _brain.Poked += Poke; } UpdateClock(); };
        Unloaded += (_, _) => { if (_brain is not null) _brain.Poked -= Poke; _timer.Stop(); };
    }

    public CreatureDesign Design
    {
        get => _design;
        set { _design = value; InvalidateVisual(); }
    }

    /// <summary>Snapshot / gallery-free still frame: fixed mood, time and gaze, no clock.</summary>
    public void Still(Mood mood, double t = 0.4, Vector gaze = default, double blinkSince = -1)
    {
        _still = true;
        _timer.Stop();
        _mood = mood;
        _t = t;
        _moodSince = 0;
        _gaze = gaze;
        _blinkStart = t - blinkSince;
        if (blinkSince < 0) _blinkStart = -10;
        InvalidateVisual();
    }

    void Poke()
    {
        if (_still || !IsVisible) return;
        _timer.Stop();
        Tick();
    }

    void UpdateClock()
    {
        if (_still) return;
        if (IsVisible && _brain is not null) { if (!_timer.IsEnabled) Tick(); }
        else _timer.Stop();
    }

    void Tick()
    {
        if (_brain is null || !IsVisible) { _timer.Stop(); return; }
        _t = Clock.Elapsed.TotalSeconds;
        var mood = _brain.MoodFor(UserIdle());
        if (mood != _mood) { _mood = mood; _moodSince = _t; }

        _gazeTarget = mood is Mood.Awake or Mood.Dancing ? GazeTarget() : default;
        var moving = (_gazeTarget - _gaze).Length > 0.04;
        _gaze = moving ? _gaze + (_gazeTarget - _gaze) * 0.35 : _gazeTarget;

        if (mood is Mood.Awake or Mood.Dancing && _t >= _nextBlink)
        {
            _blinkStart = _t;
            _nextBlink = _t + 2.5 + Rng.NextDouble() * 3.5;
        }
        var blinking = _t - _blinkStart < 0.2;

        InvalidateVisual();
        var fps = CreatureBrain.FrameRate(mood, moving, blinking);
        // wake up in time for the next blink even at the slow rate
        var ms = 1000.0 / fps;
        if (mood == Mood.Awake && !moving && !blinking) ms = Math.Min(ms, Math.Max(16, (_nextBlink - _t) * 1000));
        _timer.Interval = TimeSpan.FromMilliseconds(ms);
        if (!_timer.IsEnabled) _timer.Start();
    }

    Vector GazeTarget()
    {
        try
        {
            if (PresentationSource.FromVisual(this) is null || !Native.GetCursorPos(out var p)) return default;
            var c = PointToScreen(new Point(ActualWidth / 2, ActualHeight / 2)); // physical pixels, like GetCursorPos
            return CreatureBrain.EyeOffset(p.X - c.X, p.Y - c.Y, MaxGaze);
        }
        catch (InvalidOperationException) { return default; }
    }

    static TimeSpan UserIdle()
    {
        var info = new Native.LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<Native.LASTINPUTINFO>() };
        if (!Native.GetLastInputInfo(ref info)) return TimeSpan.Zero;
        return TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount - info.dwTime));
    }

    protected override void OnRender(DrawingContext dc)
    {
        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0) return;
        var d = _design;
        var faces = FacesOf(d);
        var t = _t;

        // body motion (design units / degrees)
        double lift = 0, tilt = 0, squash = CreatureBrain.Breath(t, _mood == Mood.Sleeping);
        switch (_mood)
        {
            case Mood.Dancing:
                var b = CreatureBrain.Bounce(t);
                lift = 3.2 * b;
                tilt = CreatureBrain.Sway(t);
                squash *= 1 - 0.07 * (1 - b); // squash on landing
                break;
            case Mood.Happy:
                lift = 5 * CreatureBrain.Hop(t - _moodSince);
                break;
        }
        var stretch = 1 + (1 - squash) * 0.7;

        dc.PushTransform(new TranslateTransform((ActualWidth - size) / 2, (ActualHeight - size) / 2));
        dc.PushTransform(new ScaleTransform(size / 40, size / 40));

        // ground shadow shrinks while in the air
        dc.DrawEllipse(faces.Shadow, null, new Point(20, 37.6), 10 * (1 - lift / 12), 1.3);

        dc.PushTransform(new TranslateTransform(0, -lift));
        dc.PushTransform(new RotateTransform(tilt, 20, 37));
        dc.PushTransform(new ScaleTransform(stretch, squash, 20, 37));

        if (d.Extra is not null && !d.ExtraOnTop) dc.DrawGeometry(d.ExtraBrush, null, d.Extra);
        dc.DrawGeometry(d.BodyBrush, null, d.Body);
        if (d.Extra is not null && d.ExtraOnTop) dc.DrawGeometry(d.ExtraBrush, null, d.Extra);

        dc.DrawEllipse(d.CheekBrush, null, new Point(20 - d.CheekDx, d.CheekY), 2.3, 1.3);
        dc.DrawEllipse(d.CheekBrush, null, new Point(20 + d.CheekDx, d.CheekY), 2.3, 1.3);

        switch (_mood)
        {
            case Mood.Sleeping:
                dc.DrawGeometry(null, faces.Pen, faces.Closed);
                DrawZ(dc, faces, t);
                break;
            case Mood.Happy:
                dc.DrawGeometry(null, faces.Pen, faces.Joy);
                dc.DrawGeometry(CreatureDesign.EyeBrush, null, faces.Open);
                break;
            default:
                var lid = CreatureBrain.Lid(t - _blinkStart);
                DrawEye(dc, d.LeftEye, d, lid);
                DrawEye(dc, d.RightEye, d, lid);
                dc.DrawGeometry(null, faces.Pen, faces.Smile);
                break;
        }

        dc.Pop(); dc.Pop(); dc.Pop();
        dc.Pop(); dc.Pop();
    }

    void DrawEye(DrawingContext dc, Point at, CreatureDesign d, double lid)
    {
        var c = at + _gaze;
        dc.DrawEllipse(CreatureDesign.EyeBrush, null, c, d.EyeRx, Math.Max(0.35, d.EyeRy * lid));
        if (lid > 0.6) dc.DrawEllipse(CreatureDesign.ShineBrush, null, new Point(c.X + d.EyeRx * 0.35, c.Y - d.EyeRy * 0.4), 0.75, 0.75);
    }

    /// <summary>Two small "z" marks drifting up and fading, a new one every 1.6 s.</summary>
    static void DrawZ(DrawingContext dc, Faces f, double t)
    {
        for (var i = 0; i < 2; i++)
        {
            var phase = (t / 1.6 + i * 0.5) % 1; // 0..1
            dc.PushOpacity(Math.Sin(Math.PI * phase));
            dc.PushTransform(new TranslateTransform(29 + phase * 4, 12 - phase * 8));
            dc.PushTransform(new ScaleTransform(0.8 + phase * 0.5, 0.8 + phase * 0.5));
            dc.DrawGeometry(null, f.ZPen, f.Z);
            dc.Pop(); dc.Pop(); dc.Pop();
        }
    }

    /// <summary>Per-design static face shapes (closed eyes, happy eyes, mouths), built once and frozen.</summary>
    sealed record Faces(Geometry Closed, Geometry Joy, Geometry Open, Geometry Smile, Geometry Z, Pen Pen, Pen ZPen, Brush Shadow);

    static Faces FacesOf(CreatureDesign d)
    {
        if (FaceCache.TryGetValue(d, out var f)) return f;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        string Arc(Point p, double rx, double dy) => string.Format(inv, "M{0},{1} Q{2},{3} {4},{1} ", p.X - rx, p.Y, p.X, p.Y + dy, p.X + rx);
        var mouthY = d.LeftEye.Y + 4.6;
        var pen = new Pen(CreatureDesign.EyeBrush, 1.3) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        pen.Freeze();
        var zPen = new Pen(d.BodyBrush, 1.1) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        zPen.Freeze();
        var shadow = new SolidColorBrush(Color.FromArgb(0x38, 0x80, 0x80, 0x80));
        shadow.Freeze();
        f = new Faces(
            Closed: Geometry.Parse(Arc(d.LeftEye, d.EyeRx, 1.8) + Arc(d.RightEye, d.EyeRx, 1.8)).Frozen(),
            Joy: Geometry.Parse(Arc(d.LeftEye + new Vector(0, 1), d.EyeRx, -3) + Arc(d.RightEye + new Vector(0, 1), d.EyeRx, -3)).Frozen(),
            Open: Geometry.Parse(string.Format(inv, "M{0},{1} Q20,{2} {3},{1} Z", 20 - 2.6, mouthY - 0.6, mouthY + 3.6, 20 + 2.6)).Frozen(),
            Smile: Geometry.Parse(Arc(new Point(20, mouthY), 1.9, 1.5)).Frozen(),
            Z: Geometry.Parse("M0,0 H3 L0,3 H3").Frozen(),
            Pen: pen, ZPen: zPen, Shadow: shadow);
        FaceCache[d] = f;
        return f;
    }
}
