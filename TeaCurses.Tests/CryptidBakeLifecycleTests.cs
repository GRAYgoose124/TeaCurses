using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class CryptidBakeLifecycleTests
{
    [Fact]
    public void NeedsBake_true_when_session_not_baked()
    {
        Assert.True(CryptidBakeLifecycle.NeedsBake(
            sessionBaked: false,
            sessionMode: CryptidGlyphMode.Mix,
            requestedMode: CryptidGlyphMode.Mix));
    }

    [Fact]
    public void NeedsBake_false_when_same_mode_already_baked()
    {
        Assert.False(CryptidBakeLifecycle.NeedsBake(
            sessionBaked: true,
            sessionMode: CryptidGlyphMode.Mix,
            requestedMode: CryptidGlyphMode.Mix));
    }

    [Theory]
    [InlineData(CryptidGlyphMode.Mix, CryptidGlyphMode.ProceduralOnly)]
    [InlineData(CryptidGlyphMode.UnicodeOnly, CryptidGlyphMode.Mix)]
    [InlineData(CryptidGlyphMode.ProceduralOnly, CryptidGlyphMode.UnicodeOnly)]
    public void NeedsBake_true_when_mode_changed(
        CryptidGlyphMode sessionMode,
        CryptidGlyphMode requestedMode)
    {
        Assert.True(CryptidBakeLifecycle.NeedsBake(
            sessionBaked: true,
            sessionMode,
            requestedMode));
    }

    [Fact]
    public void NeedsBake_false_across_repeated_chart_starts_same_mode()
    {
        // Chart load must not re-trigger bake when the session pool is warm.
        Assert.False(CryptidBakeLifecycle.NeedsBake(true, CryptidGlyphMode.Mix, CryptidGlyphMode.Mix));
        Assert.False(CryptidBakeLifecycle.NeedsBake(true, CryptidGlyphMode.Mix, CryptidGlyphMode.Mix));
        Assert.False(CryptidBakeLifecycle.NeedsBake(true, CryptidGlyphMode.Mix, CryptidGlyphMode.Mix));
    }
}
