using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class MirrorControlsMapTests
{
    [Fact]
    public void IncludeVertical_0_is_false()
        => Assert.False(MirrorControlsMap.IncludeVertical(0f));

    [Fact]
    public void IncludeVertical_1_is_true()
        => Assert.True(MirrorControlsMap.IncludeVertical(1f));

    [Theory]
    [InlineData("Left", "Right")]
    [InlineData("Right", "Left")]
    public void Intensity_0_swaps_horizontal_only(string input, string expected)
        => Assert.Equal(expected, MirrorControlsMap.Remap(input, intensity: 0f));

    [Theory]
    [InlineData("Up")]
    [InlineData("Down")]
    public void Intensity_0_leaves_vertical_unchanged(string input)
        => Assert.Equal(input, MirrorControlsMap.Remap(input, intensity: 0f));

    [Theory]
    [InlineData("Left", "Right")]
    [InlineData("Right", "Left")]
    [InlineData("Up", "Down")]
    [InlineData("Down", "Up")]
    public void Intensity_1_swaps_both_axes(string input, string expected)
        => Assert.Equal(expected, MirrorControlsMap.Remap(input, intensity: 1f));

    [Fact]
    public void Null_or_empty_passthrough()
    {
        Assert.Null(MirrorControlsMap.Remap(null, 1f));
        Assert.Equal("", MirrorControlsMap.Remap("", 1f));
    }
}
