using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class AfterimageTrailTests
{
    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(5, 3, 4)]
    [InlineData(10, 5, 6)]
    public void Intensity_maps_max_ghosts_and_lifetime(int i, int max, int life)
    {
        Assert.Equal(max, AfterimageTrail.MaxGhosts(i));
        Assert.Equal(life, AfterimageTrail.LifetimeBeats(i));
    }

    [Fact]
    public void Alpha_starts_at_base_and_hits_zero_at_lifetime()
    {
        var life = AfterimageTrail.LifetimeBeats(5);
        Assert.Equal(AfterimageTrail.BaseOpacity, AfterimageTrail.Alpha(0, life), 3);
        Assert.Equal(0f, AfterimageTrail.Alpha(life, life), 3);
        Assert.True(AfterimageTrail.Alpha(1, life) < AfterimageTrail.Alpha(0, life));
    }

    [Fact]
    public void Alpha_falls_faster_than_linear_at_mid_life()
    {
        const int life = 10;
        var mid = AfterimageTrail.Alpha(5, life);
        var linearMid = 0.5f * AfterimageTrail.BaseOpacity;
        Assert.True(mid < linearMid - 0.01f);
    }

    [Fact]
    public void ShouldDrop_only_when_position_changed()
    {
        Assert.False(AfterimageTrail.ShouldDrop(1f, 2f, 0f, 1f, 2f, 0f));
        Assert.True(AfterimageTrail.ShouldDrop(1f, 2f, 0f, 2f, 2f, 0f));
    }

    [Fact]
    public void ExcessCount_reports_how_many_oldest_to_cull()
    {
        Assert.Equal(0, AfterimageTrail.ExcessCount(5, 5));
        Assert.Equal(2, AfterimageTrail.ExcessCount(7, 5));
    }

    [Fact]
    public void ClampIntensity_bounds_1_to_10()
    {
        Assert.Equal(1, AfterimageTrail.ClampIntensity(0));
        Assert.Equal(10, AfterimageTrail.ClampIntensity(99));
    }

    [Fact]
    public void ShouldCull_when_age_reaches_lifetime()
    {
        Assert.False(AfterimageTrail.ShouldCull(2, 7));
        Assert.True(AfterimageTrail.ShouldCull(7, 7));
    }
}
