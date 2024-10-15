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

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var entityType = (item as LoadOrderItemViewModel)?.EntityType;

            return entityType switch
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
