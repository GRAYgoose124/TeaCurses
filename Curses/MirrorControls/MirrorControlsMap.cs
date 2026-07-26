namespace TeaCurses.Curses;

/// <summary>
/// Direction remapping for Mirror Controls.
/// Always Left↔Right when active; intensity ≥ 1 also swaps Up↔Down.
/// </summary>
public static class MirrorControlsMap
{
    /// <summary>Intensity 1 → include vertical; anything else → horizontal only.</summary>
    public static bool IncludeVertical(float intensity)
        => intensity >= 1f;

    public static string Remap(string inputName, float intensity)
        => Remap(inputName, mirrorHorizontal: true, mirrorVertical: IncludeVertical(intensity));

    public static string Remap(string inputName, bool mirrorHorizontal, bool mirrorVertical)
    {
        if (string.IsNullOrEmpty(inputName))
            return inputName;

        if (mirrorHorizontal)
        {
            if (inputName == "Left")
                return "Right";
            if (inputName == "Right")
                return "Left";
        }

        if (mirrorVertical)
        {
            if (inputName == "Up")
                return "Down";
            if (inputName == "Down")
                return "Up";
        }

        return inputName;
    }
}
