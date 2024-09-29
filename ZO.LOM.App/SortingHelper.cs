using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;

namespace ZO.LoadOrderManager
{
    public static class SortingHelper
    {
        //public static LoadOrdersViewModel CreateLoadOrdersViewModel(GroupSet groupSet, LoadOut loadOut, bool? suppress997 = false)
        //{
        //    var viewModel = new LoadOrdersViewModel();
        //    PopulateLoadOrdersViewModel(viewModel, groupSet, loadOut, suppress997);
        //    return viewModel;
        //}

        //public static void UpdateLoadOrdersViewModel(LoadOrdersViewModel viewModel, GroupSet groupSet, bool? suppress997 = false)
        //{
        //    var currentLoadOut = AggLoadInfo.Instance.ActiveLoadOut;
        //    PopulateLoadOrdersViewModel(viewModel, groupSet, currentLoadOut, suppress997);
        //}


        //public static void PopulateLoadOrdersViewModel(LoadOrdersViewModel viewModel, GroupSet groupSet, LoadOut loadOut, bool? suppress997 = false)
        //{
        //    viewModel.Items.Clear();
        //    var activeGroupSetID = groupSet.GroupSetID;
        //    var isCachedGroupSet = groupSet == AggLoadInfo.Instance.GetCachedGroupSet1();

        //    IEnumerable<ModGroup> groups;

        //    // Determine which groups to load based on the group set
        //    if (groupSet == AggLoadInfo.Instance.ActiveGroupSet)
        //    {
        //        groups = AggLoadInfo.Instance.Groups
        //            .Where(g => g.GroupSetID == activeGroupSetID && g.ParentID == 0)
        //            .OrderBy(g => g.Ordinal)
        //            .ToList();
        //    }
        //    else if (isCachedGroupSet)
        //    {
        //        groups = groupSet.ModGroups
        //            .Where(g => g.ParentID == 0)
        //            .OrderBy(g => g.Ordinal)
        //            .ToList();
        //    }
        //    else
        //    {
        //        // For other cases, handle as needed
        //        groups = new List<ModGroup>();
        //    }

        //    foreach (var group in groups)
        //    {
        //        // Skip group -997 if suppress997 flag is set
        //        if (group.GroupID == -997 && suppress997 == true)
        //            continue;

        //        var groupItem = new LoadOrderItemViewModel(group);
        //        viewModel.Items.Add(groupItem);

        //        // Determine the correct GroupSetID for child loading
        //        var childGroupSetID = (group.GroupID == -997 && !isCachedGroupSet) ? 1 : activeGroupSetID;

        //        AddChildGroups(groupItem, childGroupSetID, suppress997);
        //    }
        //}

        //private static void AddChildGroups(LoadOrderItemViewModel parentItem, long groupSetID, bool? suppress997 = false)
        //{
        //    // Determine if we are dealing with the cached GroupSet1
        //    var cachedGroupSet = AggLoadInfo.Instance.GetCachedGroupSet1();
        //    bool isCachedGroupSet1 = (groupSetID == 1) && (cachedGroupSet != null);

        //    List<ModGroup> childGroups;

        //    if (isCachedGroupSet1)
        //    {
        //        // Get child groups directly from the cached GroupSet1 object
        //        childGroups = cachedGroupSet.ModGroups
        //            .Where(g => g.ParentID == parentItem.GroupID)
        //            .OrderBy(g => g.Ordinal)
        //            .ToList();
        //    }
        //    else
        //    {
        //        // Get child groups from AggLoadInfo for the given GroupSetID
        //        childGroups = AggLoadInfo.Instance.Groups
        //            .Where(g => g.GroupSetID == groupSetID && g.ParentID == parentItem.GroupID)
        //            .OrderBy(g => g.Ordinal)
        //            .ToList();
        //    }

        //    foreach (var childGroup in childGroups)
        //    {
        //        if (childGroup.GroupID == -997 && suppress997 == true)
        //            continue;

        //        var childGroupItem = new LoadOrderItemViewModel(childGroup);
        //        parentItem.Children.Add(childGroupItem);

        //        // Recursive call to handle child groups
        //        AddChildGroups(childGroupItem, groupSetID, suppress997);
        //    }

        //    // Add plugins for the current group
        //    AddPluginsToGroup(parentItem, groupSetID, suppress997, isCachedGroupSet1);
        //}

        //private static void AddPluginsToGroup(LoadOrderItemViewModel parentItem, long groupSetID, bool? suppress997 = false, bool useCachedPlugins = false)
        //{
        //    List<Plugin> groupPlugins;

        //    if (useCachedPlugins)
        //    {
        //        // Retrieve plugins directly from the cached GroupSet1 object
        //        var cachedGroupSet = AggLoadInfo.Instance.GetCachedGroupSet1();
        //        groupPlugins = cachedGroupSet.ModGroups
        //            .SelectMany(g => g.Plugins)
        //            .Where(p => p.GroupID == parentItem.GroupID)
        //            .OrderBy(p => p.GroupOrdinal)
        //            .ToList();
        //    }
        //    else
        //    {
        //        // Retrieve plugins from AggLoadInfo for the given GroupSetID
        //        groupPlugins = AggLoadInfo.Instance.Plugins
        //            .Where(p => p.GroupSetID == groupSetID && p.GroupID == parentItem.GroupID)
        //            .OrderBy(p => p.GroupOrdinal)
        //            .ToList();
        //    }

        //    // Handle special case for -997 if we need to include plugins from GroupSet1
        //    if (parentItem.GroupID == -997 && groupSetID != 1 && suppress997 == false)
        //    {
        //        var additionalPlugins = AggLoadInfo.Instance.Plugins
        //            .Where(p => p.GroupSetID == 1 && p.GroupID == -997)
        //            .OrderBy(p => p.GroupOrdinal)
        //            .ToList();
        //        groupPlugins.AddRange(additionalPlugins);
        //    }

        //    foreach (var plugin in groupPlugins)
        //    {
        //        var pluginItem = new LoadOrderItemViewModel(plugin);
        //        parentItem.Children.Add(pluginItem);
        //    }
        //}

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

        public static LoadOrdersViewModel CreateLoadOrdersViewModel(GroupSet groupSet, LoadOut loadOut, bool? suppress997 = false)
        {
            var viewModel = new LoadOrdersViewModel();
            PopulateLoadOrdersViewModel(viewModel, groupSet, loadOut, suppress997);
            return viewModel;
        }

        public static void UpdateLoadOrdersViewModel(LoadOrdersViewModel viewModel, GroupSet groupSet, bool? suppress997 = false)
        {
            LoadOut currentLoadOut;

            if (groupSet.GroupSetID == 1)
            { currentLoadOut = LoadOut.Load(1); }
            else
            { currentLoadOut = AggLoadInfo.Instance.ActiveLoadOut; }
                
            PopulateLoadOrdersViewModel(viewModel, groupSet, currentLoadOut, suppress997);
        }

        public static void PopulateLoadOrdersViewModel(LoadOrdersViewModel viewModel, GroupSet groupSet, LoadOut loadOut, bool? suppress997 = false)
        {
            viewModel.Items.Clear();
            var activeGroupSetID = groupSet.GroupSetID;
            var isCachedGroupSet = groupSet == AggLoadInfo.Instance.GetCachedGroupSet1();

            IEnumerable<ModGroup> groups;

            // Determine which groups to load based on the group set
            if (groupSet == AggLoadInfo.Instance.ActiveGroupSet)
            {
                groups = AggLoadInfo.Instance.Groups
                    .Where(g => g.GroupSetID == activeGroupSetID && g.ParentID == 0)
                    .OrderBy(g => g.Ordinal)
                    .ToList();
            }
            else if (isCachedGroupSet)
            {
                groups = groupSet.ModGroups
                    .Where(g => g.ParentID == 0)
                    .OrderBy(g => g.Ordinal)
                    .ToList();
            }
            else
            {
                // For other cases, handle as needed
                groups = new List<ModGroup>();
            }

            foreach (var group in groups)
            {
                // Skip group -997 if suppress997 flag is set
                if (group.GroupID == -997 && suppress997 == true)
                    continue;

                var groupItem = new LoadOrderItemViewModel(group);
                viewModel.Items.Add(groupItem);

                // Determine the correct GroupSetID for child loading
                var childGroupSetID = (group.GroupID == -997 && !isCachedGroupSet) ? 1 : activeGroupSetID;

                AddChildGroups(groupItem, childGroupSetID, loadOut, suppress997);
            }
        }

        private static void AddChildGroups(LoadOrderItemViewModel parentItem, long groupSetID, LoadOut loadOut, bool? suppress997 = false)
        {
            // Determine if we are dealing with the cached GroupSet1
            var cachedGroupSet = AggLoadInfo.Instance.GetCachedGroupSet1();
            bool isCachedGroupSet1 = (groupSetID == 1) && (cachedGroupSet != null);

            List<ModGroup> childGroups;

            if (isCachedGroupSet1)
            {
                // Get child groups directly from the cached GroupSet1 object
                childGroups = cachedGroupSet.ModGroups
                    .Where(g => g.ParentID == parentItem.GroupID)
                    .OrderBy(g => g.Ordinal)
                    .ToList();
            }
            else
            {
                // Get child groups from AggLoadInfo for the given GroupSetID
                childGroups = AggLoadInfo.Instance.Groups
                    .Where(g => g.GroupSetID == groupSetID && g.ParentID == parentItem.GroupID)
                    .OrderBy(g => g.Ordinal)
                    .ToList();
            }

            foreach (var childGroup in childGroups)
            {
                if (childGroup.GroupID == -997 && suppress997 == true)
                    continue;

                var childGroupItem = new LoadOrderItemViewModel(childGroup);
                parentItem.Children.Add(childGroupItem);

                // Recursive call to handle child groups
                AddChildGroups(childGroupItem, groupSetID, loadOut, suppress997);
            }

            // Add plugins for the current group
            AddPluginsToGroup(parentItem, groupSetID, loadOut, suppress997, isCachedGroupSet1);
        }

        private static void AddPluginsToGroup(LoadOrderItemViewModel parentItem, long groupSetID, LoadOut loadOut, bool? suppress997 = false, bool useCachedPlugins = false)
        {
            List<Plugin> groupPlugins;

            if (useCachedPlugins)
            {
                // Retrieve plugins directly from the cached GroupSet1 object
                var cachedGroupSet = AggLoadInfo.Instance.GetCachedGroupSet1();
                groupPlugins = cachedGroupSet.ModGroups
                    .SelectMany(g => g.Plugins)
                    .Where(p => p.GroupID == parentItem.GroupID)
                    .OrderBy(p => p.GroupOrdinal)
                    .ToList();
            }
            else
            {
                // Retrieve plugins from AggLoadInfo for the given GroupSetID
                groupPlugins = AggLoadInfo.Instance.Plugins
                    .Where(p => p.GroupSetID == groupSetID && p.GroupID == parentItem.GroupID)
                    .OrderBy(p => p.GroupOrdinal)
                    .ToList();
            }

            // Handle special case for -997 if we need to include plugins from GroupSet1
            if (parentItem.GroupID == -997 && groupSetID != 1 && suppress997 == false)
            {
                var additionalPlugins = AggLoadInfo.Instance.Plugins
                    .Where(p => p.GroupSetID == 1 && p.GroupID == -997)
                    .OrderBy(p => p.GroupOrdinal)
                    .ToList();
                groupPlugins.AddRange(additionalPlugins);
            }

            foreach (var plugin in groupPlugins)
            {
                var pluginItem = new LoadOrderItemViewModel(plugin)
                {
                    IsActive = loadOut.enabledPlugins.Contains(plugin.PluginID)
                };
                parentItem.Children.Add(pluginItem);
            }
        }

         
    }
}








