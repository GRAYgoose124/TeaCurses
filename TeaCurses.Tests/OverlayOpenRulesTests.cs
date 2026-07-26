using TeaCurses.UI;
using Xunit;

namespace TeaCurses.Tests;

public class OverlayOpenRulesTests
{
    [Fact]
    public void Toggle_opens_only_in_track_menu()
    {
        Assert.True(OverlayOpenRules.AfterToggle(currentlyOpen: false, inTrackMenu: true));
        Assert.Null(OverlayOpenRules.AfterToggle(currentlyOpen: false, inTrackMenu: false));
    }

    [Fact]
    public void Toggle_closes_anywhere()
    {
        Assert.False(OverlayOpenRules.AfterToggle(currentlyOpen: true, inTrackMenu: true));
        Assert.False(OverlayOpenRules.AfterToggle(currentlyOpen: true, inTrackMenu: false));
    }

    [Fact]
    public void Force_close_when_leaving_track_menu()
    {
        Assert.True(OverlayOpenRules.ShouldForceClose(currentlyOpen: true, inTrackMenu: false));
        Assert.False(OverlayOpenRules.ShouldForceClose(currentlyOpen: true, inTrackMenu: true));
        Assert.False(OverlayOpenRules.ShouldForceClose(currentlyOpen: false, inTrackMenu: false));
    }

    [Fact]
    public void Cancel_closes_when_open()
    {
        Assert.True(OverlayOpenRules.ShouldCloseFromCancel(currentlyOpen: true, cancelPressed: true));
    }

    [Fact]
    public void Cancel_ignored_when_closed_or_not_pressed()
    {
        Assert.False(OverlayOpenRules.ShouldCloseFromCancel(currentlyOpen: false, cancelPressed: true));
        Assert.False(OverlayOpenRules.ShouldCloseFromCancel(currentlyOpen: true, cancelPressed: false));
        Assert.False(OverlayOpenRules.ShouldCloseFromCancel(currentlyOpen: false, cancelPressed: false));
    }

    [Fact]
    public void Suppress_stock_cancel_only_on_the_close_frame()
    {
        Assert.True(OverlayOpenRules.ShouldSuppressStockCancel(suppressFrame: 10, currentFrame: 10));
        Assert.False(OverlayOpenRules.ShouldSuppressStockCancel(suppressFrame: 10, currentFrame: 11));
        Assert.False(OverlayOpenRules.ShouldSuppressStockCancel(suppressFrame: -1, currentFrame: 10));
    }
}
