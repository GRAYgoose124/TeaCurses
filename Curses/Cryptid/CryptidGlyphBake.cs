using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeaCurses.Curses;

/// <summary>
/// Bakes Unicode codepoints to sprites via OS dynamic fonts.
/// Missing glyphs are skipped (caller filters the pool).
/// </summary>
public static class CryptidGlyphBake
{
    private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();
    private static readonly string[] FontFallbacks =
    {
        "Segoe UI Historic",
        "Segoe UI Symbol",
        "Noto Sans Cuneiform",
        "Noto Sans",
        "Arial Unicode MS",
        "Arial",
    };

    private static Font _font;
    private static int _fontSize = 192;

    public static void Clear()
    {
        foreach (var kv in Cache)
        {
            if (kv.Value != null)
            {
                var tex = kv.Value.texture;
                UnityEngine.Object.Destroy(kv.Value);
                if (tex != null)
                    UnityEngine.Object.Destroy(tex);
            }
        }

        Cache.Clear();
    }

    public static List<int> BakeSuccessful(IReadOnlyList<int> pool, CryptidGlyphMode mode)
    {
        var ok = new List<int>();
        if (pool == null)
            return ok;

        for (var i = 0; i < pool.Count; i++)
        {
            var cp = pool[i];
            if (Ensure(cp, mode) != null)
                ok.Add(cp);
        }

        return ok;
    }

    public static Sprite Ensure(int codepoint, CryptidGlyphMode mode)
    {
        if (Cache.TryGetValue(codepoint, out var existing) && existing != null)
            return existing;

        var baked = BakeOne(codepoint, mode);
        if (baked != null)
            Cache[codepoint] = baked;
        return baked;
    }

    public static bool TryGet(int codepoint, out Sprite sprite)
    {
        if (Cache.TryGetValue(codepoint, out sprite) && sprite != null)
            return true;
        sprite = null;
        return false;
    }

    private static Font GetFont()
    {
        if (_font != null)
            return _font;

        try
        {
            _font = Font.CreateDynamicFontFromOSFont(FontFallbacks, _fontSize);
        }
        catch (Exception ex)
        {
            TeaCurses.Plugin.Logger?.LogWarning($"CryptidGlyphBake: OS font failed: {ex.Message}");
            _font = null;
        }

        return _font;
    }

    private static Sprite BakeOne(int codepoint, CryptidGlyphMode mode)
    {
        if (mode == CryptidGlyphMode.ProceduralOnly)
            return BakeProcedural(codepoint);

        var unicode = TryBakeUnicode(codepoint);
        if (unicode != null)
            return unicode;

        if (mode == CryptidGlyphMode.UnicodeOnly)
            return null;

        // Mix: stay in unicode family seed, draw procedural stand-in.
        return BakeProcedural(codepoint);
    }

    private static Sprite TryBakeUnicode(int codepoint)
    {
        // Unity Font CharacterInfo is char-based (BMP).
        if (codepoint < 0 || codepoint > 0xFFFF)
            return null;

        var font = GetFont();
        if (font == null)
            return null;

        var ch = (char)codepoint;
        var request = ch.ToString();
        try
        {
            font.RequestCharactersInTexture(request, _fontSize, FontStyle.Normal);
            if (!font.GetCharacterInfo(ch, out var info, _fontSize, FontStyle.Normal))
                return null;

            var src = font.material != null ? font.material.mainTexture as Texture2D : null;
            if (src == null)
                return null;

            var copy = CopyGlyphRegion(src, info);
            if (copy == null)
                return null;

            var normalized = NormalizeGlyphTexture(copy);
            if (normalized != copy)
                UnityEngine.Object.Destroy(copy);
            if (normalized == null)
                return null;

            normalized.name = "CryptidGlyph_" + codepoint.ToString("X");
            var sprite = Sprite.Create(
                normalized,
                new Rect(0, 0, normalized.width, normalized.height),
                new Vector2(0.5f, 0.5f),
                CryptidGlyphNormalize.PixelsPerUnit);
            sprite.name = normalized.name;
            return sprite;
        }
        catch (Exception ex)
        {
            TeaCurses.Plugin.Logger?.LogWarning($"CryptidGlyphBake: bake U+{codepoint:X} failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Trim transparent padding, then fit ink onto a shared canvas so every
    /// Unicode glyph shares the same world footprint as procedural marks.
    /// </summary>
    private static Texture2D NormalizeGlyphTexture(Texture2D source)
    {
        if (source == null)
            return null;

        var w = source.width;
        var h = source.height;
        var pixels = source.GetPixels();
        var coverage = new float[pixels.Length];
        for (var i = 0; i < pixels.Length; i++)
            coverage[i] = pixels[i].a;

        var srcPixels = pixels;
        var srcW = w;
        var srcH = h;

        if (CryptidGlyphNormalize.TryInkBounds(
                coverage, w, h, CryptidGlyphNormalize.InkAlphaThreshold,
                out var minX, out var minY, out var maxX, out var maxY))
        {
            srcW = maxX - minX + 1;
            srcH = maxY - minY + 1;
            srcPixels = new Color[srcW * srcH];
            for (var y = 0; y < srcH; y++)
            {
                for (var x = 0; x < srcW; x++)
                    srcPixels[y * srcW + x] = pixels[(minY + y) * w + (minX + x)];
            }
        }

        CryptidGlyphNormalize.Fit(
            srcW, srcH,
            CryptidGlyphNormalize.CanvasSize,
            CryptidGlyphNormalize.PadFraction,
            out var dstW, out var dstH, out var ox, out var oy,
            CryptidGlyphNormalize.UnicodeRelativeScale);

        var canvas = CryptidGlyphNormalize.CanvasSize;
        var dest = new Texture2D(canvas, canvas, TextureFormat.RGBA32, false);
        dest.filterMode = FilterMode.Bilinear;
        var clear = new Color(0, 0, 0, 0);
        var outPixels = new Color[canvas * canvas];
        for (var i = 0; i < outPixels.Length; i++)
            outPixels[i] = clear;

        // Nearest-neighbor upsample/downsample into the fitted rect.
        for (var y = 0; y < dstH; y++)
        {
            var srcY = (int)((y + 0.5f) * srcH / dstH);
            if (srcY >= srcH) srcY = srcH - 1;
            for (var x = 0; x < dstW; x++)
            {
                var srcX = (int)((x + 0.5f) * srcW / dstW);
                if (srcX >= srcW) srcX = srcW - 1;
                outPixels[(oy + y) * canvas + (ox + x)] = srcPixels[srcY * srcW + srcX];
            }
        }

        dest.SetPixels(outPixels);
        dest.Apply(false, false);
        return dest;
    }

    private static Texture2D CopyGlyphRegion(Texture2D src, CharacterInfo info)
    {
        var u0 = info.uvBottomLeft.x;
        var v0 = info.uvBottomLeft.y;
        var u1 = info.uvTopRight.x;
        var v1 = info.uvTopRight.y;

        var x0 = Mathf.FloorToInt(Mathf.Min(u0, u1) * src.width);
        var y0 = Mathf.FloorToInt(Mathf.Min(v0, v1) * src.height);
        var x1 = Mathf.CeilToInt(Mathf.Max(u0, u1) * src.width);
        var y1 = Mathf.CeilToInt(Mathf.Max(v0, v1) * src.height);
        var w = Mathf.Max(1, x1 - x0);
        var h = Mathf.Max(1, y1 - y0);

        var readable = MakeReadable(src);
        if (readable == null)
            return null;

        try
        {
            var pixels = readable.GetPixels(x0, y0, w, h);
            ToWhiteInk(pixels);
            var dest = new Texture2D(w, h, TextureFormat.RGBA32, false);
            dest.filterMode = FilterMode.Bilinear;
            dest.SetPixels(pixels);
            dest.Apply(false, false);
            return dest;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (readable != src)
                UnityEngine.Object.Destroy(readable);
        }
    }

    /// <summary>
    /// Font atlases often bake black RGB + alpha (or grayscale in RGB).
    /// Sprites/Default multiplies by white tint — black ink stays invisible on dark stages.
    /// Force white RGB and derive coverage from alpha and/or luminance.
    /// </summary>
    private static void ToWhiteInk(Color[] pixels)
    {
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var lum = (p.r + p.g + p.b) * (1f / 3f);
            float coverage;
            if (lum < 0.2f)
                coverage = p.a; // black ink → trust alpha
            else
                coverage = Mathf.Max(p.a, lum); // white/gray glyph in RGB or alpha

            pixels[i] = new Color(1f, 1f, 1f, coverage);
        }
    }

    private static Texture2D MakeReadable(Texture2D src)
    {
        if (src == null)
            return null;

        try
        {
            // Prefer blit path — font atlases are usually non-readable.
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var readable = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            readable.Apply(false, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return readable;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Deterministic abstract mark when OS glyph blit fails (e.g. SMP cuneiform).
    /// Keeps the curse playable; still unique per codepoint.
    /// </summary>
    private static Sprite BakeProcedural(int codepoint)
    {
        const int size = CryptidGlyphNormalize.CanvasSize;
        const int margin = 32;
        const int stampRadius = 4;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var clear = new Color(0, 0, 0, 0);
        var ink = Color.white;
        var pixels = new Color[size * size];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = clear;

        var rng = new System.Random(codepoint);
        var strokes = 4 + (codepoint & 3);
        for (var s = 0; s < strokes; s++)
        {
            var x0 = rng.Next(margin, size - margin);
            var y0 = rng.Next(margin, size - margin);
            var x1 = rng.Next(margin, size - margin);
            var y1 = rng.Next(margin, size - margin);
            DrawLine(pixels, size, x0, y0, x1, y1, ink, stampRadius);
        }

        tex.SetPixels(pixels);
        tex.Apply(false, false);
        tex.name = "CryptidGlyphProc_" + codepoint.ToString("X");
        var sprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            CryptidGlyphNormalize.PixelsPerUnit); // match unicode-normalized footprint
        sprite.name = tex.name;
        return sprite;
    }

    private static void DrawLine(
        Color[] pixels,
        int size,
        int x0,
        int y0,
        int x1,
        int y1,
        Color color,
        int stampRadius)
    {
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;
        var x = x0;
        var y = y0;
        while (true)
        {
            Stamp(pixels, size, x, y, color, stampRadius);
            if (x == x1 && y == y1)
                break;
            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
    }

    private static void Stamp(Color[] pixels, int size, int x, int y, Color color, int radius)
    {
        for (var oy = -radius; oy <= radius; oy++)
        {
            for (var ox = -radius; ox <= radius; ox++)
            {
                if (ox * ox + oy * oy > radius * radius)
                    continue;
                var px = x + ox;
                var py = y + oy;
                if (px < 0 || py < 0 || px >= size || py >= size)
                    continue;
                pixels[py * size + px] = color;
            }
        }
    }
}
