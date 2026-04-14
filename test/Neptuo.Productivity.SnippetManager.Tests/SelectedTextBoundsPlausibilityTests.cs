using Avalonia;

namespace Neptuo.Productivity.SnippetManager.Tests;

public class SelectedTextBoundsPlausibilityTests
{
    [Fact]
    public void NormalCaretBounds_Accepted()
    {
        // A caret at a reasonable position with normal line height.
        var bounds = new PixelRect(414, 896, 2, 17);

        Assert.True(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds));
    }

    [Fact]
    public void ZeroWidthSelectionWithHeight_Accepted()
    {
        // A zero-width selection still has the line height.
        var bounds = new PixelRect(300, 200, 1, 16);

        Assert.True(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds));
    }

    [Fact]
    public void WideSelection_Accepted()
    {
        // A multi-character text selection.
        var bounds = new PixelRect(100, 300, 200, 18);

        Assert.True(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds));
    }

    [Fact]
    public void SmallBoundsAtValidPosition_Accepted()
    {
        // 1×1 rect at a normal position (not at screen edge) is accepted.
        var bounds = new PixelRect(500, 400, 1, 1);

        Assert.True(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds));
    }

    [Fact]
    public void DegenerateBoundsAtLeftEdge_Rejected()
    {
        // 1×1 rect at X=0 — screen left edge.
        var bounds = new PixelRect(0, 500, 1, 1);

        Assert.False(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds));
    }

    [Fact]
    public void DegenerateBoundsAtTopEdge_Rejected()
    {
        // 1×1 rect at Y=0 — screen top edge.
        var bounds = new PixelRect(500, 0, 1, 1);

        Assert.False(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds));
    }

    [Fact]
    public void DegenerateBoundsAtOrigin_Rejected()
    {
        // 1×1 rect at (0, 0) — screen corner.
        var bounds = new PixelRect(0, 0, 1, 1);

        Assert.False(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds));
    }

    [Fact]
    public void NormalBoundsAtLeftEdge_Accepted()
    {
        // A normally-sized rect at X=0 is fine — only degenerate sizes are suspicious.
        var bounds = new PixelRect(0, 500, 2, 17);

        Assert.True(MacOSTextAnchor.IsSelectedTextBoundsPlausible(bounds));
    }
}
