using System;

namespace TeaCurses;

public enum ImperfectRiftsRating
{
    Perfect,
    Great,
    Good,
    Ok,
    Miss,
}

/// <summary>
/// Chaos meter + XZ drift/shake for Imperfect Rifts.
/// </summary>
public static class ImperfectRiftsRules
{
    public const int MinIntensity = 1;
    public const int MaxIntensity = 10;
    public const int DefaultIntensity = 5;

    // Base deltas at intensity 5 before intensity multipliers.
    private const float HealPerfect = 0.08f;
    private const float DmgGreat = 0.03f;
    private const float DmgGood = 0.06f;
    private const float DmgOk = 0.10f;
    private const float DmgMiss = 0.16f;

    private const float MaxDrift = 0.45f;
    private const float MaxShake = 0.06f;

    public static float ApplyRating(float chaos, ImperfectRiftsRating rating, int intensity)
    {
        intensity = ClampIntensity(intensity);
        float t = (intensity - MinIntensity) / (float)(MaxIntensity - MinIntensity);
        float damageMult = Lerp(0.5f, 1.5f, t);
        float healMult = Lerp(1.5f, 0.5f, t);

        float delta = rating switch
        {
            ImperfectRiftsRating.Perfect => -HealPerfect * healMult,
            ImperfectRiftsRating.Great => DmgGreat * damageMult,
            ImperfectRiftsRating.Good => DmgGood * damageMult,
            ImperfectRiftsRating.Ok => DmgOk * damageMult,
            ImperfectRiftsRating.Miss => DmgMiss * damageMult,
            _ => 0f,
        };

        return Clamp01(chaos + delta);
    }

    public static void DriftOffset(
        float chaos, int intensity, int seedX, int seedY, out float dx, out float dz)
    {
        if (chaos <= 0f)
        {
            dx = 0f;
            dz = 0f;
            return;
        }

        intensity = ClampIntensity(intensity);
        float amp = MaxDrift * chaos * (intensity / (float)MaxIntensity);
        float angle = Hash01(seedX * 73856093 ^ seedY * 19349663) * (float)(Math.PI * 2.0);
        dx = (float)Math.Cos(angle) * amp;
        dz = (float)Math.Sin(angle) * amp;
    }

    public static void ShakeOffset(
        float chaos, int intensity, int seedX, int seedY, float time, out float dx, out float dz)
    {
        if (chaos <= 0f)
        {
            dx = 0f;
            dz = 0f;
            return;
        }

        intensity = ClampIntensity(intensity);
        float amp = MaxShake * chaos * (intensity / (float)MaxIntensity);
        float phase = Hash01(seedX * 83492791 ^ seedY * 297121507) * (float)(Math.PI * 2.0);
        float wx = 11.3f + Hash01(seedX + 17) * 4f;
        float wz = 13.7f + Hash01(seedY + 31) * 4f;
        dx = (float)Math.Sin(time * wx + phase) * amp;
        dz = (float)Math.Cos(time * wz + phase * 1.7f) * amp;
    }

    public static int ClampIntensity(int intensity)
    {
        if (intensity < MinIntensity) return MinIntensity;
        if (intensity > MaxIntensity) return MaxIntensity;
        return intensity;
    }

    private static float Clamp01(float v)
    {
        if (v < 0f) return 0f;
        if (v > 1f) return 1f;
        return v;
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static float Hash01(int n)
    {
        unchecked
        {
            uint x = (uint)n;
            x ^= x >> 16;
            x *= 0x7feb352du;
            x ^= x >> 15;
            x *= 0x846ca68bu;
            x ^= x >> 16;
            return (x & 0xFFFFFFu) / 16777215f;
        }
    }
}
