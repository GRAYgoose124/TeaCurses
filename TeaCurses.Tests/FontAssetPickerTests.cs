using TeaCurses.UI;
using Xunit;

namespace TeaCurses.Tests;

public class FontAssetPickerTests
{
    [Fact]
    public void Rank_prefers_score_and_combo_names()
    {
        Assert.True(FontAssetPicker.Rank("ScoreSDF") > FontAssetPicker.Rank("UIBody"));
        Assert.True(FontAssetPicker.Rank("ComboText") > FontAssetPicker.Rank("UIBody"));
    }

    [Fact]
    public void Rank_penalizes_generic_defaults()
    {
        Assert.True(FontAssetPicker.Rank("UIBody") > FontAssetPicker.Rank("LiberationSans SDF"));
        Assert.True(FontAssetPicker.Rank("UIBody") > FontAssetPicker.Rank("Arial"));
    }

    [Fact]
    public void Rank_null_or_empty_is_zero()
    {
        Assert.Equal(0, FontAssetPicker.Rank(null));
        Assert.Equal(0, FontAssetPicker.Rank(""));
    }

    [Fact]
    public void TryPickBestIndex_selects_highest_rank()
    {
        var names = new[] { "LiberationSans SDF", "MenuBody", "Score Number SDF" };
        Assert.Equal(2, FontAssetPicker.TryPickBestIndex(names));
    }

    [Fact]
    public void TryPickBestIndex_empty_returns_minus_one()
    {
        Assert.Equal(-1, FontAssetPicker.TryPickBestIndex(System.Array.Empty<string>()));
    }
}
