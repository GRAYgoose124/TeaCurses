using System.Collections.Generic;
using System.Text;
using BepInEx;
using TeaCurses.Curse;
using TeaCurses.Curses;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeaCurses.UI;

public sealed class CurseOverlay : MonoBehaviour
{
    public const int VisibleRows = 11;
    private const float PlatePad = 8f;

    private static readonly Color TitleColor = new Color(1f, 0.92f, 0.75f, 1f);
    private static readonly Color HintColor = new Color(0.75f, 0.72f, 0.68f, 1f);
    private static readonly Color RowOnColor = new Color(0.55f, 1f, 0.7f, 1f);
    private static readonly Color RowOffColor = new Color(0.88f, 0.86f, 0.82f, 1f);
    private static readonly Color HighlightColor = new Color(1f, 0.78f, 0.25f, 0.35f);
    private static readonly Color RowPlateColor = new Color(0.08f, 0.07f, 0.12f, 0.9f);
    private static readonly Color MeterLabelColor = new Color(1f, 0.85f, 0.45f, 1f);

    private Canvas _canvas;
    private RectTransform _panel;
    private TMP_Text _titleTmp;
    private TMP_Text _hintTmp;
    private TMP_Text _meterLabelTmp;
    private Text _titleUi;
    private Text _hintUi;
    private Text _meterLabelUi;
    private Image _hintPlate;
    private Image _titlePlate;
    private Image _meterPlate;
    private RectTransform _titleRoot;
    private RectTransform _hintRoot;
    private RectTransform _meterRoot;
    private readonly List<RectTransform> _rowRoots = new List<RectTransform>();
    private readonly List<TMP_Text> _rowTmps = new List<TMP_Text>();
    private readonly List<Text> _rowUis = new List<Text>();
    private readonly List<Image> _rowPlates = new List<Image>();
    private readonly List<Image> _rowHighlights = new List<Image>();
    private OverlayNavState _nav = new OverlayNavState(0, 0, VisibleRows);
    private bool _isOpen;
    private bool _built;

    public bool IsOpen => _isOpen;

    public System.Action<string> OnToggled;

    public System.Action<string> OnIntensityChanged;

    public void EnsureBuilt()
    {
        if (_built)
            return;
        Build();
        _built = true;
        SetOpen(false);
    }

    public void ToggleOpen()
    {
        EnsureBuilt();
        SetOpen(!_isOpen);
        Plugin.Logger?.LogInfo($"TeaCurses: overlay open={_isOpen}");
    }

    public void SetOpen(bool open)
    {
        EnsureBuilt();
        _isOpen = open;
        if (_canvas != null)
            _canvas.gameObject.SetActive(open);
        MenuInputGuard.SetBlocking(open);
        if (open)
        {
            GameUiAssets.EnsureCached();
            GameUiAssets.RefreshPanelAlbumArt();
            RestyleFromGameAssets();
            _nav = OverlayNav.EnsureVisible(
                new OverlayNavState(_nav.HighlightIndex, _nav.WindowStart, VisibleRows),
                CurseRegistry.All.Count);
            Refresh();
        }
    }

    public void TickInput(IInputSystem input)
    {
        if (!_isOpen || input == null)
            return;

        var count = CurseRegistry.All.Count;

        if (input.GetKeyDown(KeyCode.UpArrow) || input.GetKeyDown(KeyCode.W))
        {
            _nav = OverlayNav.Move(_nav, count, -1, wrap: true);
            Refresh();
        }
        else if (input.GetKeyDown(KeyCode.DownArrow) || input.GetKeyDown(KeyCode.S))
        {
            _nav = OverlayNav.Move(_nav, count, 1, wrap: true);
            Refresh();
        }
        else if (input.GetKeyDown(KeyCode.PageUp))
        {
            _nav = OverlayNav.Page(_nav, count, -1);
            Refresh();
        }
        else if (input.GetKeyDown(KeyCode.PageDown))
        {
            _nav = OverlayNav.Page(_nav, count, 1);
            Refresh();
        }
        else if (input.GetKeyDown(KeyCode.Home))
        {
            _nav = OverlayNav.JumpTo(_nav, count, 0);
            Refresh();
        }
        else if (input.GetKeyDown(KeyCode.End))
        {
            _nav = OverlayNav.JumpTo(_nav, count, count > 0 ? count - 1 : 0);
            Refresh();
        }
        else if (input.GetKeyDown(KeyCode.Return) || input.GetKeyDown(KeyCode.Space) || input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (TryGetHighlighted(out var def))
            {
                CurseRegistry.Toggle(def.Id);
                OnToggled?.Invoke(def.Id);
                Refresh();
            }
        }
        else if (input.GetKeyDown(KeyCode.LeftArrow) || input.GetKeyDown(KeyCode.A))
        {
            if (TryGetHighlighted(out var def) && CurseRegistry.TryStepIntensity(def.Id, -1))
            {
                OnIntensityChanged?.Invoke(def.Id);
                Refresh();
            }
        }
        else if (input.GetKeyDown(KeyCode.RightArrow) || input.GetKeyDown(KeyCode.D))
        {
            if (TryGetHighlighted(out var def) && CurseRegistry.TryStepIntensity(def.Id, 1))
            {
                OnIntensityChanged?.Invoke(def.Id);
                Refresh();
            }
        }
    }

    private void Update()
    {
        if (!_isOpen)
            return;
        TickTitleRgb();
    }

    private void TickTitleRgb()
    {
        var hue = TitleRgbCycle.HueAt(Time.unscaledTime, TitleRgbCycle.DefaultCyclesPerSecond);
        var color = Color.HSVToRGB(hue, 1f, 1f);
        if (_titleTmp != null)
            _titleTmp.color = color;
        if (_titleUi != null)
            _titleUi.color = color;
    }

    private bool TryGetHighlighted(out CurseDefinition def)
    {
        var all = CurseRegistry.All;
        if (all.Count == 0 || _nav.HighlightIndex < 0 || _nav.HighlightIndex >= all.Count)
        {
            def = null;
            return false;
        }

        def = all[_nav.HighlightIndex];
        return true;
    }

    private void Refresh()
    {
        var all = CurseRegistry.All;
        if (all.Count == 0)
        {
            SetHint("No curses registered");
            for (var i = 0; i < VisibleRows; i++)
                SetRow(i, "", false, false);
            SetMeter(null);
            FitChrome();
            return;
        }

        SetHint("[=] close  [Up/Down] move  [PgUp/PgDn] page  [Home/End]  [Enter] toggle  [Left/Right] intensity");

        for (var row = 0; row < VisibleRows; row++)
        {
            var index = _nav.WindowStart + row;
            if (index >= all.Count)
            {
                SetRow(row, "", false, false);
                continue;
            }

            var def = all[index];
            var on = CurseRegistry.IsEnabled(def.Id);
            var selected = index == _nav.HighlightIndex;
            var sb = new StringBuilder();
            sb.Append(on ? "ON " : ".  ");
            sb.Append(def.DisplayName);
            SetRow(row, sb.ToString(), selected, on, def.DangerWhenOff, def.WarnYellowWhenOff);
        }

        if (TryGetHighlighted(out var highlighted) && highlighted.HasIntensity)
            SetMeter(highlighted);
        else
            SetMeter(null);

        FitChrome();
    }

    private void SetMeter(CurseDefinition def)
    {
        if (def == null || !def.HasIntensity)
        {
            SetMeterLabel("");
            return;
        }

        CurseRegistry.TryGetIntensity(def.Id, out var intensity);
        SetMeterLabel($"INTENSITY  {intensity:0.##}   < >");
    }

    private void SetHint(string text)
    {
        if (_hintTmp != null) _hintTmp.text = text;
        if (_hintUi != null) _hintUi.text = text;
        if (_hintPlate != null)
            _hintPlate.enabled = !string.IsNullOrEmpty(text);
    }

    private void SetMeterLabel(string text)
    {
        if (_meterLabelTmp != null) _meterLabelTmp.text = text;
        if (_meterLabelUi != null) _meterLabelUi.text = text;
        if (_meterPlate != null)
            _meterPlate.enabled = !string.IsNullOrEmpty(text);
    }

    private void SetRow(
        int row,
        string text,
        bool selected,
        bool on,
        bool dangerWhenOff = false,
        bool warnYellowWhenOff = false)
    {
        var hasText = !string.IsNullOrEmpty(text);

        if (row < _rowPlates.Count && _rowPlates[row] != null)
            _rowPlates[row].enabled = hasText;

        if (row < _rowHighlights.Count && _rowHighlights[row] != null)
            _rowHighlights[row].enabled = selected && hasText;

        Color color;
        if (!hasText)
        {
            color = Color.clear;
        }
        else
        {
            CurseRowColor.Rgba(on, dangerWhenOff, warnYellowWhenOff, out var r, out var g, out var b, out var a);
            color = new Color(r, g, b, a);
        }

        if (selected && hasText)
            color = Color.Lerp(color, TitleColor, 0.35f);

        if (row < _rowTmps.Count && _rowTmps[row] != null)
        {
            _rowTmps[row].text = text;
            _rowTmps[row].color = color;
        }

        if (row < _rowUis.Count && _rowUis[row] != null)
        {
            _rowUis[row].text = text;
            _rowUis[row].color = color;
        }
    }

    private void RestyleFromGameAssets()
    {
        if (_panel != null)
        {
            var img = _panel.GetComponent<Image>();
            if (img != null)
                GameUiAssets.ApplyPanelImage(img);
        }

        if (!GameUiAssets.HasTmpFont)
            return;

        if (_titleTmp != null)
        {
            var titleColor = Color.HSVToRGB(
                TitleRgbCycle.HueAt(Time.unscaledTime, TitleRgbCycle.DefaultCyclesPerSecond),
                1f,
                1f);
            GameUiAssets.ApplyTmp(_titleTmp, 36f, titleColor, TextAlignmentOptions.Center, useDisplayFont: true);
        }
        if (_hintTmp != null)
            GameUiAssets.ApplyTmp(_hintTmp, 16f, HintColor, TextAlignmentOptions.Center);
        if (_meterLabelTmp != null)
            GameUiAssets.ApplyTmp(_meterLabelTmp, 20f, MeterLabelColor, TextAlignmentOptions.Center);

        for (var i = 0; i < _rowTmps.Count; i++)
        {
            if (_rowTmps[i] != null)
                GameUiAssets.ApplyTmp(_rowTmps[i], 24f, RowOffColor, TextAlignmentOptions.Left);
        }

        FitChrome();
    }

    private void FitChrome()
    {
        FitTitleChrome();
        FitCenteredPlate(_hintRoot, _hintTmp, _hintUi, minWidth: 160f);
        FitCenteredPlate(_meterRoot, _meterLabelTmp, _meterLabelUi, minWidth: 120f);
        for (var i = 0; i < VisibleRows; i++)
            FitRowChrome(i);
    }

    private void FitTitleChrome()
    {
        if (_titleRoot == null)
            return;

        var tip = HexPlateLayout.DefaultTipFraction;
        var textWidth = 120f;
        var height = 40f;

        if (_titleTmp != null)
        {
            _titleTmp.alignment = TextAlignmentOptions.Center;
            _titleTmp.ForceMeshUpdate();
            var pref = _titleTmp.GetPreferredValues(
                string.IsNullOrEmpty(_titleTmp.text) ? "TEA CURSES" : _titleTmp.text);
            textWidth = pref.x;
            height = Mathf.Max(pref.y + 12f, 36f);
        }
        else if (_titleUi != null)
        {
            _titleUi.alignment = TextAnchor.MiddleCenter;
            textWidth = Mathf.Max(_titleUi.preferredWidth, 80f);
            height = Mathf.Max(_titleUi.preferredHeight + 12f, 36f);
        }

        var width = HexPlateLayout.PlateWidthForText(textWidth, tip, PlatePad);
        _titleRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        _titleRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        ApplyTextInsets(_titleRoot, tip, centered: true);
    }

    private void FitCenteredPlate(RectTransform root, TMP_Text tmp, Text ui, float minWidth)
    {
        if (root == null)
            return;

        var text = tmp != null ? tmp.text : ui != null ? ui.text : "";
        if (string.IsNullOrEmpty(text))
        {
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
            return;
        }

        var tip = HexPlateLayout.DefaultTipFraction;
        var textWidth = minWidth;
        if (tmp != null)
        {
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.ForceMeshUpdate();
            textWidth = Mathf.Max(tmp.GetPreferredValues(text).x, minWidth * 0.5f);
        }
        else if (ui != null)
        {
            ui.alignment = TextAnchor.MiddleCenter;
            textWidth = Mathf.Max(ui.preferredWidth, minWidth * 0.5f);
        }

        var width = HexPlateLayout.PlateWidthForText(textWidth, tip, PlatePad);
        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        ApplyTextInsets(root, tip, centered: true);
    }

    private void FitRowChrome(int row)
    {
        if (row < 0 || row >= _rowRoots.Count)
            return;

        var root = _rowRoots[row];
        if (root == null)
            return;

        var tmp = row < _rowTmps.Count ? _rowTmps[row] : null;
        var ui = row < _rowUis.Count ? _rowUis[row] : null;
        var text = tmp != null ? tmp.text : ui != null ? ui.text : "";
        if (string.IsNullOrEmpty(text))
        {
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
            return;
        }

        var tip = HexPlateLayout.DefaultTipFraction;
        var textWidth = 80f;
        if (tmp != null)
        {
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.ForceMeshUpdate();
            textWidth = Mathf.Max(tmp.GetPreferredValues(text).x, 40f);
        }
        else if (ui != null)
        {
            ui.alignment = TextAnchor.MiddleLeft;
            textWidth = Mathf.Max(ui.preferredWidth, 40f);
        }

        var width = HexPlateLayout.PlateWidthForText(textWidth, tip, PlatePad);
        // Keep row slot full-width for vertical layout; size the plate only.
        // Root is the plate host — left-anchored content width.
        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        ApplyTextInsets(root, tip, centered: false);
    }

    private static void ApplyTextInsets(RectTransform plateRoot, float tipFraction, bool centered)
    {
        if (plateRoot == null || plateRoot.childCount < 2)
            return;

        // Children: Plate, [Highlight], Label — label is last.
        var label = plateRoot.GetChild(plateRoot.childCount - 1) as RectTransform;
        if (label == null || label.name != "Label")
            return;

        var width = plateRoot.rect.width;
        if (width <= 1f)
            width = plateRoot.sizeDelta.x;
        var inset = HexPlateLayout.TextInset(width, tipFraction, PlatePad);
        if (centered)
        {
            label.offsetMin = new Vector2(inset, label.offsetMin.y);
            label.offsetMax = new Vector2(-inset, label.offsetMax.y);
        }
        else
        {
            label.offsetMin = new Vector2(inset, label.offsetMin.y);
            label.offsetMax = new Vector2(-inset, label.offsetMax.y);
        }
    }

    private void Build()
    {
        var root = gameObject;
        Object.DontDestroyOnLoad(root);
        GameUiAssets.Invalidate();
        GameUiAssets.EnsureCached();
        GameUiAssets.RefreshPanelAlbumArt();
        HexPlateSprite.Invalidate();

        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        canvasGo.transform.SetParent(root.transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9000;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        _panel = CreatePanel(canvasGo.transform);
        CreateHeader(_panel);
        var listRoot = CreateListRoot(_panel);
        for (var i = 0; i < VisibleRows; i++)
            CreateRow(listRoot, i);
        CreateMeterSection(_panel);
        RestyleFromGameAssets();
    }

    private static RectTransform CreatePanel(Transform parent)
    {
        var go = new GameObject("Panel", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.04f, 0.18f);
        rt.anchorMax = new Vector2(0.42f, 0.86f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        GameUiAssets.ApplyPanelImage(img);
        return rt;
    }

    private void CreateHeader(Transform parent)
    {
        var titleGo = new GameObject("Title", typeof(RectTransform));
        _titleRoot = (RectTransform)titleGo.transform;
        _titleRoot.SetParent(parent, false);
        _titleRoot.anchorMin = new Vector2(0.5f, 0.95f);
        _titleRoot.anchorMax = new Vector2(0.5f, 0.95f);
        _titleRoot.pivot = new Vector2(0.5f, 0.5f);
        _titleRoot.anchoredPosition = Vector2.zero;
        _titleRoot.sizeDelta = new Vector2(200f, 44f);

        var titlePlateGo = new GameObject("Plate", typeof(RectTransform));
        var titlePlateRt = (RectTransform)titlePlateGo.transform;
        titlePlateRt.SetParent(_titleRoot, false);
        titlePlateRt.anchorMin = Vector2.zero;
        titlePlateRt.anchorMax = Vector2.one;
        titlePlateRt.offsetMin = Vector2.zero;
        titlePlateRt.offsetMax = Vector2.zero;
        _titlePlate = titlePlateGo.AddComponent<Image>();
        HexPlateSprite.Apply(_titlePlate, RowPlateColor);
        _titlePlate.enabled = true;

        var titleLabelGo = new GameObject("Label", typeof(RectTransform));
        var titleLabelRt = (RectTransform)titleLabelGo.transform;
        titleLabelRt.SetParent(_titleRoot, false);
        titleLabelRt.anchorMin = Vector2.zero;
        titleLabelRt.anchorMax = Vector2.one;
        titleLabelRt.offsetMin = new Vector2(12f, 2f);
        titleLabelRt.offsetMax = new Vector2(-12f, -2f);
        CreateLabel(titleLabelGo, "TEA CURSES", 36f, 30, TitleColor, TextAlignmentOptions.Center, TextAnchor.MiddleCenter,
            out _titleTmp, out _titleUi);

        var hintGo = new GameObject("Hint", typeof(RectTransform));
        _hintRoot = (RectTransform)hintGo.transform;
        _hintRoot.SetParent(parent, false);
        _hintRoot.anchorMin = new Vector2(0.5f, 0.87f);
        _hintRoot.anchorMax = new Vector2(0.5f, 0.87f);
        _hintRoot.pivot = new Vector2(0.5f, 0.5f);
        _hintRoot.anchoredPosition = Vector2.zero;
        _hintRoot.sizeDelta = new Vector2(420f, 28f);

        var plateGo = new GameObject("Plate", typeof(RectTransform));
        var plateRt = (RectTransform)plateGo.transform;
        plateRt.SetParent(_hintRoot, false);
        plateRt.anchorMin = Vector2.zero;
        plateRt.anchorMax = Vector2.one;
        plateRt.offsetMin = Vector2.zero;
        plateRt.offsetMax = Vector2.zero;
        _hintPlate = plateGo.AddComponent<Image>();
        HexPlateSprite.Apply(_hintPlate, RowPlateColor);
        _hintPlate.enabled = false;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.SetParent(_hintRoot, false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(14f, 0f);
        labelRt.offsetMax = new Vector2(-14f, 0f);
        CreateLabel(labelGo, "", 16f, 13, HintColor, TextAlignmentOptions.Center, TextAnchor.MiddleCenter,
            out _hintTmp, out _hintUi);
    }

    private static RectTransform CreateListRoot(Transform parent)
    {
        var go = new GameObject("List", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0f, 0.10f);
        rt.anchorMax = new Vector2(1f, 0.82f);
        rt.offsetMin = new Vector2(16f, 4f);
        rt.offsetMax = new Vector2(-16f, -4f);
        return rt;
    }

    private void CreateRow(Transform parent, int index)
    {
        var slotGo = new GameObject($"RowSlot{index}", typeof(RectTransform));
        var slotRt = (RectTransform)slotGo.transform;
        slotRt.SetParent(parent, false);
        var top = 1f - index / (float)VisibleRows;
        var bottom = 1f - (index + 1) / (float)VisibleRows;
        slotRt.anchorMin = new Vector2(0f, bottom);
        slotRt.anchorMax = new Vector2(1f, top);
        slotRt.offsetMin = new Vector2(0f, 2f);
        slotRt.offsetMax = new Vector2(0f, -2f);

        var go = new GameObject($"Row{index}", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(slotRt, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(200f, 0f);
        _rowRoots.Add(rt);

        var plateGo = new GameObject("Plate", typeof(RectTransform));
        var plateRt = (RectTransform)plateGo.transform;
        plateRt.SetParent(rt, false);
        plateRt.anchorMin = Vector2.zero;
        plateRt.anchorMax = Vector2.one;
        plateRt.offsetMin = Vector2.zero;
        plateRt.offsetMax = Vector2.zero;
        var plate = plateGo.AddComponent<Image>();
        HexPlateSprite.Apply(plate, RowPlateColor);
        plate.enabled = false;
        _rowPlates.Add(plate);

        var highlightGo = new GameObject("Highlight", typeof(RectTransform));
        var highlightRt = (RectTransform)highlightGo.transform;
        highlightRt.SetParent(rt, false);
        highlightRt.anchorMin = Vector2.zero;
        highlightRt.anchorMax = Vector2.one;
        highlightRt.offsetMin = Vector2.zero;
        highlightRt.offsetMax = Vector2.zero;
        var highlight = highlightGo.AddComponent<Image>();
        HexPlateSprite.Apply(highlight, HighlightColor);
        highlight.enabled = false;
        _rowHighlights.Add(highlight);

        var textGo = new GameObject("Label", typeof(RectTransform));
        var textRt = (RectTransform)textGo.transform;
        textRt.SetParent(rt, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(14f, 0f);
        textRt.offsetMax = new Vector2(-14f, 0f);

        CreateLabel(textGo, "", 24f, 20, RowOffColor, TextAlignmentOptions.Left, TextAnchor.MiddleLeft,
            out var tmp, out var ui);
        _rowTmps.Add(tmp);
        _rowUis.Add(ui);
    }

    private void CreateMeterSection(Transform parent)
    {
        var meterGo = new GameObject("Meter", typeof(RectTransform));
        _meterRoot = (RectTransform)meterGo.transform;
        _meterRoot.SetParent(parent, false);
        _meterRoot.anchorMin = new Vector2(0.5f, 0.06f);
        _meterRoot.anchorMax = new Vector2(0.5f, 0.06f);
        _meterRoot.pivot = new Vector2(0.5f, 0.5f);
        _meterRoot.anchoredPosition = Vector2.zero;
        _meterRoot.sizeDelta = new Vector2(220f, 32f);

        var plateGo = new GameObject("Plate", typeof(RectTransform));
        var plateRt = (RectTransform)plateGo.transform;
        plateRt.SetParent(_meterRoot, false);
        plateRt.anchorMin = Vector2.zero;
        plateRt.anchorMax = Vector2.one;
        plateRt.offsetMin = Vector2.zero;
        plateRt.offsetMax = Vector2.zero;
        _meterPlate = plateGo.AddComponent<Image>();
        HexPlateSprite.Apply(_meterPlate, RowPlateColor);
        _meterPlate.enabled = false;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.SetParent(_meterRoot, false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(14f, 0f);
        labelRt.offsetMax = new Vector2(-14f, 0f);
        CreateLabel(labelGo, "", 18f, 15, MeterLabelColor, TextAlignmentOptions.Center, TextAnchor.MiddleCenter,
            out _meterLabelTmp, out _meterLabelUi);
    }

    /// <summary>
    /// Prefer TMP only when a real Rift font asset is cached; otherwise UI.Text.
    /// Creating TextMeshProUGUI without a font throws in this game.
    /// </summary>
    private static void CreateLabel(
        GameObject host,
        string text,
        float tmpSize,
        int uiSize,
        Color color,
        TextAlignmentOptions tmpAlign,
        TextAnchor uiAlign,
        out TMP_Text tmp,
        out Text ui)
    {
        tmp = null;
        ui = null;

        if (GameUiAssets.HasTmpFont)
        {
            try
            {
                var created = host.AddComponent<TextMeshProUGUI>();
                GameUiAssets.ApplyTmp(created, tmpSize, color, tmpAlign);
                created.text = text ?? "";
                tmp = created;
                return;
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogWarning($"TeaCurses: TMP label failed, using UI.Text ({ex.Message})");
                var bad = host.GetComponent<TextMeshProUGUI>();
                if (bad != null)
                    Object.Destroy(bad);
            }
        }

        ui = host.AddComponent<Text>();
        ui.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        ui.fontSize = uiSize;
        ui.color = color;
        ui.alignment = uiAlign;
        ui.horizontalOverflow = HorizontalWrapMode.Overflow;
        ui.verticalOverflow = VerticalWrapMode.Truncate;
        ui.text = text ?? "";
    }
}
