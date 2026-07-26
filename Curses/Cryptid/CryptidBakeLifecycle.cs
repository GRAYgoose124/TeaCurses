namespace TeaCurses.Curses;

/// <summary>
/// Pure policy: glyph sprites bake once per game session (and again only if mode changes).
/// Chart start reshuffles the map; it must not re-bake when the session pool is warm.
/// </summary>
public static class CryptidBakeLifecycle
{
    public static bool NeedsBake(
        bool sessionBaked,
        CryptidGlyphMode sessionMode,
        CryptidGlyphMode requestedMode)
    {
        if (!sessionBaked)
            return true;
        return sessionMode != requestedMode;
    }
}
