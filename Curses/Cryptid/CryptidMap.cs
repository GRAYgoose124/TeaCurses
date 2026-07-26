using System;
using System.Collections.Generic;

namespace TeaCurses.Curses;

/// <summary>
/// Per-chart type→codepoint assignment and debut tracking.
/// </summary>
public sealed class CryptidMap
{
    private readonly List<int> _shuffled = new List<int>();
    private readonly Dictionary<string, int> _assigned =
        new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly HashSet<string> _seen =
        new HashSet<string>(StringComparer.Ordinal);
    private int _nextIndex;

    public void BeginChart(int seed, IReadOnlyList<int> pool)
    {
        _assigned.Clear();
        _seen.Clear();
        _nextIndex = 0;
        _shuffled.Clear();
        if (pool == null || pool.Count == 0)
            return;

        for (var i = 0; i < pool.Count; i++)
            _shuffled.Add(pool[i]);

        var rng = new Random(seed);
        for (var i = _shuffled.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            var tmp = _shuffled[i];
            _shuffled[i] = _shuffled[j];
            _shuffled[j] = tmp;
        }
    }

    public int Assign(string typeKey)
    {
        if (string.IsNullOrEmpty(typeKey))
            throw new ArgumentException("typeKey required", nameof(typeKey));

        if (_assigned.TryGetValue(typeKey, out var existing))
            return existing;

        if (_shuffled.Count == 0)
            throw new InvalidOperationException("CryptidMap has empty pool");

        var codepoint = _shuffled[_nextIndex % _shuffled.Count];
        _nextIndex++;
        _assigned[typeKey] = codepoint;
        return codepoint;
    }

    public bool IsTypeSeen(string typeKey)
    {
        return !string.IsNullOrEmpty(typeKey) && _seen.Contains(typeKey);
    }

    public void MarkTypeSeen(string typeKey)
    {
        if (!string.IsNullOrEmpty(typeKey))
            _seen.Add(typeKey);
    }
}
