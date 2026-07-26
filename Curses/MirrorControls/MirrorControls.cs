using HarmonyLib;
using RhythmRift;
using Shared.RhythmEngine;
using TeaCurses.Curse;
using TeaCurses.Curses;

namespace TeaCurses;

/// <summary>
/// Swaps Left↔Right when enabled. Intensity 0 = horizontal only;
/// intensity 1 also swaps Up↔Down. Remaps at HandlePlayerInput so
/// presses and releases stay paired in _heldButtons.
/// </summary>
[HarmonyPatch]
public static class MirrorControls
{
    public const string Name = "MirrorControls";

    public static bool IsOn => CurseRegistry.IsEnabled(Name);

    [HarmonyPatch(typeof(RRStageController), nameof(RRStageController.HandlePlayerInput),
        typeof(string), typeof(bool), typeof(FmodTimeCapsule), typeof(bool), typeof(bool))]
    [HarmonyPrefix]
    public static void HandlePlayerInputPrefix(ref string inputName)
    {
        if (!IsOn)
            return;

        CurseRegistry.TryGetIntensity(Name, out var intensity);
        inputName = MirrorControlsMap.Remap(inputName, intensity);
    }
}
