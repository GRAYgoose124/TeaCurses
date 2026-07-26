using System;
using System.Collections.Generic;

namespace TeaCurses.Curses;

/// <summary>
/// Per-owner afterimage ages and caps. Foreign owners must not age or cull
/// another monster's trail.
/// </summary>
public sealed class AfterimageLedger
{
    private readonly Dictionary<string, List<int>> _ages =
        new Dictionary<string, List<int>>(StringComparer.Ordinal);

    public int TotalCount
    {
        get
        {
            var n = 0;
            foreach (var pair in _ages)
                n += pair.Value.Count;
            return n;
        }
    }

    public IReadOnlyList<int> Ages(string ownerId)
    {
        if (ownerId == null || !_ages.TryGetValue(ownerId, out var list))
            return Array.Empty<int>();
        return list;
    }

    public void Spawn(string ownerId, int intensity)
    {
        if (ownerId == null)
            throw new ArgumentNullException(nameof(ownerId));

        if (!_ages.TryGetValue(ownerId, out var list))
        {
            list = new List<int>();
            _ages[ownerId] = list;
        }

        list.Add(0);
        TrimExcess(list, AfterimageTrail.MaxGhosts(intensity));
    }

    public void AdvanceOwner(string ownerId, int intensity)
    {
        if (ownerId == null || !_ages.TryGetValue(ownerId, out var list))
            return;

        var life = AfterimageTrail.LifetimeBeats(intensity);
        for (var i = list.Count - 1; i >= 0; i--)
        {
            list[i]++;
            if (AfterimageTrail.ShouldCull(list[i], life))
                list.RemoveAt(i);
        }

        TrimExcess(list, AfterimageTrail.MaxGhosts(intensity));
    }

    public void Clear() => _ages.Clear();

    private static void TrimExcess(List<int> list, int maxGhosts)
    {
        var excess = AfterimageTrail.ExcessCount(list.Count, maxGhosts);
        if (excess <= 0)
            return;
        list.RemoveRange(0, excess);
    }
}
