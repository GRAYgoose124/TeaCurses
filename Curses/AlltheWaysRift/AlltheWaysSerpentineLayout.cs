namespace TeaCurses.Curses;

public static class AlltheWaysSerpentineLayout
{
    public const float Amplitude = 2.25f;
    public const float Frequency = 0.85f;

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
        float phase = (distance + col * 2) * Frequency;
        localX = stockX + Amplitude * (float)System.Math.Sin(phase);
        localZ = row;
    }
}
