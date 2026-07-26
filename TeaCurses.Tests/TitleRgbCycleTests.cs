using TeaCurses.UI;
using Xunit;

namespace TeaCurses.Tests;

public class TitleRgbCycleTests
{
    [Fact]
    public void HueAt_zero_time_is_zero()
    {
        Assert.Equal(0f, TitleRgbCycle.HueAt(0f, 0.25f), 5);
    }

    [Fact]
    public void HueAt_wraps_into_unit_interval()
    {
        var hue = TitleRgbCycle.HueAt(4.2f, 0.25f); // 4.2 * 0.25 = 1.05 → 0.05
        Assert.Equal(0.05f, hue, 4);
    }

    [Fact]
    public void HueAt_advances_with_time()
    {
        var a = TitleRgbCycle.HueAt(0f, 1f);
        var b = TitleRgbCycle.HueAt(0.25f, 1f);
        Assert.Equal(0f, a, 5);
        Assert.Equal(0.25f, b, 5);
    }

    [Fact]
    public void HueAt_non_positive_speed_returns_zero()
    {
        Assert.Equal(0f, TitleRgbCycle.HueAt(10f, 0f));
        Assert.Equal(0f, TitleRgbCycle.HueAt(10f, -1f));
    }
}
