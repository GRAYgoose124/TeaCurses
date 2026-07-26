using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class VanishingPointFadeTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    [InlineData(99, 10)]
    public void ClampIntensity_bounds_1_to_10(int raw, int expected)
    {
        Assert.Equal(expected, VanishingPointFade.ClampIntensity(raw));
    }

    [Theory]
    [InlineData(1, 2f)]
    [InlineData(5, 6f)]
    [InlineData(10, 11f)]
    public void FadeStart_is_one_plus_intensity(int intensity, float expected)
    {
        Assert.Equal(expected, VanishingPointFade.FadeStart(intensity));
    }

    [Fact]
    public void DistanceBeats_is_next_minus_current()
    {
        Assert.Equal(4.5f, VanishingPointFade.DistanceBeats(10f, 5.5f), 3);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void DistanceBeats_non_finite_next_is_positive_infinity(float next)
    {
        Assert.Equal(float.PositiveInfinity, VanishingPointFade.DistanceBeats(next, 3f));
    }

    [Fact]
    public void Alpha_high_intensity_is_zero_at_or_below_one_beat()
    {
        // I >= 6: fully invisible from 1 beat before through the action row
        Assert.Equal(0f, VanishingPointFade.Alpha(1f, 6));
        Assert.Equal(0f, VanishingPointFade.Alpha(0f, 6));
        Assert.Equal(0f, VanishingPointFade.Alpha(-1f, 10));
    }

    [Fact]
    public void Alpha_intensity_below_6_keeps_action_row_visible()
    {
        Assert.Equal(VanishingPointFade.VisibilityFloor, VanishingPointFade.Alpha(0f, 5), 3);
        Assert.Equal(VanishingPointFade.VisibilityFloor, VanishingPointFade.Alpha(-0.5f, 3), 3);
        // Row before action may still go invisible at I=3..5
        Assert.Equal(0f, VanishingPointFade.Alpha(1f, 5));
    }

    [Fact]
    public void Alpha_intensity_below_3_keeps_action_row_and_row_before_visible()
    {
        Assert.Equal(VanishingPointFade.VisibilityFloor, VanishingPointFade.Alpha(1f, 2), 3);
        Assert.Equal(VanishingPointFade.VisibilityFloor, VanishingPointFade.Alpha(0f, 1), 3);
        Assert.Equal(VanishingPointFade.VisibilityFloor, VanishingPointFade.Alpha(-1f, 2), 3);
    }

    [Fact]
    public void Alpha_is_one_at_or_above_fade_start()
    {
        Assert.Equal(1f, VanishingPointFade.Alpha(6f, 5));
        Assert.Equal(1f, VanishingPointFade.Alpha(20f, 5));
        Assert.Equal(1f, VanishingPointFade.Alpha(float.PositiveInfinity, 5));
    }

    [Fact]
    public void Alpha_is_linear_between_invisible_and_fade_start()
    {
        // I=5 → fadeStart=6; midpoint d=3.5 → (3.5-1)/(6-1) = 0.5
        Assert.Equal(0.5f, VanishingPointFade.Alpha(3.5f, 5), 3);
    }

    [Fact]
    public void Alpha_intensity_one_fades_between_1_and_2_with_floor_at_one()
    {
        Assert.Equal(1f, VanishingPointFade.Alpha(2f, 1));
        Assert.Equal(0.5f, VanishingPointFade.Alpha(1.5f, 1), 3);
        Assert.Equal(VanishingPointFade.VisibilityFloor, VanishingPointFade.Alpha(1f, 1), 3);
    }

    [Fact]
    public void OpacityRgba_is_white_with_clamped_alpha()
    {
        VanishingPointFade.OpacityRgba(0.4f, out var r, out var g, out var b, out var a);
        Assert.Equal(1f, r);
        Assert.Equal(1f, g);
        Assert.Equal(1f, b);
        Assert.Equal(0.4f, a, 3);

        VanishingPointFade.OpacityRgba(2f, out _, out _, out _, out var high);
        Assert.Equal(1f, high);
        VanishingPointFade.OpacityRgba(-1f, out _, out _, out _, out var low);
        Assert.Equal(0f, low);
    }
}
