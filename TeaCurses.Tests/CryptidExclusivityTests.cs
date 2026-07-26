using System;
using TeaCurses.Curse;
using Xunit;

namespace TeaCurses.Tests;

public class CryptidExclusivityTests : IDisposable
{
    private const string AfterimageId = "Afterimage";
    private const string VanishingPointId = "VanishingPoint";
    private const string CryptidId = "Cryptid";
    private const string BlinkId = "Blink";

    public CryptidExclusivityTests()
    {
        CurseRegistry.Clear();
        CurseRegistry.Register(new CurseDefinition(AfterimageId, "Afterimage"));
        CurseRegistry.Register(new CurseDefinition(VanishingPointId, "Vanishing Point"));
        CurseRegistry.Register(new CurseDefinition(CryptidId, "Cryptid",
            new CurseIntensity(1f, 3f, 1f, 3f), dangerWhenOff: true));
        CurseRegistry.Register(new CurseDefinition(BlinkId, "Blink"));
    }

    public void Dispose()
    {
        CurseRegistry.Clear();
    }

    private static void ApplyVisualExclusivity(string enabledId)
    {
        CurseHandExclusivity.AfterEnabled(enabledId, AfterimageId, VanishingPointId);
        CurseHandExclusivity.AfterEnabled(enabledId, AfterimageId, CryptidId);
        CurseHandExclusivity.AfterEnabled(enabledId, VanishingPointId, CryptidId);
    }

    [Fact]
    public void Enabling_Cryptid_disables_Afterimage_and_VanishingPoint()
    {
        CurseRegistry.SetEnabled(AfterimageId, true);
        CurseRegistry.SetEnabled(VanishingPointId, true);
        CurseRegistry.SetEnabled(CryptidId, true);

        ApplyVisualExclusivity(CryptidId);

        Assert.True(CurseRegistry.IsEnabled(CryptidId));
        Assert.False(CurseRegistry.IsEnabled(AfterimageId));
        Assert.False(CurseRegistry.IsEnabled(VanishingPointId));
    }

    [Fact]
    public void Enabling_Afterimage_disables_Cryptid()
    {
        CurseRegistry.SetEnabled(CryptidId, true);
        CurseRegistry.SetEnabled(AfterimageId, true);

        ApplyVisualExclusivity(AfterimageId);

        Assert.True(CurseRegistry.IsEnabled(AfterimageId));
        Assert.False(CurseRegistry.IsEnabled(CryptidId));
    }

    [Fact]
    public void Enabling_Cryptid_does_not_disable_Blink()
    {
        CurseRegistry.SetEnabled(BlinkId, true);
        CurseRegistry.SetEnabled(CryptidId, true);

        ApplyVisualExclusivity(CryptidId);

        Assert.True(CurseRegistry.IsEnabled(CryptidId));
        Assert.True(CurseRegistry.IsEnabled(BlinkId));
    }
}
