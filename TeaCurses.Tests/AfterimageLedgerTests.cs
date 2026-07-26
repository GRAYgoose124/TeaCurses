using System.Collections.Generic;
using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class AfterimageLedgerTests
{
    [Fact]
    public void Foreign_owner_beats_do_not_age_or_cull_another_trail()
    {
        var ledger = new AfterimageLedger();
        ledger.Spawn("a", intensity: 5);
        ledger.Spawn("a", intensity: 5);

        ledger.AdvanceOwner("b", intensity: 5);
        ledger.AdvanceOwner("b", intensity: 5);
        ledger.AdvanceOwner("b", intensity: 5);

        Assert.Equal(new[] { 0, 0 }, ledger.Ages("a"));
        Assert.Empty(ledger.Ages("b"));
    }

    [Fact]
    public void Owner_beats_age_that_trail_and_apply_fade_cap_per_owner()
    {
        var ledger = new AfterimageLedger();
        ledger.Spawn("a", intensity: 3); // max 2, life 3
        ledger.Spawn("a", intensity: 3);
        ledger.Spawn("a", intensity: 3); // excess drops oldest

        Assert.Equal(2, ledger.Ages("a").Count);

        ledger.AdvanceOwner("a", intensity: 3);
        Assert.All(ledger.Ages("a"), age => Assert.Equal(1, age));
    }

    [Fact]
    public void Two_owners_each_keep_their_own_max_ghosts()
    {
        var ledger = new AfterimageLedger();
        for (var i = 0; i < 3; i++)
        {
            ledger.Spawn("a", intensity: 3);
            ledger.Spawn("b", intensity: 3);
        }

        Assert.Equal(2, ledger.Ages("a").Count);
        Assert.Equal(2, ledger.Ages("b").Count);
        Assert.Equal(4, ledger.TotalCount);
    }
}
