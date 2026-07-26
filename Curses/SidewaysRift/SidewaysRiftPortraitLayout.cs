namespace TeaCurses.Curses;

/// <summary>
/// Portrait frame shrink/nudge so side-approach rows stay visible under Sideways Rift.
/// </summary>
public static class SidewaysRiftPortraitLayout
{
    public const float ScaleFactor = 0.5f;

    /// <summary>Local Y added after scaling so frames sit a bit higher on screen.</summary>
    public const float UpNudge = 160f;

    public static float ScaledAxis(float stock) => stock * ScaleFactor;

    public static float NudgedY(float stockY) => stockY + UpNudge;
}
