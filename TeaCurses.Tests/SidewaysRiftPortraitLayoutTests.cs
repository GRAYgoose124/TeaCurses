using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class SidewaysRiftPortraitLayoutTests
{
    [Fact]
    public void Scale_factor_is_half()
    {
        Assert.Equal(0.5f, SidewaysRiftPortraitLayout.ScaleFactor);
    }

    [Fact]
    public void Applied_scale_is_half_of_stock()
    {
        Assert.Equal(0.5f, SidewaysRiftPortraitLayout.ScaledAxis(1f));
        Assert.Equal(1f, SidewaysRiftPortraitLayout.ScaledAxis(2f));
    }

    [Fact]
    public void Applied_position_nudges_up_from_stock()
    {
        Assert.Equal(
            20f + SidewaysRiftPortraitLayout.UpNudge,
            SidewaysRiftPortraitLayout.NudgedY(20f));
    }
}
