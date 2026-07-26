using System;
using TeaCurses.Curse;
using Xunit;

namespace TeaCurses.Tests;

public class CurseHandExclusivityTests : IDisposable
{
    private const string OneHandId = "OneHand";
    private const string AlternatingId = "AlternatingHands";

    public CurseHandExclusivityTests()
    {
        CurseRegistry.Clear();
        CurseRegistry.Register(new CurseDefinition(OneHandId, "One Hand"));
        CurseRegistry.Register(new CurseDefinition(AlternatingId, "Alternating Hands"));
    }

    public void Dispose()
    {
        CurseRegistry.Clear();
    }

    [Fact]
    public void Enabling_OneHand_disables_AlternatingHands()
    {
        CurseRegistry.SetEnabled(AlternatingId, true);
        CurseRegistry.SetEnabled(OneHandId, true);

        CurseHandExclusivity.AfterEnabled(OneHandId, OneHandId, AlternatingId);

        Assert.True(CurseRegistry.IsEnabled(OneHandId));
        Assert.False(CurseRegistry.IsEnabled(AlternatingId));
    }

    [Fact]
    public void Enabling_AlternatingHands_disables_OneHand()
    {
        CurseRegistry.SetEnabled(OneHandId, true);
        CurseRegistry.SetEnabled(AlternatingId, true);

        CurseHandExclusivity.AfterEnabled(AlternatingId, OneHandId, AlternatingId);

        Assert.True(CurseRegistry.IsEnabled(AlternatingId));
        Assert.False(CurseRegistry.IsEnabled(OneHandId));
    }

    [Fact]
    public void Disabling_does_not_change_sibling()
    {
        CurseRegistry.SetEnabled(OneHandId, false);
        CurseRegistry.SetEnabled(AlternatingId, true);

        CurseHandExclusivity.AfterEnabled(OneHandId, OneHandId, AlternatingId);

        Assert.False(CurseRegistry.IsEnabled(OneHandId));
        Assert.True(CurseRegistry.IsEnabled(AlternatingId));
    }
}
