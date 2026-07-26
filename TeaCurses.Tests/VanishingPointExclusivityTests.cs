using System;
using TeaCurses.Curse;
using Xunit;

namespace TeaCurses.Tests;

public class VanishingPointExclusivityTests : IDisposable
{
    private const string AfterimageId = "Afterimage";
    private const string VanishingPointId = "VanishingPoint";
    private const string BlinkId = "Blink";

    public VanishingPointExclusivityTests()
    {
        CurseRegistry.Clear();
        CurseRegistry.Register(new CurseDefinition(AfterimageId, "Afterimage"));
        CurseRegistry.Register(new CurseDefinition(VanishingPointId, "Vanishing Point"));
        CurseRegistry.Register(new CurseDefinition(BlinkId, "Blink"));
    }

    public void Dispose()
    {
        CurseRegistry.Clear();
    }

    [Fact]
    public void Enabling_VanishingPoint_disables_Afterimage()
    {
        CurseRegistry.SetEnabled(AfterimageId, true);
        CurseRegistry.SetEnabled(VanishingPointId, true);

        CurseHandExclusivity.AfterEnabled(VanishingPointId, AfterimageId, VanishingPointId);

        Assert.True(CurseRegistry.IsEnabled(VanishingPointId));
        Assert.False(CurseRegistry.IsEnabled(AfterimageId));
    }

    [Fact]
    public void Enabling_Afterimage_disables_VanishingPoint()
    {
        CurseRegistry.SetEnabled(VanishingPointId, true);
        CurseRegistry.SetEnabled(AfterimageId, true);

        CurseHandExclusivity.AfterEnabled(AfterimageId, AfterimageId, VanishingPointId);

        Assert.True(CurseRegistry.IsEnabled(AfterimageId));
        Assert.False(CurseRegistry.IsEnabled(VanishingPointId));
    }

    [Fact]
    public void Enabling_VanishingPoint_does_not_disable_Blink()
    {
        CurseRegistry.SetEnabled(BlinkId, true);
        CurseRegistry.SetEnabled(VanishingPointId, true);

        CurseHandExclusivity.AfterEnabled(VanishingPointId, AfterimageId, VanishingPointId);

        Assert.True(CurseRegistry.IsEnabled(VanishingPointId));
        Assert.True(CurseRegistry.IsEnabled(BlinkId));
    }
}
