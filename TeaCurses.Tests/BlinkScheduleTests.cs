using System;
using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class BlinkScheduleTests
{
    [Fact]
    public void Intensity_1_only_allows_always_visible()
    {
        var rng = new Random(1);
        for (var n = 0; n < 20; n++)
        {
            var s = BlinkSchedule.Roll(1, rng);
            Assert.Equal(1, s.WindowBeats);
            Assert.Equal(1, s.VisibleBeats);
            Assert.True(s.IsVisible(0));
            Assert.True(s.IsVisible(7));
        }
    }

    [Fact]
    public void Roll_window_never_exceeds_intensity()
    {
        var rng = new Random(42);
        for (var i = 1; i <= 10; i++)
        {
            for (var n = 0; n < 50; n++)
            {
                var s = BlinkSchedule.Roll(i, rng);
                Assert.InRange(s.WindowBeats, 1, i);
                Assert.InRange(s.VisibleBeats, 1, s.WindowBeats);
            }
        }
    }

    [Fact]
    public void Create_marks_exactly_V_phases_visible_per_window()
    {
        var s = BlinkSchedule.Create(2, 5, new Random(99));
        var count = 0;
        for (var p = 0; p < 5; p++)
        {
            if (s.IsVisible(p))
                count++;
        }

        Assert.Equal(2, count);
    }

    [Fact]
    public void Pattern_repeats_across_windows()
    {
        var s = BlinkSchedule.CreateFixedVisible(1, 3);
        Assert.True(s.IsVisible(0));
        Assert.False(s.IsVisible(1));
        Assert.False(s.IsVisible(2));
        Assert.True(s.IsVisible(3));
        Assert.False(s.IsVisible(4));
    }

    [Fact]
    public void Intensity_10_can_roll_one_in_ten()
    {
        var found = false;
        for (var seed = 0; seed < 500 && !found; seed++)
        {
            var s = BlinkSchedule.Roll(10, new Random(seed));
            if (s.WindowBeats == 10 && s.VisibleBeats == 1)
                found = true;
        }

        Assert.True(found);
    }
}
