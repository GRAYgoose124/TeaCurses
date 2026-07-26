namespace TeaCurses.Curses;

/// <summary>
/// Intensity → trail length / fade math for the Afterimage curse.
/// </summary>
public static class AfterimageTrail
{
    public const float BaseOpacity = 0.7f;

    public static int ClampIntensity(int intensity)
    {
        if (intensity < 1) return 1;
        if (intensity > 10) return 10;
        return intensity;
    }

    public static int MaxGhosts(int intensity)
    {
        // Short readable trail: I1→1 … I10→5 (not I ghosts).
        return (ClampIntensity(intensity) + 1) / 2;
    }

    public static int LifetimeBeats(int intensity) => MaxGhosts(intensity) + 1;

    public static float Alpha(int ageBeats, int lifetimeBeats)
    {
        if (lifetimeBeats <= 0) return 0f;
        if (ageBeats >= lifetimeBeats) return 0f;
        if (ageBeats < 0) ageBeats = 0;
        var t = 1f - (ageBeats / (float)lifetimeBeats);
        // Cubic falloff — older ghosts drop out quickly.
        return t * t * t * BaseOpacity;
    }

    public static bool ShouldDrop(
        float prevX, float prevY, float prevZ,
        float curX, float curY, float curZ,
        float epsilon = 0.001f)
    {
        var dx = prevX - curX;
        var dy = prevY - curY;
        var dz = prevZ - curZ;
        return (dx * dx + dy * dy + dz * dz) > (epsilon * epsilon);
    }

    public static bool ShouldCull(int ageBeats, int lifetimeBeats) =>
        ageBeats >= lifetimeBeats;

    public static int ExcessCount(int currentCount, int maxGhosts)
    {
        if (maxGhosts < 0) maxGhosts = 0;
        if (currentCount <= maxGhosts) return 0;
        return currentCount - maxGhosts;
    }
}
