namespace TeaCurses;

/// <summary>
/// One Hand: intensity toggles Primary vs Alternate.
/// </summary>
public static class OneHandRules
{
    /// <summary>
    /// Intensity 1 → Alternate; anything else (including 0 and unknowns) → Primary.
    /// </summary>
    public static BindSide RequiredSide(float intensity)
        => intensity >= 1f ? BindSide.Alternate : BindSide.Primary;

    /// <summary>
    /// True when the side is classifiable and does not match the required side.
    /// <see cref="BindSide.None"/> never matches this.
    /// </summary>
    public static bool ShouldSwallow(BindSide side, BindSide required)
        => side != BindSide.None && side != required;
}
