namespace TeaCurses;

public static class EdgeRockerRules
{
    public static bool ShouldAttackOnRelease(bool enabled, bool isReleaseInput)
        => enabled && isReleaseInput;

    public static bool? LeaderboardSubmissionOverride(bool enabled)
        => enabled ? false : null;
}
