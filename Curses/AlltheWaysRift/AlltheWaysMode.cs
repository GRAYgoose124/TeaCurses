namespace TeaCurses.Curses;

public enum AlltheWaysModeKind
{
    Diagonal = 1,
    Sideways = 2,
    Spiral = 3,
    Funnel = 4,
    Serpentine = 5,
    Switchback = 6,
    Crossroads = 7,
    Orbit = 8,
    TripleArmOut = 9,
}

public enum AlltheWaysSide
{
    Left,
    Right,
}

public static class AlltheWaysMode
{
    public const int Min = 1;
    public const int Max = 9;
    public const int Default = 1;
    public const int TurnRow = 2;
    public const int MiddleColumn = 1;
    public const int ZigZagPeriod = 4;

    public static AlltheWaysModeKind FromIntensity(float intensity)
    {
        var rounded = (int)System.Math.Round(intensity);
        if (rounded < Min) rounded = Min;
        if (rounded > Max) rounded = Max;
        return (AlltheWaysModeKind)rounded;
    }

    public static AlltheWaysSide SideForColumn(int col)
        => col == 2 ? AlltheWaysSide.Right : AlltheWaysSide.Left;

    public static AlltheWaysSide SideForMiddleSpawn(int middleSpawnIndex)
        => (middleSpawnIndex & 1) == 0 ? AlltheWaysSide.Left : AlltheWaysSide.Right;

    public static AlltheWaysSide TileGuideSide(int col) => SideForColumn(col);

    public static AlltheWaysSide Flip(AlltheWaysSide side)
        => side == AlltheWaysSide.Left ? AlltheWaysSide.Right : AlltheWaysSide.Left;

    /// <summary>
    /// Period-4 zigzag: distance 1–4 keep start; 5–8 flip; 9–12 start; …
    /// </summary>
    public static AlltheWaysSide ZigZagEffectiveSide(AlltheWaysSide start, int distance)
    {
        if (distance < 1)
            return start;
        int segment = (distance - 1) / ZigZagPeriod;
        return (segment & 1) == 0 ? start : Flip(start);
    }
}
