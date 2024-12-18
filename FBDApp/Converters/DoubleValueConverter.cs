using System;
using System.Globalization;
using System.Windows.Data;

namespace FBDApp.Converters
{
    public class DoubleValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                return doubleValue.ToString(culture);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                // 如果字符串為空或只包含空白字符，返回0
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return 0.0;
                }

                // 嘗試轉換為 double
                if (double.TryParse(stringValue, NumberStyles.Any, culture, out double result))
                {
                    return result;
                }
            }
            return 0.0;
        }
    }
}
