using System.Globalization;
using Avalonia.Data.Converters;

namespace Neptuo.Productivity.SnippetManager.Views.Controls;

public class ArrayLastItemConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string[] arr && arr.Length > 0)
            return arr[arr.Length - 1];

        if (value is IEnumerable<object> items)
            return items.LastOrDefault() ?? "";

        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
