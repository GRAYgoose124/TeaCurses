namespace TeaCurses.Curses;

public static class AlltheWaysSpiralLayout
{
    public static void SpiralOffset(int distance, out int offsetX, out int offsetZ)
    {
        offsetX = 0;
        offsetZ = 0;
        if (distance <= 0)
            return;

        int dx = 0;
        int dz = 1; // +Z first (away from action)
        int segmentLength = 1;
        int segmentProgress = 0;
        int segmentsCompleted = 0;

        for (int step = 0; step < distance; step++)
        {
            offsetX += dx;
            offsetZ += dz;
            segmentProgress++;
            if (segmentProgress < segmentLength)
                continue;

            segmentProgress = 0;
            int ndx = dz;
            int ndz = -dx;
            dx = ndx;
            dz = ndz;
            segmentsCompleted++;
            if ((segmentsCompleted % 2) == 0)
                segmentLength++;
        }
    }

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
        SpiralOffset(distance, out int ox, out int oz);
        localX = stockX + ox;
        localZ = AlltheWaysMode.TurnRow + oz;
    }
}
