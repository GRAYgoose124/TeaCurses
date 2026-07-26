using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class AlltheWaysModeTests
{
    [Theory]
    [InlineData(1f, AlltheWaysModeKind.Diagonal)]
    [InlineData(2f, AlltheWaysModeKind.Sideways)]
    [InlineData(3f, AlltheWaysModeKind.Spiral)]
    [InlineData(4f, AlltheWaysModeKind.Funnel)]
    [InlineData(9f, AlltheWaysModeKind.TripleArmOut)]
    public void FromIntensity_maps_presets(float intensity, AlltheWaysModeKind expected)
        => Assert.Equal(expected, AlltheWaysMode.FromIntensity(intensity));

    [Theory]
    [InlineData(0f, AlltheWaysModeKind.Diagonal)]
    [InlineData(1.4f, AlltheWaysModeKind.Diagonal)]
    [InlineData(1.5f, AlltheWaysModeKind.Sideways)]
    [InlineData(99f, AlltheWaysModeKind.TripleArmOut)]
    public void FromIntensity_clamps_and_rounds(float intensity, AlltheWaysModeKind expected)
        => Assert.Equal(expected, AlltheWaysMode.FromIntensity(intensity));

    [Fact]
    public void Column_and_middle_sides_match_Sideways_conventions()
    {
        Assert.Equal(AlltheWaysSide.Left, AlltheWaysMode.SideForColumn(0));
        Assert.Equal(AlltheWaysSide.Left, AlltheWaysMode.SideForColumn(1));
        Assert.Equal(AlltheWaysSide.Right, AlltheWaysMode.SideForColumn(2));
        Assert.Equal(AlltheWaysSide.Left, AlltheWaysMode.SideForMiddleSpawn(0));
        Assert.Equal(AlltheWaysSide.Right, AlltheWaysMode.SideForMiddleSpawn(1));
    }

    [Fact]
    public void ZigZag_period_4_flips_on_second_segment()
    {
        Assert.Equal(4, AlltheWaysMode.ZigZagPeriod);
        Assert.Equal(AlltheWaysSide.Left, AlltheWaysMode.ZigZagEffectiveSide(AlltheWaysSide.Left, 1));
        Assert.Equal(AlltheWaysSide.Left, AlltheWaysMode.ZigZagEffectiveSide(AlltheWaysSide.Left, 4));
        Assert.Equal(AlltheWaysSide.Right, AlltheWaysMode.ZigZagEffectiveSide(AlltheWaysSide.Left, 5));
        Assert.Equal(AlltheWaysSide.Right, AlltheWaysMode.ZigZagEffectiveSide(AlltheWaysSide.Left, 8));
        Assert.Equal(AlltheWaysSide.Left, AlltheWaysMode.ZigZagEffectiveSide(AlltheWaysSide.Left, 9));
    }
}
