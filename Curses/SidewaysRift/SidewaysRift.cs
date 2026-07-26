using System.Collections.Generic;
using HarmonyLib;
using RhythmRift;
using RhythmRift.Enemies;
using TeaCurses.Curse;
using TeaCurses.Curses;
using Unity.Mathematics;
using UnityEngine;

namespace TeaCurses;

/// <summary>
/// L-path playfield: far rows approach from side walls, turn at row 2,
/// then top-down through rows 1–0. Logical grid math unchanged.
/// Middle-lane spawns alternate Left/Right; tiles use Left as the middle guide.
/// </summary>
[HarmonyPatch]
public static class SidewaysRift
{
    public const string Name = "SidewaysRift";

    public static bool IsOn => CurseRegistry.IsEnabled(Name);

    private static RRGridView _activeGrid;
    private static int _middleSpawnCount;
    private static readonly Dictionary<int, SidewaysRiftSide> EnemySides =
        new Dictionary<int, SidewaysRiftSide>();
    private static readonly Dictionary<int, PortraitStock> PortraitStocks =
        new Dictionary<int, PortraitStock>();

    private struct PortraitStock
    {
        public Vector3 LocalScale;
        public Vector3 LocalPosition;
        public Vector2 AnchoredPosition;
        public bool IsRectTransform;
    }

    public static void ResetMiddleSpawns()
    {
        _middleSpawnCount = 0;
        EnemySides.Clear();
    }

    public static void RefreshActiveGrid()
    {
        var grid = _activeGrid;
        if (grid == null || !grid.IsInitialized)
            grid = Object.FindObjectOfType<RRGridView>();

        if (grid != null && grid.IsInitialized)
        {
            _activeGrid = grid;
            if (IsOn)
                ApplySidewaysLayout(grid);
            else
                RestoreStockLayout(grid);
        }

        RefreshPortraits();
    }

    [HarmonyPatch(typeof(RRGridView), nameof(RRGridView.InitTiles))]
    [HarmonyPostfix]
    private static void InitTilesPostfix(RRGridView __instance)
    {
        if (__instance == null || !__instance.IsInitialized)
            return;

        _activeGrid = __instance;
        ResetMiddleSpawns();
        PortraitStocks.Clear();
        if (IsOn)
            ApplySidewaysLayout(__instance);
        RefreshPortraits();
    }

    [HarmonyPatch(typeof(RRPortraitView), nameof(RRPortraitView.ApplyCustomPortrait))]
    [HarmonyPostfix]
    private static void PortraitReadyPostfix()
    {
        if (IsOn)
            RefreshPortraits();
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.Initialize))]
    [HarmonyPostfix]
    private static void EnemyInitializePostfix(RREnemy __instance)
    {
        if (!IsOn || __instance == null)
            return;

        int col = __instance.CurrentGridPosition.x;
        if (col != SidewaysRiftSides.MiddleColumn)
            return;

        SidewaysRiftSide side = SidewaysRiftSides.SideForMiddleSpawn(_middleSpawnCount++);
        EnemySides[__instance.GetInstanceID()] = side;
        if (side == SidewaysRiftSide.Right)
            SnapEnemyToSidePath(__instance, side, includeTransform: true);
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.UpdateMovement))]
    [HarmonyPrefix]
    private static void EnemyUpdateMovementPrefix(RREnemy __instance)
    {
        if (!IsOn || __instance == null)
            return;

        if (!EnemySides.TryGetValue(__instance.GetInstanceID(), out SidewaysRiftSide side))
            return;

        if (side == SidewaysRiftSide.Left)
            return;

        // Recompute anchors from tiles + side delta (never stack deltas).
        SnapEnemyToSidePath(__instance, side, includeTransform: false);
    }

    private static void ApplySidewaysLayout(RRGridView grid)
    {
        if (grid == null || grid._tileViewsByGridPosition == null)
            return;

        int numColumns = grid.NumColumns;
        foreach (KeyValuePair<int2, RRTileView> pair in grid._tileViewsByGridPosition)
        {
            RRTileView tile = pair.Value;
            if (tile == null)
                continue;

            SidewaysRiftSide side = SidewaysRiftSides.TileGuideSide(pair.Key.x);
            SidewaysRiftLayout.LocalXZ(
                pair.Key.x,
                pair.Key.y,
                side,
                numColumns,
                out float localX,
                out float localZ);

            Vector3 local = tile.transform.localPosition;
            local.x = localX;
            local.z = localZ;
            tile.transform.localPosition = local;
        }

        SyncActionArrows(grid);
        Plugin.Logger?.LogInfo(
            $"SidewaysRift: applied L-path layout (turnRow={SidewaysRiftSides.TurnRow})");
        ImperfectRifts.NotifyLayoutWritten();
    }

    private static void RestoreStockLayout(RRGridView grid)
    {
        if (grid == null || grid._tileViewsByGridPosition == null)
            return;

        int numColumns = grid.NumColumns;
        foreach (KeyValuePair<int2, RRTileView> pair in grid._tileViewsByGridPosition)
        {
            RRTileView tile = pair.Value;
            if (tile == null)
                continue;

            float stockX = SidewaysRiftLayout.StockLocalX(pair.Key.x, numColumns);
            Vector3 local = tile.transform.localPosition;
            local.x = stockX;
            local.z = pair.Key.y;
            tile.transform.localPosition = local;
        }

        SyncActionArrows(grid);
        ResetMiddleSpawns();
        Plugin.Logger?.LogInfo("SidewaysRift: restored stock tile layout");
        ImperfectRifts.NotifyLayoutWritten();
    }

    private static void RefreshPortraits()
    {
        RRPortraitUiController ui = Object.FindObjectOfType<RRPortraitUiController>();
        if (ui == null)
            return;

        if (IsOn)
            ApplyPortraitParents(ui);
        else
            RestorePortraitParents(ui);
    }

    private static void ApplyPortraitParents(RRPortraitUiController ui)
    {
        ApplyPortraitParent(ui._heroPortraitParent);
        ApplyPortraitParent(ui._counterpartPortraitParent);
        Plugin.Logger?.LogInfo(
            $"SidewaysRift: portraits scale={SidewaysRiftPortraitLayout.ScaleFactor} upNudge={SidewaysRiftPortraitLayout.UpNudge}");
    }

    private static void RestorePortraitParents(RRPortraitUiController ui)
    {
        RestorePortraitParent(ui._heroPortraitParent);
        RestorePortraitParent(ui._counterpartPortraitParent);
        Plugin.Logger?.LogInfo("SidewaysRift: restored portrait frames");
    }

    private static void ApplyPortraitParent(Transform parent)
    {
        if (parent == null)
            return;

        int id = parent.GetInstanceID();
        if (!PortraitStocks.TryGetValue(id, out PortraitStock stock))
        {
            stock = new PortraitStock
            {
                LocalScale = parent.localScale,
                LocalPosition = parent.localPosition,
                IsRectTransform = parent is RectTransform
            };
            if (stock.IsRectTransform)
                stock.AnchoredPosition = ((RectTransform)parent).anchoredPosition;
            PortraitStocks[id] = stock;
        }

        parent.localScale = new Vector3(
            SidewaysRiftPortraitLayout.ScaledAxis(stock.LocalScale.x),
            SidewaysRiftPortraitLayout.ScaledAxis(stock.LocalScale.y),
            SidewaysRiftPortraitLayout.ScaledAxis(stock.LocalScale.z));

        if (stock.IsRectTransform)
        {
            var rt = (RectTransform)parent;
            Vector2 ap = stock.AnchoredPosition;
            ap.y = SidewaysRiftPortraitLayout.NudgedY(stock.AnchoredPosition.y);
            rt.anchoredPosition = ap;
        }
        else
        {
            Vector3 pos = stock.LocalPosition;
            pos.y = SidewaysRiftPortraitLayout.NudgedY(stock.LocalPosition.y);
            parent.localPosition = pos;
        }
    }

    private static void RestorePortraitParent(Transform parent)
    {
        if (parent == null)
            return;

        int id = parent.GetInstanceID();
        if (!PortraitStocks.TryGetValue(id, out PortraitStock stock))
            return;

        parent.localScale = stock.LocalScale;
        if (stock.IsRectTransform)
            ((RectTransform)parent).anchoredPosition = stock.AnchoredPosition;
        else
            parent.localPosition = stock.LocalPosition;
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

    private static void SnapEnemyToSidePath(RREnemy enemy, SidewaysRiftSide side, bool includeTransform)
    {
        RRGridView grid = _activeGrid;
        if (grid == null || !grid.IsInitialized)
            return;

        enemy.CurrentGridWorldPosition = WorldForGridPos(enemy, grid, enemy.CurrentGridPosition, side);
        enemy.TargetWorldPosition = WorldForGridPos(enemy, grid, enemy.TargetGridPosition, side);
        if (includeTransform)
            enemy.transform.position = enemy.CurrentGridWorldPosition;
    }

    private static Vector3 WorldForGridPos(
        RREnemy enemy,
        RRGridView grid,
        int2 gridPos,
        SidewaysRiftSide side)
    {
        int x = gridPos.x;
        int y = gridPos.y;
        Vector3 tileWorld = grid.GetTileWorldPositionFromGridPosition(x, y);
        float time = 1f - Mathf.Clamp01((float)y / (float)grid.NumRows);
        float scale = 1f;
        if (enemy.ZOffsetDistanceScaleCurve != null)
            scale = enemy.ZOffsetDistanceScaleCurve.Evaluate(time);

        Vector3 baseOffset = enemy.BasePositionOffset;
        Vector3 offset = new Vector3(baseOffset.x, baseOffset.y, baseOffset.z * scale);
        Vector3 world = tileWorld + offset;

        SidewaysRiftSide guide = SidewaysRiftSides.TileGuideSide(x);
        if (side == guide)
            return world;

        SidewaysRiftLayout.LocalXZ(x, y, guide, grid.NumColumns, out float gx, out float gz);
        SidewaysRiftLayout.LocalXZ(x, y, side, grid.NumColumns, out float sx, out float sz);

        Vector3 localDelta = new Vector3(sx - gx, 0f, sz - gz);
        if (localDelta.sqrMagnitude < 1e-8f)
            return world;

        return world + grid.transform.TransformVector(localDelta);
    }
}
