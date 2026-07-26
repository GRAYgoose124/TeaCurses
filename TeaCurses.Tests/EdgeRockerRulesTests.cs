using TeaCurses;
using Xunit;

namespace TeaCurses.Tests;

public class EdgeRockerRulesTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void ShouldAttackOnRelease_maps(bool enabled, bool isRelease, bool expected)
    {
        Assert.Equal(expected, EdgeRockerRules.ShouldAttackOnRelease(enabled, isRelease));
    }

    [Fact]
    public void Leaderboard_override_false_when_on()
    {
        Assert.Equal(false, EdgeRockerRules.LeaderboardSubmissionOverride(true));
    }

    [Fact]
    public void Leaderboard_override_null_when_off()
    {
        Assert.Null(EdgeRockerRules.LeaderboardSubmissionOverride(false));
    }
}
