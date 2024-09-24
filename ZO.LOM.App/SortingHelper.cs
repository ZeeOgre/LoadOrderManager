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
            var specialGroups = new Dictionary<long, ModGroup>
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

        public static AggLoadInfo ParsePluginsTxt(AggLoadInfo? incomingAggLoadInfo = null, string? pluginsFile = null)
        {
            var aggLoadInfo = incomingAggLoadInfo ?? AggLoadInfo.Instance;
            var pluginsFilePath = pluginsFile ?? FileManager.PluginsFile;
            var defaultModGroup = aggLoadInfo.Groups.FirstOrDefault(g => g.GroupID == 1);

            if (defaultModGroup == null)
            {
                throw new Exception("Default ModGroup not found in AggLoadInfo.");
            }

            var loadOut = aggLoadInfo.ActiveLoadOut;
            var groupSet = aggLoadInfo.ActiveGroupSet;
            var enabledPlugins = new HashSet<long>();
            ModGroup currentGroup = defaultModGroup;

            var groupParentMapping = new Dictionary<long, ModGroup> { { 0, defaultModGroup } };
            var groupOrdinalTracker = new Dictionary<long, long> { { 1, 1 } };
            var pluginOrdinalTracker = new Dictionary<long, long> { { 1, 1 } };

            try
            {
                var lines = File.ReadAllLines(pluginsFilePath);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || System.Text.RegularExpressions.Regex.IsMatch(line, @"^#{1,2}(?!#)"))
                    {
                        continue;
                    }

                    if (line.StartsWith("###"))
                    {
                        int level = line.TakeWhile(c => c == '#').Count();
                        string groupInfo = line.Substring(level).Trim();
                        string[] groupParts = groupInfo.Split(new[] { "@@" }, StringSplitOptions.None);
                        string groupName = groupParts.Length > 0 ? groupParts[0].Trim() : string.Empty;
                        string groupDescription = groupParts.Length > 1 ? groupParts[1].Trim() : string.Empty;

                        ModGroup parentGroup = level == 3
                            ? aggLoadInfo.Groups.FirstOrDefault(g => g.GroupID == 1) ?? throw new Exception("Parent group not found.")
                            : groupParentMapping[level - 1];

                        if (parentGroup == null)
                        {
                            continue;
                        }

                        if (!groupOrdinalTracker.ContainsKey(parentGroup.GroupID ?? 1))
                        {
                            groupOrdinalTracker[parentGroup.GroupID ?? 1] = 1;
                        }

                        var existingGroup = aggLoadInfo.Groups.FirstOrDefault(g => g.GroupName == groupName)
                                            ?? FuzzyMatchGroup(aggLoadInfo, groupName);
                        if (existingGroup != null)
                        {
                            if (existingGroup.GroupSetID == groupSet.GroupSetID)
                            {
                                existingGroup.Description = groupDescription;
                                currentGroup = existingGroup;
                            }
                            else
                            {
                                var newGroup = existingGroup.Clone(groupSet);
                                newGroup.Description = groupDescription;
                                currentGroup = newGroup.WriteGroup();
                                groupParentMapping[level] = currentGroup;
                                groupOrdinalTracker[parentGroup.GroupID ?? 1]++;
                                if (!pluginOrdinalTracker.ContainsKey(currentGroup.GroupID ?? 1))
                                {
                                    pluginOrdinalTracker[currentGroup.GroupID ?? 1] = 1;
                                }
                                aggLoadInfo.Groups.Add(currentGroup); // Add new group to aggLoadInfo.Groups
                            }
                        }
                        else
                        {
                            var newGroup = new ModGroup(null, groupName, groupDescription, parentGroup.GroupID, groupOrdinalTracker[parentGroup.GroupID ?? 1], groupSet.GroupSetID);
                            currentGroup = newGroup.WriteGroup();
                            groupParentMapping[level] = currentGroup;
                            groupOrdinalTracker[parentGroup.GroupID ?? 1]++;
                            if (!pluginOrdinalTracker.ContainsKey(currentGroup.GroupID ?? 1))
                            {
                                pluginOrdinalTracker[currentGroup.GroupID ?? 1] = 1;
                            }
                            aggLoadInfo.Groups.Add(currentGroup); // Add new group to aggLoadInfo.Groups
                        }

                        // Reset plugin ordinal tracker for the new group
                        pluginOrdinalTracker[currentGroup.GroupID ?? 1] = 1;

                        continue;
                    }

                    bool isEnabled = line.StartsWith("*");
                    string pluginName = line.TrimStart('*').Trim();
                    var existingPlugin = aggLoadInfo.Plugins.FirstOrDefault(p => p.PluginName == pluginName);
                    if (existingPlugin != null)
                    {
                        existingPlugin.GroupID = currentGroup.GroupID;
                        existingPlugin.GroupOrdinal = pluginOrdinalTracker[currentGroup.GroupID ?? 1];
                    }
                    else
                    {
                        var plugin = new Plugin
                        {
                            PluginName = pluginName,
                            GroupID = currentGroup.GroupID,
                            GroupOrdinal = pluginOrdinalTracker[currentGroup.GroupID ?? 1],
                            GroupSetID = groupSet.GroupSetID
                        };

                        var completePlugin = plugin.WriteMod();
                        pluginOrdinalTracker[currentGroup.GroupID ?? 1]++;
                        if (completePlugin != null)
                        {
                            loadOut.LoadPlugins(new List<Plugin> { completePlugin });
                            if (isEnabled)
                            {
                                enabledPlugins.Add(completePlugin.PluginID);
                            }
                            aggLoadInfo.Plugins.Add(completePlugin);
                        }
                    }
                }

                // Directly update the enabled plugins in the loadOut
                loadOut.UpdateEnabledPlugins(enabledPlugins);
                //aggLoadInfo.ProfilePlugins.Items.Clear();
                foreach (var pluginID in enabledPlugins)
                {
                    aggLoadInfo.ProfilePlugins.Items.Add((loadOut.ProfileID, pluginID));
                }

                // Refresh GroupSetPlugins, GroupSetGroups, and ProfilePlugins
                //aggLoadInfo.RefreshMetadataFromDB();
            }
            catch (Exception ex)
            {
                // Handle exception
            }

            return aggLoadInfo;
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
