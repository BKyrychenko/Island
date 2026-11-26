using System.Windows;

namespace Skerry.Modules.Characters;

public enum Mood { Awake, Dancing, Happy, Sleeping }

/// <summary>
/// What a character is doing, shared by every visible creature view. Pure: no UI, injectable clock.
/// Priority: happy (timer finished, 3 s) > sleeping (no user input for <see cref="SleepAfter"/>) > dancing (music) > awake.
/// </summary>
public sealed class CreatureBrain(Func<DateTimeOffset>? clock = null)
{
    public static readonly TimeSpan HappyFor = TimeSpan.FromSeconds(3.2);
    readonly Func<DateTimeOffset> _now = clock ?? (() => DateTimeOffset.Now);
    DateTimeOffset _happyUntil;

    public bool MusicPlaying { get; set; }
    /// <summary>Zero = never sleeps.</summary>
    public TimeSpan SleepAfter { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Raised when something changes that should wake a slow-ticking view at once.</summary>
    public event Action? Poked;

    public void Cheer()
    {
        _happyUntil = _now() + HappyFor;
        Poked?.Invoke();
    }

    public void SetMusic(bool playing)
    {
        if (MusicPlaying == playing) return;
        MusicPlaying = playing;
        Poked?.Invoke();
    }

    public Mood MoodFor(TimeSpan userIdle)
    {
        if (_now() < _happyUntil) return Mood.Happy;
        if (SleepAfter > TimeSpan.Zero && userIdle >= SleepAfter) return Mood.Sleeping;
        return MusicPlaying ? Mood.Dancing : Mood.Awake;
    }

    // ---- motion math (seconds in, design units / factors out) ----

    /// <summary>Pupil offset toward the cursor: direction of (dx, dy), length grows with distance up to <paramref name="max"/>.</summary>
    public static Vector EyeOffset(double dx, double dy, double max, double reach = 260)
    {
        var d = Math.Sqrt(dx * dx + dy * dy);
        if (d < 1 || double.IsNaN(d)) return default;
        var k = Math.Min(1, d / reach) * max;
        return new Vector(dx / d * k, dy / d * k);
    }

    /// <summary>Vertical breathing scale around 1 (slower and deeper while asleep).</summary>
    public static double Breath(double t, bool asleep) =>
        1 + (asleep ? 0.045 : 0.025) * Math.Sin(2 * Math.PI * t / (asleep ? 4.2 : 3.0));

    /// <summary>Dance bounce height 0..1 at ~116 bpm (tempo-agnostic: one hop per beat).</summary>
    public static double Bounce(double t, double bpm = 116) => Math.Abs(Math.Sin(Math.PI * t * bpm / 60));

    /// <summary>Dance tilt in degrees, swaying once every two beats.</summary>
    public static double Sway(double t, double bpm = 116) => 7 * Math.Sin(Math.PI * t * bpm / 120);

    /// <summary>Happy: three quick hops, 0..1.</summary>
    public static double Hop(double t) => Math.Abs(Math.Sin(Math.PI * t * 3.2));

    /// <summary>Eyelid openness 1..0..1 during a blink that started <paramref name="since"/> seconds ago (lasts 0.18 s).</summary>
    public static double Lid(double since)
    {
        const double dur = 0.18;
        if (since < 0 || since >= dur) return 1;
        var x = since / dur; // 0..1
        return Math.Abs(1 - 2 * x);
    }

    /// <summary>Frames per second the view needs for a mood (≤ 30; slow while only breathing).</summary>
    public static int FrameRate(Mood mood, bool eyesMoving, bool blinking) => mood switch
    {
        Mood.Dancing or Mood.Happy => 30,
        Mood.Sleeping => 4,
        _ => eyesMoving || blinking ? 30 : 8,
    };
}
