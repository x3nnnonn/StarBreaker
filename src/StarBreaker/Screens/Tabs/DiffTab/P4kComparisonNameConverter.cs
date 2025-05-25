using Avalonia.Data.Converters;
using StarBreaker.P4k;
using StarBreaker.Extensions;
using System.Globalization;

namespace StarBreaker.Screens;

public class P4kComparisonNameConverter : IValueConverter
{
    public static readonly P4kComparisonNameConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IP4kComparisonNode node)
            return "";

        return node.GetComparisonName();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 