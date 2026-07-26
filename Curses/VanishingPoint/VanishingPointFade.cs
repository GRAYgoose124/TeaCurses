namespace TeaCurses.Curses;

/// <summary>
/// Beat-distance → alpha math for the Vanishing Point curse.
/// </summary>
public static class VanishingPointFade
{
    public const float InvisibleAtBeats = 1f;

    /// <summary>
    /// Minimum alpha when intensity softens full invisibility on near rows.
    /// </summary>
    public const float VisibilityFloor = 0.25f;

    public static int ClampIntensity(int intensity)
    {
        if (intensity < 1) return 1;
        if (intensity > 10) return 10;
        return intensity;
    }

    public static float FadeStart(int intensity) => 1f + ClampIntensity(intensity);

    public static float DistanceBeats(float nextActionRowTrueBeat, float currentTrueBeat)
    {
        if (float.IsNaN(nextActionRowTrueBeat) || float.IsInfinity(nextActionRowTrueBeat))
            return float.PositiveInfinity;
        return nextActionRowTrueBeat - currentTrueBeat;
    }

    /// <summary>
    /// Soft intensity floors: I&lt;3 keeps action row and 1-beat-before readable;
    /// I&lt;6 keeps action row readable; I≥6 allows full invisibility from 1 beat out.
    /// </summary>
    public static float MinAlpha(float distanceBeats, int intensity)
    {
        intensity = ClampIntensity(intensity);
        if (float.IsNaN(distanceBeats) || float.IsInfinity(distanceBeats))
            return 0f;

        if (intensity < 3 && distanceBeats <= InvisibleAtBeats)
            return VisibilityFloor;
        if (intensity < 6 && distanceBeats <= 0f)
            return VisibilityFloor;
        return 0f;
    }

    public static float Alpha(float distanceBeats, int intensity)
    {
        if (float.IsNaN(distanceBeats))
            return 1f;

        float raw;
        if (distanceBeats <= InvisibleAtBeats)
        {
            raw = 0f;
        }
        else
        {
            var fadeStart = FadeStart(intensity);
            if (distanceBeats >= fadeStart)
            {
                raw = 1f;
            }
            else
            {
                var span = fadeStart - InvisibleAtBeats;
                raw = span <= 0f ? 0f : (distanceBeats - InvisibleAtBeats) / span;
            }
        }

        var floor = MinAlpha(distanceBeats, intensity);
        return raw < floor ? floor : raw;
    }

    /// <summary>Opaque white RGB with the given alpha for MPB <c>_Color</c>.</summary>
    public static void OpacityRgba(float alpha, out float r, out float g, out float b, out float a)
    {
        if (alpha < 0f) alpha = 0f;
        if (alpha > 1f) alpha = 1f;
        r = 1f;
        g = 1f;
        b = 1f;
        a = alpha;
    }
}
