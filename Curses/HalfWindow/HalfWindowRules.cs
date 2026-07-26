using System;

namespace TeaCurses;

public enum HalfWindowTiming
{
    Early,
    TrueFlawless,
    Late,
}

/// <summary>
/// Half Window: intensity 0 suppresses late, 1 suppresses early;
/// safe rating avoids divide-by-zero when a side's window is zero.
/// </summary>
public static class HalfWindowRules
{
    private const float OnBeatEpsilon = 1e-6f;

    public static bool SuppressLate(float intensity) => intensity < 1f;

    public static bool SuppressEarly(float intensity) => intensity >= 1f;

    public static float EffectiveWindow(float stock, bool suppress)
        => suppress ? 0f : stock;

    /// <summary>
    /// Stock enemy hit acceptance: early if beatDiff &gt;= -before; late/on-beat if beatDiff &lt;= after.
    /// </summary>
    public static bool IsWithinEnemyHitWindow(float beatDiff, float beforeBeats, float afterBeats)
    {
        if (beatDiff >= 0f)
            return beatDiff <= afterBeats;
        return beatDiff >= -beforeBeats;
    }

    public static void EffectivePair(
        float stockBefore,
        float stockAfter,
        float intensity,
        out float before,
        out float after)
    {
        before = EffectiveWindow(stockBefore, SuppressEarly(intensity));
        after = EffectiveWindow(stockAfter, SuppressLate(intensity));
    }

    /// <summary>
    /// When the active side's window is 0, fills percent/timing and returns true.
    /// When the active window is &gt; 0, returns false so stock rating can run.
    /// </summary>
    public static bool TrySafeRatingPercent(
        float diffSeconds,
        float beforeWindow,
        float afterWindow,
        out int percent,
        out HalfWindowTiming timing)
    {
        var window = diffSeconds > 0f ? afterWindow : beforeWindow;
        if (window > 0f)
        {
            percent = 0;
            timing = HalfWindowTiming.TrueFlawless;
            return false;
        }

        if (Math.Abs(diffSeconds) <= OnBeatEpsilon)
        {
            percent = 100;
            timing = HalfWindowTiming.TrueFlawless;
            return true;
        }

        percent = 0;
        timing = diffSeconds > 0f ? HalfWindowTiming.Late : HalfWindowTiming.Early;
        return true;
    }
}
