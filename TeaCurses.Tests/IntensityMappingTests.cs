using TeaCurses.Curse;
using Xunit;

namespace TeaCurses.Tests;

public class IntensityMappingTests
{
    [Fact]
    public void Default_linear_maps_min_to_zero()
    {
        var intensity = new CurseIntensity(1f, 3f, 1f, 1f);
        Assert.Equal(0f, intensity.MapToMeter(1f));
    }

    [Fact]
    public void Default_linear_maps_max_to_twenty()
    {
        var intensity = new CurseIntensity(1f, 3f, 1f, 3f);
        Assert.Equal(20f, intensity.MapToMeter(3f));
    }

    [Fact]
    public void Custom_ToMeterRating_is_used()
    {
        var intensity = new CurseIntensity(0f, 1f, 0.1f, 0.5f, v => v * 100f);
        Assert.Equal(50f, intensity.MapToMeter(0.5f));
    }
}
