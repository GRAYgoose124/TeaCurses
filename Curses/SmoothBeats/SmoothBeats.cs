using System;
using HarmonyLib;
using RhythmRift.Enemies;
using Shared.RhythmEngine;
using TeaCurses.Curse;
using TeaCurses.Curses;
using Unity.Mathematics;
using UnityEngine;

namespace TeaCurses;

/// <summary>
/// Oscillates enemy visual motion between stock beat curve and linear smooth
/// using intensity-scaled harmonic sines.
/// </summary>
[HarmonyPatch]
public static class SmoothBeats
{
    public const string Name = "SmoothBeats";

    public static bool IsOn => CurseRegistry.IsEnabled(Name);

    private static AccessTools.FieldRef<RREnemy, AnimationCurve> MovementCurve;

    static SmoothBeats()
    {
        try
        {
            MovementCurve = AccessTools.FieldRefAccess<RREnemy, AnimationCurve>("_movementCurve");
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogWarning($"SmoothBeats: could not bind _movementCurve: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.UpdateMovement))]
    [HarmonyPostfix]
    private static void UpdateMovementPostfix(RREnemy __instance, FmodTimeCapsule fmodTimeCapsule)
    {
        if (!IsOn || __instance == null)
            return;
        if (__instance.IsHealthItem)
            return;
        if (__instance.IsSnappingToActionRow)
            return;
        if (__instance.IsPerformingSpecialActionMovement)
            return;

        var current = __instance.CurrentGridPosition;
        if (((int2)current).Equals(__instance.TargetGridPosition))
            return;

        var p = __instance.GetNormalizedProgressToNextMove(fmodTimeCapsule);
        p = Mathf.Clamp01(p);

        var curveT = p;
        if (MovementCurve != null)
        {
            try
            {
                var curve = MovementCurve(__instance);
                if (curve != null)
                    curveT = curve.Evaluate(p);
            }
            catch
            {
                curveT = p;
            }
        }

        var intensity = 5;
        if (CurseRegistry.TryGetIntensity(Name, out var value))
            intensity = Mathf.RoundToInt(value);

        var trueBeat = fmodTimeCapsule.TrueBeatNumber;
        var blend = SmoothBeatsBlend.Evaluate(intensity, trueBeat);
        var t = SmoothBeatsBlend.LerpFactor(curveT, p, blend);
        __instance.transform.position = Vector3.Lerp(
            __instance.CurrentGridWorldPosition,
            __instance.TargetWorldPosition,
            t);
    }
}
