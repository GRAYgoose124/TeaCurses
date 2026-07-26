using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class CurseRowColorTests
{
    [Fact]
    public void Off_danger_is_red()
    {
        CurseRowColor.Rgba(on: false, dangerWhenOff: true, warnYellowWhenOff: false,
            out var r, out var g, out var b, out _);
        Assert.True(r > 0.9f);
        Assert.True(g < 0.4f);
        Assert.True(b < 0.4f);
    }

    [Fact]
    public void Off_yellow_warn_is_yellow()
    {
        CurseRowColor.Rgba(on: false, dangerWhenOff: false, warnYellowWhenOff: true,
            out var r, out var g, out var b, out var a);
        Assert.Equal(1f, r);
        Assert.Equal(0.85f, g);
        Assert.Equal(0.2f, b);
        Assert.Equal(1f, a);
    }

    [Fact]
    public void Off_danger_wins_over_yellow()
    {
        CurseRowColor.Rgba(on: false, dangerWhenOff: true, warnYellowWhenOff: true,
            out var r, out var g, out var b, out _);
        Assert.True(r > 0.9f);
        Assert.True(g < 0.4f);
        Assert.True(b < 0.4f);
    }

    [Fact]
    public void Off_normal_is_beige()
    {
        CurseRowColor.Rgba(on: false, dangerWhenOff: false, warnYellowWhenOff: false,
            out var r, out var g, out var b, out var a);
        Assert.Equal(0.88f, r);
        Assert.Equal(0.86f, g);
        Assert.Equal(0.82f, b);
        Assert.Equal(1f, a);
    }

    [Fact]
    public void On_is_green_ignoring_warn_flags()
    {
        CurseRowColor.Rgba(on: true, dangerWhenOff: true, warnYellowWhenOff: true,
            out var r, out var g, out var b, out var a);
        Assert.Equal(0.55f, r);
        Assert.Equal(1f, g);
        Assert.Equal(0.7f, b);
        Assert.Equal(1f, a);
    }
}
