using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class AlltheWaysNewLayoutsTests
{
    [Fact]
    public void Funnel_converges_toward_stock_as_distance_drops()
    {
        AlltheWaysFunnelLayout.LocalXZ(0, 10, 3, out float farX, out _);
        AlltheWaysFunnelLayout.LocalXZ(0, 3, 3, out float nearX, out _);
        Assert.True(farX < nearX); // left col: farther = more negative, nearer = closer to -1
        Assert.True(nearX < -1f);
        AlltheWaysFunnelLayout.LocalXZ(1, 8, 3, out float mx, out _);
        Assert.Equal(0f, mx);
    }

    [Fact]
    public void Serpentine_is_wide()
    {
        Assert.True(AlltheWaysSerpentineLayout.Amplitude >= 2f);
        AlltheWaysSerpentineLayout.LocalXZ(0, 5, 3, out float x0, out _);
        AlltheWaysSerpentineLayout.LocalXZ(1, 5, 3, out float x1, out _);
        Assert.NotEqual(x0, x1);
        Assert.True(System.Math.Abs(x0 - (-1f)) > 0.5f || System.Math.Abs(x1) > 0.5f);
    }

    [Fact]
    public void Switchback_holds_z_at_turn()
    {
        AlltheWaysSwitchbackLayout.LocalXZ(0, 5, AlltheWaysSide.Left, 3, out float x, out float z);
        Assert.Equal(2f, z);
        Assert.Equal(-1f - 3f, x);
    }

    [Fact]
    public void Crossroads_spawn_band_uses_cube_then_arm()
    {
        // distance 1..4 = cube
        AlltheWaysCrossroadsLayout.LocalXZ(0, 3, 3, out float c1x, out float c1z);
        AlltheWaysCrossroadsLayout.CubeStep(0, -1f, 1, out float e1x, out float e1z);
        Assert.Equal(e1x, c1x);
        Assert.Equal(e1z, c1z);

        // distance 5 = cube exit + 1 arm step left
        AlltheWaysCrossroadsLayout.LocalXZ(0, 7, 3, out float ax, out float az);
        AlltheWaysCrossroadsLayout.CubeStep(0, -1f, 4, out float ex, out float ez);
        Assert.Equal(ex - 1f, ax);
        Assert.Equal(ez, az);
    }

    [Fact]
    public void Orbit_action_rows_are_not_stock()
    {
        AlltheWaysOrbitLayout.LocalXZ(1, 0, 3, out float x0, out float z0);
        Assert.False(x0 == 0f && z0 == 0f);
        AlltheWaysOrbitLayout.LocalXZ(1, 2, 3, out float x2, out float z2);
        Assert.False(x2 == 0f && z2 == 2f);
    }

    [Fact]
    public void Sideways_all_lanes_reach_shared_wall_before_turning_up()
    {
        float wall = AlltheWaysDiagonalLayout.StockLocalX(0, 3) - AlltheWaysSidewaysLayout.SharedExtra;
        AlltheWaysSidewaysLayout.LocalXZ(1, 4, AlltheWaysSide.Left, 3, out float mx, out float mz);
        Assert.True(mx <= 0f);
        Assert.Equal(2f, mz);

        AlltheWaysSidewaysLayout.LocalXZ(1, 8, AlltheWaysSide.Left, 3, out float fx, out float fz);
        Assert.Equal(wall, fx);
        Assert.True(fz > 2f);

        AlltheWaysSidewaysLayout.LocalXZ(0, 8, AlltheWaysSide.Left, 3, out float ox, out float oz);
        Assert.Equal(wall, ox);
        Assert.True(oz > 2f);
    }
}
