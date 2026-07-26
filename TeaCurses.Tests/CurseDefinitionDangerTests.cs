using System;
using TeaCurses.Curse;
using Xunit;

namespace TeaCurses.Tests;

public class CurseDefinitionDangerTests
{
    [Fact]
    public void DangerWhenOff_defaults_false()
    {
        var def = new CurseDefinition("x", "X");
        Assert.False(def.DangerWhenOff);
    }

    [Fact]
    public void DangerWhenOff_can_be_set()
    {
        var def = new CurseDefinition("Cryptid", "Cryptid", intensity: null, dangerWhenOff: true);
        Assert.True(def.DangerWhenOff);
    }

    [Fact]
    public void WarnYellowWhenOff_defaults_false()
    {
        var def = new CurseDefinition("x", "X");
        Assert.False(def.WarnYellowWhenOff);
    }

    [Fact]
    public void BlocksLeaderboard_defaults_true()
    {
        var def = new CurseDefinition("x", "X");
        Assert.True(def.BlocksLeaderboard);
    }

    [Fact]
    public void WarnYellowWhenOff_and_BlocksLeaderboard_can_be_set()
    {
        var def = new CurseDefinition(
            "edge",
            "Edge Rocker",
            warnYellowWhenOff: true,
            blocksLeaderboard: false);
        Assert.True(def.WarnYellowWhenOff);
        Assert.False(def.BlocksLeaderboard);
        Assert.False(def.DangerWhenOff);
    }
}
