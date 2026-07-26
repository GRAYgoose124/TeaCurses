using System;
using System.Collections.Generic;
using TeaCurses.UI;
using Xunit;

namespace TeaCurses.Tests;

public class AlbumArtPickerTests
{
    [Fact]
    public void TryPickIndex_empty_returns_minus_one()
    {
        Assert.Equal(-1, AlbumArtPicker.TryPickIndex(Array.Empty<bool>(), new Random(1)));
    }

    [Fact]
    public void TryPickIndex_all_false_returns_minus_one()
    {
        Assert.Equal(-1, AlbumArtPicker.TryPickIndex(new[] { false, false }, new Random(1)));
    }

    [Fact]
    public void TryPickIndex_only_selects_usable_indices()
    {
        var usable = new[] { false, true, false, true };
        var seen = new HashSet<int>();
        for (var seed = 0; seed < 50; seed++)
            seen.Add(AlbumArtPicker.TryPickIndex(usable, new Random(seed)));
        Assert.DoesNotContain(-1, seen);
        Assert.All(seen, i => Assert.True(usable[i]));
        Assert.Contains(1, seen);
        Assert.Contains(3, seen);
    }

    [Fact]
    public void TryPickIndex_seeded_rng_is_deterministic()
    {
        var usable = new[] { true, true, true };
        var a = AlbumArtPicker.TryPickIndex(usable, new Random(42));
        var b = AlbumArtPicker.TryPickIndex(usable, new Random(42));
        Assert.Equal(a, b);
    }
}
