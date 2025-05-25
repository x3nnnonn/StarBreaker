using Avalonia.Data.Converters;
using Avalonia.Media;
using StarBreaker.P4k;
using System.Globalization;

namespace StarBreaker.Screens;

public class P4kComparisonStatusToColorConverter : IValueConverter
{
    public static readonly P4kComparisonStatusToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not P4kComparisonStatus status)
            return Brushes.Transparent;

        return status switch
        {
            P4kComparisonStatus.Added => new SolidColorBrush(Color.FromArgb(40, 0, 255, 0)),      // Light green background
            P4kComparisonStatus.Removed => new SolidColorBrush(Color.FromArgb(40, 255, 0, 0)),    // Light red background
            P4kComparisonStatus.Modified => new SolidColorBrush(Color.FromArgb(40, 0, 150, 255)), // Light blue background
            P4kComparisonStatus.Unchanged => Brushes.Transparent,
            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 