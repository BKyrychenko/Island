using Skerry.Modules.Characters;
using Skerry.Views.Characters;
using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace Skerry.Tests;

public class CharacterTests
{
    [Fact]
    public void Character_cards_are_focusable_and_use_button_activation()
    {
        FakeSink.Run(_ =>
        {
            string? picked = null;
            var gallery = new CharacterGallery(new GalleryActions(id => picked = id, _ => { }, _ => { }, () => { }), null);
            gallery.Show("pebb", false, 5);
            var content = Assert.IsType<StackPanel>(Assert.Single(gallery.Children.Cast<UIElement>()));
            var cards = Assert.IsType<StackPanel>(content.Children[1]);
            Assert.Equal(3, cards.Children.Count);
            foreach (var card in cards.Children.Cast<Button>())
            {
                Assert.True(card.Focusable);
                card.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(card.Tag, picked);
            }
        });
    }

    sealed class FakeClock
    {
        public DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public void MoodPriorities()
    {
        var c = new FakeClock();
        var b = new CreatureBrain(() => c.Now) { SleepAfter = TimeSpan.FromMinutes(5) };
        Assert.Equal(Mood.Awake, b.MoodFor(TimeSpan.Zero));
        b.SetMusic(true);
        Assert.Equal(Mood.Dancing, b.MoodFor(TimeSpan.FromMinutes(1)));
        Assert.Equal(Mood.Sleeping, b.MoodFor(TimeSpan.FromMinutes(5))); // asleep even with music
        b.Cheer();
        Assert.Equal(Mood.Happy, b.MoodFor(TimeSpan.FromMinutes(30))); // a finished timer wakes it up
        c.Now += TimeSpan.FromSeconds(4);
        Assert.Equal(Mood.Sleeping, b.MoodFor(TimeSpan.FromMinutes(30)));
        Assert.Equal(Mood.Dancing, b.MoodFor(TimeSpan.FromSeconds(1))); // input → awake again
        b.SetMusic(false);
        Assert.Equal(Mood.Awake, b.MoodFor(TimeSpan.Zero));
    }

    [Fact]
    public void NeverSleepsWhenDisabled()
    {
        var b = new CreatureBrain { SleepAfter = TimeSpan.Zero };
        Assert.Equal(Mood.Awake, b.MoodFor(TimeSpan.FromHours(10)));
    }

    [Fact]
    public void PokedOnlyOnRealChanges()
    {
        var b = new CreatureBrain();
        var pokes = 0;
        b.Poked += () => pokes++;
        b.SetMusic(false);
        b.SetMusic(true);
        b.SetMusic(true);
        b.Cheer();
        Assert.Equal(2, pokes);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1000, 0, 1.5, 0)]
    [InlineData(0, -1000, 0, -1.5)]
    [InlineData(130, 0, 0.75, 0)] // half of the reach → half of the offset
    public void EyesFollowTheCursor(double dx, double dy, double ex, double ey)
    {
        var v = CreatureBrain.EyeOffset(dx, dy, 1.5);
        Assert.Equal(ex, v.X, 3);
        Assert.Equal(ey, v.Y, 3);
        Assert.True(v.Length <= 1.5 + 1e-9);
    }

    [Fact]
    public void MotionStaysInRange()
    {
        for (var t = 0.0; t < 10; t += 0.013)
        {
            Assert.InRange(CreatureBrain.Bounce(t), 0, 1);
            Assert.InRange(CreatureBrain.Hop(t), 0, 1);
            Assert.InRange(CreatureBrain.Sway(t), -7, 7);
            Assert.InRange(CreatureBrain.Breath(t, false), 0.97, 1.03);
            Assert.InRange(CreatureBrain.Breath(t, true), 0.95, 1.05);
        }
    }

    [Fact]
    public void BlinkClosesThenOpens()
    {
        Assert.Equal(1, CreatureBrain.Lid(-1));
        Assert.Equal(1, CreatureBrain.Lid(0));
        Assert.Equal(0, CreatureBrain.Lid(0.09), 3);
        Assert.Equal(1, CreatureBrain.Lid(0.18));
        Assert.Equal(1, CreatureBrain.Lid(5));
    }

    [Fact]
    public void FrameRateNeverAbove30AndSlowWhenCalm()
    {
        foreach (var m in Enum.GetValues<Mood>())
            foreach (var moving in new[] { false, true })
                foreach (var blink in new[] { false, true })
                    Assert.InRange(CreatureBrain.FrameRate(m, moving, blink), 1, 30);
        Assert.Equal(8, CreatureBrain.FrameRate(Mood.Awake, false, false));
        Assert.Equal(4, CreatureBrain.FrameRate(Mood.Sleeping, true, true));
    }

    [Fact]
    public void ThreeDistinctDesigns()
    {
        Assert.Equal(3, CreatureDesign.All.Count);
        Assert.Equal(3, CreatureDesign.All.Select(d => d.Id).Distinct().Count());
        Assert.Same(CreatureDesign.Pebb, CreatureDesign.ById("nope"));
        Assert.Same(CreatureDesign.Kubo, CreatureDesign.ById("kubo"));
        foreach (var d in CreatureDesign.All)
        {
            Assert.True(d.Body.IsFrozen);
            Assert.InRange(d.Body.Bounds.Left, 0, 40);
            Assert.InRange(d.Body.Bounds.Bottom, 30, 40);
            Assert.NotEqual(Skerry.Core.Strings.T(d.NameKey), d.NameKey);
        }
    }
}
