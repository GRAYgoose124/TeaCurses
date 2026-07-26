namespace TeaCurses.Curses;

/// <summary>
/// Who Armored should bump from 1 HP → 2 HP at spawn.
/// </summary>
public static class ArmoredRules
{
    public static bool ShouldArmor(bool isHealthItem, int currentHp)
    {
        return !isHealthItem && currentHp == 1;
    }
}
