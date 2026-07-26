using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class CryptidMorphTests
{
    [Fact]
    public void Debut_shows_stock_with_tell()
    {
        Assert.True(CryptidMorph.ShowStock(typeAlreadySeen: false));
        Assert.True(CryptidMorph.IsSuperscriptTell(typeAlreadySeen: false));
    }

    [Fact]
    public void Subsequent_is_glyph_only()
    {
        Assert.False(CryptidMorph.ShowStock(typeAlreadySeen: true));
        Assert.False(CryptidMorph.IsSuperscriptTell(typeAlreadySeen: true));
    }

    [Fact]
    public void Tell_scale_is_smaller_than_body_glyph()
    {
        Assert.True(CryptidMorph.GlyphScaleFactor(typeAlreadySeen: false)
            < CryptidMorph.GlyphScaleFactor(typeAlreadySeen: true));
        Assert.Equal(1f, CryptidMorph.GlyphScaleFactor(typeAlreadySeen: true));
    }

    [Fact]
    public void Tell_offset_sits_above_and_right_like_exponent()
    {
        CryptidMorph.TellOffset(halfWidth: 10f, halfHeight: 20f, out var x, out var y);
        Assert.True(x > 0f);
        Assert.True(y > 20f); // above the top of the body half-extent
    }
}
