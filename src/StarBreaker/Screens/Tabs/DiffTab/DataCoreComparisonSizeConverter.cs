using Avalonia.Data.Converters;
using StarBreaker.DataCore;
using StarBreaker.Extensions;
using System.Globalization;

namespace StarBreaker.Screens;

public class DataCoreComparisonSizeConverter : IValueConverter
{
    public static readonly DataCoreComparisonSizeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IDataCoreComparisonNode node)
            return "";

        return node.GetComparisonSize();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 