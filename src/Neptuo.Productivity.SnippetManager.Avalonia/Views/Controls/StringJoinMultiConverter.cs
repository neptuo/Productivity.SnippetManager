using System.Globalization;
using Avalonia.Data.Converters;

namespace Neptuo.Productivity.SnippetManager.Views.Controls;

public class StringJoinMultiConverter : IMultiValueConverter
{
    public string? Separator { get; set; }

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 0 || values[0] is not string[] items)
            return string.Empty;

        int startIndex = 0;
        if (values.Count > 1 && values[1] is int s)
            startIndex = s;

        int endIndex = items.Length;
        if (values.Count > 2 && values[2] is int e)
            endIndex = e;

        if (items.Length > endIndex)
            return string.Join(Separator, items[startIndex..endIndex]);

        if (items.Length > startIndex)
            return string.Join(Separator, items[startIndex..]);

        return string.Join(Separator, items);
    }
}
