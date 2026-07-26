namespace TeaCurses.UI;

/// <summary>
/// Rules for whether a track-select row cover is usable as overlay panel art.
/// </summary>
public static class AlbumArtOptionRules
{
    /// <summary>
    /// Folders / promo / tutorial rows and placeholder art are not usable covers.
    /// </summary>
    public static bool IsUsableCoverCandidate(bool isNonTrackRow, bool isPlaceholderArt)
    {
        return !isNonTrackRow && !isPlaceholderArt;
    }
}
