using UnityEngine;
using UnityEngine.UI;

namespace TeaCurses.UI;

/// <summary>
/// Flat-top hexagon sprite (tips on left/right, flats top/bottom),
/// meant to be stretched horizontally for menu plates.
/// </summary>
public static class HexPlateSprite
{
    private static Sprite _sprite;
    private static Texture2D _texture;
    private static float _bakedTipFraction = -1f;

    public static void Invalidate()
    {
        _sprite = null;
        if (_texture != null)
        {
            Object.Destroy(_texture);
            _texture = null;
        }

        _bakedTipFraction = -1f;
    }

    public static void Apply(Image image, Color color)
    {
        if (image == null)
            return;

        image.sprite = Get();
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = color;
    }

    public static Sprite Get()
    {
        var tipFraction = HexPlateLayout.DefaultTipFraction;
        if (_sprite != null && Mathf.Abs(_bakedTipFraction - tipFraction) < 0.0001f)
            return _sprite;

        Invalidate();

        const int width = 256;
        const int height = 64;
        _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "TeaCursesHexPlate",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };

        var pixels = new Color32[width * height];
        _bakedTipFraction = tipFraction;

        for (var y = 0; y < height; y++)
        {
            var v = (y + 0.5f) / height;
            for (var x = 0; x < width; x++)
            {
                var u = (x + 0.5f) / width;
                var alpha = HexPlateMath.Coverage(u, v, tipFraction);
                var a = (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255);
                pixels[y * width + x] = new Color32(255, 255, 255, a);
            }
        }

        _texture.SetPixels32(pixels);
        _texture.Apply(false, true);
        _sprite = Sprite.Create(
            _texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
        _sprite.name = "TeaCursesHexPlateSprite";
        return _sprite;
    }
}
