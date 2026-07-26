namespace TeaCurses.Curses;

public static class AlltheWaysFunnelLayout
{
    /// <summary>Max lateral offset (tiles) at infinite distance for one column step from middle.</summary>
    public const float MaxSpread = 2.75f;

    /// <summary>Larger = slower squeeze; smaller = converges sooner toward the turn.</summary>
    public const float Squeeze = 1.25f;

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
        int middle = AlltheWaysMode.MiddleColumn;
        float open = distance / (distance + Squeeze);
        localX = stockX + (col - middle) * MaxSpread * open;
        localZ = row;
    }
}
