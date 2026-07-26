using System;
using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class CryptidGlyphNormalizeTests
{
    [Fact]
    public void Fit_square_fills_padded_canvas()
    {
        CryptidGlyphNormalize.Fit(
            srcW: 100, srcH: 100, canvas: 256, padFraction: 0.1f,
            out var dw, out var dh, out var ox, out var oy);

        Assert.Equal(dw, dh);
        Assert.Equal(205, dw); // Round(256 * 0.8)
        Assert.Equal((256 - 205) / 2, ox);
        Assert.Equal(oy, ox);
    }

    [Fact]
    public void Fit_wide_glyph_limits_by_width()
    {
        CryptidGlyphNormalize.Fit(
            srcW: 200, srcH: 50, canvas: 256, padFraction: 0.1f,
            out var dw, out var dh, out _, out _);

        Assert.Equal(205, dw);
        Assert.Equal(51, dh);
    }

    [Fact]
    public void Fit_tall_glyph_limits_by_height()
    {
        CryptidGlyphNormalize.Fit(
            srcW: 40, srcH: 200, canvas: 256, padFraction: 0.1f,
            out var dw, out var dh, out _, out _);

        Assert.Equal(205, dh);
        Assert.True(dw < dh);
    }

    [Fact]
    public void Fit_unicode_relative_scale_shrinks_vs_default()
    {
        CryptidGlyphNormalize.Fit(100, 100, 256, 0.1f, out var full, out _, out _, out _);
        CryptidGlyphNormalize.Fit(
            100, 100, 256, 0.1f, out var shrunk, out _, out _, out _,
            CryptidGlyphNormalize.UnicodeRelativeScale);

        Assert.True(shrunk < full);
        Assert.Equal((int)Math.Round(full / 1.5f), shrunk);
    }

    [Fact]
    public void InkBounds_trims_transparent_padding()
    {
        // 4x4 with ink only in center 2x2
        var a = new float[16];
        a[5] = 1f; a[6] = 1f;
        a[9] = 1f; a[10] = 1f;

        Assert.True(CryptidGlyphNormalize.TryInkBounds(a, 4, 4, 0.05f, out var x0, out var y0, out var x1, out var y1));
        Assert.Equal(1, x0);
        Assert.Equal(1, y0);
        Assert.Equal(2, x1);
        Assert.Equal(2, y1);
    }

    [Fact]
    public void InkBounds_false_when_empty()
    {
        var a = new float[4];
        Assert.False(CryptidGlyphNormalize.TryInkBounds(a, 2, 2, 0.05f, out _, out _, out _, out _));
    }
}
