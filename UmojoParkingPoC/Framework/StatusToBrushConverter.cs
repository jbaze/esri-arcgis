using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace UmojoParkingPoC.Framework
{
    public class StatusToBrushConverter : IValueConverter
    {
        public Brush ErrorBrush { get; set; } = Brushes.IndianRed;
        public Brush DefaultBrush { get; set; } = Brushes.Gray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;
            return !string.IsNullOrEmpty(text) && text.StartsWith("Error:", StringComparison.Ordinal)
                ? ErrorBrush
                : DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
