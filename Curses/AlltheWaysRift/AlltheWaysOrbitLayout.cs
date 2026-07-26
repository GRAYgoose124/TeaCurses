namespace TeaCurses.Curses;

public static class AlltheWaysOrbitLayout
{
    public static void LocalXZ(
        int col,
        int row,
        int numColumns,
        out float localX,
        out float localZ)
    {
        float stockX = AlltheWaysDiagonalLayout.StockLocalX(col, numColumns);
        if (row < 0)
        {
            localX = stockX;
            localZ = row;
            return;
        }

        // Keep orbit all the way through the action rows (no stock field near home).
        float cx = 0f;
        float cz = AlltheWaysMode.TurnRow + 1.5f;
        float radius = 1.35f + row * 0.4f;
        float angle = row * 0.65f + col * 2.15f;
        localX = cx + radius * (float)System.Math.Cos(angle);
        localZ = cz + radius * (float)System.Math.Sin(angle);
    }
}
