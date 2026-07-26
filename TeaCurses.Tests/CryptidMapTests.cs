using System.Collections.Generic;
using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class CryptidMapTests
{
    [Fact]
    public void Assign_is_stable_per_type()
    {
        var map = new CryptidMap();
        map.BeginChart(1, new[] { 0x16A0, 0x16A1, 0x16A2 });

        var a = map.Assign("slime");
        Assert.Equal(a, map.Assign("slime"));
    }

    [Fact]
    public void Assign_unique_until_pool_exhausted()
    {
        var pool = new[] { 10, 20, 30 };
        var map = new CryptidMap();
        map.BeginChart(42, pool);

        var seen = new HashSet<int>
        {
            map.Assign("a"),
            map.Assign("b"),
            map.Assign("c"),
        };
        Assert.Equal(3, seen.Count);
    }

    [Fact]
    public void Different_seeds_reshuffle()
    {
        var pool = CryptidGlyphPool.Default;
        var a = new CryptidMap();
        var b = new CryptidMap();
        a.BeginChart(1, pool);
        b.BeginChart(2, pool);

        var same = 0;
        for (var i = 0; i < 8; i++)
        {
            if (a.Assign("t" + i) == b.Assign("t" + i))
                same++;
        }

        Assert.True(same < 8);
    }

    [Fact]
    public void Debut_flags_track_seen_types()
    {
        var map = new CryptidMap();
        map.BeginChart(0, new[] { 1, 2 });
        Assert.False(map.IsTypeSeen("bat"));
        map.MarkTypeSeen("bat");
        Assert.True(map.IsTypeSeen("bat"));
        Assert.False(map.IsTypeSeen("slime"));
    }

    [Fact]
    public void BeginChart_clears_assignments_and_debuts()
    {
        var map = new CryptidMap();
        map.BeginChart(0, new[] { 1, 2 });
        map.Assign("bat");
        map.MarkTypeSeen("bat");
        map.BeginChart(1, new[] { 9, 8 });
        Assert.False(map.IsTypeSeen("bat"));
        var code = map.Assign("bat");
        Assert.Contains(code, new[] { 9, 8 });
        Assert.Contains(map.Assign("other"), new[] { 9, 8 });
    }

    [Fact]
    public void Pool_wraps_after_exhaustion()
    {
        var map = new CryptidMap();
        map.BeginChart(0, new[] { 5 });
        Assert.Equal(5, map.Assign("a"));
        Assert.Equal(5, map.Assign("b"));
    }

    [Fact]
    public void Default_pool_has_runic_and_cuneiform()
    {
        Assert.True(CryptidGlyphPool.Default.Count >= 32);
        Assert.Contains(CryptidGlyphPool.Default, c => c >= 0x16A0 && c <= 0x16F8);
        Assert.Contains(CryptidGlyphPool.Default, c => c >= 0x12000);
    }
}
