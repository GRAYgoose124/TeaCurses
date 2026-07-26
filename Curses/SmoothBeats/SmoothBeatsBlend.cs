using System;

namespace TeaCurses.Curses;

/// <summary>
/// Harmonic sine blend between stock beat-curve motion (0) and linear smooth (1).
/// Intensity I = harmonic count and scales base angular frequency.
/// </summary>
public static class SmoothBeatsBlend
{
    /// <summary>Fundamental period in beats at intensity 1.</summary>
    public const float BasePeriodBeats = 16f;

    public static int ClampIntensity(int intensity)
    {
        if (intensity < 1) return 1;
        if (intensity > 10) return 10;
        return intensity;
    }

    /// <summary>ω = (2π / BasePeriodBeats) * I</summary>
    public static float AngularFrequency(int intensity)
    {
        var i = ClampIntensity(intensity);
        return (float)(2.0 * Math.PI / BasePeriodBeats * i);
    }

    /// <summary>
    /// blend = (avg_{k=1..I} sin(k·ω·trueBeat) + 1) / 2 ∈ [0,1]
    /// </summary>
    public static float Evaluate(int intensity, float trueBeat)
    {
        var i = ClampIntensity(intensity);
        var omega = AngularFrequency(i);
        var sum = 0f;
        for (var k = 1; k <= i; k++)
            sum += (float)Math.Sin(k * omega * trueBeat);
        var avg = sum / i;
        return (avg + 1f) * 0.5f;
    }

    /// <summary>Lerp factor for position: stock curve vs linear progress.</summary>
    public static float LerpFactor(float stockCurveT, float linearProgress, float blend)
    {
        if (blend <= 0f) return stockCurveT;
        if (blend >= 1f) return linearProgress;
        return stockCurveT + (linearProgress - stockCurveT) * blend;
    }
}
