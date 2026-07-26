using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class ArmoredRulesTests
{
    [Fact]
    public void Armors_non_item_with_exactly_one_hp()
    {
        Assert.True(ArmoredRules.ShouldArmor(isHealthItem: false, currentHp: 1));
    }

    [Fact]
    public void Skips_health_items()
    {
        Assert.False(ArmoredRules.ShouldArmor(isHealthItem: true, currentHp: 1));
    }

    [Fact]
    public void Skips_multi_hp_and_non_positive()
    {
        Assert.False(ArmoredRules.ShouldArmor(false, 2));
        Assert.False(ArmoredRules.ShouldArmor(false, 0));
        Assert.False(ArmoredRules.ShouldArmor(false, -1));
    }
}
