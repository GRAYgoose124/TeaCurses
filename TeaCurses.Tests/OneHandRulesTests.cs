using TeaCurses;
using Xunit;

namespace TeaCurses.Tests;

public class OneHandRulesTests
{
    [Fact]
    public void RequiredSide_0_is_Primary()
        => Assert.Equal(BindSide.Primary, OneHandRules.RequiredSide(0f));

    [Fact]
    public void RequiredSide_1_is_Alternate()
        => Assert.Equal(BindSide.Alternate, OneHandRules.RequiredSide(1f));

    [Fact]
    public void RequiredSide_unknown_clamps_to_Primary()
        => Assert.Equal(BindSide.Primary, OneHandRules.RequiredSide(0.5f));

    [Fact]
    public void ShouldSwallow_wrong_side()
        => Assert.True(OneHandRules.ShouldSwallow(BindSide.Alternate, BindSide.Primary));

    [Fact]
    public void ShouldSwallow_matching_side_false()
        => Assert.False(OneHandRules.ShouldSwallow(BindSide.Primary, BindSide.Primary));

    [Fact]
    public void ShouldSwallow_None_false()
        => Assert.False(OneHandRules.ShouldSwallow(BindSide.None, BindSide.Primary));
}
