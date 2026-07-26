using System.Collections.Generic;

namespace TeaCurses.Curses;

/// <summary>
/// Complex Runic + Cuneiform codepoints for Cryptid glyph assignment.
/// </summary>
public static class CryptidGlyphPool
{
    /// <summary>
    /// Mixed historic scripts — prefer multi-stroke codepoints.
    /// </summary>
    public static readonly IReadOnlyList<int> Default = BuildDefault();

    private static IReadOnlyList<int> BuildDefault()
    {
        var list = new List<int>(64);

        // Runic BMP (Unicode-only mode relies on these — SMP cuneiform needs font fallbacks)
        for (var cp = 0x16A0; cp <= 0x16F8; cp++)
            list.Add(cp);

        // Cuneiform SMP (Mix / Procedural; Unicode-only skips failed OS bakes)
        int[] cuneiform =
        {
            0x12000, 0x12009, 0x12016, 0x1201F, 0x1202D, 0x1203A, 0x12048, 0x12055,
            0x12063, 0x12070, 0x1207E, 0x1208C, 0x1209A, 0x120A8, 0x120B6, 0x120C4,
            0x120D2, 0x120E0, 0x120EE, 0x120FC, 0x1210A, 0x12118, 0x12126, 0x12134,
        };
        for (var i = 0; i < cuneiform.Length; i++)
            list.Add(cuneiform[i]);

        return list;
    }
}
