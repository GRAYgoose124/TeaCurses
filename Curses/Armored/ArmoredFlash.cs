namespace TeaCurses.Curses;

/// <summary>
/// Linear decay for the generic hurt flash/flinch fallback.
/// </summary>
public static class ArmoredFlash
{
    public const float LifetimeSeconds = 0.2f;

    public static float Strength(float elapsedSeconds)
    {
        if (elapsedSeconds <= 0f)
            return 1f;
        if (elapsedSeconds >= LifetimeSeconds)
            return 0f;
        return 1f - (elapsedSeconds / LifetimeSeconds);
    }
}
