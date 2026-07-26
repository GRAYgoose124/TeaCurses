namespace TeaCurses.Curses;

/// <summary>
/// Maps stock grid row Y to inverted local Z so home (Y=0) sits near the
/// visual top while logical coords stay unchanged. Extra visual-only rows
/// stay on the spawn (bottom) side so approach vision is not lost.
/// </summary>
public static class UpwardsRiftLayout
{
    /// <summary>Rows to raise the action row past a maxY mirror.</summary>
    public const int DefaultActionRowLift = 0;

    public static float InvertedLocalZ(
        int gridY,
        int numRows,
        int extraBottomRows,
        int actionRowLift = DefaultActionRowLift)
    {
        _ = extraBottomRows; // range still present on the grid; placement is spawn-side
        int maxY = numRows - 1;
        if (gridY < 0)
            return actionRowLift + gridY;

        return maxY + actionRowLift - gridY;
    }
}
