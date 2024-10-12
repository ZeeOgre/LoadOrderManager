using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ZO.LoadOrderManager
{

    public class GroupIDToIsEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long groupID)
            {
                return groupID > 0;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GroupIDToIsExpandedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long groupID)
            {
                return groupID > 0;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class ItemStateToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3)
                return Brushes.Transparent; // Default color if not enough values

            bool isSelected = values[0] is bool selected && selected;
            bool isHighlighted = values[1] is bool highlighted && highlighted;
            EntityType type = values[2] is EntityType entityType ? entityType : EntityType.Url; // Default to Unknown if not valid

            string target = parameter as string ?? "Background"; // Check if we're dealing with background or foreground

            // Foreground color handling
            if (target == "Foreground")
            {
                if (isHighlighted)
                {
                    return Application.Current.TryFindResource("HighlightForegroundBrush") as SolidColorBrush ?? Brushes.Black; // Set foreground to black for better readability when highlighted
                }

                if (isSelected)
                {
                    // Retrieve the selected foreground brush based on theme
                    var selectedForegroundBrush = Application.Current.TryFindResource("SelectedForegroundBrush") as SolidColorBrush;
                    return selectedForegroundBrush ?? Brushes.White; // Fallback to white if no resource found
                }

                if (type == EntityType.Plugin)
                {
                    // Retrieve the default plugin foreground brush
                    var pluginForegroundBrush = Application.Current.TryFindResource("PluginForegroundBrush") as SolidColorBrush;
                    return pluginForegroundBrush ?? Brushes.White; // Fallback to white if no resource found
                }

                var foregroundBrush = Application.Current.TryFindResource("ControlForegroundBrush") as SolidColorBrush;
                return foregroundBrush ?? Brushes.Black; // Fallback to black if no resource found
            }

            // Retrieve resource brushes dynamically based on the current theme
            var groupBrush = Application.Current.TryFindResource("GroupBackgroundBrush") as SolidColorBrush;
            var highlightBrush = Application.Current.TryFindResource("SearchHighlightBackgroundBrush") as SolidColorBrush;
            var selectedGroupBrush = Application.Current.TryFindResource("SelectedGroupBackgroundBrush") as SolidColorBrush;
            var selectedPluginBrush = Application.Current.TryFindResource("SelectedPluginBackgroundBrush") as SolidColorBrush;

            // Handle highlighted state (for search results)
            if (isHighlighted)
            {
                return highlightBrush ?? Brushes.Yellow; // Fallback to yellow if no resource found
            }

            // Handle selected state
            if (isSelected)
            {
                return type == EntityType.Group
                    ? (selectedGroupBrush ?? Brushes.Teal)  // Fallback to teal for selected groups
                    : (selectedPluginBrush ?? Brushes.SteelBlue); // Fallback to steel blue for selected plugins
            }

            // Default colors for groups and plugins
            return type == EntityType.Group
                ? (groupBrush ?? Brushes.LightBlue) // Fallback to light blue for groups
                : Brushes.Transparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GroupItemStyleSelector : StyleSelector
    {
        public Style GroupStyle { get; set; } = null!;
        public Style DefaultStyle { get; set; } = null!;

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
                else if (long.TryParse(url, out _))
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


    public class LoadOutAndPluginToIsEnabledConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is LoadOut loadOut && values[1] is long pluginID)
            {
                return loadOut.IsPluginEnabled(pluginID);
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null; // or return Binding.DoNothing; depending on your preference
        }
    }


    public class FilesToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ObservableCollection<FileInfo> files)
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
