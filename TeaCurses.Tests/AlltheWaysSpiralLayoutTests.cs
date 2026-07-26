using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

/// <summary>Locks Mode 3 to the original spiral (670bc0a).</summary>
public class AlltheWaysSpiralLayoutTests
{
    [Fact]
    public void SpiralOffset_zero_is_origin()
    {
        AlltheWaysSpiralLayout.SpiralOffset(0, out int x, out int z);
        Assert.Equal(0, x);
        Assert.Equal(0, z);
    }

    [Fact]
    public void SpiralOffset_first_steps_go_up_then_right_cw()
    {
        AlltheWaysSpiralLayout.SpiralOffset(1, out int x1, out int z1);
        Assert.Equal(0, x1);
        Assert.Equal(1, z1);

        AlltheWaysSpiralLayout.SpiralOffset(2, out int x2, out int z2);
        Assert.Equal(1, x2);
        Assert.Equal(1, z2);

        AlltheWaysSpiralLayout.SpiralOffset(3, out int x3, out int z3);
        Assert.Equal(1, x3);
        Assert.Equal(0, z3);

        AlltheWaysSpiralLayout.SpiralOffset(4, out int x4, out int z4);
        Assert.Equal(1, x4);
        Assert.Equal(-1, z4);
    }

    [Fact]
    public void LocalXZ_joins_stock_x_at_turn_and_spirals_for_far_rows()
    {
        AlltheWaysSpiralLayout.LocalXZ(1, 2, 3, out float jx, out float jz);
        Assert.Equal(0f, jx);
        Assert.Equal(2f, jz);

        AlltheWaysSpiralLayout.LocalXZ(1, 3, 3, out float fx, out float fz);
        Assert.Equal(0f, fx);
        Assert.Equal(3f, fz);
    }

    [Fact]
    public void Extra_bottom_rows_stay_stock()
    {
        AlltheWaysSpiralLayout.LocalXZ(0, -1, 3, out float x, out float z);
        Assert.Equal(-1f, x);
        Assert.Equal(-1f, z);
    }

    [Fact]
    public void Mode3_matches_original_formula_stockX_plus_spiral_offset()
    {
        // Regression guard: any tour/perimeter rewrite must fail this.
        for (int col = 0; col < 3; col++)
        {
            for (int row = 3; row <= 12; row++)
            {
                float stockX = AlltheWaysDiagonalLayout.StockLocalX(col, 3);
                int d = row - AlltheWaysMode.TurnRow;
                AlltheWaysSpiralLayout.SpiralOffset(d, out int ox, out int oz);
                AlltheWaysSpiralLayout.LocalXZ(col, row, 3, out float x, out float z);
                Assert.Equal(stockX + ox, x);
                Assert.Equal(AlltheWaysMode.TurnRow + oz, z);
            }
        }
    }
}
