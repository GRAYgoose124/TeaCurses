using TeaCurses.UI;
using Xunit;

namespace TeaCurses.Tests;

public class AlbumArtOptionRulesTests
{
    [Fact]
    public void Track_with_real_art_is_usable()
    {
        Assert.True(AlbumArtOptionRules.IsUsableCoverCandidate(isNonTrackRow: false, isPlaceholderArt: false));
    }

    [Fact]
    public void Folder_row_is_not_usable()
    {
        Assert.False(AlbumArtOptionRules.IsUsableCoverCandidate(isNonTrackRow: true, isPlaceholderArt: false));
    }

    [Fact]
    public void Placeholder_art_is_not_usable()
    {
        Assert.False(AlbumArtOptionRules.IsUsableCoverCandidate(isNonTrackRow: false, isPlaceholderArt: true));
    }
}
