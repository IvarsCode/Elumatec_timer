using Avalonia.Data.Converters;
using System;
using System.Globalization;

// Put this class in the global namespace so XAML can reference it with x:Static StringConverters.IsNotNullOrEmpty
public static class StringConverters
{
    public static readonly IValueConverter IsNotNullOrEmpty = new IsNotNullOrEmptyConverter();
}

public class IsNotNullOrEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
            return !string.IsNullOrEmpty(s);
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
