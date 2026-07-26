using System.Collections.Generic;
using RhythmRift;
using Shared;
using Shared.TrackSelection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeaCurses.UI;

/// <summary>
/// Pulls live Rift UI assets (TMP fonts, track album art) from the current scene.
/// Prefers the score/combo display font.
/// Panel album art is re-resolved via <see cref="RefreshPanelAlbumArt"/> (prefer current track).
/// </summary>
public static class GameUiAssets
{
    private static readonly Color FlatPanelColor = new Color(0.08f, 0.07f, 0.12f, 0.94f);

    private static TMP_FontAsset _tmpFont;
    private static Material _tmpMaterial;
    private static TMP_FontAsset _displayFont;
    private static Material _displayMaterial;
    private static bool _displayFontIsPreferred;
    private static Sprite _panelSprite;

    public static bool HasTmpFont
    {
        get
        {
            EnsureCached();
            return _tmpFont != null || _displayFont != null;
        }
    }

    public static void EnsureCached()
    {
        if (_tmpFont == null)
            TryCacheBodyFont();

        // Upgrade to score/combo font when it becomes available.
        if (!_displayFontIsPreferred)
            TryCacheDisplayFont();
    }

    private static void TryCacheBodyFont()
    {
        try
        {
            TMP_FontAsset best = null;
            Material bestMat = null;
            var bestRank = int.MinValue;

            foreach (var tmp in UnityEngine.Object.FindObjectsOfType<TMP_Text>(true))
            {
                if (tmp == null)
                    continue;
                TMP_FontAsset font = null;
                try
                {
                    font = tmp.font;
                }
                catch
                {
                    continue;
                }

                if (font == null)
                    continue;

                var rank = FontAssetPicker.Rank(font.name);
                // Prefer ordinary UI fonts for body (avoid score/combo for list text readability).
                if (rank >= 100)
                    rank -= 80;

                if (rank > bestRank)
                {
                    bestRank = rank;
                    best = font;
                    try
                    {
                        bestMat = tmp.fontSharedMaterial;
                    }
                    catch
                    {
                        bestMat = null;
                    }
                }
            }

            if (best != null)
            {
                _tmpFont = best;
                _tmpMaterial = bestMat;
            }
        }
        catch
        {
            // Scene may not be ready for FindObjectsOfType.
        }
    }

    private static void TryCacheDisplayFont()
    {
        try
        {
            // Stage score text.
            foreach (var view in UnityEngine.Object.FindObjectsOfType<RRStageUIView>(true))
            {
                if (view?._scoreText?.font == null)
                    continue;
                SetDisplayFont(view._scoreText.font, SafeMaterial(view._scoreText), preferred: true);
                return;
            }

            foreach (var combo in UnityEngine.Object.FindObjectsOfType<ComboTextVFX>(true))
            {
                if (combo?._comboText?.font == null)
                    continue;
                SetDisplayFont(combo._comboText.font, SafeMaterial(combo._comboText), preferred: true);
                return;
            }

            // Loaded font assets (works on track select if assets are resident).
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (fonts != null && fonts.Length > 0)
            {
                var names = new List<string>(fonts.Length);
                for (var i = 0; i < fonts.Length; i++)
                    names.Add(fonts[i] != null ? fonts[i].name : "");

                var pick = FontAssetPicker.TryPickBestIndex(names);
                if (pick >= 0 && FontAssetPicker.Rank(names[pick]) >= 100)
                {
                    SetDisplayFont(fonts[pick], fonts[pick].material, preferred: true);
                    return;
                }
            }

            // Fall back to best-ranked live TMP text.
            TMP_Text bestTmp = null;
            var bestRank = int.MinValue;
            foreach (var tmp in UnityEngine.Object.FindObjectsOfType<TMP_Text>(true))
            {
                if (tmp?.font == null)
                    continue;
                var rank = FontAssetPicker.Rank(tmp.font.name);
                if (rank > bestRank)
                {
                    bestRank = rank;
                    bestTmp = tmp;
                }
            }

            if (bestTmp != null)
                SetDisplayFont(bestTmp.font, SafeMaterial(bestTmp), preferred: bestRank >= 100);
        }
        catch
        {
            // ignore
        }
    }

    private static void SetDisplayFont(TMP_FontAsset font, Material material, bool preferred)
    {
        _displayFont = font;
        _displayMaterial = material;
        _displayFontIsPreferred = preferred;
    }

    private static Material SafeMaterial(TMP_Text tmp)
    {
        try
        {
            return tmp.fontSharedMaterial;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Re-gather current-track / fallback album art and store the winner for
    /// <see cref="ApplyPanelImage"/>. Call on each overlay open.
    /// </summary>
    public static void RefreshPanelAlbumArt()
    {
        _panelSprite = null;
        try
        {
            var preferred = new List<Sprite>();
            var preferredUsable = new List<bool>();

            // 1) Currently selected list-row (tracks only — skip folders/promo/tutorial)
            try
            {
                foreach (var opt in UnityEngine.Object.FindObjectsOfType<BaseTrackSelectionOption>(true))
                {
                    if (opt == null || !opt.IsSelected)
                        continue;

                    Sprite sprite = null;
                    try
                    {
                        var img = opt._albumArtImage;
                        if (img != null)
                            sprite = img.sprite;
                    }
                    catch
                    {
                        continue;
                    }

                    if (sprite == null)
                        continue;

                    var isNonTrack = IsNonTrackOption(opt);
                    var isDefault = false;
                    try
                    {
                        isDefault = opt._defaultAlbumArt != null
                            && ReferenceEquals(sprite, opt._defaultAlbumArt);
                    }
                    catch
                    {
                        // ignore
                    }

                    preferred.Add(sprite);
                    preferredUsable.Add(AlbumArtOptionRules.IsUsableCoverCandidate(isNonTrack, isDefault));
                    break;
                }
            }
            catch
            {
                // ignore
            }

            // 2) Track select featured (prefer live scene controller; honor mid-crossfade)
            try
            {
                TrackSelectionSceneController best = null;
                foreach (var ctrl in UnityEngine.Object.FindObjectsOfType<TrackSelectionSceneController>(true))
                {
                    if (ctrl == null)
                        continue;
                    if (best == null)
                        best = ctrl;
                    // Prefer the active hierarchy instance over inactive duplicates.
                    if (ctrl.gameObject.activeInHierarchy)
                    {
                        best = ctrl;
                        break;
                    }
                }

                if (best != null)
                {
                    Sprite sprite = null;
                    try
                    {
                        Sprite album = null;
                        Sprite next = null;
                        if (best._albumArt != null)
                            album = best._albumArt.sprite;
                        if (best._nextAlbumArt != null)
                            next = best._nextAlbumArt.sprite;

                        // ChangeAlbumArt writes the new cover to _nextAlbumArt first and
                        // only copies to _albumArt after the transition. Prefer next when
                        // it differs so we don't stick on the previous track's art.
                        if (next != null && !ReferenceEquals(next, album))
                            sprite = next;
                        else
                            sprite = album ?? next;
                    }
                    catch
                    {
                        sprite = null;
                    }

                    if (sprite != null)
                    {
                        var isNoTracks = false;
                        try
                        {
                            isNoTracks = best._noTracksAlbumArt != null
                                && ReferenceEquals(sprite, best._noTracksAlbumArt);
                        }
                        catch
                        {
                            // ignore
                        }

                        preferred.Add(sprite);
                        preferredUsable.Add(!isNoTracks);
                    }
                }
            }
            catch
            {
                // ignore
            }

            // 3) Loadout
            try
            {
                foreach (var loadout in UnityEngine.Object.FindObjectsOfType<LoadoutScreenManager>(true))
                {
                    if (loadout == null)
                        continue;
                    Sprite sprite = null;
                    try
                    {
                        var img = loadout._albumArt;
                        if (img != null)
                            sprite = img.sprite;
                    }
                    catch
                    {
                        continue;
                    }

                    preferred.Add(sprite);
                    preferredUsable.Add(sprite != null);
                    break;
                }
            }
            catch
            {
                // ignore
            }

            // 4) Stage
            try
            {
                foreach (var stage in UnityEngine.Object.FindObjectsOfType<RRStageController>(true))
                {
                    if (stage == null)
                        continue;
                    Sprite sprite = null;
                    try
                    {
                        sprite = stage._albumArt;
                    }
                    catch
                    {
                        continue;
                    }

                    preferred.Add(sprite);
                    preferredUsable.Add(sprite != null);
                    break;
                }
            }
            catch
            {
                // ignore
            }

            // 5) List-row fallback pool
            var fallback = new List<Sprite>();
            var fallbackUsable = new List<bool>();
            try
            {
                var options = UnityEngine.Object.FindObjectsOfType<BaseTrackSelectionOption>(true);
                if (options != null)
                {
                    foreach (var opt in options)
                    {
                        if (opt == null)
                            continue;

                        Sprite sprite = null;
                        try
                        {
                            var img = opt._albumArtImage;
                            if (img != null)
                                sprite = img.sprite;
                        }
                        catch
                        {
                            continue;
                        }

                        if (sprite == null)
                            continue;

                        var isNonTrack = IsNonTrackOption(opt);
                        var isDefault = false;
                        try
                        {
                            isDefault = opt._defaultAlbumArt != null
                                && ReferenceEquals(sprite, opt._defaultAlbumArt);
                        }
                        catch
                        {
                            // ignore
                        }

                        fallback.Add(sprite);
                        fallbackUsable.Add(AlbumArtOptionRules.IsUsableCoverCandidate(isNonTrack, isDefault));
                    }
                }
            }
            catch
            {
                // ignore
            }

            var pick = AlbumArtResolver.TryResolve(preferredUsable, fallbackUsable, new System.Random());
            if (pick.Kind == AlbumArtSourceKind.Preferred)
                _panelSprite = preferred[pick.Index];
            else if (pick.Kind == AlbumArtSourceKind.Fallback)
                _panelSprite = fallback[pick.Index];
        }
        catch
        {
            _panelSprite = null;
        }
    }

    private static bool IsNonTrackOption(BaseTrackSelectionOption opt)
    {
        try
        {
            var type = opt.TrackOptionType;
            return type == BaseTrackSelectionOptionGroup.TrackSelectAlternateOptionType.Folder
                || type == BaseTrackSelectionOptionGroup.TrackSelectAlternateOptionType.Tutorial
                || type == BaseTrackSelectionOptionGroup.TrackSelectAlternateOptionType.Promo;
        }
        catch
        {
            return false;
        }
    }

    public static void ApplyTmp(TMP_Text label, float fontSize, Color color, TextAlignmentOptions align)
    {
        ApplyTmp(label, fontSize, color, align, useDisplayFont: false);
    }

    public static void ApplyTmp(
        TMP_Text label,
        float fontSize,
        Color color,
        TextAlignmentOptions align,
        bool useDisplayFont)
    {
        if (label == null)
            return;

        var font = useDisplayFont
            ? (_displayFont ?? _tmpFont)
            : (_tmpFont ?? _displayFont);
        var material = useDisplayFont
            ? (_displayMaterial ?? _tmpMaterial)
            : (_tmpMaterial ?? _displayMaterial);

        if (font == null)
            return;

        try
        {
            label.font = font;
            if (material != null)
                label.fontSharedMaterial = material;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = align;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
        }
        catch
        {
            // TMP can throw if material/font mismatch — leave label alone.
        }
    }

    public static void ApplyPanelImage(Image img)
    {
        EnsureCached();
        if (img == null)
            return;
        if (_panelSprite != null)
        {
            // Force Unity UI to accept a swap even if SoftReference reuses identity.
            if (ReferenceEquals(img.sprite, _panelSprite))
                img.sprite = null;
            img.sprite = _panelSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = new Color(1f, 1f, 1f, 0.9f);
        }
        else
        {
            img.sprite = null;
            img.color = FlatPanelColor;
        }
    }

    public static void Invalidate()
    {
        _tmpFont = null;
        _tmpMaterial = null;
        _displayFont = null;
        _displayMaterial = null;
        _displayFontIsPreferred = false;
        _panelSprite = null;
    }
}
