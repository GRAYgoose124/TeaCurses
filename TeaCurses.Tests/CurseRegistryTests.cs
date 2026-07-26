using System;
using TeaCurses.Curse;
using Xunit;

namespace TeaCurses.Tests;

public class CurseRegistryTests : IDisposable
{
    public CurseRegistryTests()
    {
        CurseRegistry.Clear();
    }

    public void Dispose()
    {
        CurseRegistry.Clear();
    }

    [Fact]
    public void Toggle_is_independent_per_curse()
    {
        CurseRegistry.Register(new CurseDefinition("A", "Alpha"));
        CurseRegistry.Register(new CurseDefinition("B", "Beta"));

        CurseRegistry.Toggle("A");

        Assert.True(CurseRegistry.IsEnabled("A"));
        Assert.False(CurseRegistry.IsEnabled("B"));
    }

    [Fact]
    public void Unknown_id_IsEnabled_is_false()
    {
        Assert.False(CurseRegistry.IsEnabled("missing"));
    }

    [Fact]
    public void TryStepIntensity_false_when_no_intensity()
    {
        CurseRegistry.Register(new CurseDefinition("plain", "Plain"));
        Assert.False(CurseRegistry.TryStepIntensity("plain", 1));
    }

    [Fact]
    public void TryStepIntensity_wraps_past_max_to_min()
    {
        CurseRegistry.Register(new CurseDefinition(
            "scaled",
            "Scaled",
            new CurseIntensity(1f, 2f, 0.5f, 1.5f)));

        Assert.True(CurseRegistry.TryStepIntensity("scaled", 1));
        Assert.True(CurseRegistry.TryGetIntensity("scaled", out var mid));
        Assert.Equal(2f, mid);

        Assert.True(CurseRegistry.TryStepIntensity("scaled", 1));
        Assert.True(CurseRegistry.TryGetIntensity("scaled", out var wrapped));
        Assert.Equal(1f, wrapped);
    }

    [Fact]
    public void TryStepIntensity_wraps_past_min_to_max()
    {
        CurseRegistry.Register(new CurseDefinition(
            "scaled",
            "Scaled",
            new CurseIntensity(1f, 2f, 0.5f, 1.5f)));

        Assert.True(CurseRegistry.TryStepIntensity("scaled", -1));
        Assert.True(CurseRegistry.TryGetIntensity("scaled", out var mid));
        Assert.Equal(1f, mid);

        Assert.True(CurseRegistry.TryStepIntensity("scaled", -1));
        Assert.True(CurseRegistry.TryGetIntensity("scaled", out var wrapped));
        Assert.Equal(2f, wrapped);
    }

    [Fact]
    public void GetMeterRating_uses_ToMeterRating()
    {
        CurseRegistry.Register(new CurseDefinition(
            "scaled",
            "Scaled",
            new CurseIntensity(0f, 10f, 1f, 5f, v => v * 2f)));

        Assert.Equal(10f, CurseRegistry.GetMeterRating("scaled"));
    }

    [Fact]
    public void GetMeterRating_default_linear_map_to_0_20()
    {
        CurseRegistry.Register(new CurseDefinition(
            "scaled",
            "Scaled",
            new CurseIntensity(0f, 10f, 1f, 5f)));

        Assert.Equal(10f, CurseRegistry.GetMeterRating("scaled"));
    }

    [Fact]
    public void AnyEnabledBlocksLeaderboard_false_when_none_enabled()
    {
        CurseRegistry.Register(new CurseDefinition(
            "edge", "Edge", blocksLeaderboard: true));
        Assert.False(CurseRegistry.AnyEnabledBlocksLeaderboard());
    }

    [Fact]
    public void AnyEnabledBlocksLeaderboard_true_when_blocking_curse_on()
    {
        CurseRegistry.Register(new CurseDefinition(
            "edge", "Edge", blocksLeaderboard: true));
        CurseRegistry.Register(new CurseDefinition("plain", "Plain"));
        CurseRegistry.SetEnabled("edge", true);
        Assert.True(CurseRegistry.AnyEnabledBlocksLeaderboard());
    }

    [Fact]
    public void AnyEnabledBlocksLeaderboard_ignores_enabled_non_blocking()
    {
        CurseRegistry.Register(new CurseDefinition("plain", "Plain"));
        CurseRegistry.SetEnabled("plain", true);
        Assert.False(CurseRegistry.AnyEnabledBlocksLeaderboard());
    }
}
