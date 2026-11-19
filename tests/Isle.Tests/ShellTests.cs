using System.Windows;
using Isle;
using Isle.Core;
using Isle.Modules.Hotkeys;

namespace Isle.Tests;

public class SpringTests
{
    [Fact]
    public void Expand_spring_settles_with_small_overshoot()
    {
        var s = new Spring(120) { Target = 520 };
        double max = 0, t = 0, perceived = -1;
        while (s.Step(1.0 / 60, 170, 20.5) && t < 3)
        {
            t += 1.0 / 60;
            max = Math.Max(max, s.Value);
            if (Math.Abs(s.Value - 520) > 4) perceived = -1;      // still visibly moving (>1% of travel)
            else if (perceived < 0) perceived = t;
        }
        Assert.Equal(520, s.Value);
        Assert.InRange(perceived, 0.3, 0.5);    // ~350–450 ms feel
        Assert.True(t < 1.0);                   // render hook released within a second
        Assert.InRange(max, 520.5, 540);        // a slight overshoot, not a wobble
    }

    [Fact]
    public void Collapse_is_faster_than_expand()
    {
        static double Time(double k, double c)
        {
            var s = new Spring(520) { Target = 120 };
            double t = 0;
            while (s.Step(1.0 / 60, k, c)) t += 1.0 / 60;
            return t;
        }
        Assert.True(Time(290, 30.6) < Time(170, 20.5));
    }

    [Fact]
    public void Large_dt_stays_stable()
    {
        var s = new Spring(0) { Target = 100 };
        for (var i = 0; i < 40; i++) s.Step(0.5, 290, 30.6); // stalled frames
        Assert.Equal(100, s.Value);
    }
}

public class PillQueueTests
{
    static PillItem Item(string id, int prio, bool compact = false, bool banner = false) => new()
    {
        Id = id, Priority = prio, View = () => new FrameworkElement(),
        Compact = compact ? () => new FrameworkElement() : null, IsBanner = banner,
    };

    [Fact]
    public void Highest_priority_wins_and_runner_up_shares()
    {
        var q = new PillQueue();
        q.Set(Item("idle", PillPriority.Idle));
        q.Set(Item("music", PillPriority.Music, compact: true));
        q.Set(Item("timer", PillPriority.TimerRunning, compact: true));
        Assert.Equal("timer", q.Primary?.Id);
        Assert.Equal("music", q.Secondary?.Id);
        q.Remove("timer");
        Assert.Equal("music", q.Primary?.Id);
        Assert.Null(q.Secondary); // idle never takes the side slot
    }

    [Fact]
    public void Banner_never_shares_and_newest_wins_ties()
    {
        var q = new PillQueue();
        q.Set(Item("music", PillPriority.Music, compact: true));
        q.Set(Item("caps", PillPriority.Transient, banner: true));
        q.Set(Item("layout", PillPriority.Transient, banner: true));
        Assert.Equal("layout", q.Primary?.Id);
        Assert.Null(q.Secondary);
    }

    [Fact]
    public void Changed_fires_only_on_real_change()
    {
        var q = new PillQueue();
        var n = 0;
        q.Changed += () => n++;
        q.Set(Item("music", PillPriority.Music));
        q.Set(Item("idle", PillPriority.Idle)); // lower, no visible change
        Assert.Equal(1, n);
        Assert.False(q.Remove("nope"));
    }
}

public class GestureTests
{
    [Theory]
    [InlineData("Alt+Space", Native.MOD_ALT, 0x20u)]
    [InlineData("Win+Alt+N", Native.MOD_WIN | Native.MOD_ALT, (uint)'N')]
    [InlineData("ctrl + shift + f5", Native.MOD_CONTROL | Native.MOD_SHIFT, 0x74u)]
    [InlineData("Win+Alt+Space", Native.MOD_WIN | Native.MOD_ALT, 0x20u)]
    public void Parses(string text, uint mods, uint key)
    {
        Assert.True(Gesture.TryParse(text, out var g));
        Assert.Equal(mods, g.Modifiers);
        Assert.Equal(key, g.Key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Alt")]
    [InlineData("Alt+A+B")]
    [InlineData("Alt+Nope")]
    public void Rejects(string text) => Assert.False(Gesture.TryParse(text, out _));

    [Fact]
    public void Roundtrips_to_canonical_text()
    {
        Assert.True(Gesture.TryParse("alt+win+c", out var g));
        Assert.Equal("Win+Alt+C", g.ToString());
    }
}

public class ExcludedAppTests
{
    [Theory]
    [InlineData("obs64.exe", "obs64", true)]
    [InlineData(" Code ", "code", true)]
    [InlineData("chrome", "chromium", false)]
    [InlineData(".exe", "", false)]
    public void Matches_process_names(string listed, string process, bool expected) =>
        Assert.Equal(expected, IslandWindow.IsSameApp(listed, process));
}

public class ScreenSelectionTests
{
    [Fact]
    public void Device_selection_survives_monitor_reordering_and_falls_back_to_primary()
    {
        var primary = new ScreenInfo((IntPtr)1, 0, 0, 1920, 1080, true, 1, "DISPLAY1");
        var secondary = new ScreenInfo((IntPtr)2, 1920, 0, 1920, 1080, false, 1.5, "DISPLAY2");
        Assert.Equal(secondary, Screens.Resolve([primary, secondary], 0, "display2"));
        Assert.Equal(primary, Screens.Resolve([primary], 1, "DISPLAY2"));
        Assert.Equal(secondary, Screens.Resolve([primary, secondary], 1, ""));
    }
}
