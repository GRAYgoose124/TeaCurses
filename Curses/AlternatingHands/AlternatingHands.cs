using System;
using System.Collections.Generic;
using HarmonyLib;
using RhythmRift;
using Shared.RhythmEngine;
using TeaCurses.Curse;
using UnityEngine.InputSystem;

namespace TeaCurses;

/// <summary>
/// Forces successive gameplay presses to alternate between the first and
/// second binding on each direction action (e.g. arrows vs WASD). Same side
/// twice on a different note beat locks inputs using the game's GlassGuitar-style
/// lockout. On the same TargetBeat (chart row): same hand continues a chord
/// without flipping Expected; the opposite hand (a flam) is allowed and advances
/// alternation like a new beat.
/// </summary>
[HarmonyPatch]
public static class AlternatingHands
{
    public const string Name = "AlternatingHands";

    public static bool IsOn => CurseRegistry.IsEnabled(Name);

    /// <summary>Next required side; None means either is accepted (first press).</summary>
    public static BindSide Expected = BindSide.None;

    /// <summary>Chart beat of the current same-hand group (enemy TargetBeat).</summary>
    private static float LastAcceptedBeat;

    /// <summary>Last connected bind side; None means no group yet.</summary>
    private static BindSide LastAcceptedSide = BindSide.None;

    /// <summary>
    /// Parallel to BeatmapPlayer._rawInputData Downs only (releases are ignored).
    /// HandlePlayerInput only receives the action name (Left/Right/...), not which binding.
    /// </summary>
    private static readonly Queue<BindSide> PendingSides = new Queue<BindSide>();

    /// <summary>Side awaiting resolve after AttackEnemiesAtPositions.</summary>
    private static BindSide PendingCommitSide = BindSide.None;

    private static bool AttackObservedThisInput;

    private static bool AttackWasErrantThisInput;

    private static bool HasHitTargetBeat;

    private static float HitTargetBeat;

    private static RRStageController PendingStage;

    private static string PendingInputName;

    private static FmodTimeCapsule PendingInputTimeCapsule;

    public static void Reset()
    {
        Expected = BindSide.None;
        LastAcceptedBeat = 0f;
        LastAcceptedSide = BindSide.None;
        PendingSides.Clear();
        ClearPendingCommit();
    }

    /// <summary>
    /// After a miss/errant, the next connected hit may use either hand (same as
    /// the first hit of a stage). Does not touch PendingSides.
    /// </summary>
    internal static void RestartAlternation()
    {
        Expected = BindSide.None;
        LastAcceptedBeat = 0f;
        LastAcceptedSide = BindSide.None;
    }

    private static void ClearPendingCommit()
    {
        PendingCommitSide = BindSide.None;
        AttackObservedThisInput = false;
        AttackWasErrantThisInput = false;
        HasHitTargetBeat = false;
        HitTargetBeat = 0f;
        PendingStage = null;
        PendingInputName = null;
        PendingInputTimeCapsule = default;
    }

    internal static bool IsSameBeatGroup(float beat) =>
        LastAcceptedSide != BindSide.None && beat == LastAcceptedBeat;

    /// <summary>
    /// Grouping key is chart TargetBeat. Same row + same hand = chord (ok).
    /// Same row + opposite hand = flam (ok when it matches Expected).
    /// New row must match Expected.
    /// </summary>
    internal static bool ShouldLock(BindSide side, float beat)
    {
        if (side == BindSide.None)
            return false;

        if (IsSameBeatGroup(beat))
        {
            if (side == LastAcceptedSide)
                return false; // chord continuation
            // Flam: other hand on the same row — only legal if it is the expected alternate.
            return Expected != BindSide.None && side != Expected;
        }

        return Expected != BindSide.None && side != Expected;
    }

    /// <summary>
    /// Advances alternation after a connected hit. Same-row same-hand chords do
    /// not flip Expected; same-row opposite-hand flams do.
    /// </summary>
    internal static void CommitAcceptedPress(BindSide side, float beat)
    {
        if (side == BindSide.None)
            return;

        if (IsSameBeatGroup(beat) && side == LastAcceptedSide)
            return; // chord: keep Expected aimed at the other hand for the next row

        Expected = Opposite(side);
        LastAcceptedBeat = beat;
        LastAcceptedSide = side;
    }

    internal static BindSide Opposite(BindSide side) => side switch
    {
        BindSide.Primary => BindSide.Alternate,
        BindSide.Alternate => BindSide.Primary,
        _ => BindSide.None,
    };

    /// <summary>
    /// Classify which binding slot fired: among non-composite bindings that share
    /// the trigger's control-scheme group (Keyboard / Gamepad / ...), index 0 =
    /// Primary, index 1 = Alternate.
    /// </summary>
    internal static BindSide ClassifyFromContext(InputAction.CallbackContext ctx)
    {
        InputAction action = ctx.action;
        InputControl control = ctx.control;
        if (action == null || control == null)
            return BindSide.None;

        int triggerIndex = FindBindingIndexForControl(action, control);
        if (triggerIndex < 0)
            return BindSide.None;

        InputBinding trigger = action.bindings[triggerIndex];
        string scheme = FirstGroup(trigger.groups);
        if (string.IsNullOrEmpty(scheme))
            scheme = GuessSchemeFromPath(trigger.effectivePath ?? trigger.path);

        var peers = new List<int>();
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding b = action.bindings[i];
            if (b.isComposite || b.isPartOfComposite)
                continue;
            if (!SameScheme(b, scheme))
                continue;
            peers.Add(i);
        }

        if (peers.Count < 2)
            return BindSide.None;

        int slot = peers.IndexOf(triggerIndex);
        if (slot < 0)
            return BindSide.None;

        return (slot % 2 == 0) ? BindSide.Primary : BindSide.Alternate;
    }

    private static int FindBindingIndexForControl(InputAction action, InputControl control)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding b = action.bindings[i];
            if (b.isComposite || b.isPartOfComposite)
                continue;
            string path = b.effectivePath ?? b.path;
            if (string.IsNullOrEmpty(path))
                continue;
            if (InputControlPath.Matches(path, control))
                return i;
        }

        // Fallback: match by control leaf name (e.g. "a", "leftArrow").
        string leaf = control.name;
        if (string.IsNullOrEmpty(leaf))
            return -1;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding b = action.bindings[i];
            if (b.isComposite || b.isPartOfComposite)
                continue;
            string path = b.effectivePath ?? b.path;
            if (path != null && path.EndsWith("/" + leaf, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static bool SameScheme(InputBinding binding, string scheme)
    {
        if (string.IsNullOrEmpty(scheme))
            return true;
        if (!string.IsNullOrEmpty(binding.groups) && GroupListContains(binding.groups, scheme))
            return true;
        string path = binding.effectivePath ?? binding.path;
        return GuessSchemeFromPath(path) == scheme;
    }

    private static string FirstGroup(string groups)
    {
        if (string.IsNullOrEmpty(groups))
            return null;
        int semi = groups.IndexOf(';');
        return semi < 0 ? groups : groups.Substring(0, semi);
    }

    private static bool GroupListContains(string groups, string scheme)
    {
        foreach (string part in groups.Split(';'))
        {
            if (string.Equals(part.Trim(), scheme, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string GuessSchemeFromPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        if (path.IndexOf("Keyboard", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Keyboard";
        if (path.IndexOf("Gamepad", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Gamepad";
        if (path.IndexOf("Joystick", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Gamepad";
        return null;
    }

    [HarmonyPatch(typeof(RRStageController), nameof(RRStageController.BeginPlay))]
    [HarmonyPostfix]
    public static void OnBeginPlay()
    {
        Reset();
    }

    /// <summary>
    /// When a press is accepted into _rawInputData, record which bind side produced it.
    /// </summary>
    [HarmonyPatch(typeof(BeatmapPlayer), nameof(BeatmapPlayer.RegisterInput))]
    [HarmonyPrefix]
    public static void RegisterInputPrefix(BeatmapPlayer __instance, ref int __state)
    {
        __state = __instance._rawInputData.Count;
    }

    [HarmonyPatch(typeof(BeatmapPlayer), nameof(BeatmapPlayer.RegisterInput))]
    [HarmonyPostfix]
    public static void RegisterInputPostfix(
        BeatmapPlayer __instance,
        InputAction.CallbackContext inputContext,
        int __state)
    {
        if (!IsOn)
            return;
        if (__instance._rawInputData.Count <= __state)
            return;

        // Releases also enqueue into _rawInputData and invoke HandlePlayerInput, but our
        // prefix used to skip them without dequeuing — that desynced PendingSides and
        // locked the next Down even when the player was alternating correctly.
        if (!inputContext.ReadValueAsButton())
            return;

        BindSide side = ClassifyFromContext(inputContext);
        PendingSides.Enqueue(side);
    }

    [HarmonyPatch(typeof(RREnemyController), nameof(RREnemyController.AttackEnemiesAtPositions))]
    [HarmonyPostfix]
    public static void AttackEnemiesAtPositionsPostfix(
        List<RREnemyController.EnemyHitData> __result,
        bool hadErrantInput)
    {
        if (!IsOn)
            return;

        AttackObservedThisInput = true;
        AttackWasErrantThisInput = hadErrantInput;
        HasHitTargetBeat = false;
        if (__result == null || __result.Count == 0)
            return;

        // Chart row identity — shared by every monster on that beat, unlike input TrueBeatNumber.
        HitTargetBeat = __result[0].TargetBeat;
        HasHitTargetBeat = true;
    }

    [HarmonyPatch(typeof(RRStageController), nameof(RRStageController.HandlePlayerInput),
        typeof(string), typeof(bool), typeof(FmodTimeCapsule), typeof(bool), typeof(bool))]
    [HarmonyPrefix]
    public static bool HandlePlayerInputPrefix(
        RRStageController __instance,
        string inputName,
        bool isReleaseInput,
        FmodTimeCapsule inputTimeCapsule,
        bool isDebugInput,
        bool isBaneInput)
    {
        ClearPendingCommit();

        if (!IsOn || isReleaseInput || isDebugInput || isBaneInput)
            return true;

        BindSide side = BindSide.None;
        if (PendingSides.Count > 0)
            side = PendingSides.Dequeue();

        // Consume the queued side even while locked out so the queue stays aligned.
        if (__instance._areInputsLockedOut)
            return true;

        if (side == BindSide.None)
            return true;

        // Resolve after AttackEnemies so we can group by enemy TargetBeat (same chart
        // row). Prefix cannot tell a same-row chord mash from a new fractional row
        // using input TrueBeatNumber alone.
        PendingCommitSide = side;
        PendingStage = __instance;
        PendingInputName = inputName;
        PendingInputTimeCapsule = inputTimeCapsule;
        return true;
    }

    [HarmonyPatch(typeof(RRStageController), nameof(RRStageController.HandlePlayerInput),
        typeof(string), typeof(bool), typeof(FmodTimeCapsule), typeof(bool), typeof(bool))]
    [HarmonyPostfix]
    public static void HandlePlayerInputPostfix(bool isReleaseInput)
    {
        if (!IsOn || isReleaseInput)
        {
            ClearPendingCommit();
            return;
        }

        BindSide side = PendingCommitSide;
        if (side == BindSide.None || !AttackObservedThisInput || !HasHitTargetBeat)
        {
            // Empty/errant with no hit row: reopen either-hand choice when we saw an errant attack.
            if (AttackObservedThisInput && AttackWasErrantThisInput)
                RestartAlternation();
            ClearPendingCommit();
            return;
        }

        float beat = HitTargetBeat;

        // Miss/errant first: reopen either hand. Do not lockout for "wrong" hand on a miss.
        if (AttackWasErrantThisInput)
        {
            RestartAlternation();
            ClearPendingCommit();
            return;
        }

        if (ShouldLock(side, beat))
        {
            if (PendingStage != null)
                LockInputs(PendingStage, PendingInputName, PendingInputTimeCapsule);
            else
                ClearPendingCommit();
            return;
        }

        CommitAcceptedPress(side, beat);
        ClearPendingCommit();
    }

    [HarmonyPatch(typeof(BeatmapPlayer), nameof(BeatmapPlayer.RaiseOnInputMissEvent))]
    [HarmonyPostfix]
    public static void RaiseOnInputMissEventPostfix()
    {
        if (!IsOn)
            return;
        RestartAlternation();
    }

    [HarmonyPatch(typeof(BeatmapPlayer), nameof(BeatmapPlayer.PerformErrantInput))]
    [HarmonyPostfix]
    public static void PerformErrantInputPostfix()
    {
        if (!IsOn)
            return;
        RestartAlternation();
    }

    private static void LockInputs(
        RRStageController stage,
        string inputName,
        FmodTimeCapsule inputTimeCapsule)
    {
        stage._areInputsLockedOut = true;
        stage._beatNumberInputLockStartedOn = inputTimeCapsule.TrueBeatNumber;
        stage._gridView.SetLockedOverlayStatus(true);

        var mapping = stage.BeatmapPlayer?.ActiveInputMapping;
        string direction = mapping != null
            ? mapping.GetResultingAction(inputName, string.Empty)
            : inputName;
        if (!string.IsNullOrEmpty(direction))
            stage._gridView.AnimateLockedInput(direction);

        Reset();

        Plugin.Logger?.LogInfo(
            $"AlternatingHands: lockout after non-alternating bind ({inputName})");
    }
}
