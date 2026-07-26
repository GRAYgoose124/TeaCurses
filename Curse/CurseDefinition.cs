using System;

namespace TeaCurses.Curse;

public sealed class CurseIntensity
{
    public float Min { get; }
    public float Max { get; }
    public float Step { get; }
    public float Default { get; }
    public Func<float, float> ToMeterRating { get; }

    public CurseIntensity(
        float min,
        float max,
        float step,
        float defaultValue,
        Func<float, float> toMeterRating = null)
    {
        if (max < min)
            throw new ArgumentException("max must be >= min");
        if (step <= 0f)
            throw new ArgumentException("step must be > 0");

        Min = min;
        Max = max;
        Step = step;
        Default = Clamp(defaultValue, min, max);
        ToMeterRating = toMeterRating ?? DefaultLinearMap;
    }

    public float MapToMeter(float value)
    {
        return ToMeterRating(Clamp(value, Min, Max));
    }

    private float DefaultLinearMap(float value)
    {
        if (Math.Abs(Max - Min) < 1e-6f)
            return 0f;
        var t = (value - Min) / (Max - Min);
        return t * 20f;
    }

    internal static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}

public sealed class CurseDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    public CurseIntensity Intensity { get; }
    public bool DangerWhenOff { get; }
    public bool WarnYellowWhenOff { get; }
    public bool BlocksLeaderboard { get; }

    public CurseDefinition(
        string id,
        string displayName,
        CurseIntensity intensity = null,
        bool dangerWhenOff = false,
        bool warnYellowWhenOff = false,
        bool blocksLeaderboard = true)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Intensity = intensity;
        DangerWhenOff = dangerWhenOff;
        WarnYellowWhenOff = warnYellowWhenOff;
        BlocksLeaderboard = blocksLeaderboard;
    }

    public bool HasIntensity => Intensity != null;
}
