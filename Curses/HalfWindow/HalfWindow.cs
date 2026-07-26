using HarmonyLib;
using RhythmRift;
using Shared;
using Shared.RhythmEngine;
using TeaCurses.Curse;
using UnityEngine;

namespace TeaCurses;

/// <summary>
/// Zeros half of the stock hit window. Intensity 0 = no late (early-only);
/// intensity 1 = no early (late-only). Removed-half presses mistime stock-style.
/// Enemy hits write the public backing fields; BeatmapPlayer reads that pair.
/// </summary>
[HarmonyPatch]
public static class HalfWindow
{
    public const string Name = "HalfWindow";

    public static bool IsOn => CurseRegistry.IsEnabled(Name);

    private static float ReadIntensity()
    {
        CurseRegistry.TryGetIntensity(Name, out var value);
        return value;
    }

    private static void ApplyEffectiveFields(InputRatingsDefinition def)
    {
        HalfWindowRules.EffectivePair(
            def._beforeBeatHitWindow,
            def._afterBeatHitWindow,
            ReadIntensity(),
            out var before,
            out var after);
        def._beforeBeatHitWindow = before;
        def._afterBeatHitWindow = after;
    }

    [HarmonyPatch(typeof(RREnemyController), nameof(RREnemyController.AttackEnemiesAtPositions))]
    [HarmonyPrefix]
    public static void AttackEnemiesPrefix(
        InputRatingsDefinition inputRatingsDefinition,
        ref object __state)
    {
        __state = null;
        if (!IsOn || inputRatingsDefinition == null)
            return;

        __state = new float[]
        {
            inputRatingsDefinition._beforeBeatHitWindow,
            inputRatingsDefinition._afterBeatHitWindow,
        };
        ApplyEffectiveFields(inputRatingsDefinition);
    }

    [HarmonyPatch(typeof(RREnemyController), nameof(RREnemyController.AttackEnemiesAtPositions))]
    [HarmonyPostfix]
    public static void AttackEnemiesPostfix(
        InputRatingsDefinition inputRatingsDefinition,
        object __state)
    {
        if (__state is not float[] stock || inputRatingsDefinition == null || stock.Length < 2)
            return;

        inputRatingsDefinition._beforeBeatHitWindow = stock[0];
        inputRatingsDefinition._afterBeatHitWindow = stock[1];
    }

    [HarmonyPatch(typeof(BeatmapPlayer), nameof(BeatmapPlayer.IsInputWithinInputWindow))]
    [HarmonyPrefix]
    public static bool IsInputWithinInputWindowPrefix(
        BeatmapPlayer __instance,
        float inputDifferenceInSeconds,
        ref bool __result)
    {
        if (!IsOn)
            return true;

        var def = __instance.ActiveInputRatingsDefinition;
        if (def == null)
            return true;

        HalfWindowRules.EffectivePair(
            def._beforeBeatHitWindow,
            def._afterBeatHitWindow,
            ReadIntensity(),
            out var before,
            out var after);

        // Stock: early uses Abs(diff) < before; late uses diff < after.
        if (inputDifferenceInSeconds < 0f)
            __result = Mathf.Abs(inputDifferenceInSeconds) < before;
        else
            __result = inputDifferenceInSeconds < after;
        return false;
    }

    [HarmonyPatch(typeof(InputRatingsDefinition), nameof(InputRatingsDefinition.GetRatingPercent))]
    [HarmonyPrefix]
    public static bool GetRatingPercentPrefix(
        InputRatingsDefinition __instance,
        float inputDifferenceInSeconds,
        out InputTiming inputTiming,
        ref int __result)
    {
        inputTiming = default;
        if (!IsOn || __instance == null)
            return true;

        HalfWindowRules.EffectivePair(
            __instance._beforeBeatHitWindow,
            __instance._afterBeatHitWindow,
            ReadIntensity(),
            out var before,
            out var after);

        if (!HalfWindowRules.TrySafeRatingPercent(
                inputDifferenceInSeconds, before, after,
                out var percent, out var timing))
            return true;

        __result = percent;
        inputTiming = ToInputTiming(timing);
        return false;
    }

    private static InputTiming ToInputTiming(HalfWindowTiming timing) => timing switch
    {
        HalfWindowTiming.Early => InputTiming.Early,
        HalfWindowTiming.Late => InputTiming.Late,
        _ => InputTiming.TrueFlawless,
    };
}
