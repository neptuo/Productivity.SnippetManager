using System.Globalization;
using System.Windows.Data;

namespace Neptuo.Productivity.SnippetManager.Views.Controls;

public class StringEmptyConverter : IValueConverter
{
    public object? TrueValue { get; set; }
    public object? FalseValue { get; set; }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string? stringValue = value as string;
        return string.IsNullOrEmpty(stringValue) ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
