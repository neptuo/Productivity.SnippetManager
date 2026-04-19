using Avalonia;

namespace Neptuo.Productivity.SnippetManager.Tests;

public class SelectedTextBoundsPlausibilityTests
{
    private static readonly PixelRect MainDisplay = new(0, 0, 2560, 1440);
    private static readonly PixelRect SecondaryLeftDisplay = new(-1920, 0, 1920, 1080);
    private static readonly PixelRect SecondaryAboveDisplay = new(0, -1080, 1920, 1080);

    [Fact]
    public void NormalCaretBounds_Accepted()
    {
        // A caret at a reasonable position with normal line height.
        var bounds = new PixelRect(414, 896, 2, 17);

        Assert.True(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, MainDisplay));
    }

    [Fact]
    public void ZeroWidthSelectionWithHeight_Accepted()
    {
        // A zero-width selection still has the line height.
        var bounds = new PixelRect(300, 200, 1, 16);

        Assert.True(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, MainDisplay));
    }

    [Fact]
    public void WideSelection_Accepted()
    {
        // A multi-character text selection.
        var bounds = new PixelRect(100, 300, 200, 18);

        Assert.True(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, MainDisplay));
    }

    [Fact]
    public void SmallBoundsAtValidPosition_Accepted()
    {
        // 1×1 rect at a normal position (not at screen edge) is accepted.
        var bounds = new PixelRect(500, 400, 1, 1);

        Assert.True(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, MainDisplay));
    }

    [Fact]
    public void DegenerateBoundsAtLeftEdge_Rejected()
    {
        // 1×1 rect at X=0 — screen left edge.
        var bounds = new PixelRect(0, 500, 1, 1);

        Assert.False(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, MainDisplay));
    }

    [Fact]
    public void DegenerateBoundsAtTopEdge_Rejected()
    {
        // 1×1 rect at Y=0 — screen top edge.
        var bounds = new PixelRect(500, 0, 1, 1);

        Assert.False(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, MainDisplay));
    }

    [Fact]
    public void DegenerateBoundsAtOrigin_Rejected()
    {
        // 1×1 rect at (0, 0) — screen corner.
        var bounds = new PixelRect(0, 0, 1, 1);

        Assert.False(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, MainDisplay));
    }

    [Fact]
    public void NormalBoundsAtLeftEdge_Accepted()
    {
        // A normally-sized rect at X=0 is fine — only degenerate sizes are suspicious.
        var bounds = new PixelRect(0, 500, 2, 17);

        Assert.True(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, MainDisplay));
    }

    [Fact]
    public void DegenerateBoundsAtRightEdge_Rejected()
    {
        // 1×1 rect at the right edge of the display.
        var bounds = new PixelRect(2559, 500, 1, 1);

        Assert.False(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, MainDisplay));
    }

    [Fact]
    public void DegenerateBoundsAtBottomEdge_Rejected()
    {
        // 1×1 rect at the bottom edge of the display.
        var bounds = new PixelRect(500, 1439, 1, 1);

        Assert.False(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, MainDisplay));
    }

    [Fact]
    public void SecondaryDisplay_NegativeCoordinates_ValidPosition_Accepted()
    {
        // A caret on a secondary display to the left (negative X coordinates).
        var bounds = new PixelRect(-960, 540, 1, 1);

        Assert.True(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, SecondaryLeftDisplay));
    }

    [Fact]
    public void SecondaryDisplay_DegenerateAtLeftEdge_Rejected()
    {
        // 1×1 rect at the left edge of a secondary display with negative coordinates.
        var bounds = new PixelRect(-1920, 500, 1, 1);

        Assert.False(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, SecondaryLeftDisplay));
    }

    [Fact]
    public void SecondaryDisplay_DegenerateAtRightEdge_Rejected()
    {
        // 1×1 rect at the right edge of the secondary left display (X=0 is the right edge).
        var bounds = new PixelRect(-1, 500, 1, 1);

        Assert.False(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, SecondaryLeftDisplay));
    }

    [Fact]
    public void SecondaryAboveDisplay_DegenerateAtTopEdge_Rejected()
    {
        // 1×1 rect at the top edge of a display above the main one (negative Y).
        var bounds = new PixelRect(500, -1080, 1, 1);

        Assert.False(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, SecondaryAboveDisplay));
    }

    [Fact]
    public void SecondaryAboveDisplay_ValidPosition_Accepted()
    {
        // A caret at a valid position on a display above the main one.
        var bounds = new PixelRect(500, -540, 1, 1);

        Assert.True(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, SecondaryAboveDisplay));
    }

    [Fact]
    public void NullScreenBounds_DegenerateSize_Accepted()
    {
        // When screen bounds are unavailable, a 1×1 rect is accepted
        // since we can't verify screen edges.
        var bounds = new PixelRect(0, 0, 1, 1);

        Assert.True(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, null));
    }

    [Fact]
    public void NullScreenBounds_NormalSize_Accepted()
    {
        // Normal-sized bounds are always accepted regardless of screen info.
        var bounds = new PixelRect(414, 896, 2, 17);

        Assert.True(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds, null));
    }
}
