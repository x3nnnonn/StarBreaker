using Avalonia.Data.Converters;
using StarBreaker.P4k;
using StarBreaker.Extensions;
using System.Globalization;

namespace StarBreaker.Screens;

public class P4kComparisonDateConverter : IValueConverter
{
    public static readonly P4kComparisonDateConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IP4kComparisonNode node)
            return "";

        return node.GetComparisonDate();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 