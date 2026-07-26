using TeaCurses;
using Xunit;

namespace TeaCurses.Tests;

public class ImperfectRiftsRulesTests
{
    [Fact]
    public void Perfect_heals_chaos()
    {
        var next = ImperfectRiftsRules.ApplyRating(0.5f, ImperfectRiftsRating.Perfect, 5);
        Assert.True(next < 0.5f);
    }

    [Theory]
    [InlineData(ImperfectRiftsRating.Great)]
    [InlineData(ImperfectRiftsRating.Good)]
    [InlineData(ImperfectRiftsRating.Ok)]
    [InlineData(ImperfectRiftsRating.Miss)]
    public void Non_perfect_damages(ImperfectRiftsRating rating)
    {
        var next = ImperfectRiftsRules.ApplyRating(0.2f, rating, 5);
        Assert.True(next > 0.2f);
    }

    [Fact]
    public void Damage_order_Miss_gt_Ok_gt_Good_gt_Great()
    {
        float great = ImperfectRiftsRules.ApplyRating(0f, ImperfectRiftsRating.Great, 5);
        float good = ImperfectRiftsRules.ApplyRating(0f, ImperfectRiftsRating.Good, 5);
        float ok = ImperfectRiftsRules.ApplyRating(0f, ImperfectRiftsRating.Ok, 5);
        float miss = ImperfectRiftsRules.ApplyRating(0f, ImperfectRiftsRating.Miss, 5);
        Assert.True(miss > ok && ok > good && good > great && great > 0f);
    }

    [Fact]
    public void Clamp_stays_in_0_1()
    {
        Assert.Equal(0f, ImperfectRiftsRules.ApplyRating(0f, ImperfectRiftsRating.Perfect, 10));
        Assert.Equal(1f, ImperfectRiftsRules.ApplyRating(1f, ImperfectRiftsRating.Miss, 10));
    }

    [Fact]
    public void Higher_intensity_more_damage_less_heal()
    {
        float dmgLow = ImperfectRiftsRules.ApplyRating(0f, ImperfectRiftsRating.Good, 1);
        float dmgHigh = ImperfectRiftsRules.ApplyRating(0f, ImperfectRiftsRating.Good, 10);
        float healLow = ImperfectRiftsRules.ApplyRating(0.5f, ImperfectRiftsRating.Perfect, 1);
        float healHigh = ImperfectRiftsRules.ApplyRating(0.5f, ImperfectRiftsRating.Perfect, 10);
        Assert.True(dmgHigh > dmgLow);
        Assert.True(healHigh > healLow); // less heal ⇒ chaos stays higher
    }

    [Fact]
    public void Zero_chaos_zero_drift_and_shake()
    {
        ImperfectRiftsRules.DriftOffset(0f, 10, 1, 2, out float dx, out float dz);
        ImperfectRiftsRules.ShakeOffset(0f, 10, 1, 2, 3.5f, out float sx, out float sz);
        Assert.Equal(0f, dx);
        Assert.Equal(0f, dz);
        Assert.Equal(0f, sx);
        Assert.Equal(0f, sz);
    }

    [Fact]
    public void Drift_stable_for_same_seed_and_chaos()
    {
        ImperfectRiftsRules.DriftOffset(0.6f, 5, 2, 3, out float a0, out float b0);
        ImperfectRiftsRules.DriftOffset(0.6f, 5, 2, 3, out float a1, out float b1);
        Assert.Equal(a0, a1);
        Assert.Equal(b0, b1);
        Assert.True(System.Math.Abs(a0) + System.Math.Abs(b0) > 0f);
    }

    [Fact]
    public void Shake_magnitude_rises_with_chaos()
    {
        ImperfectRiftsRules.ShakeOffset(0.2f, 5, 0, 0, 1.25f, out float x0, out float z0);
        ImperfectRiftsRules.ShakeOffset(0.9f, 5, 0, 0, 1.25f, out float x1, out float z1);
        float m0 = System.Math.Abs(x0) + System.Math.Abs(z0);
        float m1 = System.Math.Abs(x1) + System.Math.Abs(z1);
        Assert.True(m1 > m0);
    }
}
