using Avalonia.Data.Converters;
using StarBreaker.DataCore;
using StarBreaker.Extensions;
using System.Globalization;

namespace StarBreaker.Screens;

public class DataCoreComparisonNameConverter : IValueConverter
{
    public static readonly DataCoreComparisonNameConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IDataCoreComparisonNode node)
            return "";

        return node.GetComparisonName();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 