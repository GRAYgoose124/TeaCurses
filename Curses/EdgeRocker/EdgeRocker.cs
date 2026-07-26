using System.Collections.Generic;
using HarmonyLib;
using RhythmRift;
using Shared.Pins;
using Shared.RhythmEngine;
using TeaCurses.Curse;

namespace TeaCurses;

/// <summary>
/// Treats gameplay releases as full hit attempts.
/// </summary>
[HarmonyPatch]
public static class EdgeRocker
{
    public const string Name = "EdgeRocker";

    public static bool IsOn => CurseRegistry.IsEnabled(Name);

    [HarmonyPatch(typeof(RRStageController), nameof(RRStageController.BeginPlay))]
    [HarmonyPostfix]
    public static void OnBeginPlay(RRStageController __instance)
    {
        ApplyLeaderboardGate(__instance);
    }

    /// <summary>
    /// Mid-stage enable must still block submit; re-apply at upload time.
    /// Off never forces the flag back to true.
    /// </summary>
    [HarmonyPatch(typeof(RRStageController), nameof(RRStageController.UploadScoreToLeaderboardAndRefreshUi))]
    [HarmonyPrefix]
    public static void UploadScorePrefix(RRStageController __instance)
    {
        ApplyLeaderboardGate(__instance);
    }

    private static void ApplyLeaderboardGate(RRStageController stage)
    {
        if (stage == null)
            return;
        if (EdgeRockerRules.LeaderboardSubmissionOverride(
                CurseRegistry.AnyEnabledBlocksLeaderboard()) is { } allow)
        {
            stage._shouldAllowLeaderboardSubmission = allow;
        }
    }

    [HarmonyPatch(typeof(RRStageController), nameof(RRStageController.HandlePlayerInput),
        typeof(string), typeof(bool), typeof(FmodTimeCapsule), typeof(bool), typeof(bool))]
    [HarmonyPrefix]
    public static void HandlePlayerInputPrefix(
        RRStageController __instance,
        string inputName,
        bool isReleaseInput,
        ref bool __state)
    {
        __state = false;
        if (!isReleaseInput || __instance?._playerController == null)
            return;

        var mapping = __instance.BeatmapPlayer?.ActiveInputMapping;
        if (mapping == null)
            return;

        string resultingAction = mapping.GetResultingAction(inputName);
        uint held = 0u;
        if (__instance._heldButtons != null)
            __instance._heldButtons.TryGetValue(resultingAction, out held);
        __state = held > 0;
    }

    [HarmonyPatch(typeof(RRStageController), nameof(RRStageController.HandlePlayerInput),
        typeof(string), typeof(bool), typeof(FmodTimeCapsule), typeof(bool), typeof(bool))]
    [HarmonyPostfix]
    public static void HandlePlayerInputPostfix(
        RRStageController __instance,
        string inputName,
        bool isReleaseInput,
        FmodTimeCapsule inputTimeCapsule,
        bool isDebugInput,
        bool isBaneInput,
        bool __state)
    {
        if (!EdgeRockerRules.ShouldAttackOnRelease(IsOn, isReleaseInput) || !__state)
            return;
        if (__instance?._playerController == null || __instance._enemyController == null)
            return;

        var mapping = __instance.BeatmapPlayer?.ActiveInputMapping;
        if (mapping == null)
            return;

        string resultingAction = mapping.GetResultingAction(inputName);
        FillPressPositions(__instance, resultingAction);

        if (__instance._areInputsLockedOut)
        {
            __instance._gridView.AnimateLockedInput(resultingAction);
            return;
        }

        bool hadErrantInput;
        List<RREnemyController.EnemyHitData> hitDatas =
            __instance._enemyController.AttackEnemiesAtPositions(
                __instance._positionsToAttack,
                inputTimeCapsule,
                __instance.BeatmapPlayer.FmodTimeCapsule,
                __instance.BeatmapPlayer.ActiveInputRatingsDefinition,
                isDebugInput,
                out hadErrantInput);

        float trueBeatNumber = inputTimeCapsule.TrueBeatNumber;
        if (hadErrantInput &&
            __instance._enemyController.ShouldInputsCountAsErrant(
                trueBeatNumber + __instance._minBeatsAwayFromEnemyForErrants))
        {
            __instance._beatNumbersOfErrantInputs.Add(inputTimeCapsule.TrueBeatNumber);
            int burst = 0;
            for (int i = __instance._beatNumbersOfErrantInputs.Count - 1; i >= 0; i--)
            {
                if (trueBeatNumber >
                    __instance._beatNumbersOfErrantInputs[i] + __instance._beatLengthOfLongErrantWindow)
                {
                    __instance._beatNumbersOfErrantInputs.RemoveAt(i);
                }
                else if (__instance._beatNumbersOfErrantInputs[i] >
                         trueBeatNumber - __instance._beatLengthOfBurstErrantWindow)
                {
                    burst++;
                }
            }

            if (burst >= __instance._numErrantsDuringBurstWindow ||
                __instance._beatNumbersOfErrantInputs.Count >= __instance._numErrantsDuringLongWindow)
            {
                __instance._areInputsLockedOut = true;
                __instance._beatNumberInputLockStartedOn = trueBeatNumber;
                __instance._gridView.SetLockedOverlayStatus(isActive: true);
                __instance._beatNumbersOfErrantInputs.Clear();
            }
        }

        if (hadErrantInput &&
            PinsController.IsPinActive("GlassGuitar") &&
            !__instance._isTutorial &&
            !__instance._isCalibrationTest &&
            !__instance._isPracticeMode)
        {
            __instance.HandleCodaErrantDamage();
        }

        __instance.ProcessHitData(
            hitDatas,
            __instance._positionsToAttack,
            resultingAction,
            isBaneInput,
            isDebugInput);
    }

    private static void FillPressPositions(RRStageController stage, string resultingAction)
    {
        stage._positionsToAttack.Clear();
        stage._lastSuccessfulInputs.Clear();
        switch (resultingAction)
        {
            case "Left":
                stage._positionsToAttack.Add(stage._leftArrowGridPosition);
                stage._lastSuccessfulInputs.Add("Left");
                break;
            case "Right":
                stage._positionsToAttack.Add(stage._rightArrowGridPosition);
                stage._lastSuccessfulInputs.Add("Right");
                break;
            case "Up":
                stage._positionsToAttack.Add(stage._midArrowGridPosition);
                stage._lastSuccessfulInputs.Add("Up");
                break;
            case "Down":
                stage._positionsToAttack.Add(stage._leftArrowGridPosition);
                stage._lastSuccessfulInputs.Add("Left");
                stage._positionsToAttack.Add(stage._midArrowGridPosition);
                stage._lastSuccessfulInputs.Add("Up");
                stage._positionsToAttack.Add(stage._rightArrowGridPosition);
                stage._lastSuccessfulInputs.Add("Right");
                break;
        }
    }
}
