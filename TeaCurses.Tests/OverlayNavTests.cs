using TeaCurses.UI;
using Xunit;

namespace TeaCurses.Tests;

public class OverlayNavTests
{
    [Fact]
    public void Move_down_clamps_at_last_item()
    {
        var state = new OverlayNavState(highlightIndex: 2, windowStart: 0, visibleCount: 3);
        var next = OverlayNav.Move(state, itemCount: 3, delta: 1);
        Assert.Equal(2, next.HighlightIndex);
    }

    [Fact]
    public void Move_up_clamps_at_first_item()
    {
        var state = new OverlayNavState(highlightIndex: 0, windowStart: 0, visibleCount: 3);
        var next = OverlayNav.Move(state, itemCount: 5, delta: -1);
        Assert.Equal(0, next.HighlightIndex);
    }

    [Fact]
    public void Move_down_scrolls_window_to_keep_highlight_visible()
    {
        var state = new OverlayNavState(highlightIndex: 2, windowStart: 0, visibleCount: 3);
        var next = OverlayNav.Move(state, itemCount: 10, delta: 1);
        Assert.Equal(3, next.HighlightIndex);
        Assert.Equal(1, next.WindowStart);
        Assert.True(next.HighlightIndex >= next.WindowStart);
        Assert.True(next.HighlightIndex < next.WindowStart + next.VisibleCount);
    }

    [Fact]
    public void Move_up_scrolls_window_backward()
    {
        var state = new OverlayNavState(highlightIndex: 3, windowStart: 3, visibleCount: 3);
        var next = OverlayNav.Move(state, itemCount: 10, delta: -1);
        Assert.Equal(2, next.HighlightIndex);
        Assert.Equal(2, next.WindowStart);
    }

    [Fact]
    public void Move_with_zero_items_stays_at_zero()
    {
        var state = new OverlayNavState(0, 0, 5);
        var next = OverlayNav.Move(state, itemCount: 0, delta: 1);
        Assert.Equal(0, next.HighlightIndex);
        Assert.Equal(0, next.WindowStart);
    }

    [Fact]
    public void Move_down_with_wrap_from_last_goes_to_first()
    {
        var state = new OverlayNavState(highlightIndex: 9, windowStart: 7, visibleCount: 3);
        var next = OverlayNav.Move(state, itemCount: 10, delta: 1, wrap: true);
        Assert.Equal(0, next.HighlightIndex);
        Assert.Equal(0, next.WindowStart);
    }

    [Fact]
    public void Move_up_with_wrap_from_first_goes_to_last()
    {
        var state = new OverlayNavState(highlightIndex: 0, windowStart: 0, visibleCount: 3);
        var next = OverlayNav.Move(state, itemCount: 10, delta: -1, wrap: true);
        Assert.Equal(9, next.HighlightIndex);
        Assert.Equal(7, next.WindowStart);
    }

    [Fact]
    public void Page_down_moves_by_visible_count_and_clamps()
    {
        var state = new OverlayNavState(highlightIndex: 2, windowStart: 0, visibleCount: 3);
        var next = OverlayNav.Page(state, itemCount: 10, direction: 1);
        Assert.Equal(5, next.HighlightIndex);
        Assert.True(next.HighlightIndex >= next.WindowStart);
        Assert.True(next.HighlightIndex < next.WindowStart + next.VisibleCount);

        var clamped = OverlayNav.Page(next, itemCount: 10, direction: 1);
        Assert.Equal(8, clamped.HighlightIndex);
        var atEnd = OverlayNav.Page(clamped, itemCount: 10, direction: 1);
        Assert.Equal(9, atEnd.HighlightIndex);
    }

    [Fact]
    public void Page_up_clamps_at_first()
    {
        var state = new OverlayNavState(highlightIndex: 2, windowStart: 0, visibleCount: 3);
        var next = OverlayNav.Page(state, itemCount: 10, direction: -1);
        Assert.Equal(0, next.HighlightIndex);
        Assert.Equal(0, next.WindowStart);
    }

    [Fact]
    public void JumpTo_home_and_end()
    {
        var state = new OverlayNavState(highlightIndex: 5, windowStart: 3, visibleCount: 3);
        var home = OverlayNav.JumpTo(state, itemCount: 10, index: 0);
        Assert.Equal(0, home.HighlightIndex);
        Assert.Equal(0, home.WindowStart);

        var end = OverlayNav.JumpTo(state, itemCount: 10, index: 9);
        Assert.Equal(9, end.HighlightIndex);
        Assert.Equal(7, end.WindowStart);
    }

    [Fact]
    public void Wrap_page_jump_with_zero_items_stay_at_zero()
    {
        var state = new OverlayNavState(0, 0, 5);
        Assert.Equal(0, OverlayNav.Move(state, 0, 1, wrap: true).HighlightIndex);
        Assert.Equal(0, OverlayNav.Page(state, 0, 1).HighlightIndex);
        Assert.Equal(0, OverlayNav.JumpTo(state, 0, 3).HighlightIndex);
    }
}
