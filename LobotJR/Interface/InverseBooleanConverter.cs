using System;
using System.Globalization;
using System.Windows.Data;

namespace LobotJR.Interface
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool) || !(value is bool))
            {
                throw new ArgumentException("Target type must be Visibility, and source must be boolean");
            }
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool) || !(value is bool))
            {
                throw new ArgumentException("Target type must be boolean, and source must be Visibility");
            }
            return !(bool)value;
        }
    }
}
