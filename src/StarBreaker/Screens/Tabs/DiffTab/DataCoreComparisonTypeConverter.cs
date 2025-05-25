using Avalonia.Data.Converters;
using StarBreaker.DataCore;
using StarBreaker.Extensions;
using System.Globalization;

namespace StarBreaker.Screens;

public class DataCoreComparisonTypeConverter : IValueConverter
{
    public static readonly DataCoreComparisonTypeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IDataCoreComparisonNode node)
            return "";

        return node.GetComparisonDate(); // Using GetComparisonDate which shows record type for DataCore
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 