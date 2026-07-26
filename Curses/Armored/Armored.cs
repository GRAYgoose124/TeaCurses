using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RhythmRift.Enemies;
using Shared.RhythmEngine;
using TeaCurses.Curse;
using TeaCurses.Curses;
using UnityEngine;

namespace TeaCurses;

/// <summary>
/// Stock 1-HP monsters become 2-HP. Missing hurt clips get a short tint/scale flash.
/// </summary>
[HarmonyPatch]
public static class Armored
{
    public const string Name = "Armored";

    private const float ScalePunch = 0.12f;

    public static bool IsOn => CurseRegistry.IsEnabled(Name);

    private static AccessTools.FieldRef<RREnemy, SpriteAnimationData> HitMovementAnimationData;

    private static readonly ConditionalWeakTable<object, Marker> ArmoredInstances =
        new ConditionalWeakTable<object, Marker>();

    private static readonly ConditionalWeakTable<object, FlashState> Flashes =
        new ConditionalWeakTable<object, FlashState>();

    private sealed class Marker
    {
    }

    private sealed class FlashState
    {
        public float StartUnscaledTime;
        public Vector3 BaseScale;
        public Color SavedTint;
        public float SavedOverlay;
        public bool Active;
    }

    static Armored()
    {
        try
        {
            HitMovementAnimationData =
                AccessTools.FieldRefAccess<RREnemy, SpriteAnimationData>("_hitMovementAnimationData");
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogWarning($"Armored: could not bind _hitMovementAnimationData: {ex.Message}");
        }
    }

    private static bool IsTracked(RREnemy enemy)
    {
        return enemy != null && ArmoredInstances.TryGetValue(enemy, out _);
    }

    private static bool HasValidHitAnim(RREnemy enemy)
    {
        if (HitMovementAnimationData == null || enemy == null)
            return false;
        try
        {
            return HitMovementAnimationData(enemy).IsValid;
        }
        catch
        {
            return false;
        }
    }

    private static void StartFlash(RREnemy enemy)
    {
        if (enemy == null)
            return;

        var flash = Flashes.GetValue(enemy, _ => new FlashState());
        flash.StartUnscaledTime = Time.unscaledTime;
        flash.BaseScale = enemy.transform.localScale;
        flash.SavedTint = enemy.TintColorShaderAnimValue;
        flash.SavedOverlay = enemy.TintOverlayShaderAnimValue;
        flash.Active = true;
    }

    private static void ApplyFlash(RREnemy enemy, float strength)
    {
        if (enemy == null)
            return;

        if (!Flashes.TryGetValue(enemy, out var flash) || !flash.Active)
            return;

        if (strength <= 0.0001f)
        {
            enemy.TintColorShaderAnimValue = flash.SavedTint;
            enemy.TintOverlayShaderAnimValue = flash.SavedOverlay;
            enemy.transform.localScale = flash.BaseScale;
            flash.Active = false;
            return;
        }

        enemy.TintColorShaderAnimValue = Color.white;
        enemy.TintOverlayShaderAnimValue = Mathf.Clamp01(strength);
        var s = 1f + ScalePunch * strength;
        enemy.transform.localScale = flash.BaseScale * s;
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.Initialize))]
    [HarmonyPostfix]
    private static void InitializePostfix(RREnemy __instance)
    {
        if (!IsOn || __instance == null)
            return;

        if (!ArmoredRules.ShouldArmor(__instance.IsHealthItem, __instance.CurrentHealthValue))
            return;

        __instance.CurrentHealthValue = 2;
        ArmoredInstances.Remove(__instance);
        ArmoredInstances.Add(__instance, new Marker());
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.PerformTakeDamageBehaviour))]
    [HarmonyPostfix]
    private static void TakeDamagePostfix(RREnemy __instance)
    {
        if (__instance == null || !IsTracked(__instance))
            return;

        if (HasValidHitAnim(__instance))
            return;

        StartFlash(__instance);
        ApplyFlash(__instance, 1f);
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.UpdateAnimations))]
    [HarmonyPostfix]
    private static void UpdateAnimationsPostfix(RREnemy __instance)
    {
        if (__instance == null)
            return;

        if (!Flashes.TryGetValue(__instance, out var flash) || !flash.Active)
            return;

        var elapsed = Time.unscaledTime - flash.StartUnscaledTime;
        ApplyFlash(__instance, ArmoredFlash.Strength(elapsed));
    }
}
