using Avalonia.Data.Converters;
using StarBreaker.P4k;
using System.Globalization;

namespace StarBreaker.Screens;

public class P4kComparisonStatusToTextConverter : IValueConverter
{
    public static readonly P4kComparisonStatusToTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not P4kComparisonStatus status)
            return "";

        return status switch
        {
            P4kComparisonStatus.Added => "ADDED",
            P4kComparisonStatus.Removed => "REMOVED",
            P4kComparisonStatus.Modified => "MODIFIED",
            P4kComparisonStatus.Unchanged => "",
            _ => ""
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}