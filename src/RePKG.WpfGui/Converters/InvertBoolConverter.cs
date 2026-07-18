using System;
using System.Globalization;
using System.Windows.Data;

namespace RePKG.WpfGui.Converters;

/// <summary>
/// 反转 Bool 值转换器
/// </summary>
[ValueConversion(typeof(bool), typeof(bool))]
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;

        return false;
    }
}
