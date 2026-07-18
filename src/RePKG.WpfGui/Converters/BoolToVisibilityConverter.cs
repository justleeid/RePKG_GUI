using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RePKG.WpfGui.Converters;

/// <summary>
/// Bool 到 Visibility 转换器
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    public Visibility TrueValue { get; set; } = Visibility.Visible;
    public Visibility FalseValue { get; set; } = Visibility.Collapsed;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue ? TrueValue : FalseValue;

        return FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
            return visibility == TrueValue;

        return false;
    }
}
