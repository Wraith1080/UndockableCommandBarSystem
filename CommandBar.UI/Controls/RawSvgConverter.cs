using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace CommandBar.UI.Controls
{
    public class RawSvgConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string svgContent && !string.IsNullOrWhiteSpace(svgContent))
            {
                try
                {
                    WpfDrawingSettings settings = new WpfDrawingSettings { IncludeRuntime = true, TextAsGeometry = false };

                    // Trick SharpVectors into reading our memory string as if it were a file
                    using (StringReader stringReader = new StringReader(svgContent))
                    using (XmlReader xmlReader = XmlReader.Create(stringReader))
                    {
                        FileSvgReader reader = new FileSvgReader(settings);
                        DrawingGroup drawing = reader.Read(xmlReader);
                        if (drawing != null) return new DrawingImage(drawing);
                    }
                }
                catch { /* Fallback silently if the SVG is malformed */ }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}