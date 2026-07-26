using System;

namespace TeaCurses.Curses;

/// <summary>
/// Per-enemy visibility duty cycle: exactly <see cref="VisibleBeats"/> phases
/// visible in each window of <see cref="WindowBeats"/> enemy-update beats.
/// </summary>
public sealed class BlinkSchedule
{
    private readonly bool[] _visiblePhases;

    public int VisibleBeats { get; }
    public int WindowBeats { get; }

    private BlinkSchedule(int visibleBeats, int windowBeats, bool[] visiblePhases)
    {
        VisibleBeats = visibleBeats;
        WindowBeats = windowBeats;
        _visiblePhases = visiblePhases;
    }

    /// <summary>
    /// Intensity I (1–10): W ~ U(1..I), V ~ U(1..W); V random phases marked visible.
    /// </summary>
    public static BlinkSchedule Roll(int intensity, Random rng)
    {
        if (rng == null)
            throw new ArgumentNullException(nameof(rng));

        var i = intensity;
        if (i < 1) i = 1;
        if (i > 10) i = 10;

        var window = rng.Next(1, i + 1);
        var visible = rng.Next(1, window + 1);
        return Create(visible, window, rng);
    }

    public static BlinkSchedule Create(int visibleBeats, int windowBeats, Random rng)
    {
        if (rng == null)
            throw new ArgumentNullException(nameof(rng));
        if (windowBeats < 1)
            throw new ArgumentOutOfRangeException(nameof(windowBeats));
        if (visibleBeats < 1 || visibleBeats > windowBeats)
            throw new ArgumentOutOfRangeException(nameof(visibleBeats));

        var phases = new bool[windowBeats];
        var order = new int[windowBeats];
        for (var i = 0; i < windowBeats; i++)
            order[i] = i;

        // Partial Fisher–Yates: shuffle first visibleBeats slots into place.
        for (var i = 0; i < visibleBeats; i++)
        {
            var j = rng.Next(i, windowBeats);
            var tmp = order[i];
            order[i] = order[j];
            order[j] = tmp;
            phases[order[i]] = true;
        }

        return new BlinkSchedule(visibleBeats, windowBeats, phases);
    }

    /// <summary>Always-visible schedule (curse off / intensity edge).</summary>
    public static BlinkSchedule AlwaysVisible { get; } = CreateFixedVisible(1, 1);

    public static BlinkSchedule CreateFixedVisible(int visibleBeats, int windowBeats)
    {
        if (windowBeats < 1)
            throw new ArgumentOutOfRangeException(nameof(windowBeats));
        if (visibleBeats < 1 || visibleBeats > windowBeats)
            throw new ArgumentOutOfRangeException(nameof(visibleBeats));

        var phases = new bool[windowBeats];
        for (var i = 0; i < visibleBeats; i++)
            phases[i] = true;
        return new BlinkSchedule(visibleBeats, windowBeats, phases);
    }

    public bool IsVisible(int phase)
    {
        if (_visiblePhases == null || _visiblePhases.Length == 0)
            return true;
        var w = _visiblePhases.Length;
        var p = phase % w;
        if (p < 0)
            p += w;
        return _visiblePhases[p];
    }
}
