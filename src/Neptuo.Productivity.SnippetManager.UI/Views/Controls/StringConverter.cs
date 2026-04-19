using System.Globalization;
using System.Windows.Data;

namespace Neptuo.Productivity.SnippetManager.Views.Controls;

public class StringConverter : IValueConverter
{
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => $"{Prefix}{value}{Suffix}";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
