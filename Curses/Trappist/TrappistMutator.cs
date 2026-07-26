using System;
using System.Collections.Generic;
using HarmonyLib;
using RhythmRift;
using RhythmRift.Traps;
using Shared.RhythmEngine;
using Unity.Mathematics;
using UnityEngine;
using static RhythmRift.Traps.RRTrapController;

namespace TeaCurses.Curses;

/// <summary>
/// Runtime glue: move trap grid entries / views; morph via muted destroy+respawn.
/// </summary>
public static class TrappistMutator
{
    private static AccessTools.FieldRef<TrapInstance, int2> GridPositionRef;
    private static AccessTools.FieldRef<TrapInstance, int> CurrentHealthRef;
    private static AccessTools.FieldRef<RRTrapController, List<TrapInstance>> ActiveListRef;
    private static AccessTools.FieldRef<RRTrapController, Dictionary<int2, TrapInstance>> ByPosRef;
    private static AccessTools.FieldRef<RRTrapController, Dictionary<Guid, TrapInstance>> ByIdRef;
    private static AccessTools.FieldRef<RRTrapController, IRRGridDataAccessor> GridAccessorRef;

    private static bool _bound;

    public static void EnsureBound()
    {
        if (_bound)
            return;
        try
        {
            GridPositionRef = AccessTools.FieldRefAccess<TrapInstance, int2>("_gridPosition");
            CurrentHealthRef = AccessTools.FieldRefAccess<TrapInstance, int>("_currentHealth");
            ActiveListRef = AccessTools.FieldRefAccess<RRTrapController, List<TrapInstance>>("_activeTrapInstances");
            ByPosRef = AccessTools.FieldRefAccess<RRTrapController, Dictionary<int2, TrapInstance>>("_trapInstancesByGridPosition");
            ByIdRef = AccessTools.FieldRefAccess<RRTrapController, Dictionary<Guid, TrapInstance>>("_trapInstancesById");
            GridAccessorRef = AccessTools.FieldRefAccess<RRTrapController, IRRGridDataAccessor>("_gridDataAccessor");
            _bound = true;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogWarning($"TrappistMutator: bind failed: {ex.Message}");
        }
    }

    public static List<TrappistTrapKind> GetLoadedKinds(RRTrapController controller)
    {
        EnsureBound();
        var list = new List<TrappistTrapKind>();
        if (controller == null)
            return list;

        var field = AccessTools.Field(typeof(RRTrapController), "_loadedTrapAssetDataByType");
        if (field == null)
            return list;

        var dict = field.GetValue(controller) as System.Collections.IDictionary;
        if (dict == null)
            return list;

        foreach (var key in dict.Keys)
        {
            if (key is RRTrapType rt && TryFromGame(rt, out var kind))
                list.Add(kind);
        }

        return list;
    }

    public static bool TryMoveTrap(
        RRTrapController controller,
        TrapInstance trap,
        int newX,
        int newY)
    {
        EnsureBound();
        if (controller == null || trap == null || !_bound)
            return false;

        var byPos = ByPosRef(controller);
        var oldPos = GridPositionRef(trap);
        var newPos = new int2(newX, newY);
        if (oldPos.Equals(newPos))
            return true;

        if (byPos.ContainsKey(newPos))
            return false;

        byPos.Remove(oldPos);
        GridPositionRef(trap) = newPos;
        byPos[newPos] = trap;

        var view = trap.ActiveTrapView;
        if (view != null)
        {
            var grid = GridAccessorRef(controller);
            if (grid != null)
            {
                var world = grid.GetTileWorldPositionFromGridPosition(newX, newY);
                view.transform.position = world;
            }
        }

        return true;
    }

    public static TrapInstance TryMorphTrap(
        RRTrapController controller,
        TrapInstance trap,
        RRTrapType newType,
        FmodTimeCapsule time,
        bool muteSfx,
        Guid preserveId,
        Guid childId,
        int2 orientation)
    {
        EnsureBound();
        if (controller == null || trap == null || !_bound)
            return null;

        var pos = GridPositionRef(trap);
        var health = CurrentHealthRef(trap);
        if (health < 1)
            health = 1;

        RemoveTrapFromController(controller, trap, returnView: true);

        var spawned = controller.SpawnTrapInternal(
            preserveId,
            newType,
            pos,
            health,
            orientation,
            time);

        if (spawned == null)
            return null;

        if (muteSfx)
            spawned.HasQueuedDespawnSfx = true;

        if (childId != Guid.Empty && newType == RRTrapType.PortalIn)
            spawned.SetChildTrap(childId);

        return spawned;
    }

    public static void RemoveTrapFromController(RRTrapController controller, TrapInstance trap, bool returnView)
    {
        EnsureBound();
        if (controller == null || trap == null || !_bound)
            return;

        var byPos = ByPosRef(controller);
        var byId = ByIdRef(controller);
        var active = ActiveListRef(controller);

        byPos.Remove(GridPositionRef(trap));
        byId.Remove(trap.TrapId);
        active.Remove(trap);

        if (returnView)
        {
            var view = trap.ActiveTrapView;
            if (view != null)
                view.ReturnToPool();
        }
    }

    public static bool TryFromGame(RRTrapType type, out TrappistTrapKind kind)
    {
        switch (type)
        {
            case RRTrapType.Coals:
                kind = TrappistTrapKind.Coals;
                return true;
            case RRTrapType.PortalIn:
                kind = TrappistTrapKind.PortalIn;
                return true;
            case RRTrapType.PortalOut:
                kind = TrappistTrapKind.PortalOut;
                return true;
            case RRTrapType.Bounce:
                kind = TrappistTrapKind.Bounce;
                return true;
            case RRTrapType.Mystery:
                kind = TrappistTrapKind.Mystery;
                return true;
            default:
                kind = TrappistTrapKind.Coals;
                return false;
        }
    }

    public static RRTrapType ToGame(TrappistTrapKind kind)
    {
        switch (kind)
        {
            case TrappistTrapKind.PortalIn:
                return RRTrapType.PortalIn;
            case TrappistTrapKind.PortalOut:
                return RRTrapType.PortalOut;
            case TrappistTrapKind.Bounce:
                return RRTrapType.Bounce;
            case TrappistTrapKind.Mystery:
                return RRTrapType.Mystery;
            default:
                return RRTrapType.Coals;
        }
    }

    public static RRTrapDirection ToDirection(int directionIndex)
    {
        if (directionIndex < 0)
            directionIndex = 0;
        directionIndex %= 8;
        return (RRTrapDirection)directionIndex;
    }
}
