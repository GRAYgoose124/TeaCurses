using HarmonyLib;
using Shared.MenuOptions;
using Shared.TrackSelection;
using TeaCurses.UI;

namespace TeaCurses.Patches;

[HarmonyPatch]
public static class MenuInputBlockPatches
{
    [HarmonyPatch(typeof(OptionsScreenInputController), nameof(OptionsScreenInputController.Update))]
    [HarmonyPrefix]
    public static bool OptionsUpdatePrefix()
    {
        // Overlay open, or cancel-close frame: do not let Cancel / nav fire.
        return !MenuInputGuard.IsBlocking && !MenuInputGuard.ShouldSuppressStockCancel;
    }

    [HarmonyPatch(typeof(CustomTracksSelectionSceneController), nameof(CustomTracksSelectionSceneController.Update))]
    [HarmonyPrefix]
    public static bool CustomTracksUpdatePrefix(CustomTracksSelectionSceneController __instance)
    {
        if (MenuInputGuard.ShouldSuppressStockCancel)
        {
            // Skip this frame's Update entirely — do not touch InputDisabled, or it sticks true.
            return false;
        }

        if (!MenuInputGuard.IsBlocking)
            return true;

        // Overlay open: keep background track-query work; only skip input via InputDisabled.
        if (!__instance.InputDisabled)
            __instance.InputDisabled = true;
        return true;
    }

    [HarmonyPatch(typeof(TrackSelectionSceneController), nameof(TrackSelectionSceneController.Update))]
    [HarmonyPrefix]
    public static bool StockTracksUpdatePrefix(TrackSelectionSceneController __instance)
    {
        if (MenuInputGuard.ShouldSuppressStockCancel)
            return false;

        if (!MenuInputGuard.IsBlocking)
            return true;

        if (!__instance.InputDisabled)
            __instance.InputDisabled = true;
        return true;
    }
}
