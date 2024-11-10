using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LobotJR.Interface
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class InverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Visibility) || !(value is bool))
            {
                throw new ArgumentException("Target type must be Visibility, and source must be boolean");
            }
            return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool) || !(value is Visibility))
            {
                throw new ArgumentException("Target type must be boolean, and source must be Visibility");
            }
            return (Visibility)value != Visibility.Visible;
        }
    }
}
