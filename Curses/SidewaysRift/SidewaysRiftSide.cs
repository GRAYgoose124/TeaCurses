namespace TeaCurses.Curses;

public enum SidewaysRiftSide
{
    Left,
    Right
}

public static class SidewaysRiftSides
{
    public const int TurnRow = 2;
    public const int MiddleColumn = 1;

    public static SidewaysRiftSide SideForColumn(int col)
    {
        if (col == 2)
            return SidewaysRiftSide.Right;
        return SidewaysRiftSide.Left;
    }

    public static SidewaysRiftSide SideForMiddleSpawn(int middleSpawnIndex)
    {
        return (middleSpawnIndex & 1) == 0 ? SidewaysRiftSide.Left : SidewaysRiftSide.Right;
    }

    public static SidewaysRiftSide TileGuideSide(int col) => SideForColumn(col);
}
