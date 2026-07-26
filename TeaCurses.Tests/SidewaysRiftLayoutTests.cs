using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class SidewaysRiftLayoutTests
{
    [Fact]
    public void Column_sides_are_fixed_outers_and_left_guide_for_middle()
    {
        Assert.Equal(SidewaysRiftSide.Left, SidewaysRiftSides.SideForColumn(0));
        Assert.Equal(SidewaysRiftSide.Left, SidewaysRiftSides.SideForColumn(1));
        Assert.Equal(SidewaysRiftSide.Right, SidewaysRiftSides.SideForColumn(2));
    }

    [Fact]
    public void Middle_spawn_alternates_even_left_odd_right()
    {
        Assert.Equal(SidewaysRiftSide.Left, SidewaysRiftSides.SideForMiddleSpawn(0));
        Assert.Equal(SidewaysRiftSide.Right, SidewaysRiftSides.SideForMiddleSpawn(1));
        Assert.Equal(SidewaysRiftSide.Left, SidewaysRiftSides.SideForMiddleSpawn(2));
    }

    [Fact]
    public void Rows_through_turn_stay_stock()
    {
        SidewaysRiftLayout.LocalXZ(0, 0, SidewaysRiftSide.Left, 3, out float x0, out float z0);
        Assert.Equal(-1f, x0);
        Assert.Equal(0f, z0);

        SidewaysRiftLayout.LocalXZ(1, 2, SidewaysRiftSide.Right, 3, out float x2, out float z2);
        Assert.Equal(0f, x2);
        Assert.Equal(2f, z2);
    }

    [Fact]
    public void Extra_bottom_rows_stay_stock()
    {
        SidewaysRiftLayout.LocalXZ(2, -1, SidewaysRiftSide.Right, 3, out float x, out float z);
        Assert.Equal(1f, x);
        Assert.Equal(-1f, z);
    }

    [Fact]
    public void Side_arm_holds_z_at_turn_and_walks_x_outward()
    {
        SidewaysRiftLayout.LocalXZ(0, 5, SidewaysRiftSide.Left, 3, out float lx, out float lz);
        Assert.Equal(2f, lz);
        Assert.Equal(-1f - 3f, lx);

        SidewaysRiftLayout.LocalXZ(2, 5, SidewaysRiftSide.Right, 3, out float rx, out float rz);
        Assert.Equal(2f, rz);
        Assert.Equal(1f + 3f, rx);

        SidewaysRiftLayout.LocalXZ(1, 5, SidewaysRiftSide.Right, 3, out float mx, out float mz);
        Assert.Equal(2f, mz);
        Assert.Equal(0f + 3f, mx);
    }
}
