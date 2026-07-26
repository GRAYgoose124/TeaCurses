namespace TeaCurses.Curses;

public enum CryptidGlyphMode
{
    UnicodeOnly = 1,
    ProceduralOnly = 2,
    Mix = 3,
}

/// <summary>
/// Intensity 1 = found Unicode only; 2 = procedural only; 3 = mix.
/// </summary>
public static class CryptidGlyphModeRules
{
    public const int Min = 1;
    public const int Max = 3;
    public const int Default = 3;

    public static CryptidGlyphMode FromIntensity(float intensity)
    {
        var rounded = (int)System.Math.Round(intensity);
        if (rounded < Min)
            rounded = Min;
        if (rounded > Max)
            rounded = Max;
        return (CryptidGlyphMode)rounded;
    }
}
