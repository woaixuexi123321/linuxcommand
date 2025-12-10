using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace LinuxCommandCenter.Converters
{
    public class MultiStringContainsConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count >= 2 && values[0] is string str && values[1] is string param)
            {
                // 处理格式 "search:trueValue:falseValue"
                var parts = param.Split(':');
                if (parts.Length == 3)
                {
                    return str.Contains(parts[0], StringComparison.OrdinalIgnoreCase) ? parts[1] : parts[2];
                }
                else if (parts.Length == 2)
                {
                    return str.Contains(parts[0], StringComparison.OrdinalIgnoreCase) ? parts[1] : null;
                }
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}