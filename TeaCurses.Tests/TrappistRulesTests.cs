using System;
using System.Collections.Generic;
using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class TrappistRulesTests
{
    private static readonly TrappistTrapKind[] AllPlayable =
    {
        TrappistTrapKind.Coals,
        TrappistTrapKind.Bounce,
        TrappistTrapKind.Mystery,
        TrappistTrapKind.PortalIn,
        TrappistTrapKind.PortalOut,
    };

    [Theory]
    [InlineData(1, 2, 5, 1, 1)]
    [InlineData(2, 2, 5, 1, 1)]
    [InlineData(3, 1, 7, 2, 2)]
    [InlineData(5, 1, 7, 2, 2)]
    [InlineData(6, 1, 9, 3, 3)]
    [InlineData(8, 1, 9, 3, 3)]
    [InlineData(9, 1, 9, 4, 5)]
    [InlineData(10, 1, 9, 4, 5)]
    public void Duplicate_band_matches_intensity(
        int intensity,
        int period,
        int maxCells,
        int perTick,
        int spawnBurst)
    {
        Assert.Equal(period, TrappistRules.DuplicatePeriodBeats(intensity));
        Assert.Equal(maxCells, TrappistRules.MaxClusterCells(intensity));
        Assert.Equal(perTick, TrappistRules.DuplicatesPerTick(intensity));
        Assert.Equal(spawnBurst, TrappistRules.SpawnBurstCells(intensity));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Morph_every_beat_including_immediate_first(int intensity)
    {
        Assert.Equal(1, TrappistRules.MorphPeriodBeats(intensity));
        Assert.True(TrappistRules.IsMorphDue(intensity, 0, -1));
        Assert.True(TrappistRules.IsMorphDue(intensity, 1, 0));
    }

    [Fact]
    public void Duplicate_first_opportunity_is_immediate_for_short_lived_traps()
    {
        Assert.True(TrappistRules.IsDuplicateDue(1, 0, -1));
        Assert.True(TrappistRules.IsDuplicateDue(10, 0, -1));
        Assert.False(TrappistRules.IsDuplicateDue(1, 0, 0)); // same beat already fired
        Assert.True(TrappistRules.IsDuplicateDue(1, 2, 0));
    }

    [Theory]
    [InlineData(5, false, false)]
    [InlineData(6, true, false)]
    [InlineData(9, true, true)]
    public void Soft_deceit_thresholds(int intensity, bool deceit, bool extraBeat)
    {
        Assert.Equal(deceit, TrappistRules.SoftDeceitEnabled(intensity));
        Assert.Equal(extraBeat, TrappistRules.SoftDeceitExtraBeatAfterMorph(intensity));
    }

    [Fact]
    public void Portal_roots_can_grow_cluster_but_duplicate_cells_are_non_portal()
    {
        Assert.True(TrappistRules.CanGrowCluster(TrappistTrapKind.PortalIn));
        Assert.True(TrappistRules.CanGrowCluster(TrappistTrapKind.Coals));
        Assert.False(TrappistRules.CanGrowCluster(TrappistTrapKind.PortalOut));
        Assert.False(TrappistRules.CanDuplicate(TrappistTrapKind.PortalIn));
        Assert.True(TrappistRules.CanDuplicate(TrappistTrapKind.Bounce));
    }

    [Fact]
    public void Duplicate_kind_varies_among_loaded_non_portals()
    {
        var loaded = AllPlayable;
        var rng = new Random(11);
        var seen = new HashSet<TrappistTrapKind>();
        for (var n = 0; n < 60; n++)
        {
            var kind = TrappistRules.ChooseDuplicateKind(loaded, rng, 10);
            Assert.True(TrappistRules.CanDuplicate(kind));
            Assert.NotEqual(TrappistTrapKind.PortalIn, kind);
            seen.Add(kind);
        }

        Assert.True(seen.Count >= 2);
    }

    [Fact]
    public void Remix_never_picks_disallowed_or_unloaded_types()
    {
        var loaded = new[] { TrappistTrapKind.Coals, TrappistTrapKind.Bounce };
        var spawn = new TrappistSpawnData
        {
            Type = TrappistTrapKind.Coals,
            DropX = 2,
            DropRow = 4,
            Health = 4,
            DirectionIndex = -1,
        };
        var rng = new Random(7);
        for (var n = 0; n < 80; n++)
        {
            var remixed = TrappistRules.RemixSpawn(spawn, intensity: 10, loaded, rng);
            Assert.True(TrappistRules.IsAllowedTarget(remixed.Type));
            Assert.True(remixed.Type == TrappistTrapKind.Coals || remixed.Type == TrappistTrapKind.Bounce);
        }
    }

    [Fact]
    public void Duplicate_prefers_cardinal_neighbor_inside_3x3()
    {
        var owned = new HashSet<(int X, int Y)> { (1, 4) };
        var cell = TrappistRules.ChooseDuplicateCell(1, 4, owned, new HashSet<(int, int)>(), 10, new Random(0));
        Assert.True(cell.ShouldDuplicate);
        Assert.Equal(1, Math.Abs(cell.X - 1) + Math.Abs(cell.Y - 4));
    }

    [Fact]
    public void Duplicate_respects_max_cells()
    {
        var owned = new HashSet<(int X, int Y)> { (1, 4), (0, 4), (2, 4), (1, 3), (1, 5) };
        // I=1 max is 5 — full.
        Assert.False(TrappistRules.ChooseDuplicateCell(1, 4, owned, new HashSet<(int, int)>(), 1, new Random(2)).ShouldDuplicate);
    }

    [Fact]
    public void Cloak_thresholds()
    {
        Assert.Equal(-1, TrappistRules.CloakUntilBeatAfterMorph(5, 10));
        Assert.Equal(10, TrappistRules.CloakUntilBeatAfterMorph(6, 10));
        Assert.Equal(11, TrappistRules.CloakUntilBeatAfterMorph(9, 10));
    }
}

public class TrappistLedgerTests
{
    [Fact]
    public void Register_and_cluster_members()
    {
        var ledger = new TrappistLedger();
        var root = Guid.NewGuid();
        var dupe = Guid.NewGuid();
        ledger.Register(root, TrappistTrapKind.Bounce, 1, 4, 0, isClusterRoot: true);
        ledger.Register(
            dupe,
            TrappistTrapKind.Mystery,
            2,
            4,
            0,
            clusterRootId: root,
            originX: 1,
            originY: 4,
            isClusterRoot: false);
        Assert.Equal(2, ledger.MembersOfCluster(root).Count);
        Assert.Contains((2, 4), ledger.ClusterOwnedCells(root));
    }
}
