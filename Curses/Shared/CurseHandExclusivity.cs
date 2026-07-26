using TeaCurses.Curse;

namespace TeaCurses;

/// <summary>
/// One Hand and Alternating Hands cannot both be enabled.
/// Call after a curse toggle when the toggled id may have become enabled.
/// </summary>
public static class CurseHandExclusivity
{
    public static void AfterEnabled(string enabledId, string oneHandId, string alternatingId)
    {
        if (string.IsNullOrEmpty(enabledId) || !CurseRegistry.IsEnabled(enabledId))
            return;

        if (enabledId == oneHandId)
            CurseRegistry.SetEnabled(alternatingId, false);
        else if (enabledId == alternatingId)
            CurseRegistry.SetEnabled(oneHandId, false);
    }
}
