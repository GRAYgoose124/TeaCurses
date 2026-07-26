using TeaCurses;
using Xunit;

namespace TeaCurses.Tests;

public class HalfWindowRulesTests
{
    [Theory]
    [InlineData(0f, true, false)]
    [InlineData(0.5f, true, false)]
    [InlineData(1f, false, true)]
    [InlineData(2f, false, true)]
    public void Intensity_maps_suppress_flags(float intensity, bool suppressLate, bool suppressEarly)
    {
        Assert.Equal(suppressLate, HalfWindowRules.SuppressLate(intensity));
        Assert.Equal(suppressEarly, HalfWindowRules.SuppressEarly(intensity));
    }

    [Fact]
    public void EffectiveWindow_zeros_when_suppressed()
    {
        Assert.Equal(0f, HalfWindowRules.EffectiveWindow(0.12f, suppress: true));
        Assert.Equal(0.12f, HalfWindowRules.EffectiveWindow(0.12f, suppress: false));
    }

    [Fact]
    public void Safe_rating_on_beat_with_zero_window_is_true_flawless()
    {
        Assert.True(HalfWindowRules.TrySafeRatingPercent(
            0f, beforeWindow: 0f, afterWindow: 0.1f,
            out var percent, out var timing));
        Assert.Equal(100, percent);
        Assert.Equal(HalfWindowTiming.TrueFlawless, timing);
    }

    [Fact]
    public void Safe_rating_nonzero_diff_with_zero_window_is_miss_floor()
    {
        Assert.True(HalfWindowRules.TrySafeRatingPercent(
            0.05f, beforeWindow: 0.1f, afterWindow: 0f,
            out var percent, out var timing));
        Assert.Equal(0, percent);
        Assert.Equal(HalfWindowTiming.Late, timing);
    }

    [Fact]
    public void Safe_rating_defers_when_active_window_nonzero()
    {
        Assert.False(HalfWindowRules.TrySafeRatingPercent(
            -0.01f, beforeWindow: 0.1f, afterWindow: 0f,
            out _, out _));
    }

    [Theory]
    [InlineData(0f, 0.2f, 0f, true)]   // on-beat, no late
    [InlineData(-0.1f, 0.2f, 0f, true)] // early, no late
    [InlineData(0.1f, 0.2f, 0f, false)] // late rejected when after=0
    [InlineData(0f, 0f, 0.2f, true)]    // on-beat, no early
    [InlineData(0.1f, 0f, 0.2f, true)]  // late, no early
    [InlineData(-0.1f, 0f, 0.2f, false)] // early rejected when before=0
    public void Enemy_hit_window_matches_stock_acceptance(
        float beatDiff, float beforeBeats, float afterBeats, bool accepted)
    {
        Assert.Equal(
            accepted,
            HalfWindowRules.IsWithinEnemyHitWindow(beatDiff, beforeBeats, afterBeats));
    }

    [Fact]
    public void Effective_pair_zeros_suppressed_half_for_intensity_0()
    {
        HalfWindowRules.EffectivePair(
            stockBefore: 0.25f, stockAfter: 0.25f, intensity: 0f,
            out var before, out var after);
        Assert.Equal(0.25f, before);
        Assert.Equal(0f, after);
    }

    [Fact]
    public void Effective_pair_zeros_suppressed_half_for_intensity_1()
    {
        HalfWindowRules.EffectivePair(
            stockBefore: 0.25f, stockAfter: 0.25f, intensity: 1f,
            out var before, out var after);
        Assert.Equal(0f, before);
        Assert.Equal(0.25f, after);
    }
}
