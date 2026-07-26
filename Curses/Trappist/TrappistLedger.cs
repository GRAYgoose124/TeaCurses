using System;
using System.Collections.Generic;

namespace TeaCurses.Curses;

public sealed class TrappistLedgerEntry
{
    public Guid TrapId;
    public Guid PairId; // Guid.Empty if unpaired
    public Guid ClusterRootId;
    public TrappistTrapKind Kind;
    public int X;
    public int Y;
    public int OriginX;
    public int OriginY;
    public int SpawnBeat;
    public int LastDuplicateBeat;
    public int LastMorphBeat;
    public int CloakUntilBeat; // inclusive; -1 = none
    public bool IsPortalPrimary;
    public bool IsClusterRoot;
    /// <summary>When soft deceit cloaks as Mystery, reveal this kind after cloak ends.</summary>
    public TrappistTrapKind? PendingRevealKind;
}

/// <summary>
/// Tracks active Trappist-managed traps for duplicate/morph/deceit.
/// </summary>
public sealed class TrappistLedger
{
    private readonly Dictionary<Guid, TrappistLedgerEntry> _byId =
        new Dictionary<Guid, TrappistLedgerEntry>();

    public int Count => _byId.Count;

    public void Clear() => _byId.Clear();

    public void Register(
        Guid trapId,
        TrappistTrapKind kind,
        int x,
        int y,
        int spawnBeat,
        Guid pairId = default,
        bool isPortalPrimary = false,
        Guid clusterRootId = default,
        int originX = int.MinValue,
        int originY = int.MinValue,
        bool isClusterRoot = true)
    {
        if (clusterRootId == Guid.Empty)
            clusterRootId = trapId;
        if (originX == int.MinValue)
            originX = x;
        if (originY == int.MinValue)
            originY = y;

        _byId[trapId] = new TrappistLedgerEntry
        {
            TrapId = trapId,
            PairId = pairId,
            ClusterRootId = clusterRootId,
            Kind = kind,
            X = x,
            Y = y,
            OriginX = originX,
            OriginY = originY,
            SpawnBeat = spawnBeat,
            LastDuplicateBeat = -1,
            LastMorphBeat = -1,
            CloakUntilBeat = -1,
            IsPortalPrimary = isPortalPrimary,
            IsClusterRoot = isClusterRoot,
        };
    }

    public bool TryGet(Guid trapId, out TrappistLedgerEntry entry) =>
        _byId.TryGetValue(trapId, out entry);

    public void Unregister(Guid trapId)
    {
        if (!_byId.TryGetValue(trapId, out var entry))
            return;

        _byId.Remove(trapId);
        if (entry.PairId != Guid.Empty && _byId.TryGetValue(entry.PairId, out var pair))
        {
            pair.PairId = Guid.Empty;
            pair.IsPortalPrimary = false;
            _byId[entry.PairId] = pair;
        }
    }

    public void UnregisterPair(Guid primaryId)
    {
        if (!_byId.TryGetValue(primaryId, out var primary))
            return;
        var pairId = primary.PairId;
        _byId.Remove(primaryId);
        if (pairId != Guid.Empty)
            _byId.Remove(pairId);
    }

    public void UnregisterCluster(Guid rootId)
    {
        var dead = new List<Guid>();
        foreach (var e in _byId.Values)
        {
            if (e.ClusterRootId == rootId)
                dead.Add(e.TrapId);
        }

        for (var i = 0; i < dead.Count; i++)
            _byId.Remove(dead[i]);
    }

    public IEnumerable<TrappistLedgerEntry> All() => _byId.Values;

    public List<TrappistLedgerEntry> MembersOfCluster(Guid rootId)
    {
        var list = new List<TrappistLedgerEntry>();
        foreach (var e in _byId.Values)
        {
            if (e.ClusterRootId == rootId)
                list.Add(e);
        }

        return list;
    }

    public HashSet<(int X, int Y)> OccupiedCells()
    {
        var set = new HashSet<(int X, int Y)>();
        foreach (var e in _byId.Values)
            set.Add((e.X, e.Y));
        return set;
    }

    public HashSet<(int X, int Y)> ClusterOwnedCells(Guid rootId)
    {
        var set = new HashSet<(int X, int Y)>();
        foreach (var e in _byId.Values)
        {
            if (e.ClusterRootId == rootId)
                set.Add((e.X, e.Y));
        }

        return set;
    }

    public void UpdatePosition(Guid trapId, int x, int y)
    {
        if (!_byId.TryGetValue(trapId, out var e))
            return;
        e.X = x;
        e.Y = y;
        _byId[trapId] = e;
    }

    public void MarkDuplicated(Guid trapId, int beat)
    {
        if (!_byId.TryGetValue(trapId, out var e))
            return;
        e.LastDuplicateBeat = beat;
        _byId[trapId] = e;
    }

    public void MarkMorphed(
        Guid trapId,
        TrappistTrapKind newKind,
        int beat,
        int cloakUntil,
        TrappistTrapKind? pendingReveal = null)
    {
        if (!_byId.TryGetValue(trapId, out var e))
            return;
        e.Kind = newKind;
        e.LastMorphBeat = beat;
        e.CloakUntilBeat = cloakUntil;
        e.PendingRevealKind = pendingReveal;
        _byId[trapId] = e;
    }

    public void ClearPendingReveal(Guid trapId)
    {
        if (!_byId.TryGetValue(trapId, out var e))
            return;
        e.PendingRevealKind = null;
        e.CloakUntilBeat = -1;
        _byId[trapId] = e;
    }

    public bool IsCloaked(Guid trapId, int intensity, int currentBeat)
    {
        if (!_byId.TryGetValue(trapId, out var e))
            return false;
        return TrappistRules.IsCloaked(intensity, currentBeat, e.CloakUntilBeat);
    }
}
