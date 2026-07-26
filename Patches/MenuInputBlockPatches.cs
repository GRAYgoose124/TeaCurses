using HarmonyLib;
using Shared.MenuOptions;
using Shared.TrackSelection;
using TeaCurses.UI;

namespace TeaCurses.Patches;

[HarmonyPatch]
public static class MenuInputBlockPatches
{
    private static bool ShouldHoldStockMenus
        => MenuInputGuard.IsBlocking || MenuInputGuard.ShouldSuppressStockCancel;

    [HarmonyPatch(typeof(OptionsScreenInputController), nameof(OptionsScreenInputController.Update))]
    [HarmonyPrefix]
    public static bool OptionsUpdatePrefix()
    {
        return !ShouldHoldStockMenus;
    }

    [HarmonyPatch(typeof(CustomTracksSelectionSceneController), nameof(CustomTracksSelectionSceneController.Update))]
    [HarmonyPrefix]
    public static bool CustomTracksUpdatePrefix(CustomTracksSelectionSceneController __instance)
    {
        if (!ShouldHoldStockMenus)
            return true;

        // Keep background track-query work; only skip the input section by
        // forcing InputDisabled for this frame if something cleared it.
        if (!__instance.InputDisabled)
            __instance.InputDisabled = true;
        return true;
    }

    [HarmonyPatch(typeof(TrackSelectionSceneController), nameof(TrackSelectionSceneController.Update))]
    [HarmonyPrefix]
    public static bool StockTracksUpdatePrefix(TrackSelectionSceneController __instance)
    {
        if (!ShouldHoldStockMenus)
            return true;

        if (!__instance.InputDisabled)
            __instance.InputDisabled = true;
        return true;
    }
}
