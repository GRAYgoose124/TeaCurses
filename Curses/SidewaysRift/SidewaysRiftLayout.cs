namespace TeaCurses.Curses;

/// <summary>
/// Maps stock grid (col, row) + approach side to local XZ along an L-path:
/// rows past the turn approach from the wall at z=TurnRow, then rows 0–2
/// stay stock top-down into the action row.
/// </summary>
public static class SidewaysRiftLayout
{
    public static float StockLocalX(int col, int numColumns)
    {
        return col - (numColumns - 1) / 2f;
    }

    public static void LocalXZ(
        int col,
        int row,
        SidewaysRiftSide side,
        int numColumns,
        out float localX,
        out float localZ)
    {
        float stockX = StockLocalX(col, numColumns);
        if (row < 0 || row <= SidewaysRiftSides.TurnRow)
        {
            localX = stockX;
            localZ = row;
            return;
        }

        int distance = row - SidewaysRiftSides.TurnRow;
        localZ = SidewaysRiftSides.TurnRow;
        localX = side == SidewaysRiftSide.Left
            ? stockX - distance
            : stockX + distance;
    }
}
