namespace ZO.LoadOrderManager
{
    public static class SortingHelper
    {
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

        public static int FuzzyCompareStrings(string str1, string str2)
        {
            return LevenshteinDistance(str1, str2);
        }

        //    public static LoadOrdersViewModel CreateLoadOrdersViewModel(GroupSet groupSet, LoadOut loadOut, bool suppress997, bool isCached = false)
        //    {
        //        var viewModel = new LoadOrdersViewModel
        //        {
        //            SelectedGroupSet = groupSet,
        //            SelectedLoadOut = loadOut,
        //            Suppress997 = suppress997,
        //            IsCached = isCached
        //        };
        //        PopulateLoadOrdersViewModel(viewModel, groupSet, loadOut, suppress997, isCached);
        //        return viewModel;
        //    }

        //    public static void PopulateLoadOrdersViewModel(LoadOrdersViewModel viewModel, GroupSet groupSet, LoadOut loadOut, bool suppress997, bool isCached = false)
        //    {
        //        Application.Current.Dispatcher.Invoke(() =>
        //        {
        //            viewModel.Items.Clear();
        //            viewModel.SelectedGroupSet = groupSet;
        //            viewModel.SelectedLoadOut = loadOut;
        //            viewModel.Suppress997 = suppress997;
        //            viewModel.IsCached = isCached;

        //            var activeGroupSetID = groupSet.GroupSetID;
        //            var isCachedGroupSet = groupSet == AggLoadInfo.Instance.GetCachedGroupSet1();

        //            var sortedGroups = GetSortedGroups(activeGroupSetID);

        //            var groupDictionary = new Dictionary<long, LoadOrderItemViewModel>();

        //            foreach (var group in sortedGroups)
        //            {
        //                if (group.GroupID == -997 && suppress997)
        //                    continue;

        //                var groupItem = new LoadOrderItemViewModel(group);
        //                groupDictionary[(long)group.GroupID] = groupItem;

        //                if (group.ParentID.HasValue && groupDictionary.ContainsKey(group.ParentID.Value))
        //                {
        //                    groupDictionary[group.ParentID.Value].Children.Add(groupItem);
        //                }
        //                else
        //                {
        //                    viewModel.Items.Add(groupItem);
        //                }

        //                AddPluginsToGroup(groupItem, activeGroupSetID, loadOut, suppress997);
        //            }
        //        });
        //    }

        //    private static List<ModGroup> GetSortedGroups(long groupSetID)
        //    {
        //        var groupSetGroups = AggLoadInfo.Instance.GroupSetGroups.Items
        //            .Where(g => g.groupSetID == groupSetID)
        //            .Select(g => (groupID: g.groupID, groupSetID: g.groupSetID, parentID: g.parentID, Ordinal: g.Ordinal))
        //            .OrderBy(g => g.parentID)
        //            .ThenBy(g => g.Ordinal)
        //            .ToList();

        //        var sortedGroups = groupSetGroups
        //            .Select(g => ModGroup.LoadModGroup(g.groupID, g.groupSetID))
        //            .Where(g => g != null)
        //            .ToList();

        //        return sortedGroups;
        //    }

        //    private static void AddPluginsToGroup(LoadOrderItemViewModel parentItem, long groupSetID, LoadOut loadOut, bool? suppress997 = false)
        //    {
        //        var groupPlugins = AggLoadInfo.Instance.GroupSetPlugins.Items
        //            .Where(p => p.groupSetID == groupSetID && p.groupID == parentItem.GroupID)
        //            .Select(p => (groupSetID: p.groupSetID, groupID: p.groupID, pluginID: p.pluginID, Ordinal: p.Ordinal))
        //            .OrderBy(p => p.Ordinal)
        //            .ToList();

        //        foreach (var plugin in groupPlugins)
        //        {
        //            var pluginItem = new LoadOrderItemViewModel(Plugin.LoadPlugin(plugin.pluginID, null, AggLoadInfo.Instance.ActiveGroupSet.GroupSetID))
        //            {
        //                IsActive = loadOut.enabledPlugins.Contains(plugin.pluginID)
        //            };
        //            parentItem.Children.Add(pluginItem);
        //        }
        //    }
    }
}








