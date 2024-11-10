using System;
using System.Globalization;
using System.Windows.Data;

namespace LobotJR.Interface
{
    public class HexConverter : IValueConverter
    {
        public static string TrimPrefix(string value)
        {
            if (value.StartsWith("0x"))
            {
                return value.Substring(2);
            }
            if (value.StartsWith("#"))
            {
                return value.Substring(1);
            }
            return value;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return $"0x{intValue:X6}";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (int.TryParse(TrimPrefix(value.ToString()), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var result))
            {
                return result;
            }
            return value;
        }
    }
}
