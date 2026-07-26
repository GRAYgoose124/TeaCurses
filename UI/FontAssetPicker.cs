using System;
using System.Collections.Generic;

namespace TeaCurses.UI;

/// <summary>
/// Ranks TMP font asset names so we prefer Rift's display/score/combo look
/// over generic body/default fonts.
/// </summary>
public static class FontAssetPicker
{
    public static int Rank(string fontName)
    {
        if (string.IsNullOrEmpty(fontName))
            return 0;

        var n = fontName.ToLowerInvariant();
        var rank = 10;

        if (n.Contains("score") || n.Contains("combo") || n.Contains("multiplier"))
            rank += 100;
        if (n.Contains("display") || n.Contains("title") || n.Contains("header") || n.Contains("banner"))
            rank += 40;
        if (n.Contains("number") || n.Contains("numeral"))
            rank += 20;

        if (n.Contains("liberation") || n.Contains("arial") || n.Contains("roboto") || n.Contains("noto"))
            rank -= 50;

        return rank;
    }

    /// <summary>
    /// Returns the index of the highest-ranked name, or -1 if none.
    /// </summary>
    public static int TryPickBestIndex(IReadOnlyList<string> fontNames)
    {
        if (fontNames == null || fontNames.Count == 0)
            return -1;

        var best = -1;
        var bestRank = int.MinValue;
        for (var i = 0; i < fontNames.Count; i++)
        {
            var rank = Rank(fontNames[i]);
            if (rank > bestRank)
            {
                bestRank = rank;
                best = i;
            }
        }

        return best;
    }
}
