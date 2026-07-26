namespace TeaCurses.Curses;

public static class AlltheWaysDiagonalLayout
{
    public static float StockLocalX(int col, int numColumns)
        => col - (numColumns - 1) / 2f;

    public static void LocalXZ(
        int col,
        int row,
        AlltheWaysSide side,
        int numColumns,
        out float localX,
        out float localZ)
    {
        float stockX = StockLocalX(col, numColumns);
        if (row < 0 || row <= AlltheWaysMode.TurnRow)
        {
            localX = stockX;
            localZ = row;
            return;
        }

        int distance = row - AlltheWaysMode.TurnRow;
        AlltheWaysSide effective = col == AlltheWaysMode.MiddleColumn
            ? AlltheWaysMode.ZigZagEffectiveSide(side, distance)
            : side;

        localZ = row;
        localX = effective == AlltheWaysSide.Left
            ? stockX - distance
            : stockX + distance;
    }
}
