using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Neptuo.Productivity.SnippetManager.Models;

namespace Neptuo.Productivity.SnippetManager.Views.Controls;

public class StringJoinMultiConverter : IMultiValueConverter
{
    public string? Separator { get; set; }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 0)
            return String.Empty;

        string[] items = (string[])values[0];

        int startIndex = 0;
        if (values.Length > 1)
            startIndex = (int)values[1];

        int endIndex = items.Length;
        if (values.Length > 2)
            endIndex = (int)values[2];

        if (items.Length > endIndex)
            return string.Join(Separator, items[startIndex..endIndex]);

        if (items.Length > startIndex)
            return string.Join(Separator, items[startIndex..]);

        return string.Join(Separator, items);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
