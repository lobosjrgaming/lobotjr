using System.Globalization;
using System.Windows.Controls;

namespace LobotJR.Interface
{
    public class NumericInputValidator : ValidationRule
    {
        public bool IsReal { get; set; }
        public bool IsNegative { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var stringValue = value as string;
            bool result;
            double numericValue;
            if (IsReal)
            {
                result = int.TryParse(stringValue, out var intValue);
                numericValue = intValue;
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
            return new ValidationResult(result, result ? null : $"Input must be a {(IsNegative ? "" : "positive ")}{(IsReal ? "real" : "whole")} number.");
        }
    }
}
