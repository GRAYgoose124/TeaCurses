using System;

namespace TeaCurses.UI;

/// <summary>
/// Smooth hue cycle for the overlay title (RGB rainbow).
/// </summary>
public static class TitleRgbCycle
{
    public const float DefaultCyclesPerSecond = 0.25f;

    /// <summary>
    /// Returns hue in [0, 1) for <paramref name="unscaledTime"/> at the given cycle rate.
    /// </summary>
    public static float HueAt(float unscaledTime, float cyclesPerSecond)
    {
        if (cyclesPerSecond <= 0f)
            return 0f;

        var hue = unscaledTime * cyclesPerSecond;
        hue -= (float)Math.Floor(hue);
        if (hue < 0f)
            hue += 1f;
        return hue;
    }
}
