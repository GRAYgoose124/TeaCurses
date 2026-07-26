using System;

namespace TeaCurses.Curses;

/// <summary>
/// Fit arbitrary glyph bitmaps onto a shared canvas so world size stays consistent.
/// </summary>
public static class CryptidGlyphNormalize
{
    public const int CanvasSize = 256;
    public const float PixelsPerUnit = 256f;
    public const float PadFraction = 0.1f;
    public const float InkAlphaThreshold = 0.05f;
    /// <summary>
    /// Unicode ink was landing ~1.5× larger than procedural marks; shrink fit box.
    /// </summary>
    public const float UnicodeRelativeScale = 1f / 1.5f;

    public static void Fit(
        int srcW,
        int srcH,
        int canvas,
        float padFraction,
        out int dstW,
        out int dstH,
        out int offsetX,
        out int offsetY,
        float relativeScale = 1f)
    {
        if (srcW < 1) srcW = 1;
        if (srcH < 1) srcH = 1;
        if (canvas < 1) canvas = 1;
        if (padFraction < 0f) padFraction = 0f;
        if (padFraction > 0.45f) padFraction = 0.45f;
        if (relativeScale < 0.05f) relativeScale = 0.05f;
        if (relativeScale > 1f) relativeScale = 1f;

        var maxFit = canvas * (1f - 2f * padFraction) * relativeScale;
        var scale = Math.Min(maxFit / srcW, maxFit / srcH);
        dstW = Math.Max(1, (int)Math.Round(srcW * scale));
        dstH = Math.Max(1, (int)Math.Round(srcH * scale));
        if (dstW > canvas) dstW = canvas;
        if (dstH > canvas) dstH = canvas;
        offsetX = (canvas - dstW) / 2;
        offsetY = (canvas - dstH) / 2;
    }

    /// <summary>
    /// Inclusive ink bounds from an alpha (or coverage) buffer, row-major.
    /// </summary>
    public static bool TryInkBounds(
        float[] coverage,
        int width,
        int height,
        float threshold,
        out int minX,
        out int minY,
        out int maxX,
        out int maxY)
    {
        minX = width;
        minY = height;
        maxX = -1;
        maxY = -1;
        if (coverage == null || width < 1 || height < 1)
            return false;

        for (var y = 0; y < height; y++)
        {
            var row = y * width;
            for (var x = 0; x < width; x++)
            {
                if (coverage[row + x] <= threshold)
                    continue;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        return maxX >= minX && maxY >= minY;
    }
}
