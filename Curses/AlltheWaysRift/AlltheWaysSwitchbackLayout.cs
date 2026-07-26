namespace TeaCurses.Curses;

public static class AlltheWaysSwitchbackLayout
{
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

        AlltheWaysSide start = side;
        if ((col & 1) == 1)
            start = AlltheWaysMode.Flip(start);

        int distance = row - AlltheWaysMode.TurnRow;
        AlltheWaysSide effective = AlltheWaysMode.ZigZagEffectiveSide(start, distance);
        localZ = AlltheWaysMode.TurnRow;
        localX = effective == AlltheWaysSide.Left
            ? stockX - distance
            : stockX + distance;
    }
}
