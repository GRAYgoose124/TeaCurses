using System.Collections.Generic;
using HarmonyLib;
using RhythmRift;
using TeaCurses.Curse;
using TeaCurses.Curses;
using Unity.Mathematics;
using UnityEngine;

namespace TeaCurses;

/// <summary>
/// Inverts playfield tile local Z so logical home (Y=0) appears at the top
/// and spawns at the bottom. Logical grid math and player sprite untouched.
/// </summary>
[HarmonyPatch]
public static class UpwardsRift
{
    public const string Name = "UpwardsRift";

    public static bool IsOn => CurseRegistry.IsEnabled(Name);

    private static RRGridView _activeGrid;

    public static void RefreshActiveGrid()
    {
        var grid = _activeGrid;
        if (grid == null || !grid.IsInitialized)
            grid = Object.FindObjectOfType<RRGridView>();

        if (grid == null || !grid.IsInitialized)
            return;

        _activeGrid = grid;
        if (IsOn)
            ApplyInvertedLayout(grid);
        else
            RestoreStockLayout(grid);
    }

    [HarmonyPatch(typeof(RRGridView), nameof(RRGridView.InitTiles))]
    [HarmonyPostfix]
    private static void InitTilesPostfix(RRGridView __instance)
    {
        if (__instance == null || !__instance.IsInitialized)
            return;

        _activeGrid = __instance;
        if (IsOn)
            ApplyInvertedLayout(__instance);
    }

    private static void ApplyInvertedLayout(RRGridView grid)
    {
        if (grid == null || grid._tileViewsByGridPosition == null)
            return;

        int extra = grid._extraVisualOnlyBottomRows;
        int numRows = grid.NumRows;

        foreach (KeyValuePair<int2, RRTileView> pair in grid._tileViewsByGridPosition)
        {
            RRTileView tile = pair.Value;
            if (tile == null)
                continue;

            Vector3 local = tile.transform.localPosition;
            local.z = UpwardsRiftLayout.InvertedLocalZ(pair.Key.y, numRows, extra);
            tile.transform.localPosition = local;
        }

        SyncActionArrows(grid);
        Plugin.Logger?.LogInfo(
            $"UpwardsRift: applied inverted tile layout (actionRowLift={UpwardsRiftLayout.DefaultActionRowLift})");
        ImperfectRifts.NotifyLayoutWritten();
    }

    private static void RestoreStockLayout(RRGridView grid)
    {
        if (grid == null || grid._tileViewsByGridPosition == null)
            return;

        foreach (KeyValuePair<int2, RRTileView> pair in grid._tileViewsByGridPosition)
        {
            RRTileView tile = pair.Value;
            if (tile == null)
                continue;

            Vector3 local = tile.transform.localPosition;
            local.z = pair.Key.y;
            tile.transform.localPosition = local;
        }

        SyncActionArrows(grid);
        Plugin.Logger?.LogInfo("UpwardsRift: restored stock tile layout");
        ImperfectRifts.NotifyLayoutWritten();
    }

    private static void SyncActionArrows(RRGridView grid)
    {
        RRArrowView[] arrows = grid._arrows;
        if (arrows == null)
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
