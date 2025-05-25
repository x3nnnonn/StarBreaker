using Avalonia.Data.Converters;
using Avalonia.Media;
using StarBreaker.DataCore;
using System.Globalization;

namespace StarBreaker.Screens;

public class DataCoreComparisonStatusToColorConverter : IValueConverter
{
    public static readonly DataCoreComparisonStatusToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DataCoreComparisonStatus status)
            return Brushes.Transparent;

        return status switch
        {
            DataCoreComparisonStatus.Added => new SolidColorBrush(Color.FromArgb(40, 0, 255, 0)),      // Light green background
            DataCoreComparisonStatus.Removed => new SolidColorBrush(Color.FromArgb(40, 255, 0, 0)),    // Light red background
            DataCoreComparisonStatus.Modified => new SolidColorBrush(Color.FromArgb(40, 0, 150, 255)), // Light blue background
            DataCoreComparisonStatus.Unchanged => Brushes.Transparent,
            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 