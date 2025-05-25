using Avalonia.Data.Converters;
using StarBreaker.DataCore;
using System.Globalization;

namespace StarBreaker.Screens;

public class DataCoreComparisonStatusToTextConverter : IValueConverter
{
    public static readonly DataCoreComparisonStatusToTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DataCoreComparisonStatus status)
            return "";

        return status switch
        {
            DataCoreComparisonStatus.Added => "ADDED",
            DataCoreComparisonStatus.Removed => "REMOVED",
            DataCoreComparisonStatus.Modified => "MODIFIED",
            DataCoreComparisonStatus.Unchanged => "",
            _ => ""
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 