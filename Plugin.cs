using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using RiftOfTheNecroManager;
using TeaCurses.Curse;
using TeaCurses.Curses;
using TeaCurses.UI;
using UnityEngine;

namespace TeaCurses;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : RiftPlugin
{
    internal static new ManualLogSource Logger;
    internal static Plugin Instance;

    internal static readonly Setting<KeyCode> ToggleKey = new(
        "Overlay",
        "ToggleKey",
        KeyCode.Equals,
        "Key that opens/closes the TeaCurses curse overlay.");

    private static readonly Type[] PatchTypes =
    {
        typeof(AlternatingHands),
        typeof(MirrorControls),
        typeof(OneHand),
        typeof(Blink),
        typeof(Afterimage),
        typeof(SmoothBeats),
        typeof(UpwardsRift),
        typeof(SidewaysRift),
        typeof(AlltheWaysRift),
        typeof(VanishingPoint),
        typeof(Armored),
        typeof(Trappist),
        typeof(HalfWindow),
        typeof(Cryptid),
        typeof(EdgeRocker),
        typeof(ImperfectRifts),
        typeof(Patches.MenuInputBlockPatches),
    };

    private CurseOverlay _overlay;
    private bool _initialized;
    private bool _overlayReady;
    private bool _loggedReady;
    private bool _overlayBuildFailed;
    private float _nextOverlayRetryTime;

    protected override void OnInit()
    {
        Instance = this;
        Logger = base.Logger;

        CurseRegistry.Clear();
        CurseRegistry.Register(new CurseDefinition(AlternatingHands.Name, "Alternating Hands"));
        CurseRegistry.Register(new CurseDefinition(
            MirrorControls.Name,
            "Mirror Controls",
            new CurseIntensity(0f, 1f, 1f, 0f)));
        CurseRegistry.Register(new CurseDefinition(
            OneHand.Name,
            "One Hand",
            new CurseIntensity(0f, 1f, 1f, 0f)));
        CurseRegistry.Register(new CurseDefinition(
            Blink.Name,
            "Blink",
            new CurseIntensity(1f, 10f, 1f, 5f)));
        CurseRegistry.Register(new CurseDefinition(
            Afterimage.Name,
            "Afterimage",
            new CurseIntensity(1f, 10f, 1f, 5f)));
        CurseRegistry.Register(new CurseDefinition(
            SmoothBeats.Name,
            "Smooth Beats",
            new CurseIntensity(1f, 10f, 1f, 5f)));
        CurseRegistry.Register(new CurseDefinition(UpwardsRift.Name, "Upwards Rift"));
        CurseRegistry.Register(new CurseDefinition(SidewaysRift.Name, "Sideways Rift"));
        CurseRegistry.Register(new CurseDefinition(
            AlltheWaysRift.Name,
            "AlltheWays Rift",
            new CurseIntensity(
                AlltheWaysMode.Min,
                AlltheWaysMode.Max,
                1f,
                AlltheWaysMode.Default)));
        CurseRegistry.Register(new CurseDefinition(
            VanishingPoint.Name,
            "Vanishing Point",
            new CurseIntensity(1f, 10f, 1f, 5f)));
        CurseRegistry.Register(new CurseDefinition(Armored.Name, "Armored"));
        CurseRegistry.Register(new CurseDefinition(
            Trappist.Name,
            "Trappist",
            new CurseIntensity(1f, 10f, 1f, 5f)));
        CurseRegistry.Register(new CurseDefinition(
            HalfWindow.Name,
            "Half Window",
            new CurseIntensity(
                HalfWindowRules.MinIntensity,
                HalfWindowRules.MaxIntensity,
                1f,
                HalfWindowRules.DefaultIntensity)));
        CurseRegistry.Register(new CurseDefinition(
            Cryptid.Name,
            "Cryptid",
            new CurseIntensity(
                CryptidGlyphModeRules.Min,
                CryptidGlyphModeRules.Max,
                1f,
                CryptidGlyphModeRules.Default),
            dangerWhenOff: true));
        CurseRegistry.Register(new CurseDefinition(
            EdgeRocker.Name,
            "Edge Rocker",
            warnYellowWhenOff: true));
        CurseRegistry.Register(new CurseDefinition(
            ImperfectRifts.Name,
            "Imperfect Rifts",
            new CurseIntensity(
                ImperfectRiftsRules.MinIntensity,
                ImperfectRiftsRules.MaxIntensity,
                1f,
                ImperfectRiftsRules.DefaultIntensity)));

        Harmony.UnpatchSelf();
        foreach (var type in PatchTypes)
            Harmony.PatchAll(type);
        foreach (var nested in typeof(ImperfectRifts).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (nested.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0)
                Harmony.PatchAll(nested);
        }

        // Glyph sprites once per game load — chart BeginPlay only reshuffles the map.
        Cryptid.WarmupAtGameLoad();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} ready — press {ToggleKey.Entry.Value} (or Plus) for curses");
        _initialized = true;
        base.OnInit();
    }

    protected override void OnUnload()
    {
        _initialized = false;
        _overlayReady = false;
        if (_overlay != null)
        {
            Destroy(_overlay.gameObject);
            _overlay = null;
        }

        CurseRegistry.Clear();
        Instance = null;
    }

    private void Update()
    {
        if (!_initialized)
            return;

        EnsureOverlay();

        if (_overlay == null)
            return;

        var inTrackMenu = TrackMenuGate.IsInTrackMenu();
        if (OverlayOpenRules.ShouldForceClose(_overlay.IsOpen, inTrackMenu))
            _overlay.SetOpen(false);

        var input = UnityInput.Current;
        if (input == null)
            return;

        KeyCode toggle = ToggleKey;
        if (input.GetKeyDown(toggle)
            || (toggle == KeyCode.Equals && input.GetKeyDown(KeyCode.Plus))
            || (toggle == KeyCode.Plus && input.GetKeyDown(KeyCode.Equals)))
        {
            var next = OverlayOpenRules.AfterToggle(_overlay.IsOpen, inTrackMenu);
            if (next is { } open)
                _overlay.SetOpen(open);
        }

        _overlay.TickInput(input);
    }

    private void EnsureOverlay()
    {
        if (_overlayReady)
            return;
        if (_overlayBuildFailed && Time.unscaledTime < _nextOverlayRetryTime)
            return;

        try
        {
            if (_overlay != null)
            {
                Destroy(_overlay.gameObject);
                _overlay = null;
            }

            var overlayHost = new GameObject("TeaCursesOverlayHost");
            DontDestroyOnLoad(overlayHost);
            _overlay = overlayHost.AddComponent<CurseOverlay>();
            _overlay.OnToggled = id =>
            {
                CurseHandExclusivity.AfterEnabled(id, OneHand.Name, AlternatingHands.Name);
                CurseHandExclusivity.AfterEnabled(id, Afterimage.Name, VanishingPoint.Name);
                CurseHandExclusivity.AfterEnabled(id, Afterimage.Name, Cryptid.Name);
                CurseHandExclusivity.AfterEnabled(id, VanishingPoint.Name, Cryptid.Name);
                if (id == AlternatingHands.Name)
                    AlternatingHands.Reset();
                if (id == UpwardsRift.Name)
                    UpwardsRift.RefreshActiveGrid();
                if (id == SidewaysRift.Name)
                    SidewaysRift.RefreshActiveGrid();
                if (id == AlltheWaysRift.Name)
                    AlltheWaysRift.RefreshActiveGrid();
                if (id == ImperfectRifts.Name)
                    ImperfectRifts.OnOverlayToggled(CurseRegistry.IsEnabled(id));
                else if (id == UpwardsRift.Name || id == SidewaysRift.Name || id == AlltheWaysRift.Name)
                    ImperfectRifts.NotifyLayoutWritten();
            };
            _overlay.OnIntensityChanged = id =>
            {
                if (id == AlltheWaysRift.Name)
                    AlltheWaysRift.RefreshActiveGrid();
            };
            _overlay.EnsureBuilt();
            _overlayReady = true;
            _overlayBuildFailed = false;
            if (!_loggedReady)
            {
                Logger.LogInfo("TeaCurses: overlay ready");
                _loggedReady = true;
            }
        }
        catch (Exception ex)
        {
            _overlayBuildFailed = true;
            _nextOverlayRetryTime = Time.unscaledTime + 2f;
            _overlayReady = false;
            if (_overlay != null)
            {
                Destroy(_overlay.gameObject);
                _overlay = null;
            }

            Logger.LogError($"TeaCurses overlay build failed: {ex}");
        }
    }
}
