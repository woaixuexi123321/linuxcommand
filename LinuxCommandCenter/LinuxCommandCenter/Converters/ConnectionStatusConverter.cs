using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace LinuxCommandCenter.Converters // 确保命名空间正确
{
    public class ConnectionStatusConverter : IValueConverter // 确保类名与XAML中完全一致
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                // 根据你的逻辑返回颜色
                return status == "Connected" ? "#10B981" : "#EF4444";
            }
            return "#EF4444"; // 默认颜色
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 如果不需要反向转换，抛出异常即可
            throw new NotImplementedException();
        }
    }
}