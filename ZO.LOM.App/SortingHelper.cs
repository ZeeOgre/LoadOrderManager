using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ZO.LoadOrderManager
{
    public static class SortingHelper
    {
        public static void SortGroupsAndPlugins(
            ObservableCollection<ModGroup> groups,
            IEnumerable<Plugin> sortedPlugins,
            ObservableCollection<LoadOrderItemViewModel> targetCollection)
        {
            // Clear the target collection first
            targetCollection.Clear();

            // Handle special groups
            var specialGroups = new Dictionary<int, ModGroup>
            {
                { 1, groups.FirstOrDefault(g => g.GroupID == 1) },    // Default root group
                { -997, groups.FirstOrDefault(g => g.GroupID == -997) },  // Uncategorized
                { -998, groups.FirstOrDefault(g => g.GroupID == -998) },  // Never load
                { -999, groups.FirstOrDefault(g => g.GroupID == -999) } // Bethesda core files
                
                
            };

            // Add the default group first, if it exists
            if (specialGroups[1] != null)
            {
                AddGroupAndChildren(specialGroups[1], sortedPlugins, targetCollection, groups);
            }

            // Add special groups at the bottom
            AddSpecialGroups(targetCollection, sortedPlugins, specialGroups);
        }

        private static void AddGroupAndChildren(
            ModGroup group,
            IEnumerable<Plugin> sortedPlugins,
            ObservableCollection<LoadOrderItemViewModel> targetCollection,
            ObservableCollection<ModGroup> allGroups)
        {
            var groupVM = new LoadOrderItemViewModel
            {
                DisplayName = group.GroupName,
                EntityType = EntityType.Group,
                GroupID = group.GroupID
            };
            targetCollection.Add(groupVM);

            // Get child groups
            var childGroups = allGroups
                .Where(g => g.ParentID == group.GroupID)
                .OrderBy(g => g.Ordinal)
                .ToList();

            // Add child groups and their children
            foreach (var childGroup in childGroups)
            {
                AddGroupAndChildren(childGroup, sortedPlugins, targetCollection, allGroups);
            }

            // Add plugins for this group
            var plugins = sortedPlugins
                .Where(p => p.GroupID == group.GroupID)
                .OrderBy(p => p.GroupOrdinal)
                .ToList();

            foreach (var plugin in plugins)
            {
                var pluginVM = new LoadOrderItemViewModel
                {
                    DisplayName = plugin.PluginName,
                    EntityType = EntityType.Plugin,
                    PluginData = plugin
                };
                targetCollection.Add(pluginVM);
            }
        }

        private static void AddSpecialGroups(
            ObservableCollection<LoadOrderItemViewModel> targetCollection,
            IEnumerable<Plugin> sortedPlugins,
            Dictionary<int, ModGroup> specialGroups)
        {
            foreach (var groupId in specialGroups.Keys.Where(id => id < -900))
            {
                if (specialGroups[groupId] != null)
                {
                    AddGroupAndChildren(specialGroups[groupId], sortedPlugins, targetCollection, new ObservableCollection<ModGroup>());
                }
            }
        }
    }
}
