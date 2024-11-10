using System.Globalization;
using System.Windows.Controls;

namespace LobotJR.Interface
{
    public class NumericInputValidator : ValidationRule
    {
        public bool IsReal { get; set; } = false;
        public bool IsNegative { get; set; } = false;
        public bool IsHex { get; set; } = false;
        public int? Min { get; set; }
        public int? Max { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var stringValue = value as string;
            bool result;
            double numericValue;
            if (!IsReal)
            {
                if (IsHex)
                {
                    if (stringValue.StartsWith("0x"))
                    {
                        stringValue = stringValue.Substring(2);
                    }
                    else if (stringValue.StartsWith("#"))
                    {
                        stringValue = stringValue.Substring(1);
                    }
                    result = int.TryParse(stringValue, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var intValue);
                    numericValue = intValue;
                }
                else
                {
                    result = int.TryParse(stringValue, out var intValue);
                    numericValue = intValue;
                }
            }
            else
            {
                result = float.TryParse(stringValue, out var floatValue);
                numericValue = floatValue;
            }
            if (result)
            {
                result = IsNegative || numericValue >= 0;
            }
            if (result)
            {
                if (Min.HasValue && numericValue < Min)
                {
                    return new ValidationResult(false, $"Input must be >= {Min.Value}.");
                }
                if (Max.HasValue && numericValue > Max)
                {
                    return new ValidationResult(false, $"Input must be <= {Max.Value}.");
                }
            }
            return new ValidationResult(result, result ? null : $"Input must be a {(IsNegative ? "" : "positive ")}{(IsReal ? "real" : "whole")} number.");
        }
    }
}
