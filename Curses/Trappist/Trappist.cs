using System;
using System.Collections.Generic;
using HarmonyLib;
using RhythmRift;
using RhythmRift.Traps;
using Shared.RhythmEngine;
using TeaCurses.Curse;
using TeaCurses.Curses;
using Unity.Mathematics;
using UnityEngine;
using static RhythmRift.Traps.RRTrapController;

namespace TeaCurses;

/// <summary>
/// Remixes chart traps at spawn; duplicates them into a 1×1→3×3 cluster and morphs types.
/// No translational lane drift. Trapless charts are a no-op.
/// </summary>
[HarmonyPatch]
public static class Trappist
{
    public const string Name = "Trappist";

    public static bool IsOn => CurseRegistry.IsEnabled(Name);

    internal static readonly TrappistLedger Ledger = new TrappistLedger();

    private static readonly System.Random Rng = new System.Random();

    private static int _lastTickBeat = int.MinValue;

    public static int GetIntensity()
    {
        if (CurseRegistry.TryGetIntensity(Name, out var value))
            return Mathf.RoundToInt(value);
        return 5;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RRTrapController), nameof(RRTrapController.Initialize))]
    private static void InitializePrefix(Dictionary<RRTrapType, int> expectedTrapNumbersByType)
    {
        if (!IsOn || expectedTrapNumbersByType == null)
            return;

        ResetLedger();

        // Headroom for 3×3 cluster duplicates per type.
        EnsurePreload(expectedTrapNumbersByType, RRTrapType.Coals, 9);
        EnsurePreload(expectedTrapNumbersByType, RRTrapType.Bounce, 9);
        EnsurePreload(expectedTrapNumbersByType, RRTrapType.PortalIn, 2);
        EnsurePreload(expectedTrapNumbersByType, RRTrapType.PortalOut, 2);
        EnsurePreload(expectedTrapNumbersByType, RRTrapType.Mystery, 9);
    }

    private static void EnsurePreload(Dictionary<RRTrapType, int> dict, RRTrapType type, int minCount)
    {
        if (!dict.ContainsKey(type))
            dict[type] = minCount;
        else if (dict[type] < minCount)
            dict[type] = minCount;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RRTrapController), nameof(RRTrapController.SpawnTrap))]
    private static void SpawnTrapPrefix(RRTrapController __instance, ref TrapSpawnData trapSpawnData)
    {
        if (!IsOn)
            return;

        var loaded = TrappistMutator.GetLoadedKinds(__instance);
        if (loaded.Count == 0)
        {
            loaded = new List<TrappistTrapKind>
            {
                TrappistTrapKind.Coals,
                TrappistTrapKind.Bounce,
                TrappistTrapKind.Mystery,
                TrappistTrapKind.PortalIn,
                TrappistTrapKind.PortalOut,
            };
        }

        if (!TrappistMutator.TryFromGame(trapSpawnData.TrapType, out var kind))
            return;

        var snap = new TrappistSpawnData
        {
            Type = kind,
            DropX = RRGridView.GetLaneGridXValue(trapSpawnData.TrapDropLane),
            DropRow = trapSpawnData.TrapDropRow,
            Health = trapSpawnData.TrapHealth,
            DirectionIndex = trapSpawnData.HasDirection ? (int)trapSpawnData.TrapDirection : -1,
            HasChild = trapSpawnData.HasChildTrapData,
            ChildX = trapSpawnData.HasChildTrapData
                ? RRGridView.GetLaneGridXValue(trapSpawnData.ChildTrapLane)
                : 0,
            ChildRow = trapSpawnData.HasChildTrapData ? trapSpawnData.ChildTrapRow : 0,
        };

        var remixed = TrappistRules.RemixSpawn(snap, GetIntensity(), loaded, Rng);
        ApplyRemixToSpawnData(ref trapSpawnData, remixed);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RRTrapController), nameof(RRTrapController.SpawnTrap))]
    private static void SpawnTrapPostfix(
        RRTrapController __instance,
        TrapSpawnData trapSpawnData,
        FmodTimeCapsule fmodTimeCapsule)
    {
        if (!IsOn || __instance == null)
            return;

        var x = RRGridView.GetLaneGridXValue(trapSpawnData.TrapDropLane);
        var y = trapSpawnData.TrapDropRow;
        var accessor = __instance.GetTrapDataAtPosition(new int2(x, y));
        if (!(accessor is TrapInstance trap))
            return;

        if (!TrappistMutator.TryFromGame(trap.TrapType, out var kind))
            return;

        var beat = Mathf.FloorToInt(fmodTimeCapsule.TrueBeatNumber);

        if (kind == TrappistTrapKind.PortalIn && trap.ChildTrapId != Guid.Empty)
        {
            Ledger.Register(
                trap.TrapId,
                kind,
                trap.GridPosition.x,
                trap.GridPosition.y,
                beat,
                pairId: trap.ChildTrapId,
                isPortalPrimary: true);

            var childAcc = __instance.GetTrapDataWithId(trap.ChildTrapId);
            if (childAcc is TrapInstance child)
            {
                Ledger.Register(
                    child.TrapId,
                    TrappistTrapKind.PortalOut,
                    child.GridPosition.x,
                    child.GridPosition.y,
                    beat,
                    pairId: trap.TrapId,
                    isPortalPrimary: false);
            }
        }
        else if (kind != TrappistTrapKind.PortalOut)
        {
            Ledger.Register(
                trap.TrapId,
                kind,
                trap.GridPosition.x,
                trap.GridPosition.y,
                beat,
                isClusterRoot: true);
        }

        // Front-load cluster growth so short-lived chart traps still expand.
        if (TrappistRules.CanGrowCluster(kind)
            && Ledger.TryGet(trap.TrapId, out var rootEntry))
        {
            var intensity = GetIntensity();
            var loadedKinds = TrappistMutator.GetLoadedKinds(__instance);
            if (loadedKinds.Count == 0)
            {
                loadedKinds = new List<TrappistTrapKind>
                {
                    TrappistTrapKind.Coals,
                    TrappistTrapKind.Bounce,
                    TrappistTrapKind.Mystery,
                    TrappistTrapKind.PortalIn,
                    TrappistTrapKind.PortalOut,
                };
            }

            TryDuplicateBurst(
                __instance,
                rootEntry,
                intensity,
                beat,
                fmodTimeCapsule,
                loadedKinds,
                TrappistRules.SpawnBurstCells(intensity));
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RRTrapController), nameof(RRTrapController.UpdateSystem))]
    private static void UpdateSystemPostfix(RRTrapController __instance, FmodTimeCapsule fmodTimeCapsule)
    {
        if (!IsOn || __instance == null)
            return;

        var beat = Mathf.FloorToInt(fmodTimeCapsule.TrueBeatNumber);
        if (beat == _lastTickBeat)
            return;
        _lastTickBeat = beat;

        PruneMissing(__instance);

        var intensity = GetIntensity();
        var loaded = TrappistMutator.GetLoadedKinds(__instance);
        var snapshot = new List<TrappistLedgerEntry>(Ledger.All());

        foreach (var entry in snapshot)
        {
            if (!Ledger.TryGet(entry.TrapId, out var live))
                continue;

            if (live.PendingRevealKind.HasValue
                && live.CloakUntilBeat >= 0
                && beat > live.CloakUntilBeat)
            {
                ApplyReveal(__instance, live, intensity, beat, fmodTimeCapsule, loaded);
                continue;
            }

            // Only roots drive growth / morph; portal-outs ride with portal-in.
            if (!live.IsClusterRoot)
                continue;
            if (live.Kind == TrappistTrapKind.PortalOut)
                continue;

            if (TrappistRules.CanGrowCluster(live.Kind)
                && TrappistRules.IsDuplicateDue(
                    intensity,
                    beat - live.SpawnBeat,
                    live.LastDuplicateBeat < 0 ? -1 : live.LastDuplicateBeat - live.SpawnBeat))
            {
                TryDuplicateBurst(
                    __instance,
                    live,
                    intensity,
                    beat,
                    fmodTimeCapsule,
                    loaded,
                    TrappistRules.DuplicatesPerTick(intensity));
            }

            if (!Ledger.TryGet(entry.TrapId, out live))
                continue;

            if (TrappistRules.IsMorphDue(
                    intensity,
                    beat - live.SpawnBeat,
                    live.LastMorphBeat < 0 ? -1 : live.LastMorphBeat - live.SpawnBeat))
            {
                TryMorph(__instance, live, intensity, beat, fmodTimeCapsule, loaded);
            }
        }
    }

    private static void TryDuplicateBurst(
        RRTrapController controller,
        TrappistLedgerEntry root,
        int intensity,
        int beat,
        FmodTimeCapsule time,
        List<TrappistTrapKind> loaded,
        int count)
    {
        Ledger.MarkDuplicated(root.TrapId, beat);
        for (var n = 0; n < count; n++)
        {
            if (!TryDuplicateOnce(controller, root, intensity, beat, time, loaded))
                break;
            if (!Ledger.TryGet(root.TrapId, out root))
                break;
        }
    }

    private static bool TryDuplicateOnce(
        RRTrapController controller,
        TrappistLedgerEntry root,
        int intensity,
        int beat,
        FmodTimeCapsule time,
        List<TrappistTrapKind> loaded)
    {
        var trap = controller.GetTrapDataWithId(root.TrapId) as TrapInstance;
        if (trap == null)
        {
            Ledger.Unregister(root.TrapId);
            return false;
        }

        var owned = Ledger.ClusterOwnedCells(root.ClusterRootId);
        var occupied = Ledger.OccupiedCells();
        var cell = TrappistRules.ChooseDuplicateCell(
            root.OriginX,
            root.OriginY,
            owned,
            occupied,
            intensity,
            Rng);

        if (!cell.ShouldDuplicate)
            return false;

        var kind = TrappistRules.ChooseDuplicateKind(loaded, Rng, intensity);
        var orientation = trap.TrapOrientation;
        if (kind == TrappistTrapKind.Bounce)
        {
            orientation = RRUtils.GetTrapOrientationFromDirection(
                TrappistMutator.ToDirection(Rng.Next(0, TrappistRules.DirectionCount)));
        }
        else
        {
            orientation = int2.zero;
        }

        var id = Guid.NewGuid();
        var spawned = controller.SpawnTrapInternal(
            id,
            TrappistMutator.ToGame(kind),
            new int2(cell.X, cell.Y),
            Math.Max(1, trap.CurrentHealth),
            orientation,
            time);

        if (spawned == null)
            return false;

        spawned.HasQueuedDespawnSfx = true;

        Ledger.Register(
            id,
            kind,
            cell.X,
            cell.Y,
            root.SpawnBeat,
            clusterRootId: root.ClusterRootId,
            originX: root.OriginX,
            originY: root.OriginY,
            isClusterRoot: false);
        Ledger.MarkDuplicated(id, beat);
        return true;
    }

    private static void TryMorph(
        RRTrapController controller,
        TrappistLedgerEntry entry,
        int intensity,
        int beat,
        FmodTimeCapsule time,
        List<TrappistTrapKind> loaded)
    {
        if (controller.GetTrapDataWithId(entry.TrapId) == null)
        {
            Ledger.Unregister(entry.TrapId);
            return;
        }

        // Each cluster cell picks its own morph target for more chaos.
        var members = entry.Kind == TrappistTrapKind.PortalIn
            ? new List<TrappistLedgerEntry> { entry }
            : Ledger.MembersOfCluster(entry.ClusterRootId);

        members.Sort((a, b) => b.IsClusterRoot.CompareTo(a.IsClusterRoot));

        foreach (var member in members)
        {
            if (!Ledger.TryGet(member.TrapId, out var live))
                continue;
            if (live.Kind == TrappistTrapKind.PortalOut)
                continue;

            var target = TrappistRules.ChooseMorphTarget(live.Kind, loaded, Rng, intensity);
            // Duplicates stay among duplicable kinds (no new portals mid-cluster).
            if (!live.IsClusterRoot && target == TrappistTrapKind.PortalIn)
                target = TrappistRules.ChooseDuplicateKind(loaded, Rng, intensity);

            var deceit = TrappistRules.SoftDeceitEnabled(intensity);
            var appliedKind = deceit && loaded.Contains(TrappistTrapKind.Mystery)
                ? TrappistTrapKind.Mystery
                : target;
            var pending = deceit && appliedKind == TrappistTrapKind.Mystery && target != TrappistTrapKind.Mystery
                ? (TrappistTrapKind?)target
                : null;
            var cloakUntil = pending.HasValue
                ? TrappistRules.CloakUntilBeatAfterMorph(intensity, beat)
                : -1;

            MorphOne(controller, live, appliedKind, pending, cloakUntil, beat, time);
        }
    }

    private static void ApplyReveal(
        RRTrapController controller,
        TrappistLedgerEntry entry,
        int intensity,
        int beat,
        FmodTimeCapsule time,
        List<TrappistTrapKind> loaded)
    {
        if (!entry.PendingRevealKind.HasValue)
            return;
        var reveal = entry.PendingRevealKind.Value;
        var members = Ledger.MembersOfCluster(entry.ClusterRootId);
        if (members.Count == 0)
            members.Add(entry);

        foreach (var member in members)
        {
            if (!Ledger.TryGet(member.TrapId, out var live))
                continue;
            MorphOne(controller, live, reveal, pending: null, cloakUntil: -1, beat, time);
        }
    }

    private static void MorphOne(
        RRTrapController controller,
        TrappistLedgerEntry entry,
        TrappistTrapKind appliedKind,
        TrappistTrapKind? pending,
        int cloakUntil,
        int beat,
        FmodTimeCapsule time)
    {
        var trap = controller.GetTrapDataWithId(entry.TrapId) as TrapInstance;
        if (trap == null)
        {
            Ledger.Unregister(entry.TrapId);
            return;
        }

        // Demote portal pair when leaving portals.
        if (entry.Kind == TrappistTrapKind.PortalIn && entry.PairId != Guid.Empty
            && appliedKind != TrappistTrapKind.PortalIn)
        {
            var child = controller.GetTrapDataWithId(entry.PairId) as TrapInstance;
            if (child != null)
                TrappistMutator.RemoveTrapFromController(controller, child, returnView: true);
            Ledger.Unregister(entry.PairId);
        }

        // Duplicates cannot become portals (no paired exit).
        if (!entry.IsClusterRoot && appliedKind == TrappistTrapKind.PortalIn)
            appliedKind = TrappistTrapKind.Bounce;

        var orientation = trap.TrapOrientation;
        if (appliedKind == TrappistTrapKind.Bounce)
        {
            orientation = RRUtils.GetTrapOrientationFromDirection(
                TrappistMutator.ToDirection(Rng.Next(0, TrappistRules.DirectionCount)));
        }

        Guid childId = Guid.Empty;
        TrapInstance childTrap = null;

        if (appliedKind == TrappistTrapKind.PortalIn && entry.IsClusterRoot)
        {
            if (entry.PairId != Guid.Empty)
            {
                childTrap = controller.GetTrapDataWithId(entry.PairId) as TrapInstance;
                childId = entry.PairId;
            }

            if (childTrap == null)
            {
                childId = Guid.NewGuid();
                var cx = TrappistRules.ClampLane(entry.X >= TrappistRules.MaxLaneX ? entry.X - 1 : entry.X + 1);
                var cy = entry.Y;
                childTrap = controller.SpawnTrapInternal(
                    childId,
                    RRTrapType.PortalOut,
                    new int2(cx, cy),
                    trap.CurrentHealth,
                    orientation,
                    time);
                if (childTrap != null)
                    childTrap.HasQueuedDespawnSfx = true;
            }
        }

        var id = entry.TrapId;
        var lastDup = entry.LastDuplicateBeat;
        var spawned = TrappistMutator.TryMorphTrap(
            controller,
            trap,
            TrappistMutator.ToGame(appliedKind),
            time,
            muteSfx: true,
            preserveId: id,
            childId: childId,
            orientation: orientation);

        if (spawned == null)
            return;

        if (appliedKind == TrappistTrapKind.PortalIn && childTrap != null)
        {
            spawned.SetChildTrap(childId);
            Ledger.Register(
                childId,
                TrappistTrapKind.PortalOut,
                childTrap.GridPosition.x,
                childTrap.GridPosition.y,
                entry.SpawnBeat,
                pairId: id,
                isPortalPrimary: false);
            Ledger.MarkMorphed(childId, TrappistTrapKind.PortalOut, beat, cloakUntil, null);
        }

        Ledger.Register(
            id,
            appliedKind,
            spawned.GridPosition.x,
            spawned.GridPosition.y,
            entry.SpawnBeat,
            pairId: appliedKind == TrappistTrapKind.PortalIn ? childId : Guid.Empty,
            isPortalPrimary: appliedKind == TrappistTrapKind.PortalIn,
            clusterRootId: entry.ClusterRootId,
            originX: entry.OriginX,
            originY: entry.OriginY,
            isClusterRoot: entry.IsClusterRoot);
        Ledger.MarkMorphed(id, appliedKind, beat, cloakUntil, entry.IsClusterRoot ? pending : null);
        if (lastDup >= 0)
            Ledger.MarkDuplicated(id, lastDup);
    }

    private static void PruneMissing(RRTrapController controller)
    {
        var dead = new List<Guid>();
        foreach (var e in Ledger.All())
        {
            if (controller.GetTrapDataWithId(e.TrapId) == null)
                dead.Add(e.TrapId);
        }

        for (var i = 0; i < dead.Count; i++)
            Ledger.Unregister(dead[i]);
    }

    private static void ApplyRemixToSpawnData(ref TrapSpawnData data, TrappistSpawnData remixed)
    {
        data.TrapType = TrappistMutator.ToGame(remixed.Type);
        data.TrapDropLane = RRGridView.GetGridXValueLaneDesignation(remixed.DropX);
        data.TrapDropRow = remixed.DropRow;
        data.TrapHealth = remixed.Health;

        if (remixed.DirectionIndex >= 0)
            data.TrapDirection = TrappistMutator.ToDirection(remixed.DirectionIndex);

        if (remixed.Type == TrappistTrapKind.PortalIn && remixed.HasChild)
        {
            data.ChildTrapLane = RRGridView.GetGridXValueLaneDesignation(remixed.ChildX);
            data.ChildTrapRow = remixed.ChildRow;
        }
    }

    public static void ResetLedger()
    {
        Ledger.Clear();
        _lastTickBeat = int.MinValue;
    }
}
