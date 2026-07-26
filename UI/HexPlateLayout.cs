using System;

namespace TeaCurses.UI;

/// <summary>
/// Sizes hex menu plates so tips stay outside left-justified text.
/// Slope angle is the tip edge vs the horizontal (30° ≈ short tips on wide rows).
/// </summary>
public static class HexPlateLayout
{
    /// <summary>Default tip slope for wide overlay rows (~30° from horizontal).</summary>
    public const float DefaultSlopeDegrees = 30f;

    /// <summary>
    /// Tip length as a fraction of plate width for a given slope and aspect (W/H).
    /// </summary>
    public static float TipFractionForSlopeDegrees(float degrees, float widthOverHeight)
    {
        if (widthOverHeight < 0.5f)
            widthOverHeight = 0.5f;
        if (degrees < 5f)
            degrees = 5f;
        if (degrees > 80f)
            degrees = 80f;

        var radians = degrees * (float)(Math.PI / 180.0);
        var tan = (float)Math.Tan(radians);
        if (tan < 0.01f)
            tan = 0.01f;

        var tip = 1f / (2f * widthOverHeight * tan);
        return ClampTipFraction(tip);
    }

    /// <summary>
    /// Baked tip fraction for typical wide overlay rows (~30° tip slope).
    /// </summary>
    public static float DefaultTipFraction =>
        TipFractionForSlopeDegrees(DefaultSlopeDegrees, widthOverHeight: 18f);

    /// <summary>
    /// Full plate width so text sits in the flat body: tips + pad on both sides.
    /// </summary>
    public static float PlateWidthForText(float textWidth, float tipFraction, float padEachSide)
    {
        if (textWidth < 0f) textWidth = 0f;
        if (padEachSide < 0f) padEachSide = 0f;
        var tip = ClampTipFraction(tipFraction);

        var denom = 1f - 2f * tip;
        if (denom < 0.1f)
            denom = 0.1f;
        return (textWidth + 2f * padEachSide) / denom;
    }

    public static float TextInset(float plateWidth, float tipFraction, float pad)
    {
        if (plateWidth < 0f) plateWidth = 0f;
        if (pad < 0f) pad = 0f;
        return ClampTipFraction(tipFraction) * plateWidth + pad;
    }

    public static float ClampTipFraction(float tip)
    {
        if (tip < 0.03f) return 0.03f;
        if (tip > 0.45f) return 0.45f;
        return tip;
    }
}
