using System.Globalization;
using System.Windows.Data;

namespace Neptuo.Productivity.SnippetManager.Views.Controls;

public class ArrayLastItemConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<object> items)
            return items.Last();

        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
