using System.Globalization;
using Avalonia.Data.Converters;

namespace Neptuo.Productivity.SnippetManager.Views.Controls;

public class NumberCompareConverter : IValueConverter
{
    public int EdgeValue { get; set; }
    public object? EqualValue { get; set; }
    public object? GreaterValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i)
            return i > EdgeValue ? GreaterValue : EqualValue;

        return EqualValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
