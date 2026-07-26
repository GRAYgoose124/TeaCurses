namespace TeaCurses.UI;

/// <summary>
/// Open/close decisions for the curse overlay.
/// Open only on track-select screens; close is always allowed.
/// </summary>
public static class OverlayOpenRules
{
    /// <summary>
    /// After a toggle key press: true → open, false → close, null → ignore.
    /// </summary>
    public static bool? AfterToggle(bool currentlyOpen, bool inTrackMenu)
    {
        if (currentlyOpen)
            return false;
        if (inTrackMenu)
            return true;
        return null;
    }

    /// <summary>
    /// Leave track select while open → force close.
    /// </summary>
    public static bool ShouldForceClose(bool currentlyOpen, bool inTrackMenu)
        => currentlyOpen && !inTrackMenu;

    /// <summary>
    /// UI Cancel / Escape while open → close.
    /// </summary>
    public static bool ShouldCloseFromCancel(bool currentlyOpen, bool cancelPressed)
        => currentlyOpen && cancelPressed;
}
