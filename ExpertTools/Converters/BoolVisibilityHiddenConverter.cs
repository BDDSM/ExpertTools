using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace ExpertTools.Converters
{
    public class BoolVisibilityCollapseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (bool)value;

            if (v)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (Visibility)value;

            if (v == Visibility.Hidden)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
