namespace TeaCurses.Curses;

/// <summary>
/// Overlay row text colors for enabled / disabled / warn-when-off curses.
/// </summary>
public static class CurseRowColor
{
    public static void Rgba(
        bool on,
        bool dangerWhenOff,
        bool warnYellowWhenOff,
        out float r,
        out float g,
        out float b,
        out float a)
    {
        a = 1f;
        if (on)
        {
            r = 0.55f;
            g = 1f;
            b = 0.7f;
            return;
        }

        if (dangerWhenOff)
        {
            r = 1f;
            g = 0.25f;
            b = 0.25f;
            return;
        }

        if (warnYellowWhenOff)
        {
            r = 1f;
            g = 0.85f;
            b = 0.2f;
            return;
        }

        r = 0.88f;
        g = 0.86f;
        b = 0.82f;
    }
}
