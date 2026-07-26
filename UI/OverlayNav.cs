namespace TeaCurses.UI;

/// <summary>
/// Highlight / window math for the curse list.
/// Single-step Move clamps by default; pass wrap:true to wrap at ends.
/// </summary>
public readonly struct OverlayNavState
{
    public int HighlightIndex { get; }
    public int WindowStart { get; }
    public int VisibleCount { get; }

    public OverlayNavState(int highlightIndex, int windowStart, int visibleCount)
    {
        HighlightIndex = highlightIndex;
        WindowStart = windowStart;
        VisibleCount = visibleCount < 1 ? 1 : visibleCount;
    }
}

public static class OverlayNav
{
    public static OverlayNavState Move(OverlayNavState state, int itemCount, int delta, bool wrap = false)
    {
        if (itemCount <= 0)
            return new OverlayNavState(0, 0, state.VisibleCount);

        var highlight = state.HighlightIndex + delta;
        if (wrap)
        {
            highlight %= itemCount;
            if (highlight < 0)
                highlight += itemCount;
        }
        else
        {
            if (highlight < 0)
                highlight = 0;
            if (highlight > itemCount - 1)
                highlight = itemCount - 1;
        }

        return EnsureVisible(new OverlayNavState(highlight, state.WindowStart, state.VisibleCount), itemCount);
    }

    public static OverlayNavState Page(OverlayNavState state, int itemCount, int direction)
    {
        if (direction == 0)
            return EnsureVisible(state, itemCount);

        var step = direction > 0 ? 1 : -1;
        return Move(state, itemCount, step * state.VisibleCount, wrap: false);
    }

    public static OverlayNavState JumpTo(OverlayNavState state, int itemCount, int index)
    {
        if (itemCount <= 0)
            return new OverlayNavState(0, 0, state.VisibleCount);

        return EnsureVisible(new OverlayNavState(index, state.WindowStart, state.VisibleCount), itemCount);
    }

    public static OverlayNavState EnsureVisible(OverlayNavState state, int itemCount)
    {
        if (itemCount <= 0)
            return new OverlayNavState(0, 0, state.VisibleCount);

        var visible = state.VisibleCount;
        if (visible > itemCount)
            visible = itemCount;

        var highlight = state.HighlightIndex;
        if (highlight < 0)
            highlight = 0;
        if (highlight > itemCount - 1)
            highlight = itemCount - 1;

        var windowStart = state.WindowStart;
        if (windowStart < 0)
            windowStart = 0;

        var maxStart = itemCount - visible;
        if (maxStart < 0)
            maxStart = 0;
        if (windowStart > maxStart)
            windowStart = maxStart;

        if (highlight < windowStart)
            windowStart = highlight;
        else if (highlight >= windowStart + visible)
            windowStart = highlight - visible + 1;

        if (windowStart < 0)
            windowStart = 0;
        if (windowStart > maxStart)
            windowStart = maxStart;

        return new OverlayNavState(highlight, windowStart, visible);
    }
}
