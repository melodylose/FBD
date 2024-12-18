using System;
using System.Globalization;
using System.Windows.Data;
using FBDApp.Models;

namespace FBDApp.Converters
{
    public class ComparisonOperatorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ComparisonOperator op)
            {
                return op switch
                {
                    ComparisonOperator.GreaterThan => ">",
                    ComparisonOperator.Equal => "=",
                    ComparisonOperator.LessThan => "<",
                    _ => "="
                };
            }
            return "=";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str switch
                {
                    ">" => ComparisonOperator.GreaterThan,
                    "=" => ComparisonOperator.Equal,
                    "<" => ComparisonOperator.LessThan,
                    _ => ComparisonOperator.Equal
                };
            }
            return ComparisonOperator.Equal;
        }
    }
}
