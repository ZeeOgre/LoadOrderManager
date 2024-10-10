using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Linq;
using System.Windows.Controls;
using System.Windows;

namespace ZO.LoadOrderManager
{
    public enum EntityType
    {
        Plugin,
        Group,
        LoadOut,
        Url
    }


    public class EntityTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GroupTemplate { get; set; }
        public DataTemplate PluginTemplate { get; set; }
        public DataTemplate LoadOutTemplate { get; set; }

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
