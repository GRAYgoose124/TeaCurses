using TeaCurses.UI;
using Xunit;

namespace TeaCurses.Tests;

public class HexPlateLayoutTests
{
    [Fact]
    public void TipFraction_for_30deg_on_wide_row_is_shorter_than_legacy_014()
    {
        // Wide menu row ~700x36 → tipFraction must be well under the old 0.14 bake.
        var tip = HexPlateLayout.TipFractionForSlopeDegrees(30f, widthOverHeight: 700f / 36f);
        Assert.True(tip < 0.08f);
        Assert.True(tip > 0.03f);
    }

    [Fact]
    public void PlateWidth_includes_tips_outside_text()
    {
        // text 100, tip 0.06, pad 8 each side → width = (100+16)/(1-0.12)
        var width = HexPlateLayout.PlateWidthForText(textWidth: 100f, tipFraction: 0.06f, padEachSide: 8f);
        Assert.Equal(131.818f, width, 2);
    }

    [Fact]
    public void TextInset_clears_tip_plus_pad()
    {
        var inset = HexPlateLayout.TextInset(plateWidth: 200f, tipFraction: 0.06f, pad: 8f);
        Assert.Equal(20f, inset, 3);
    }
}
