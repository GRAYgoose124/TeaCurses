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
/// Fades enemy sprites by beats-until-action-row. Invisible at ≤1 beat
/// remaining through the hit.
/// </summary>
[HarmonyPatch]
public static class VanishingPoint
{
    public const string Name = "VanishingPoint";

    private const string ProxyName = "TeaCursesVanishingPointProxy";

    public static bool IsOn => CurseRegistry.IsEnabled(Name);

    private static AccessTools.FieldRef<RREnemy, SpriteRenderer> MonsterShadow;

    private static readonly ConditionalWeakTable<object, ProxyState> Proxies =
        new ConditionalWeakTable<object, ProxyState>();

    private static Material SpritesDefault;

    static VanishingPoint()
    {
        try
        {
            MonsterShadow = AccessTools.FieldRefAccess<RREnemy, SpriteRenderer>("_monsterShadow");
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogWarning($"VanishingPoint: could not bind _monsterShadow: {ex.Message}");
        }
    }

    private static int ReadIntensity()
    {
        var intensity = 5;
        if (CurseRegistry.TryGetIntensity(Name, out var value))
            intensity = Mathf.RoundToInt(value);
        return VanishingPointFade.ClampIntensity(intensity);
    }

    private static Material GetSpritesDefault()
    {
        if (SpritesDefault != null)
            return SpritesDefault;

        var shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            return null;

        SpritesDefault = new Material(shader) { name = "TeaCursesVanishingPoint" };
        return SpritesDefault;
    }

    private static SpriteRenderer GetShadow(RREnemy enemy)
    {
        if (MonsterShadow == null || enemy == null)
            return null;
        try
        {
            return MonsterShadow(enemy);
        }
        catch
        {
            return null;
        }
    }

    private static void SetShadowAlpha(RREnemy enemy, float alpha)
    {
        var shadow = GetShadow(enemy);
        if (shadow == null)
            return;
        var sc = shadow.color;
        sc.a = Mathf.Clamp01(alpha);
        shadow.color = sc;
    }

    private static void SetStockBodyVisible(RREnemy enemy, bool visible)
    {
        var main = enemy.SpriteRenderer;
        var shadow = GetShadow(enemy);

        if (main != null)
            main.enabled = visible;

        var renderers = enemy.GetComponentsInChildren<SpriteRenderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null || r == shadow)
                continue;
            if (r.gameObject != null && r.gameObject.name == ProxyName)
                continue;
            if (r.gameObject != null && r.gameObject.name == "TeaCursesCryptidProxy")
                continue;
            r.enabled = visible;
        }
    }

    private static ProxyState EnsureProxy(RREnemy enemy, SpriteRenderer source)
    {
        var state = Proxies.GetOrCreateValue(enemy);
        if (state.Renderer != null)
            return state;

        var mat = GetSpritesDefault();
        // Parent to the enemy root (not under the stock SpriteRenderer) so a
        // disabled renderer does not hide the proxy.
        var go = new GameObject(ProxyName);
        go.transform.SetParent(enemy.transform, false);

        var renderer = go.AddComponent<SpriteRenderer>();
        if (mat != null)
            renderer.sharedMaterial = mat;

        state.Object = go;
        state.Renderer = renderer;
        return state;
    }

    private static void SyncProxy(ProxyState state, SpriteRenderer source, float alpha)
    {
        if (state?.Renderer == null || source == null || state.Object == null)
            return;

        var r = state.Renderer;
        var t = r.transform;
        var parent = t.parent;
        t.position = source.transform.position;
        t.rotation = source.transform.rotation;

        var followLossy = source.transform.lossyScale;
        if (parent != null)
        {
            var parentLossy = parent.lossyScale;
            t.localScale = new Vector3(
                SafeDiv(followLossy.x, parentLossy.x),
                SafeDiv(followLossy.y, parentLossy.y),
                SafeDiv(followLossy.z, parentLossy.z));
        }
        else
        {
            t.localScale = followLossy;
        }

        r.enabled = true;
        r.sprite = source.sprite;
        r.flipX = source.flipX;
        r.flipY = source.flipY;
        r.sortingLayerID = source.sortingLayerID;
        r.sortingOrder = source.sortingOrder;
        r.maskInteraction = source.maskInteraction;
        var c = Color.white;
        c.a = Mathf.Clamp01(alpha);
        r.color = c;
    }

    private static float SafeDiv(float a, float b)
        => Mathf.Abs(b) < 0.0001f ? a : a / b;

    private static void TearDownProxy(RREnemy enemy)
    {
        if (!Proxies.TryGetValue(enemy, out var state))
            return;

        if (state.Object != null)
            UnityEngine.Object.Destroy(state.Object);

        state.Object = null;
        state.Renderer = null;
    }

    private static void Restore(RREnemy enemy)
    {
        if (enemy == null)
            return;

        TearDownProxy(enemy);
        SetStockBodyVisible(enemy, visible: true);
        SetShadowAlpha(enemy, 1f);
    }

    private static void EnforceFade(RREnemy enemy, float alpha)
    {
        var main = enemy.SpriteRenderer;
        if (main == null || main.sprite == null)
        {
            Restore(enemy);
            return;
        }

        // Stock body materials ignore vertex alpha (Afterimage ghosts use default mat).
        SetStockBodyVisible(enemy, visible: false);
        var proxy = EnsureProxy(enemy, main);
        SyncProxy(proxy, main, alpha);
        SetShadowAlpha(enemy, alpha);
    }

    private static void Enforce(RREnemy enemy, FmodTimeCapsule time)
    {
        if (enemy == null)
            return;

        if (!IsOn || enemy.IsHealthItem)
        {
            if (!Afterimage.IsOn && !Cryptid.IsOn && !Blink.IsOn)
                Restore(enemy);
            return;
        }

        // Blink is hiding this enemy: keep stock hidden and destroy the proxy.
        if (Blink.IsOn && !Blink.IsEnemyScheduleVisible(enemy))
        {
            TearDownProxy(enemy);
            SetStockBodyVisible(enemy, visible: false);
            SetShadowAlpha(enemy, 0f);
            return;
        }

        var distance = VanishingPointFade.DistanceBeats(
            enemy.NextActionRowTrueBeatNumber,
            time.TrueBeatNumber);
        var alpha = VanishingPointFade.Alpha(distance, ReadIntensity());
        EnforceFade(enemy, alpha);
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.Initialize))]
    [HarmonyPostfix]
    private static void InitializePostfix(RREnemy __instance)
    {
        if (__instance == null)
            return;
        if (!IsOn || __instance.IsHealthItem)
        {
            if (!Afterimage.IsOn && !Cryptid.IsOn && !Blink.IsOn)
                Restore(__instance);
        }
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.OnSpawn))]
    [HarmonyPostfix]
    private static void OnSpawnPostfix(RREnemy __instance)
    {
        if (__instance == null)
            return;
        TearDownProxy(__instance);
        if (!IsOn || __instance.IsHealthItem)
        {
            if (!Afterimage.IsOn && !Cryptid.IsOn && !Blink.IsOn)
                Restore(__instance);
        }
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.UpdateAnimations))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void UpdateAnimationsPostfix(RREnemy __instance, FmodTimeCapsule fmodTimeCapsule)
    {
        Enforce(__instance, fmodTimeCapsule);
    }

    private sealed class ProxyState
    {
        public GameObject Object;
        public SpriteRenderer Renderer;
    }
}
