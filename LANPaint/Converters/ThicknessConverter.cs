﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace LANPaint.Converters
{
    [ValueConversion(typeof(string), typeof(double))]
    public class ThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value?.ToString()) ? 0 : double.Parse(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }
    }
}
