namespace TeaCurses.Curses;

public enum AfterimageBeatBucket
{
    OnBeat,
    HalfBeat,
    Other,
}

/// <summary>
/// Stock on-beat / half-beat / other classification (same thresholds as RREnemy shadows)
/// mapped to a fixed tint palette for afterimage ghosts.
/// </summary>
public static class AfterimageBeatTint
{
    public static AfterimageBeatBucket Classify(float spawnTrueBeatNumber)
    {
        var frac = spawnTrueBeatNumber % 1f;
        if (frac < 0f)
            frac += 1f;

        if (frac <= 0.05f || frac >= 0.95f)
            return AfterimageBeatBucket.OnBeat;
        if (frac >= 0.45f && frac <= 0.55f)
            return AfterimageBeatBucket.HalfBeat;
        return AfterimageBeatBucket.Other;
    }

    public static void Rgb(AfterimageBeatBucket bucket, out float r, out float g, out float b)
    {
        switch (bucket)
        {
            case AfterimageBeatBucket.OnBeat:
                r = 0.20f;
                g = 0.95f;
                b = 1.00f;
                break;
            case AfterimageBeatBucket.HalfBeat:
                r = 1.00f;
                g = 0.25f;
                b = 0.90f;
                break;
            default:
                r = 1.00f;
                g = 0.75f;
                b = 0.15f;
                break;
        }
    }
}
