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
/// Randomly hides enemy sprites on a per-enemy beat cycle. Intensity 1–10
/// controls how sparse visibility can get (down to 1-in-10).
/// </summary>
[HarmonyPatch]
public static class Blink
{
    public const string Name = "Blink";

    public static bool IsOn => CurseRegistry.IsEnabled(Name);

    /// <summary>
    /// Whether Blink's schedule currently wants this enemy shown.
    /// When Blink is off, always true.
    /// </summary>
    public static bool IsEnemyScheduleVisible(RREnemy enemy)
    {
        if (!IsOn || enemy == null || enemy.IsHealthItem)
            return true;

        if (!States.TryGetValue(enemy, out var state) || state.Schedule == null)
            return true;

        return state.Schedule.IsVisible(state.Phase);
    }

    private static readonly ConditionalWeakTable<object, Holder> States =
        new ConditionalWeakTable<object, Holder>();

    private static readonly System.Random Rng = new System.Random();

    private static AccessTools.FieldRef<RREnemy, SpriteRenderer> MonsterShadow;

    static Blink()
    {
        try
        {
            MonsterShadow = AccessTools.FieldRefAccess<RREnemy, SpriteRenderer>("_monsterShadow");
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogWarning($"Blink: could not bind _monsterShadow: {ex.Message}");
        }
    }

    private static void EnsureState(RREnemy enemy)
    {
        if (enemy == null || enemy.IsHealthItem)
            return;

        if (!IsOn)
        {
            // Vanishing Point / Afterimage / Cryptid may already control visibility.
            if (!VanishingPoint.IsOn && !Afterimage.IsOn && !Cryptid.IsOn)
                ApplyVisibility(enemy, visible: true);
            return;
        }

        if (States.TryGetValue(enemy, out var existing) && existing.Schedule != null)
            return;

        var intensity = 5;
        if (CurseRegistry.TryGetIntensity(Name, out var value))
            intensity = Mathf.RoundToInt(value);

        var schedule = BlinkSchedule.Roll(intensity, Rng);
        var holder = States.GetOrCreateValue(enemy);
        holder.Schedule = schedule;
        holder.Phase = 0;
        if (Afterimage.IsOn || Cryptid.IsOn)
        {
            ApplyVisibility(enemy, visible: false);
        }
        else if (VanishingPoint.IsOn)
        {
            if (!schedule.IsVisible(0))
                ApplyVisibility(enemy, visible: false);
        }
        else
        {
            ApplyVisibility(enemy, schedule.IsVisible(0));
        }
    }

    private static void Reroll(RREnemy enemy)
    {
        if (enemy == null || enemy.IsHealthItem)
            return;

        if (!IsOn)
        {
            if (!VanishingPoint.IsOn && !Afterimage.IsOn && !Cryptid.IsOn)
                ApplyVisibility(enemy, visible: true);
            return;
        }

        var intensity = 5;
        if (CurseRegistry.TryGetIntensity(Name, out var value))
            intensity = Mathf.RoundToInt(value);

        var schedule = BlinkSchedule.Roll(intensity, Rng);
        var holder = States.GetOrCreateValue(enemy);
        holder.Schedule = schedule;
        holder.Phase = 0;
        if (Afterimage.IsOn || Cryptid.IsOn)
        {
            ApplyVisibility(enemy, visible: false);
        }
        else if (VanishingPoint.IsOn)
        {
            if (!schedule.IsVisible(0))
                ApplyVisibility(enemy, visible: false);
        }
        else
        {
            ApplyVisibility(enemy, schedule.IsVisible(0));
        }
    }

    private static void Advance(RREnemy enemy)
    {
        if (!IsOn || enemy == null || enemy.IsHealthItem)
            return;

        EnsureState(enemy);
        if (!States.TryGetValue(enemy, out var state) || state.Schedule == null)
            return;

        state.Phase++;
        var visible = state.Schedule.IsVisible(state.Phase);
        if (Afterimage.IsOn || Cryptid.IsOn)
        {
            ApplyVisibility(enemy, visible: false);
            return;
        }

        if (VanishingPoint.IsOn)
        {
            if (!visible)
                ApplyVisibility(enemy, visible: false);
            return;
        }

        ApplyVisibility(enemy, visible);
    }

    private static void Enforce(RREnemy enemy)
    {
        if (enemy == null)
            return;

        if (!IsOn || enemy.IsHealthItem)
        {
            if (!VanishingPoint.IsOn && !Afterimage.IsOn && !Cryptid.IsOn)
                ApplyVisibility(enemy, visible: true);
            return;
        }

        EnsureState(enemy);
        if (!States.TryGetValue(enemy, out var state) || state.Schedule == null)
        {
            if (!VanishingPoint.IsOn && !Afterimage.IsOn && !Cryptid.IsOn)
                ApplyVisibility(enemy, visible: true);
            return;
        }

        // Afterimage / Cryptid already hide stock bodies.
        if (Afterimage.IsOn || Cryptid.IsOn)
        {
            ApplyVisibility(enemy, visible: false);
            return;
        }

        var visible = state.Schedule.IsVisible(state.Phase);
        if (VanishingPoint.IsOn)
        {
            // Hide wins; when schedule wants show, Vanishing Point drives the proxy.
            if (!visible)
                ApplyVisibility(enemy, visible: false);
            return;
        }

        ApplyVisibility(enemy, visible);
    }

    private static void ApplyVisibility(RREnemy enemy, bool visible)
    {
        var main = enemy.SpriteRenderer;
        if (main != null)
            main.enabled = visible;

        if (MonsterShadow != null)
        {
            try
            {
                var shadow = MonsterShadow(enemy);
                if (shadow != null)
                    shadow.enabled = visible;
            }
            catch
            {
                // ignore missing shadow
            }
        }

        var renderers = enemy.GetComponentsInChildren<SpriteRenderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null || r == main)
                continue;
            if (r.gameObject != null
                && (r.gameObject.name == "TeaCursesCryptidProxy"
                    || r.gameObject.name == "TeaCursesVanishingPointProxy"))
                continue;
            r.enabled = visible;
        }
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.Initialize))]
    [HarmonyPostfix]
    private static void InitializePostfix(RREnemy __instance)
    {
        EnsureState(__instance);
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.OnSpawn))]
    [HarmonyPostfix]
    private static void OnSpawnPostfix(RREnemy __instance)
    {
        Reroll(__instance);
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.PerformBeatActions), typeof(FmodTimeCapsule))]
    [HarmonyPostfix]
    private static void PerformBeatActionsPostfix(RREnemy __instance, FmodTimeCapsule fmodTimeCapsule)
    {
        Advance(__instance);
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.UpdateAnimations), typeof(FmodTimeCapsule))]
    [HarmonyPostfix]
    private static void UpdateAnimationsPostfix(RREnemy __instance, FmodTimeCapsule fmodTimeCapsule)
    {
        Enforce(__instance);
    }

    private sealed class Holder
    {
        public BlinkSchedule Schedule;
        public int Phase;
    }
}
