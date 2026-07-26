using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class AfterimageBeatTintTests
{
    [Theory]
    [InlineData(0f, AfterimageBeatBucket.OnBeat)]
    [InlineData(0.04f, AfterimageBeatBucket.OnBeat)]
    [InlineData(0.96f, AfterimageBeatBucket.OnBeat)]
    [InlineData(12.0f, AfterimageBeatBucket.OnBeat)]
    [InlineData(0.5f, AfterimageBeatBucket.HalfBeat)]
    [InlineData(3.5f, AfterimageBeatBucket.HalfBeat)]
    [InlineData(0.25f, AfterimageBeatBucket.Other)]
    [InlineData(0.75f, AfterimageBeatBucket.Other)]
    public void Classify_matches_stock_shadow_buckets(float spawnBeat, AfterimageBeatBucket expected)
    {
        Assert.Equal(expected, AfterimageBeatTint.Classify(spawnBeat));
    }

    [Fact]
    public void Rgb_palette_differs_per_bucket()
    {
        AfterimageBeatTint.Rgb(AfterimageBeatBucket.OnBeat, out var or, out var og, out var ob);
        AfterimageBeatTint.Rgb(AfterimageBeatBucket.HalfBeat, out var hr, out var hg, out var hb);
        AfterimageBeatTint.Rgb(AfterimageBeatBucket.Other, out var xr, out var xg, out var xb);

        Assert.False(or == hr && og == hg && ob == hb);
        Assert.False(or == xr && og == xg && ob == xb);
        Assert.False(hr == xr && hg == xg && hb == xb);
    }
}
