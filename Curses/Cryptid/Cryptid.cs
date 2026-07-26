using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RhythmRift;
using RhythmRift.Enemies;
using Shared.RhythmEngine;
using TeaCurses.Curse;
using TeaCurses.Curses;
using UnityEngine;

namespace TeaCurses;

/// <summary>
/// Replaces field enemy art with shuffled Unicode/procedural glyphs.
/// Debut types keep real art with a superscript glyph tell; later instances are glyph-only.
/// </summary>
[HarmonyPatch]
public static class Cryptid
{
    public const string Name = "Cryptid";
    private const string ProxyName = "TeaCursesCryptidProxy";
    private const float GlyphScale = 6f;

    public static bool IsOn => CurseRegistry.IsEnabled(Name);

    private static readonly CryptidMap Map = new CryptidMap();
    private static readonly ConditionalWeakTable<object, InstanceState> States =
        new ConditionalWeakTable<object, InstanceState>();

    private static AccessTools.FieldRef<RREnemy, SpriteRenderer> MonsterShadow;
    private static Material SpritesDefault;
    private static bool ChartReady;
    private static CryptidGlyphMode ActiveMode = CryptidGlyphMode.Mix;

    static Cryptid()
    {
        try
        {
            MonsterShadow = AccessTools.FieldRefAccess<RREnemy, SpriteRenderer>("_monsterShadow");
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogWarning($"Cryptid: could not bind _monsterShadow: {ex.Message}");
        }
    }

    private static CryptidGlyphMode ReadMode()
    {
        var intensity = CryptidGlyphModeRules.Default;
        if (CurseRegistry.TryGetIntensity(Name, out var value))
            intensity = Mathf.RoundToInt(value);
        return CryptidGlyphModeRules.FromIntensity(intensity);
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

        SpritesDefault = new Material(shader) { name = "TeaCursesCryptid" };
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

    public static void BeginChart()
    {
        CryptidGlyphBake.Clear();
        ActiveMode = ReadMode();
        var baked = CryptidGlyphBake.BakeSuccessful(CryptidGlyphPool.Default, ActiveMode);
        if (baked.Count == 0 && ActiveMode == CryptidGlyphMode.UnicodeOnly)
        {
            Plugin.Logger?.LogWarning("Cryptid: no OS Unicode glyphs; falling back to Mix");
            ActiveMode = CryptidGlyphMode.Mix;
            baked = CryptidGlyphBake.BakeSuccessful(CryptidGlyphPool.Default, ActiveMode);
        }

        if (baked.Count == 0)
        {
            Plugin.Logger?.LogWarning("Cryptid: glyph bake produced empty pool");
            Map.BeginChart(Environment.TickCount, CryptidGlyphPool.Default);
            ChartReady = true;
            return;
        }

        Map.BeginChart(Environment.TickCount, baked);
        ChartReady = true;
        Plugin.Logger?.LogInfo($"Cryptid: chart map ready ({baked.Count} glyphs, mode={ActiveMode})");
    }

    private static void EnsureChart()
    {
        if (!ChartReady)
            BeginChart();
    }

    private static string TypeKey(RREnemy enemy)
    {
        if (enemy == null)
            return "";
        return enemy.EnemyTypeId.ToString();
    }

    private static InstanceState EnsureState(RREnemy enemy)
    {
        var state = States.GetOrCreateValue(enemy);
        if (state.Initialized)
            return state;

        var key = TypeKey(enemy);
        state.TypeKey = key;
        state.AlreadySeen = Map.IsTypeSeen(key);
        if (!state.AlreadySeen)
            Map.MarkTypeSeen(key);

        state.Codepoint = Map.Assign(key);
        state.Initialized = true;
        return state;
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
            if (r.gameObject != null
                && (r.gameObject.name == ProxyName
                    || r.gameObject.name == "TeaCursesVanishingPointProxy"))
                continue;
            r.enabled = visible;
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

    private static ProxyState EnsureProxy(RREnemy enemy, InstanceState inst)
    {
        if (inst.Proxy?.Renderer != null)
            return inst.Proxy;

        var mat = GetSpritesDefault();
        var go = new GameObject(ProxyName);
        go.transform.SetParent(enemy.transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        if (mat != null)
            renderer.sharedMaterial = mat;

        if (!CryptidGlyphBake.TryGet(inst.Codepoint, out var sprite) || sprite == null)
            sprite = CryptidGlyphBake.Ensure(inst.Codepoint, ActiveMode);

        renderer.sprite = sprite;
        inst.Proxy = new ProxyState { Object = go, Renderer = renderer };
        return inst.Proxy;
    }

    private static void TearDownProxy(InstanceState inst)
    {
        if (inst?.Proxy == null)
            return;
        if (inst.Proxy.Object != null)
            UnityEngine.Object.Destroy(inst.Proxy.Object);
        inst.Proxy = null;
    }

    private static void TearDownProxy(RREnemy enemy)
    {
        if (States.TryGetValue(enemy, out var state))
            TearDownProxy(state);
    }

    private static void Restore(RREnemy enemy)
    {
        if (enemy == null)
            return;
        TearDownProxy(enemy);
        SetStockBodyVisible(enemy, visible: true);
        SetShadowAlpha(enemy, 1f);
    }

    private static void SyncProxy(
        ProxyState proxy,
        SpriteRenderer source,
        Sprite glyph,
        bool superscriptTell)
    {
        if (proxy?.Renderer == null || source == null || proxy.Object == null)
            return;

        var r = proxy.Renderer;
        var t = r.transform;
        var parent = t.parent;

        var scaleFactor = CryptidMorph.GlyphScaleFactor(typeAlreadySeen: !superscriptTell);
        var followLossy = source.transform.lossyScale * (GlyphScale * scaleFactor);

        var worldPos = source.transform.position;
        if (superscriptTell)
        {
            var halfW = 0f;
            var halfH = 0f;
            if (source.sprite != null)
            {
                var b = source.sprite.bounds;
                halfW = Mathf.Abs(b.extents.x * source.transform.lossyScale.x);
                halfH = Mathf.Abs(b.extents.y * source.transform.lossyScale.y);
            }

            CryptidMorph.TellOffset(halfW, halfH, out var ox, out var oy);
            // Flip-aware exponent lean so it stays "above-right" of the facing.
            if (source.flipX)
                ox = -ox;
            worldPos += new Vector3(ox, oy, 0f);
        }

        t.position = worldPos;
        t.rotation = source.transform.rotation;

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
        r.sprite = glyph != null ? glyph : r.sprite;
        r.flipX = source.flipX;
        r.flipY = source.flipY;
        r.sortingLayerID = source.sortingLayerID;
        r.sortingOrder = source.sortingOrder + 1;
        r.maskInteraction = source.maskInteraction;
        r.color = Color.white;
    }

    private static float SafeDiv(float a, float b)
        => Mathf.Abs(b) < 0.0001f ? a : a / b;

    private static void Enforce(RREnemy enemy, FmodTimeCapsule time)
    {
        if (enemy == null)
            return;

        if (!IsOn || enemy.IsHealthItem)
        {
            if (!Afterimage.IsOn && !VanishingPoint.IsOn && !Blink.IsOn)
                Restore(enemy);
            else
                TearDownProxy(enemy);
            return;
        }

        if (Blink.IsOn && !Blink.IsEnemyScheduleVisible(enemy))
        {
            TearDownProxy(enemy);
            SetStockBodyVisible(enemy, visible: false);
            SetShadowAlpha(enemy, 0f);
            return;
        }

        EnsureChart();

        var main = enemy.SpriteRenderer;
        if (main == null)
            return;

        var inst = EnsureState(enemy);
        var isTell = CryptidMorph.IsSuperscriptTell(inst.AlreadySeen);
        var showStock = CryptidMorph.ShowStock(inst.AlreadySeen);

        CryptidGlyphBake.TryGet(inst.Codepoint, out var glyph);
        if (glyph == null)
            glyph = CryptidGlyphBake.Ensure(inst.Codepoint, ActiveMode);

        var proxy = EnsureProxy(enemy, inst);
        SyncProxy(proxy, main, glyph, superscriptTell: isTell);

        SetStockBodyVisible(enemy, showStock);
        SetShadowAlpha(enemy, showStock ? 1f : 0f);
    }

    [HarmonyPatch(typeof(RRStageController), nameof(RRStageController.BeginPlay))]
    [HarmonyPostfix]
    private static void OnBeginPlay()
    {
        BeginChart();
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.OnSpawn))]
    [HarmonyPostfix]
    private static void OnSpawnPostfix(RREnemy __instance)
    {
        if (__instance == null)
            return;

        if (States.TryGetValue(__instance, out var existing))
        {
            TearDownProxy(existing);
            existing.Initialized = false;
            existing.Proxy = null;
        }

        if (!IsOn || __instance.IsHealthItem)
        {
            if (!Afterimage.IsOn && !VanishingPoint.IsOn && !Blink.IsOn)
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

    private sealed class InstanceState
    {
        public bool Initialized;
        public bool AlreadySeen;
        public string TypeKey;
        public int Codepoint;
        public ProxyState Proxy;
    }

    private sealed class ProxyState
    {
        public GameObject Object;
        public SpriteRenderer Renderer;
    }
}
