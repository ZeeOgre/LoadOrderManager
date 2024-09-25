using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ZO.LoadOrderManager
{
    public static class SortingHelper
    {
        public static ObservableCollection<ModGroup> CombineGroups(GroupSet activeGroupSet, GroupSet specialGroupSet)
        {
            var combinedGroups = new ObservableCollection<ModGroup>(activeGroupSet.ModGroups);

            foreach (var specialGroup in specialGroupSet.ModGroups)
            {
                // Exclude GroupID 1 from specialGroupSet if it exists in activeGroupSet
                if (specialGroup.GroupID == 1 && combinedGroups.Any(g => g.GroupID == 1))
                {
                    continue;
                }

                if (!combinedGroups.Any(g => g.GroupID == specialGroup.GroupID))
                {
                    combinedGroups.Add(specialGroup);
                }
            }

            return combinedGroups;
        }

        public static void SortGroupsAndPlugins(
            ObservableCollection<ModGroup> groups,
            IEnumerable<Plugin> sortedPlugins,
            ObservableCollection<LoadOrderItemViewModel> targetCollection,
            GroupSet activeGroupSet)
        {
            // Clear the target collection first
            targetCollection.Clear();

            // Handle special groups
            var specialGroups = new Dictionary<long, ModGroup>
            {
                { 1, groups.FirstOrDefault(g => g.GroupID == 1) },    // Default root group
                { -997, groups.FirstOrDefault(g => g.GroupID == -997) },  // Uncategorized
                { -998, groups.FirstOrDefault(g => g.GroupID == -998) },  // Never load
                { -999, groups.FirstOrDefault(g => g.GroupID == -999) } // Bethesda core files
            };

            // Add the default group first, if it exists and GroupSetID is 1
            if (activeGroupSet.GroupSetID == 1 && specialGroups[1] != null)
            {
                AddGroupAndChildren(specialGroups[1], sortedPlugins, targetCollection, groups);
            }

            // Add other groups if GroupSetID is not 1
            if (activeGroupSet.GroupSetID != 1)
            {
                foreach (var group in groups.Where(g => g.GroupID != 1 && g.GroupID >= 0))
                {
                    AddGroupAndChildren(group, sortedPlugins, targetCollection, groups);
                }
            }

            // Add special groups at the bottom if GroupSetID is 1
            if (activeGroupSet.GroupSetID == 1)
            {
                AddSpecialGroups(targetCollection, sortedPlugins, specialGroups);
            }
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
            Dictionary<long, ModGroup> specialGroups)
        {
            foreach (var groupId in specialGroups.Keys.Where(id => id < -900))
            {
                if (specialGroups[groupId] != null)
                {
                    AddGroupAndChildren(specialGroups[groupId], sortedPlugins, targetCollection, new ObservableCollection<ModGroup>());
                }
            }
        }

        public static int LevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.IsNullOrEmpty(target) ? 0 : target.Length;
            }

            if (string.IsNullOrEmpty(target))
            {
                return source.Length;
            }

            int sourceLength = source.Length;
            int targetLength = target.Length;

            var distance = new int[sourceLength + 1, targetLength + 1];

            for (int i = 0; i <= sourceLength; distance[i, 0] = i++) { }
            for (int j = 0; j <= targetLength; distance[0, j] = j++) { }

            for (int i = 1; i <= sourceLength; i++)
            {
                for (int j = 1; j <= targetLength; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    distance[i, j] = Math.Min(
                        Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost);
                }
            }

            return distance[sourceLength, targetLength];
        }

        // New fuzzy matching methods
        public static int FuzzyCompareStrings(string str1, string str2)
        {
            return LevenshteinDistance(str1, str2);
        }
    }



}
