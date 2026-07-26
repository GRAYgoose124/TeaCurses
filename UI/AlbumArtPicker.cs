using System;
using System.Collections.Generic;

namespace TeaCurses.UI;

public static class AlbumArtPicker
{
    /// <summary>
    /// Picks a random index among entries marked usable. Returns -1 if none.
    /// </summary>
    public static int TryPickIndex(IReadOnlyList<bool> usable, Random rng)
    {
        if (usable == null || usable.Count == 0 || rng == null)
            return -1;

        var valid = new List<int>(usable.Count);
        for (var i = 0; i < usable.Count; i++)
        {
            if (usable[i])
                valid.Add(i);
        }

        if (valid.Count == 0)
            return -1;

        return valid[rng.Next(valid.Count)];
    }
}
