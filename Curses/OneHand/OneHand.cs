using HarmonyLib;
using Shared.RhythmEngine;
using TeaCurses.Curse;
using UnityEngine.InputSystem;

namespace TeaCurses;

/// <summary>
/// Restricts gameplay to a single bind side. Intensity 0 = Primary, 1 = Alternate.
/// Wrong-side Downs are ignored at RegisterInput (no action / miss / lockout).
/// </summary>
[HarmonyPatch]
public static class OneHand
{
    public const string Name = "OneHand";

    public static bool IsOn => CurseRegistry.IsEnabled(Name);

    [HarmonyPatch(typeof(BeatmapPlayer), nameof(BeatmapPlayer.RegisterInput))]
    [HarmonyPrefix]
    public static bool RegisterInputPrefix(InputAction.CallbackContext inputContext)
    {
        if (!IsOn)
            return true;

        // Releases must still update held-control state.
        if (!inputContext.ReadValueAsButton())
            return true;

        BindSide side = AlternatingHands.ClassifyFromContext(inputContext);
        CurseRegistry.TryGetIntensity(Name, out var intensity);
        BindSide required = OneHandRules.RequiredSide(intensity);
        return !OneHandRules.ShouldSwallow(side, required);
    }
}
