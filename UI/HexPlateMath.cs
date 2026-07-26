using System;

namespace TeaCurses.UI;

/// <summary>
/// Flat-top hexagon coverage in unit UV space (tips on left/right).
/// </summary>
public static class HexPlateMath
{
    public static float Coverage(float u, float v, float tipFraction)
    {
        var tip = HexPlateLayout.ClampTipFraction(tipFraction);

        float leftBound;
        float rightBound;
        if (v <= 0.5f)
        {
            var t = v / 0.5f;
            leftBound = Lerp(tip, 0f, t);
            rightBound = Lerp(1f - tip, 1f, t);
        }
        else
        {
            var t = (v - 0.5f) / 0.5f;
            leftBound = Lerp(0f, tip, t);
            rightBound = Lerp(1f, 1f - tip, t);
        }

        var distLeft = leftBound - u;
        var distRight = u - rightBound;
        var dist = distLeft > distRight ? distLeft : distRight;

        const float soft = 0.008f;
        if (dist >= soft)
            return 0f;
        if (dist <= -soft)
            return 1f;

        var tSoft = (dist + soft) / (2f * soft);
        return 1f - SmoothStep(0f, 1f, tSoft);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        var t = (x - edge0) / (edge1 - edge0);
        if (t < 0f) t = 0f;
        if (t > 1f) t = 1f;
        return t * t * (3f - 2f * t);
    }
}
