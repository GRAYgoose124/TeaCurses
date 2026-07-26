namespace TeaCurses.Curses;

/// <summary>
/// AlltheWays I=2: every lane takes the full perimeter —
/// out to the shared side wall, around/up the far edge, back to its stock X, then further out.
/// (Standalone Sideways Rift keeps the short L-path.)
/// </summary>
public static class AlltheWaysSidewaysLayout
{
    public const int SharedExtra = 2;
    public const int UpLeg = 4;

    public static void LocalXZ(
        int col,
        int row,
        AlltheWaysSide side,
        int numColumns,
        out float localX,
        out float localZ)
    {
        float stockX = AlltheWaysDiagonalLayout.StockLocalX(col, numColumns);
        if (row < 0 || row <= AlltheWaysMode.TurnRow)
        {
            localX = stockX;
            localZ = row;
            return;
        }

        int distance = row - AlltheWaysMode.TurnRow;
        // All three lanes share the same far-end tour (left wall → up → across → out).
        // Ignoring per-column side prevents early turn-up on the right/middle.
        _ = side;
        float wallX = AlltheWaysDiagonalLayout.StockLocalX(0, numColumns) - SharedExtra;

        float x = stockX;
        float z = AlltheWaysMode.TurnRow;
        int dirWall = -1;

        for (int step = 0; step < distance; step++)
        {
            // Leg 1: to shared wall along turn row
            if (System.Math.Abs(x - wallX) > 1e-4f)
            {
                x += dirWall;
                continue;
            }

            // Leg 2: up the wall
            float farZ = AlltheWaysMode.TurnRow + UpLeg;
            if (z < farZ - 1e-4f)
            {
                z += 1f;
                continue;
            }

            // Leg 3: across far edge back to this lane's stock X
            if (System.Math.Abs(x - stockX) > 1e-4f)
            {
                x += System.Math.Sign(stockX - x);
                continue;
            }

            // Leg 4: further +Z above the lane
            z += 1f;
        }

        localX = x;
        localZ = z;
    }
}
