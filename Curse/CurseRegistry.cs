using System;
using System.Collections.Generic;

namespace TeaCurses.Curse;

public static class CurseRegistry
{
    private static readonly List<CurseDefinition> Definitions = new List<CurseDefinition>();
    private static readonly Dictionary<string, bool> Enabled = new Dictionary<string, bool>(StringComparer.Ordinal);
    private static readonly Dictionary<string, float> Intensities = new Dictionary<string, float>(StringComparer.Ordinal);

    public static IReadOnlyList<CurseDefinition> All => Definitions;

    public static void Clear()
    {
        Definitions.Clear();
        Enabled.Clear();
        Intensities.Clear();
    }

    public static void Register(CurseDefinition definition)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));
        if (Enabled.ContainsKey(definition.Id))
            throw new InvalidOperationException($"Curse already registered: {definition.Id}");

        Definitions.Add(definition);
        Enabled[definition.Id] = false;
        if (definition.Intensity != null)
            Intensities[definition.Id] = definition.Intensity.Default;
    }

    public static bool IsEnabled(string id)
    {
        return Enabled.TryGetValue(id, out var on) && on;
    }

    public static void SetEnabled(string id, bool enabled)
    {
        if (!Enabled.ContainsKey(id))
            return;
        Enabled[id] = enabled;
    }

    public static void Toggle(string id)
    {
        if (!Enabled.ContainsKey(id))
            return;
        Enabled[id] = !Enabled[id];
    }

    public static bool TryGetIntensity(string id, out float value)
    {
        return Intensities.TryGetValue(id, out value);
    }

    public static bool TryStepIntensity(string id, int direction)
    {
        if (direction == 0)
            return false;

        var def = Find(id);
        if (def?.Intensity == null)
            return false;

        if (!Intensities.TryGetValue(id, out var current))
            current = def.Intensity.Default;

        var next = current + direction * def.Intensity.Step;
        if (next > def.Intensity.Max)
            next = def.Intensity.Min;
        else if (next < def.Intensity.Min)
            next = def.Intensity.Max;
        Intensities[id] = next;
        return true;
    }

    public static float GetMeterRating(string id)
    {
        var def = Find(id);
        if (def?.Intensity == null)
            throw new InvalidOperationException($"Curse has no intensity: {id}");

        if (!Intensities.TryGetValue(id, out var value))
            value = def.Intensity.Default;

        return def.Intensity.MapToMeter(value);
    }

    public static bool AnyEnabledBlocksLeaderboard()
    {
        for (var i = 0; i < Definitions.Count; i++)
        {
            var def = Definitions[i];
            if (def.BlocksLeaderboard && IsEnabled(def.Id))
                return true;
        }

        return false;
    }

    private static CurseDefinition Find(string id)
    {
        for (var i = 0; i < Definitions.Count; i++)
        {
            if (Definitions[i].Id == id)
                return Definitions[i];
        }

        return null;
    }
}
