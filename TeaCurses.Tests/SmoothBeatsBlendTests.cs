using System;
using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class SmoothBeatsBlendTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(99)]
    public void Evaluate_stays_in_unit_interval(int intensity)
    {
        for (var beat = 0f; beat < 64f; beat += 0.25f)
        {
            var b = SmoothBeatsBlend.Evaluate(intensity, beat);
            Assert.InRange(b, 0f, 1f);
        }
    }

    [Fact]
    public void Intensity_1_matches_single_sine()
    {
        var omega = SmoothBeatsBlend.AngularFrequency(1);
        for (var beat = 0f; beat < 32f; beat += 0.5f)
        {
            var expected = ((float)Math.Sin(omega * beat) + 1f) * 0.5f;
            Assert.Equal(expected, SmoothBeatsBlend.Evaluate(1, beat), 5);
        }
    }

    [Fact]
    public void Intensity_2_differs_from_intensity_1_at_same_beat()
    {
        // Pick a beat where 2nd harmonic pulls the average away from the fundamental alone.
        var beat = 1.7f;
        var a = SmoothBeatsBlend.Evaluate(1, beat);
        var b = SmoothBeatsBlend.Evaluate(2, beat);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void AngularFrequency_scales_with_clamped_intensity()
    {
        var w1 = SmoothBeatsBlend.AngularFrequency(1);
        var w5 = SmoothBeatsBlend.AngularFrequency(5);
        var w99 = SmoothBeatsBlend.AngularFrequency(99);
        Assert.Equal(w1 * 5f, w5, 5);
        Assert.Equal(SmoothBeatsBlend.AngularFrequency(10), w99, 5);
    }

    [Fact]
    public void LerpFactor_endpoints()
    {
        Assert.Equal(0.2f, SmoothBeatsBlend.LerpFactor(0.2f, 0.8f, 0f));
        Assert.Equal(0.8f, SmoothBeatsBlend.LerpFactor(0.2f, 0.8f, 1f));
        Assert.Equal(0.5f, SmoothBeatsBlend.LerpFactor(0.2f, 0.8f, 0.5f), 5);
    }

    [Fact]
    public void ClampIntensity_bounds()
    {
        Assert.Equal(1, SmoothBeatsBlend.ClampIntensity(0));
        Assert.Equal(10, SmoothBeatsBlend.ClampIntensity(100));
        Assert.Equal(7, SmoothBeatsBlend.ClampIntensity(7));
    }
}
