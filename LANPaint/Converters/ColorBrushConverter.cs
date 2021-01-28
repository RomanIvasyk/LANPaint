﻿using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LANPaint.Converters
{
    [ValueConversion(typeof(Color), typeof(Brush))]
    public class ColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is null ? null : new SolidColorBrush((Color)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((SolidColorBrush) value)?.Color;
        }
    }
}
