using System.Windows;
using System.Windows.Controls;

namespace ZO.LoadOrderManager
{
    public enum EntityType
    {
        Plugin,
        Group,
        LoadOut,
        GroupSet,
        Url
    }


    public class EntityTypeTemplateSelector : DataTemplateSelector
    {
        public required DataTemplate GroupTemplate { get; set; }
        public required DataTemplate PluginTemplate { get; set; }
        public required DataTemplate LoadOutTemplate { get; set; }
        public required DataTemplate CollapsedTemplate { get; set; }  // Adding the CollapsedTemplate

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var loadOrderItem = item as LoadOrderItemViewModel;

            if (loadOrderItem == null)
                return base.SelectTemplate(item, container);

            // First, check if the item's visibility is collapsed, if so return the CollapsedTemplate
            if (loadOrderItem.PluginVisibility == System.Windows.Visibility.Collapsed)
            {
                return CollapsedTemplate;
            }

            // Otherwise, select the appropriate template based on EntityType
            return loadOrderItem.EntityType switch
            {
                EntityType.Group => GroupTemplate,
                EntityType.Plugin => PluginTemplate,
                EntityType.LoadOut => LoadOutTemplate,
                _ => base.SelectTemplate(item, container),
            };
        }
    }
        public static class EntityTypeHelper
        {
        public static object? GetUnderlyingObject(LoadOrderItemViewModel item)
        {
            return item.EntityType switch
            {
                EntityType.Group => item.GetModGroup(),
                EntityType.Plugin => item.PluginData,
                _ => null,
            };
        }
    }
}
