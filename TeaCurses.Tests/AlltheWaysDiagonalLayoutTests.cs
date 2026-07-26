using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class AlltheWaysDiagonalLayoutTests
{
    [Fact]
    public void Rows_through_turn_and_extra_bottom_stay_stock()
    {
        AlltheWaysDiagonalLayout.LocalXZ(0, 0, AlltheWaysSide.Left, 3, out float x0, out float z0);
        Assert.Equal(-1f, x0);
        Assert.Equal(0f, z0);

        AlltheWaysDiagonalLayout.LocalXZ(1, 2, AlltheWaysSide.Right, 3, out float x2, out float z2);
        Assert.Equal(0f, x2);
        Assert.Equal(2f, z2);

        AlltheWaysDiagonalLayout.LocalXZ(2, -1, AlltheWaysSide.Right, 3, out float xb, out float zb);
        Assert.Equal(1f, xb);
        Assert.Equal(-1f, zb);
    }

    [Fact]
    public void Far_outers_keep_z_equals_row_and_walk_x_diagonally()
    {
        AlltheWaysDiagonalLayout.LocalXZ(0, 5, AlltheWaysSide.Left, 3, out float lx, out float lz);
        Assert.Equal(5f, lz);
        Assert.Equal(-1f - 3f, lx);

        AlltheWaysDiagonalLayout.LocalXZ(2, 5, AlltheWaysSide.Right, 3, out float rx, out float rz);
        Assert.Equal(5f, rz);
        Assert.Equal(1f + 3f, rx);
    }

    [Fact]
    public void Middle_zigzags_every_four_distance_rows()
    {
        // d=3 → segment 0 → keep Right → +3
        AlltheWaysDiagonalLayout.LocalXZ(1, 5, AlltheWaysSide.Right, 3, out float mx, out float mz);
        Assert.Equal(5f, mz);
        Assert.Equal(3f, mx);

        // d=5 → segment 1 → flip Left start → Right → +5
        AlltheWaysDiagonalLayout.LocalXZ(1, 7, AlltheWaysSide.Left, 3, out float zx, out float zz);
        Assert.Equal(7f, zz);
        Assert.Equal(5f, zx);
    }
}
