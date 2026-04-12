using Avalonia;

namespace Neptuo.Productivity.SnippetManager.Tests;

public class WindowPositioningTests
{
    [Fact]
    public void CalculatePosition_CentersWindowWhenAnchorIsMissing()
    {
        var workingArea = new PixelRect(100, 50, 1000, 800);

        var position = WindowPositioning.CalculatePosition(anchor: null, workingArea, windowWidth: 400, windowHeight: 200);

        Assert.Equal(400, position.X);
        Assert.Equal(350, position.Y);
    }

    [Fact]
    public void CalculatePosition_PlacesWindowToTheRightAndBelowWhenThereIsRoom()
    {
        var workingArea = new PixelRect(0, 0, 1200, 800);
        var anchor = new WindowPositionAnchor(new PixelRect(200, 150, 10, 20), "selected text range");

        var position = WindowPositioning.CalculatePosition(anchor, workingArea, windowWidth: 400, windowHeight: 100);

        Assert.Equal(210, position.X);
        Assert.Equal(170, position.Y);
    }

    [Fact]
    public void CalculatePosition_PlacesWindowToTheLeftWhenTheAnchorIsNearTheRightEdge()
    {
        var workingArea = new PixelRect(0, 0, 1000, 800);
        var anchor = new WindowPositionAnchor(new PixelRect(950, 100, 20, 20), "focused element");

        var position = WindowPositioning.CalculatePosition(anchor, workingArea, windowWidth: 400, windowHeight: 100);

        Assert.Equal(550, position.X);
        Assert.Equal(120, position.Y);
    }

    [Fact]
    public void CalculatePosition_PlacesWindowAboveWhenTheAnchorIsNearTheBottomEdge()
    {
        var workingArea = new PixelRect(0, 0, 1000, 800);
        var anchor = new WindowPositionAnchor(new PixelRect(200, 760, 20, 20), "focused window");

        var position = WindowPositioning.CalculatePosition(anchor, workingArea, windowWidth: 400, windowHeight: 100);

        Assert.Equal(220, position.X);
        Assert.Equal(660, position.Y);
    }
}
