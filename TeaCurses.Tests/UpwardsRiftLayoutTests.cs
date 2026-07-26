using TeaCurses.Curses;
using Xunit;

namespace TeaCurses.Tests;

public class UpwardsRiftLayoutTests
{
    [Fact]
    public void Home_row_maps_to_top_z()
    {
        Assert.Equal(7f, UpwardsRiftLayout.InvertedLocalZ(0, 8, 0, actionRowLift: 0));
    }

    [Fact]
    public void Back_row_maps_to_bottom_z()
    {
        Assert.Equal(0f, UpwardsRiftLayout.InvertedLocalZ(7, 8, 0, actionRowLift: 0));
    }

    [Fact]
    public void Extra_rows_stay_on_spawn_side_below_back_row()
    {
        Assert.Equal(7f, UpwardsRiftLayout.InvertedLocalZ(0, 8, 2, actionRowLift: 0));
        Assert.Equal(0f, UpwardsRiftLayout.InvertedLocalZ(7, 8, 2, actionRowLift: 0));
        Assert.Equal(-1f, UpwardsRiftLayout.InvertedLocalZ(-1, 8, 2, actionRowLift: 0));
        Assert.Equal(-2f, UpwardsRiftLayout.InvertedLocalZ(-2, 8, 2, actionRowLift: 0));
    }

    [Fact]
    public void Lift_raises_home_above_max_row()
    {
        Assert.Equal(8f, UpwardsRiftLayout.InvertedLocalZ(0, 8, 0, actionRowLift: 1));
        Assert.Equal(1f, UpwardsRiftLayout.InvertedLocalZ(7, 8, 0, actionRowLift: 1));
    }

    [Fact]
    public void Default_lift_is_zero()
    {
        Assert.Equal(0, UpwardsRiftLayout.DefaultActionRowLift);
        Assert.Equal(7f, UpwardsRiftLayout.InvertedLocalZ(0, 8, 0));
    }
}
