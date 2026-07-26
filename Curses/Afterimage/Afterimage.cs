using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RhythmRift.Enemies;
using Shared.RhythmEngine;
using TeaCurses.Curse;
using TeaCurses.Curses;
using UnityEngine;

namespace TeaCurses;

/// <summary>
/// Hides the real enemy sprite and shows a beat-sampled fading trail of
/// independent ghost sprites at prior positions.
/// </summary>
[HarmonyPatch]
public static class Afterimage
{
    public const string Name = "Afterimage";

    public static bool IsOn => CurseRegistry.IsEnabled(Name);

    private static readonly ConditionalWeakTable<object, SampleState> Samples =
        new ConditionalWeakTable<object, SampleState>();

    private static readonly List<Ghost> Active = new List<Ghost>();
    private static readonly Stack<GameObject> Pool = new Stack<GameObject>();

    private static AccessTools.FieldRef<RREnemy, SpriteRenderer> MonsterShadow;
    private static Transform _host;
    private static Material SpritesDefault;

    static Afterimage()
    {
        try
        {
            MonsterShadow = AccessTools.FieldRefAccess<RREnemy, SpriteRenderer>("_monsterShadow");
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogWarning($"Afterimage: could not bind _monsterShadow: {ex.Message}");
        }
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

        SpritesDefault = new Material(shader) { name = "TeaCursesAfterimage" };
        return SpritesDefault;
    }

    private static int ReadIntensity()
    {
        var intensity = 5;
        if (CurseRegistry.TryGetIntensity(Name, out var value))
            intensity = Mathf.RoundToInt(value);
        return AfterimageTrail.ClampIntensity(intensity);
    }

    private static Transform Host
    {
        get
        {
            if (_host != null)
                return _host;

            var go = new GameObject("TeaCursesAfterimageHost");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _host = go.transform;
            return _host;
        }
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

    private static void Enforce(RREnemy enemy)
    {
        if (enemy == null)
            return;

        if (!IsOn || enemy.IsHealthItem)
        {
            if (!VanishingPoint.IsOn && !Cryptid.IsOn && !Blink.IsOn)
                ApplyVisibility(enemy, visible: true);
            return;
        }

        ApplyVisibility(enemy, visible: false);
    }

    private static void ClearAllGhosts()
    {
        for (var i = Active.Count - 1; i >= 0; i--)
            ReturnGhost(Active[i]);
        Active.Clear();
    }

    private static void ReturnGhost(Ghost ghost)
    {
        if (ghost?.Object == null)
            return;

        ghost.Object.SetActive(false);
        Pool.Push(ghost.Object);
    }

    private static GameObject RentGhostObject()
    {
        while (Pool.Count > 0)
        {
            var recycled = Pool.Pop();
            if (recycled != null)
                return recycled;
        }

        var go = new GameObject("AfterimageGhost");
        go.transform.SetParent(Host, false);
        go.AddComponent<SpriteRenderer>();
        go.SetActive(false);
        return go;
    }

    private static void SpawnGhost(RREnemy enemy, Vector3 worldPos, int intensity)
    {
        var src = enemy.SpriteRenderer;
        if (src == null || src.sprite == null)
            return;

        var go = RentGhostObject();
        go.transform.SetParent(Host, false);
        go.transform.position = worldPos;
        go.transform.rotation = src.transform.rotation;
        go.transform.localScale = src.transform.lossyScale;

        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = src.sprite;
        renderer.flipX = src.flipX;
        renderer.flipY = src.flipY;
        renderer.sortingLayerID = src.sortingLayerID;
        renderer.sortingOrder = src.sortingOrder;
        // Stock enemy materials ignore SpriteRenderer.color.a — same as Vanishing Point:
        // force Sprites/Default so tint + fade alpha actually apply.
        var mat = GetSpritesDefault();
        if (mat != null)
            renderer.sharedMaterial = mat;

        var life = AfterimageTrail.LifetimeBeats(intensity);
        var bucket = AfterimageBeatTint.Classify(enemy.SpawnTrueBeatNumber);
        AfterimageBeatTint.Rgb(bucket, out var tr, out var tg, out var tb);
        var tint = new Color(tr, tg, tb, 1f);
        var color = tint;
        color.a = AfterimageTrail.Alpha(0, life);
        renderer.color = color;

        go.SetActive(true);
        Active.Add(new Ghost
        {
            Object = go,
            Renderer = renderer,
            Owner = enemy,
            AgeBeats = 0,
            LifetimeBeats = life,
            BaseColor = tint,
        });

        CullExcessForOwner(enemy, intensity);
    }

    private static void AgeOwnerGhosts(RREnemy enemy, int intensity)
    {
        if (enemy == null)
            return;

        var life = AfterimageTrail.LifetimeBeats(intensity);
        for (var i = Active.Count - 1; i >= 0; i--)
        {
            var g = Active[i];
            if (g?.Object == null)
            {
                Active.RemoveAt(i);
                continue;
            }

            if (!ReferenceEquals(g.Owner, enemy))
                continue;

            g.AgeBeats++;
            if (AfterimageTrail.ShouldCull(g.AgeBeats, life))
            {
                ReturnGhost(g);
                Active.RemoveAt(i);
                continue;
            }

            var c = g.BaseColor;
            c.a = AfterimageTrail.Alpha(g.AgeBeats, g.LifetimeBeats);
            if (g.Renderer != null)
                g.Renderer.color = c;
        }

        CullExcessForOwner(enemy, intensity);
    }

    private static void CullExcessForOwner(RREnemy enemy, int intensity)
    {
        var max = AfterimageTrail.MaxGhosts(intensity);
        var owned = 0;
        for (var i = 0; i < Active.Count; i++)
        {
            if (ReferenceEquals(Active[i]?.Owner, enemy))
                owned++;
        }

        var excess = AfterimageTrail.ExcessCount(owned, max);
        for (var e = 0; e < excess; e++)
        {
            for (var i = 0; i < Active.Count; i++)
            {
                if (!ReferenceEquals(Active[i]?.Owner, enemy))
                    continue;
                ReturnGhost(Active[i]);
                Active.RemoveAt(i);
                break;
            }
        }
    }

    private static void Advance(RREnemy enemy)
    {
        if (enemy == null || enemy.IsHealthItem)
            return;

        if (!IsOn)
        {
            if (Active.Count > 0)
                ClearAllGhosts();
            if (!VanishingPoint.IsOn && !Cryptid.IsOn && !Blink.IsOn)
                ApplyVisibility(enemy, visible: true);
            return;
        }

        var intensity = ReadIntensity();
        AgeOwnerGhosts(enemy, intensity);

        var world = enemy.CurrentGridWorldPosition;
        var grid = enemy.CurrentGridPosition;
        var state = Samples.GetOrCreateValue(enemy);

        if (state.HasSample
            && (state.GridX != grid.x || state.GridY != grid.y)
            && AfterimageTrail.ShouldDrop(
                state.X, state.Y, state.Z,
                world.x, world.y, world.z))
        {
            SpawnGhost(enemy, new Vector3(state.X, state.Y, state.Z), intensity);
        }

        state.X = world.x;
        state.Y = world.y;
        state.Z = world.z;
        state.GridX = grid.x;
        state.GridY = grid.y;
        state.HasSample = true;
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.Initialize))]
    [HarmonyPostfix]
    private static void InitializePostfix(RREnemy __instance)
    {
        Enforce(__instance);
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.OnSpawn))]
    [HarmonyPostfix]
    private static void OnSpawnPostfix(RREnemy __instance)
    {
        if (__instance == null)
            return;

        var state = Samples.GetOrCreateValue(__instance);
        state.HasSample = false;
        Enforce(__instance);
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.PerformBeatActions))]
    [HarmonyPrefix]
    private static void PerformBeatActionsPrefix(RREnemy __instance)
    {
        if (!IsOn || __instance == null || __instance.IsHealthItem)
            return;

        // Capture leave-behind grid cell before ArriveAtTargetPosition.
        var world = __instance.CurrentGridWorldPosition;
        var grid = __instance.CurrentGridPosition;
        var state = Samples.GetOrCreateValue(__instance);
        state.X = world.x;
        state.Y = world.y;
        state.Z = world.z;
        state.GridX = grid.x;
        state.GridY = grid.y;
        state.HasSample = true;
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.PerformBeatActions))]
    [HarmonyPostfix]
    private static void PerformBeatActionsPostfix(RREnemy __instance)
    {
        Advance(__instance);
    }

    [HarmonyPatch(typeof(RREnemy), nameof(RREnemy.UpdateAnimations), typeof(FmodTimeCapsule))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void UpdateAnimationsPostfix(RREnemy __instance, FmodTimeCapsule fmodTimeCapsule)
    {
        if (!IsOn)
        {
            if (Active.Count > 0)
                ClearAllGhosts();
            if (__instance != null && !__instance.IsHealthItem
                && !VanishingPoint.IsOn && !Cryptid.IsOn && !Blink.IsOn)
                ApplyVisibility(__instance, visible: true);
            return;
        }

        Enforce(__instance);
    }

    private sealed class SampleState
    {
        public float X;
        public float Y;
        public float Z;
        public int GridX;
        public int GridY;
        public bool HasSample;
    }

    private sealed class Ghost
    {
        public GameObject Object;
        public SpriteRenderer Renderer;
        public RREnemy Owner;
        public int AgeBeats;
        public int LifetimeBeats;
        public Color BaseColor;
    }
}
