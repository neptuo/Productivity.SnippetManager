using Avalonia;

namespace Neptuo.Productivity.SnippetManager;

internal static class WindowPositioning
{
    public const int DefaultWidth = 400;
    public const int DefaultHeight = 300;

    public static PixelPoint CalculatePosition(WindowPositionAnchor? anchor, PixelRect workingArea, double windowWidth, double windowHeight)
    {
        int normalizedWidth = NormalizeDimension(windowWidth, DefaultWidth);
        int normalizedHeight = NormalizeDimension(windowHeight, DefaultHeight);

        if (anchor is not { } resolvedAnchor)
            return Center(workingArea, normalizedWidth, normalizedHeight);

        int workingAreaRight = workingArea.X + workingArea.Width;
        int workingAreaBottom = workingArea.Y + workingArea.Height;
        int anchorRight = resolvedAnchor.Bounds.X + resolvedAnchor.Bounds.Width;
        int anchorBottom = resolvedAnchor.Bounds.Y + resolvedAnchor.Bounds.Height;

        int x = anchorRight + normalizedWidth <= workingAreaRight
            ? anchorRight
            : resolvedAnchor.Bounds.X - normalizedWidth >= workingArea.X
                ? resolvedAnchor.Bounds.X - normalizedWidth
                : resolvedAnchor.Bounds.X;

        int y = anchorBottom + normalizedHeight <= workingAreaBottom
            ? anchorBottom
            : resolvedAnchor.Bounds.Y - normalizedHeight >= workingArea.Y
                ? resolvedAnchor.Bounds.Y - normalizedHeight
                : resolvedAnchor.Bounds.Y;

        x = Clamp(x, workingArea.X, workingAreaRight - normalizedWidth);
        y = Clamp(y, workingArea.Y, workingAreaBottom - normalizedHeight);

        return new PixelPoint(x, y);
    }

    private static PixelPoint Center(PixelRect workingArea, int windowWidth, int windowHeight)
    {
        int x = workingArea.X + Math.Max(0, (workingArea.Width - windowWidth) / 2);
        int y = workingArea.Y + Math.Max(0, (workingArea.Height - windowHeight) / 2);
        return new PixelPoint(x, y);
    }

    private static int NormalizeDimension(double value, int defaultValue)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            return defaultValue;

        return (int)Math.Ceiling(value);
    }

    private static int Clamp(int value, int min, int max)
    {
        if (max < min)
            return min;

        return Math.Clamp(value, min, max);
    }
}
