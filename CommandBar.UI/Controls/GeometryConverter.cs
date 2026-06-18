using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CommandBar.UI.Controls
{
    public class StringToPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string geoString && !string.IsNullOrWhiteSpace(geoString))
                {
                    // Generates a BRAND NEW Path object for every menu item!
                    return new Path
                    {
                        Data = Geometry.Parse(geoString),
                        Fill = Brushes.DarkGray,
                        Width = 16,
                        Height = 16,
                        Stretch = Stretch.Uniform
                    };
                }
            }
            catch
            {
                // Silently swallow invalid SVG strings so the app doesn't crash
            }

            // Return UnsetValue (instead of null) to tell WPF to safely ignore this binding
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}