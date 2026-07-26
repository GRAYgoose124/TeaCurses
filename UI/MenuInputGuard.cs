using System.Collections.Generic;
using Shared.MenuOptions;
using Shared.TrackSelection;
using UnityEngine;

namespace TeaCurses.UI;

/// <summary>
/// While the curse overlay is open, suppress stock menu navigation via the game's
/// InputDisabled flags and OptionsScreenInputController.IsInputDisabled.
/// </summary>
public static class MenuInputGuard
{
    private static bool _blocking;
    private static int _suppressStockCancelFrame = -1;
    private static bool? _savedCustomInputDisabled;
    private static bool? _savedStockInputDisabled;
    private static readonly List<(OptionsScreenInputController Controller, bool WasDisabled)> SavedOptions =
        new List<(OptionsScreenInputController, bool)>();

    public static bool IsBlocking => _blocking;

    /// <summary>
    /// True on the frame cancel closed the overlay — stock menus must not see Cancel.
    /// </summary>
    public static bool ShouldSuppressStockCancel
        => OverlayOpenRules.ShouldSuppressStockCancel(_suppressStockCancelFrame, Time.frameCount);

    public static void SuppressStockCancelThisFrame()
    {
        _suppressStockCancelFrame = Time.frameCount;
    }

    public static void SetBlocking(bool block)
    {
        if (block)
            Acquire();
        else
            Release();
    }

    private static void Acquire()
    {
        if (_blocking)
            return;

        // Snapshot option controllers BEFORE setting track InputDisabled — that
        // setter also forces _optionsInputController.IsInputDisabled = true, so
        // capturing afterward would restore "disabled" on close.
        SavedOptions.Clear();
        foreach (var controller in Object.FindObjectsOfType<OptionsScreenInputController>())
        {
            if (controller == null)
                continue;
            SavedOptions.Add((controller, controller.IsInputDisabled));
        }

        var custom = Object.FindObjectOfType<CustomTracksSelectionSceneController>();
        if (custom != null)
        {
            _savedCustomInputDisabled = custom.InputDisabled;
            custom.InputDisabled = true;
        }

        var stock = Object.FindObjectOfType<TrackSelectionSceneController>();
        if (stock != null)
        {
            _savedStockInputDisabled = stock.InputDisabled;
            stock.InputDisabled = true;
        }

        foreach (var (controller, _) in SavedOptions)
        {
            if (controller != null)
                controller.IsInputDisabled = true;
        }

        _blocking = true;
        Plugin.Logger?.LogInfo("TeaCurses: menu input blocked");
    }

    private static void Release()
    {
        if (!_blocking)
            return;

        // Always re-enable menu input on close. Restoring the Acquire-time snapshot
        // is unsafe: opening the overlay during track-select intro / folder transition
        // can snapshot InputDisabled=true, then leave the player stuck after close
        // once the game would otherwise have cleared that flag.
        foreach (var (controller, _) in SavedOptions)
        {
            if (controller != null)
                controller.IsInputDisabled = false;
        }

        SavedOptions.Clear();
        _savedCustomInputDisabled = null;
        _savedStockInputDisabled = null;

        var custom = Object.FindObjectOfType<CustomTracksSelectionSceneController>();
        if (custom != null)
            custom.InputDisabled = false;

        var stock = Object.FindObjectOfType<TrackSelectionSceneController>();
        if (stock != null)
            stock.InputDisabled = false;

        // Also clear any option controllers spawned after Acquire.
        foreach (var controller in Object.FindObjectsOfType<OptionsScreenInputController>())
        {
            if (controller != null)
                controller.IsInputDisabled = false;
        }

        _blocking = false;
        Plugin.Logger?.LogInfo("TeaCurses: menu input restored");
    }
}
