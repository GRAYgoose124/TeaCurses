using System;

namespace TeaCurses;

public enum HalfWindowTiming
{
    Early,
    TrueFlawless,
    Late,
}

/// <summary>
/// Half Window: intensity 0 = no late, 1 = no early, 2 = no both;
/// safe rating avoids divide-by-zero when a side's window is zero.
/// </summary>
public static class HalfWindowRules
{
    public const float MinIntensity = 0f;
    public const float MaxIntensity = 2f;
    public const float DefaultIntensity = 0f;

    private const float OnBeatEpsilon = 1e-6f;

    /// <summary>0 = early-only; 2 = both off (also suppress late).</summary>
    public static bool SuppressLate(float intensity) => intensity < 1f || intensity >= 2f;

    /// <summary>1 = late-only; 2 = both off.</summary>
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

    /// <summary>
    /// Player input window under Half Window. On-beat always accepted so intensity 2
    /// (both halves zero) remains true-flawless-only rather than impossible.
    /// </summary>
    public static bool IsWithinPlayerInputWindow(float diffSeconds, float before, float after)
    {
        if (Math.Abs(diffSeconds) <= OnBeatEpsilon)
            return true;
        if (diffSeconds < 0f)
            return Math.Abs(diffSeconds) < before;
        return diffSeconds < after;
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
