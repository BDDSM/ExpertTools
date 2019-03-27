using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace ExpertTools.Converters
{
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (bool)value;

            return !(v);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (bool)value;

            return !(v);
        }
    }
}
