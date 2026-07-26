using System.Collections.Generic;
using TeaCurses.Curses;
using Xunit;
using Xunit.Abstractions;

namespace TeaCurses.Tests;

public class AlltheWaysTripleArmOutLayoutTests
{
    private readonly ITestOutputHelper _out;

    public AlltheWaysTripleArmOutLayoutTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void Arms_begin_near_center_and_split_outward()
    {
        AlltheWaysTripleArmOutLayout.ArmPoint(0, 1, out float a0x, out float a0z);
        AlltheWaysTripleArmOutLayout.ArmPoint(1, 1, out float a1x, out float a1z);
        AlltheWaysTripleArmOutLayout.ArmPoint(2, 1, out float a2x, out float a2z);

        float cX = AlltheWaysTripleArmOutLayout.CenterX;
        float cZ = AlltheWaysTripleArmOutLayout.CenterZ;
        float r1 = Radius(a0x, a0z, cX, cZ);
        Assert.True(r1 < 1f, $"step1 should be near center, r={r1}");

        // 120° apart even near center
        Assert.True(Dist(a0x, a0z, a1x, a1z) > 0.2f);
        Assert.True(Dist(a1x, a1z, a2x, a2z) > 0.2f);

        AlltheWaysTripleArmOutLayout.ArmPoint(0, AlltheWaysTripleArmOutLayout.TipArmDistance, out float t0x, out float t0z);
        AlltheWaysTripleArmOutLayout.ArmPoint(1, AlltheWaysTripleArmOutLayout.TipArmDistance, out float t1x, out float t1z);
        float rTip = Radius(t0x, t0z, cX, cZ);
        Assert.True(rTip > r1 * 3f, $"tips should be much farther out: r1={r1} tip={rTip}");
        Assert.True(Dist(t0x, t0z, t1x, t1z) > Dist(a0x, a0z, a1x, a1z),
            "arms must split apart toward the tips");
    }

    [Fact]
    public void Action_row_tiles_sit_at_arm_tips()
    {
        for (int col = 0; col < 3; col++)
        {
            AlltheWaysTripleArmOutLayout.TipLocal(col, 3, out float tipX, out float tipZ);
            AlltheWaysTripleArmOutLayout.LocalXZ(col, 0, 3, out float homeX, out float homeZ);
            Assert.Equal(tipX, homeX);
            Assert.Equal(tipZ, homeZ);

            // Not stock bottom action seats
            float stockX = AlltheWaysDiagonalLayout.StockLocalX(col, 3);
            Assert.False(homeX == stockX && homeZ == 0f);
        }
    }

    [Fact]
    public void Far_rows_are_nearer_center_than_action_tips()
    {
        float cX = AlltheWaysTripleArmOutLayout.CenterX;
        float cZ = AlltheWaysTripleArmOutLayout.CenterZ;
        AlltheWaysTripleArmOutLayout.LocalXZ(1, 10, 3, out float farX, out float farZ);
        AlltheWaysTripleArmOutLayout.LocalXZ(1, 0, 3, out float tipX, out float tipZ);
        Assert.True(Radius(farX, farZ, cX, cZ) < Radius(tipX, tipZ, cX, cZ));
    }

    [Fact]
    public void Occupancy_map_has_no_full_tile_collisions()
    {
        bool ok = AlltheWaysTripleArmOutLayout.TryBuildOccupancy(
            3, 0, 14, out _, out string ascii);
        _out.WriteLine(ascii);
        Assert.True(ok, ascii);
    }

    [Fact]
    public void Approach_arm_distance_is_monotonic_toward_tip()
    {
        // Regression: TurnRow fold mapped row3->11 and row2->10, swapping last approach tiles.
        int prev = int.MaxValue;
        for (int row = 0; row <= AlltheWaysTripleArmOutLayout.TipArmDistance + 2; row++)
        {
            int d = AlltheWaysTripleArmOutLayout.ArmDistanceForRow(row);
            Assert.True(d <= prev, $"row {row} dist {d} should not increase vs prior {prev}");
            prev = d;
        }
        Assert.Equal(AlltheWaysTripleArmOutLayout.TipArmDistance, AlltheWaysTripleArmOutLayout.ArmDistanceForRow(0));
        Assert.Equal(AlltheWaysTripleArmOutLayout.TipArmDistance - 1, AlltheWaysTripleArmOutLayout.ArmDistanceForRow(1));
        Assert.Equal(AlltheWaysTripleArmOutLayout.TipArmDistance - 2, AlltheWaysTripleArmOutLayout.ArmDistanceForRow(2));
        Assert.Equal(AlltheWaysTripleArmOutLayout.TipArmDistance - 3, AlltheWaysTripleArmOutLayout.ArmDistanceForRow(3));
        // Outer stretch: field rows past TurnRow stay further out than TipDistance-row alone
        Assert.True(AlltheWaysTripleArmOutLayout.ArmDistanceForRow(5) >
            AlltheWaysTripleArmOutLayout.TipDistance - 5);
    }

    [Fact]
    public void Same_lane_approach_tiles_do_not_stack()
    {
        for (int col = 0; col < 3; col++)
        {
            var seen = new System.Collections.Generic.HashSet<(int, int)>();
            for (int row = 0; row < AlltheWaysTripleArmOutLayout.TipArmDistance - AlltheWaysTripleArmOutLayout.HubShareDistance; row++)
            {
                AlltheWaysTripleArmOutLayout.LocalXZ(col, row, 3, out float x, out float z);
                var key = ((int)System.Math.Round(x), (int)System.Math.Round(z));
                Assert.True(seen.Add(key), $"col {col} row {row} stacked on {key}");
            }
        }
    }

    [Fact]
    public void Phases_are_120_degrees()
    {
        Assert.Equal(0f, AlltheWaysTripleArmOutLayout.ArmPhaseRadians(0), 3);
        Assert.Equal((float)(2 * System.Math.PI / 3), AlltheWaysTripleArmOutLayout.ArmPhaseRadians(1), 3);
        Assert.Equal((float)(4 * System.Math.PI / 3), AlltheWaysTripleArmOutLayout.ArmPhaseRadians(2), 3);
    }

    private static float Radius(float x, float z, float cX, float cZ)
    {
        float dx = x - cX;
        float dz = z - cZ;
        return (float)System.Math.Sqrt(dx * dx + dz * dz);
    }

    private static float Dist(float ax, float az, float bx, float bz)
    {
        float dx = ax - bx;
        float dz = az - bz;
        return (float)System.Math.Sqrt(dx * dx + dz * dz);
    }
}
