using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
using System.Windows.Input;

namespace LinuxCommandCenter.Converters
{
    public class StringContainsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str && parameter is string param)
            {
                // 处理带两个参数的情况 "search:trueValue:falseValue"
                var parts = param.Split(':');
                if (parts.Length == 3)
                {
                    return str.Contains(parts[0], StringComparison.OrdinalIgnoreCase) ? parts[1] : parts[2];
                }
                return str.Contains(parts[0], StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringEqualsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str && parameter is string param)
            {
                var parts = param.Split(':');
                if (parts.Length == 3)
                {
                    return str.Equals(parts[0], StringComparison.OrdinalIgnoreCase) ? parts[1] : parts[2];
                }
                return str.Equals(parts[0], StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringFormatConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is string format)
            {
                return string.Format(culture, format, value);
            }
            return value?.ToString();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colors)
            {
                var colorParts = colors.Split(':');
                if (colorParts.Length == 2)
                {
                    return boolValue ? Brush.Parse(colorParts[0]) : Brush.Parse(colorParts[1]);
                }
            }
            return Brushes.Black;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string strings)
            {
                var stringParts = strings.Split(':');
                if (stringParts.Length == 2)
                {
                    return boolValue ? stringParts[0] : stringParts[1];
                }
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsTypeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value != null && parameter is string typeName)
            {
                var type = Type.GetType(typeName);
                return type != null && type.IsInstanceOfType(value);
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsNotNullConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ToCommandConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Action action)
            {
                // 创建一个简单的命令来执行Action
                return new MiniCommand(action);
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private class MiniCommand : ICommand
        {
            private readonly Action _action;

            public event EventHandler? CanExecuteChanged;

            public MiniCommand(Action action)
            {
                _action = action;
            }

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter)
            {
                _action?.Invoke();
            }
        }
    }
}

public class EnumToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && parameter is Type enumType)
        {
            return Enum.Parse(enumType, str);
        }
        return null;
    }
}

public class ConnectionStatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status == "Connected" ? "#10B981" : "#EF4444";
        }
        return "#EF4444";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}