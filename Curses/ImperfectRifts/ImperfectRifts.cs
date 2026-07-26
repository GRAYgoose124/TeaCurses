using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RhythmRift;
using Shared;
using TeaCurses.Curse;
using Unity.Mathematics;
using UnityEngine;

namespace TeaCurses;

/// <summary>
/// Rating-driven field chaos: non-Perfect hits drift/shake tiles;
/// Perfects repair. Bases are cached after layout curses write absolute positions.
/// </summary>
[HarmonyPatch]
public static class ImperfectRifts
{
    public const string Name = "ImperfectRifts";

    public static bool IsOn => CurseRegistry.IsEnabled(Name);

    private static float Chaos;
    private static RRGridView _activeGrid;
    private static readonly Dictionary<int2, Vector3> Bases = new Dictionary<int2, Vector3>();

    /// <summary>
    /// Zero chaos and re-apply (stock/layout bases). Chart begin, grid init, and stage retry.
    /// </summary>
    public static void ResetChaosForNewAttempt()
    {
        Chaos = 0f;
        EnsureGrid();
        if (!IsOn)
            return;

        CacheBases(_activeGrid);
        ApplyOffsets(Time.unscaledTime);
    }

    public static void NotifyLayoutWritten()
    {
        if (!IsOn)
            return;

        EnsureGrid();
        CacheBases(_activeGrid);
        ApplyOffsets(Time.unscaledTime);
    }

    public static void OnOverlayToggled(bool enabled)
    {
        EnsureGrid();
        if (enabled)
        {
            CacheBases(_activeGrid);
            ApplyOffsets(Time.unscaledTime);
            return;
        }

        RestoreBases(_activeGrid);
        Chaos = 0f;
        Bases.Clear();
    }

    [HarmonyPatch(typeof(RRStageController), nameof(RRStageController.BeginPlay))]
    [HarmonyPostfix]
    private static void BeginPlayPostfix(RRStageController __instance)
    {
        if (__instance?._gridView != null)
            _activeGrid = __instance._gridView;
        ResetChaosForNewAttempt();
    }

    [HarmonyPatch(typeof(RRGridView), nameof(RRGridView.InitTiles))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void InitTilesPostfix(RRGridView __instance)
    {
        if (__instance == null || !__instance.IsInitialized)
            return;

        _activeGrid = __instance;
        ResetChaosForNewAttempt();
    }

    /// <summary>
    /// Pause / quick / results retry often goes through <see cref="StageController{T}.RetryStage"/>
    /// without a fresh BeginPlay on the same stage instance.
    /// </summary>
    [HarmonyPatch]
    private static class RetryStagePatch
    {
        private static MethodBase TargetMethod()
        {
            var closed = typeof(StageController<>).MakeGenericType(typeof(RRBeatmapPlayer));
            return AccessTools.Method(
                closed,
                "RetryStage",
                new[] { typeof(bool), typeof(bool), typeof(bool) });
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            ResetChaosForNewAttempt();
        }
    }

    [HarmonyPatch(typeof(RRStageController), nameof(RRStageController.ProcessHitData))]
    [HarmonyPostfix]
    private static void ProcessHitDataPostfix(
        List<RREnemyController.EnemyHitData> hitDatas,
        bool isBaneInput)
    {
        if (!IsOn || isBaneInput)
            return;

        int intensity = CurrentIntensity();

        if (hitDatas == null || hitDatas.Count == 0)
        {
            Chaos = ImperfectRiftsRules.ApplyRating(Chaos, ImperfectRiftsRating.Miss, intensity);
            return;
        }

        for (int i = 0; i < hitDatas.Count; i++)
        {
            Chaos = ImperfectRiftsRules.ApplyRating(
                Chaos,
                MapRating(hitDatas[i].InputRating),
                intensity);
        }
    }

    [HarmonyPatch(typeof(RRStageController), nameof(RRStageController.LateUpdate))]
    [HarmonyPostfix]
    private static void LateUpdatePostfix()
    {
        if (!IsOn)
            return;

        ApplyOffsets(Time.unscaledTime);
    }

    private static ImperfectRiftsRating MapRating(InputRating rating)
    {
        switch (rating)
        {
            case InputRating.Perfect:
                return ImperfectRiftsRating.Perfect;
            case InputRating.Great:
                return ImperfectRiftsRating.Great;
            case InputRating.Good:
                return ImperfectRiftsRating.Good;
            case InputRating.Ok:
                return ImperfectRiftsRating.Ok;
            case InputRating.Miss:
            default:
                return ImperfectRiftsRating.Miss;
        }
    }

    private static int CurrentIntensity()
    {
        if (!CurseRegistry.TryGetIntensity(Name, out float value))
            return ImperfectRiftsRules.DefaultIntensity;
        return ImperfectRiftsRules.ClampIntensity(Mathf.RoundToInt(value));
    }

    private static void EnsureGrid()
    {
        if (_activeGrid != null && _activeGrid.IsInitialized)
            return;

        var grid = Object.FindObjectOfType<RRGridView>();
        if (grid != null && grid.IsInitialized)
            _activeGrid = grid;
    }

    private static void CacheBases(RRGridView grid)
    {
        Bases.Clear();
        if (grid == null || grid._tileViewsByGridPosition == null)
            return;

        foreach (KeyValuePair<int2, RRTileView> pair in grid._tileViewsByGridPosition)
        {
            RRTileView tile = pair.Value;
            if (tile == null)
                continue;
            Bases[pair.Key] = tile.transform.localPosition;
        }
    }

    private static void RestoreBases(RRGridView grid)
    {
        if (grid == null || grid._tileViewsByGridPosition == null || Bases.Count == 0)
            return;

        foreach (KeyValuePair<int2, RRTileView> pair in grid._tileViewsByGridPosition)
        {
            RRTileView tile = pair.Value;
            if (tile == null || !Bases.TryGetValue(pair.Key, out Vector3 baseLocal))
                continue;
            tile.transform.localPosition = baseLocal;
        }

        SyncActionArrows(grid);
    }

    private static void ApplyOffsets(float time)
    {
        var grid = _activeGrid;
        if (grid == null || !grid.IsInitialized || grid._tileViewsByGridPosition == null)
            return;
        if (Bases.Count == 0)
            CacheBases(grid);

        int intensity = CurrentIntensity();

        foreach (KeyValuePair<int2, RRTileView> pair in grid._tileViewsByGridPosition)
        {
            RRTileView tile = pair.Value;
            if (tile == null || !Bases.TryGetValue(pair.Key, out Vector3 baseLocal))
                continue;

            ImperfectRiftsRules.DriftOffset(
                Chaos, intensity, pair.Key.x, pair.Key.y, out float dx, out float dz);
            ImperfectRiftsRules.ShakeOffset(
                Chaos, intensity, pair.Key.x, pair.Key.y, time, out float sx, out float sz);

            Vector3 local = baseLocal;
            local.x = baseLocal.x + dx + sx;
            local.z = baseLocal.z + dz + sz;
            tile.transform.localPosition = local;
        }

        SyncActionArrows(grid);
    }

    private static void SyncActionArrows(RRGridView grid)
    {
        RRArrowView[] arrows = grid._arrows;
        if (arrows == null || grid._tileViewsByGridPosition == null)
            return;

        var tiles = grid._tileViewsByGridPosition;
        for (int i = 0; i < arrows.Length; i++)
        {
            RRArrowView arrow = arrows[i];
            if (arrow == null)
                continue;

            if (!tiles.TryGetValue(new int2(i, 0), out RRTileView homeTile) || homeTile == null)
                continue;

            arrow.transform.localPosition = homeTile.transform.localPosition;
        }
    }
}
