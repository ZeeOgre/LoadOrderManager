using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ZO.LoadOrderManager
{

    class GroupIDToAbsoluteValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int groupId)
            {
                // Ensure reserved negative group IDs are always at the bottom
                if (groupId < 0)
                {
                    return int.MaxValue + groupId;
                }
                return groupId;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GroupItemStyleSelector : StyleSelector
    {
        public Style GroupStyle { get; set; }
        public Style DefaultStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is ModGroup)
            {
                return GroupStyle;
            }
            return DefaultStyle;
        }
    }

    public class ArchiveFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string format)
            {
                format = format.ToLower();
                if (format.Contains("7z"))
                {
                    return "7z";
                }
                else if (format.Contains("zip"))
                {
                    return "zip";
                }
            }
            return "zip"; // Default to zip if no match
        }


        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string format)
            {
                return format.ToLower(); // Simply return the selected format
            }
            return null; // or return a default if needed
        }

    }

    public class BethesdaUrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string url && !string.IsNullOrEmpty(url))
            {
                if (url.StartsWith("https://"))
                {
                    var uri = new Uri(url);
                    return uri.Segments.Last().TrimEnd('/');
                }
                else if (Guid.TryParse(url, out _))
                {
                    return $"https://creations.bethesda.net/en/starfield/details/{url}";
                }
            }
            return "Bethesda";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NexusUrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string url && !string.IsNullOrEmpty(url))
            {
                if (url.StartsWith("https://"))
                {
                    var uri = new Uri(url);
                    return uri.Segments.Last().TrimEnd('/');
                }
                else if (int.TryParse(url, out _))
                {
                    return $"https://www.nexusmods.com/starfield/mods/{url}";
                }
            }
            return "Nexus";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PathToFolderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.IO.Path.GetDirectoryName(value?.ToString() ?? string.Empty) ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PathToFileNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.IO.Path.GetFileName(value?.ToString() ?? string.Empty) ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BackupStatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            if (value is string stringValue)
            {
                return string.IsNullOrEmpty(stringValue) ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }


    public class StringNullOrEmptyToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FilesToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<FileInfo> files)
            {
                return string.Join(", ", files.Select(f => f.Filename));
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class EnumFlagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var state = (ModState)value;
            var flag = (ModState)parameter;
            return state.HasFlag(flag);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var state = (ModState)value;
            var flag = (ModState)parameter;
            if ((bool)value)
            {
                return state | flag;
            }
            else
            {
                return state & ~flag;
            }
        }
    }
}
