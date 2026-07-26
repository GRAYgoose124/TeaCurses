namespace TeaCurses.Curses;

/// <summary>
/// Debut: real monster + superscript glyph tell above the head (always on).
/// Subsequent of that type: glyph-only body replacement.
/// </summary>
public static class CryptidMorph
{
    /// <summary>Tell glyph size vs full body-replacement glyph.</summary>
    public const float TellScaleFactor = 0.4f;

    /// <summary>Upward offset as a multiple of sprite half-height (above the head).</summary>
    public const float TellUpFactor = 1.25f;

    /// <summary>Rightward offset as a multiple of sprite half-width (exponent lean).</summary>
    public const float TellRightFactor = 0.45f;

    public static bool ShowStock(bool typeAlreadySeen) => !typeAlreadySeen;

    public static bool IsSuperscriptTell(bool typeAlreadySeen) => !typeAlreadySeen;

    public static float GlyphScaleFactor(bool typeAlreadySeen)
        => typeAlreadySeen ? 1f : TellScaleFactor;

    public static void TellOffset(float halfWidth, float halfHeight, out float x, out float y)
    {
        if (halfWidth < 0f) halfWidth = 0f;
        if (halfHeight < 0f) halfHeight = 0f;
        x = halfWidth * TellRightFactor;
        y = halfHeight * TellUpFactor;
    }
}
