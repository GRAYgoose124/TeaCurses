using System;
using System.Collections.Generic;

namespace TeaCurses.UI;

public enum AlbumArtSourceKind
{
    None = 0,
    Preferred = 1,
    Fallback = 2,
}

public readonly struct AlbumArtPick
{
    public AlbumArtSourceKind Kind { get; }
    public int Index { get; }

    public AlbumArtPick(AlbumArtSourceKind kind, int index)
    {
        Kind = kind;
        Index = index;
    }

    public static AlbumArtPick None => new AlbumArtPick(AlbumArtSourceKind.None, -1);
}

public static class AlbumArtResolver
{
    /// <summary>
    /// Returns the first preferred usable index, else a random fallback index, else None.
    /// Preferred and fallback lists are independent; Index is into the winning list.
    /// </summary>
    public static AlbumArtPick TryResolve(
        IReadOnlyList<bool> preferredUsableInOrder,
        IReadOnlyList<bool> fallbackUsable,
        Random rng)
    {
        if (preferredUsableInOrder != null)
        {
            for (var i = 0; i < preferredUsableInOrder.Count; i++)
            {
                if (preferredUsableInOrder[i])
                    return new AlbumArtPick(AlbumArtSourceKind.Preferred, i);
            }
        }

        var fallback = AlbumArtPicker.TryPickIndex(fallbackUsable, rng);
        if (fallback < 0)
            return AlbumArtPick.None;

        return new AlbumArtPick(AlbumArtSourceKind.Fallback, fallback);
    }
}
