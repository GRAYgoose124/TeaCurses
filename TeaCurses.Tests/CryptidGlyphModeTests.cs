using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class CryptidGlyphModeTests
{
    [Theory]
    [InlineData(1f, CryptidGlyphMode.UnicodeOnly)]
    [InlineData(2f, CryptidGlyphMode.ProceduralOnly)]
    [InlineData(3f, CryptidGlyphMode.Mix)]
    public void FromIntensity_maps_1_2_3(float intensity, CryptidGlyphMode expected)
    {
        Assert.Equal(expected, CryptidGlyphModeRules.FromIntensity(intensity));
    }

    [Theory]
    [InlineData(0f, CryptidGlyphMode.UnicodeOnly)]
    [InlineData(0.4f, CryptidGlyphMode.UnicodeOnly)]
    [InlineData(1.4f, CryptidGlyphMode.UnicodeOnly)]
    [InlineData(1.6f, CryptidGlyphMode.ProceduralOnly)]
    [InlineData(2.6f, CryptidGlyphMode.Mix)]
    [InlineData(99f, CryptidGlyphMode.Mix)]
    public void FromIntensity_clamps_and_rounds(float intensity, CryptidGlyphMode expected)
    {
        Assert.Equal(expected, CryptidGlyphModeRules.FromIntensity(intensity));
    }
}
