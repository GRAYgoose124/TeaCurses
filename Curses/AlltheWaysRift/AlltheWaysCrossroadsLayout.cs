namespace TeaCurses.Curses;

public static class AlltheWaysCrossroadsLayout
{
    public const int CubeSteps = 4;

    public static void LocalXZ(
        int col,
        int row,
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

        // Spawn pocket: first 4 distance steps curl a 2×2 square, then cardinal arm.
        if (distance <= CubeSteps)
        {
            CubeStep(col, stockX, distance, out localX, out localZ);
            return;
        }

        CubeStep(col, stockX, CubeSteps, out float exitX, out float exitZ);
        int arm = distance - CubeSteps;
        ExtendFromCubeExit(col, stockX, exitX, exitZ, arm, out localX, out localZ);
    }

    /// <summary>
    /// 2×2 square CW. Step 1..4. Anchored so the square sits on the spawn side of each arm.
    /// </summary>
    public static void CubeStep(int col, float stockX, int step, out float localX, out float localZ)
    {
        // Clamp step into 1..4
        if (step < 1) step = 1;
        if (step > CubeSteps) step = CubeSteps;

        // Local square offsets (unit tiles): (0,0)→(+1,0)→(+1,+1)→(0,+1)
        int ox = 0;
        int oz = 0;
        switch (step)
        {
            case 1: ox = 0; oz = 0; break;
            case 2: ox = 1; oz = 0; break;
            case 3: ox = 1; oz = 1; break;
            default: ox = 0; oz = 1; break;
        }

        float turn = AlltheWaysMode.TurnRow;
        if (col == AlltheWaysMode.MiddleColumn)
        {
            // Cube sits above the corridor, far side.
            localX = stockX + (ox - 0.5f);
            localZ = turn + 3f + oz;
            return;
        }

        if (col == 0)
        {
            // Left: cube on the left flank, opening toward +X into the arm.
            localX = stockX - 3f + oz;      // grow "out" then around
            localZ = turn + ox;
            return;
        }

        // Right: mirror
        localX = stockX + 3f - oz;
        localZ = turn + ox;
    }

    private static void ExtendFromCubeExit(
        int col,
        float stockX,
        float exitX,
        float exitZ,
        int arm,
        out float localX,
        out float localZ)
    {
        if (col == AlltheWaysMode.MiddleColumn)
        {
            localX = stockX;
            localZ = exitZ + arm;
            return;
        }

        if (col == 0)
        {
            localX = exitX - arm;
            localZ = exitZ;
            return;
        }

        localX = exitX + arm;
        localZ = exitZ;
    }
}
