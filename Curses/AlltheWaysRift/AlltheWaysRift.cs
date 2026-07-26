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
/// Field layouts by intensity: diagonal, braid, spiral.
/// Logical grid math unchanged. Diagonal/Braid middle-lane spawns alternate
/// Left/Right; tiles use Left as the middle guide. Spiral has no sides.
/// </summary>
[HarmonyPatch]
public static class AlltheWaysRift
{
    public const string Name = "AlltheWaysRift";

    public static bool IsOn => CurseRegistry.IsEnabled(Name);

    public static AlltheWaysModeKind CurrentMode
    {
        get
        {
            if (!CurseRegistry.TryGetIntensity(Name, out float intensity))
                return AlltheWaysMode.FromIntensity(AlltheWaysMode.Default);
            return AlltheWaysMode.FromIntensity(intensity);
        }
    }

    private static RRGridView _activeGrid;
    private static int _middleSpawnCount;
    private static readonly Dictionary<int, AlltheWaysSide> EnemySides =
        new Dictionary<int, AlltheWaysSide>();
    private static readonly Dictionary<int, PortraitStock> PortraitStocks =
        new Dictionary<int, PortraitStock>();

    private struct PortraitStock
    {
        public Vector3 LocalScale;
        public Vector3 LocalPosition;
        public Vector2 AnchoredPosition;
        public bool IsRectTransform;
    }

    private static bool UsesMiddleSides =>
        CurrentMode == AlltheWaysModeKind.Diagonal
        || CurrentMode == AlltheWaysModeKind.Sideways
        || CurrentMode == AlltheWaysModeKind.Switchback;

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
                ApplyLayout(grid);
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
            ApplyLayout(__instance);
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
        if (!IsOn || __instance == null || !UsesMiddleSides)
            return;

        int col = __instance.CurrentGridPosition.x;
        if (col != AlltheWaysMode.MiddleColumn)
            return;

        AlltheWaysSide side = AlltheWaysMode.SideForMiddleSpawn(_middleSpawnCount++);
        EnemySides[__instance.GetInstanceID()] = side;
        if (side == AlltheWaysSide.Right)
            SnapEnemyToSidePath(__instance, side, includeTransform: true);
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.UpdateMovement))]
    [HarmonyPrefix]
    private static void EnemyUpdateMovementPrefix(RREnemy __instance)
    {
        if (!IsOn || __instance == null || !UsesMiddleSides)
            return;

        if (!EnemySides.TryGetValue(__instance.GetInstanceID(), out AlltheWaysSide side))
            return;

        if (side == AlltheWaysSide.Left)
            return;

        // Recompute anchors from tiles + side delta (never stack deltas).
        SnapEnemyToSidePath(__instance, side, includeTransform: false);
    }

    private static void ApplyLayout(RRGridView grid)
    {
        if (grid == null || grid._tileViewsByGridPosition == null)
            return;

        AlltheWaysModeKind mode = CurrentMode;
        int numColumns = grid.NumColumns;
        int numRows = grid.NumRows;
        _ = numRows;
        foreach (KeyValuePair<int2, RRTileView> pair in grid._tileViewsByGridPosition)
        {
            RRTileView tile = pair.Value;
            if (tile == null)
                continue;

            LocalXZForMode(
                mode,
                pair.Key.x,
                pair.Key.y,
                AlltheWaysMode.TileGuideSide(pair.Key.x),
                numColumns,
                numRows,
                out float localX,
                out float localZ);

            Vector3 local = tile.transform.localPosition;
            local.x = localX;
            local.z = localZ;
            tile.transform.localPosition = local;
        }

        SyncActionArrows(grid);
        Plugin.Logger?.LogInfo(
            $"AlltheWaysRift: applied {mode} layout (turnRow={AlltheWaysMode.TurnRow})");
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

            float stockX = AlltheWaysDiagonalLayout.StockLocalX(pair.Key.x, numColumns);
            Vector3 local = tile.transform.localPosition;
            local.x = stockX;
            local.z = pair.Key.y;
            tile.transform.localPosition = local;
        }

        SyncActionArrows(grid);
        ResetMiddleSpawns();
        Plugin.Logger?.LogInfo("AlltheWaysRift: restored stock tile layout");
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
            $"AlltheWaysRift: portraits scale={SidewaysRiftPortraitLayout.ScaleFactor} upNudge={SidewaysRiftPortraitLayout.UpNudge}");
    }

    private static void RestorePortraitParents(RRPortraitUiController ui)
    {
        RestorePortraitParent(ui._heroPortraitParent);
        RestorePortraitParent(ui._counterpartPortraitParent);
        Plugin.Logger?.LogInfo("AlltheWaysRift: restored portrait frames");
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

    private static void SnapEnemyToSidePath(RREnemy enemy, AlltheWaysSide side, bool includeTransform)
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
        AlltheWaysSide side)
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

        AlltheWaysModeKind mode = CurrentMode;
        if (mode != AlltheWaysModeKind.Diagonal
            && mode != AlltheWaysModeKind.Sideways
            && mode != AlltheWaysModeKind.Switchback)
            return world;

        AlltheWaysSide guide = AlltheWaysMode.TileGuideSide(x);
        if (side == guide)
            return world;

        LocalXZForMode(mode, x, y, guide, grid.NumColumns, grid.NumRows, out float gx, out float gz);
        LocalXZForMode(mode, x, y, side, grid.NumColumns, grid.NumRows, out float sx, out float sz);

        Vector3 localDelta = new Vector3(sx - gx, 0f, sz - gz);
        if (localDelta.sqrMagnitude < 1e-8f)
            return world;

        return world + grid.transform.TransformVector(localDelta);
    }

    private static void LocalXZForMode(
        AlltheWaysModeKind mode,
        int col,
        int row,
        AlltheWaysSide side,
        int numColumns,
        int numRows,
        out float localX,
        out float localZ)
    {
        switch (mode)
        {
            case AlltheWaysModeKind.Sideways:
                AlltheWaysSidewaysLayout.LocalXZ(col, row, side, numColumns, out localX, out localZ);
                break;
            case AlltheWaysModeKind.Spiral:
                AlltheWaysSpiralLayout.LocalXZ(col, row, numColumns, out localX, out localZ);
                break;
            case AlltheWaysModeKind.Funnel:
                AlltheWaysFunnelLayout.LocalXZ(col, row, numColumns, out localX, out localZ);
                break;
            case AlltheWaysModeKind.Serpentine:
                AlltheWaysSerpentineLayout.LocalXZ(col, row, numColumns, out localX, out localZ);
                break;
            case AlltheWaysModeKind.Switchback:
                AlltheWaysSwitchbackLayout.LocalXZ(col, row, side, numColumns, out localX, out localZ);
                break;
            case AlltheWaysModeKind.Crossroads:
                AlltheWaysCrossroadsLayout.LocalXZ(col, row, numColumns, out localX, out localZ);
                break;
            case AlltheWaysModeKind.Orbit:
                AlltheWaysOrbitLayout.LocalXZ(col, row, numColumns, out localX, out localZ);
                break;
            case AlltheWaysModeKind.TripleArmOut:
                AlltheWaysTripleArmOutLayout.LocalXZ(col, row, numColumns, out localX, out localZ);
                break;
            default:
                AlltheWaysDiagonalLayout.LocalXZ(
                    col, row, side, numColumns, out localX, out localZ);
                break;
        }
    }
}
