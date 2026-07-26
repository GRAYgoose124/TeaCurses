using TeaCurses.UI;
using Xunit;

namespace TeaCurses.Tests;

public class HexPlateMathTests
{
    [Fact]
    public void Coverage_center_is_solid()
    {
        Assert.Equal(1f, HexPlateMath.Coverage(0.5f, 0.5f, 0.14f), 3);
    }

    [Fact]
    public void Coverage_outside_corner_is_empty()
    {
        Assert.Equal(0f, HexPlateMath.Coverage(0.001f, 0.05f, 0.14f), 3);
    }

    [Fact]
    public void Coverage_right_tip_mid_is_solid()
    {
        Assert.True(HexPlateMath.Coverage(0.98f, 0.5f, 0.14f) > 0.5f);
    }

    [Fact]
    public void Coverage_top_flat_middle_is_solid()
    {
        Assert.True(HexPlateMath.Coverage(0.5f, 0.95f, 0.14f) > 0.5f);
    }
}
