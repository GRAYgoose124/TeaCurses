using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class ArmoredFlashTests
{
    [Fact]
    public void Strength_is_one_at_start()
    {
        Assert.Equal(1f, ArmoredFlash.Strength(0f));
        Assert.Equal(1f, ArmoredFlash.Strength(-0.05f));
    }

    [Fact]
    public void Strength_is_half_at_midpoint()
    {
        Assert.Equal(0.5f, ArmoredFlash.Strength(ArmoredFlash.LifetimeSeconds * 0.5f), 3);
    }

    [Fact]
    public void Strength_is_zero_at_and_after_lifetime()
    {
        Assert.Equal(0f, ArmoredFlash.Strength(ArmoredFlash.LifetimeSeconds));
        Assert.Equal(0f, ArmoredFlash.Strength(ArmoredFlash.LifetimeSeconds + 1f));
    }
}
