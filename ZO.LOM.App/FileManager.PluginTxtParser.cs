using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ZO.LoadOrderManager
{
    public static partial class FileManager
    {
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

                        var existingGroup = aggLoadInfo.Groups.FirstOrDefault(g => g.GroupName == groupName);
                        if (existingGroup != null)
                        {
                            if (existingGroup.GroupSetID == groupSet.GroupSetID)
                            {
                                existingGroup.Description = groupDescription;
                                currentGroup = existingGroup;
                            }
                            else
                            {
                                if (existingGroup.GroupID > 0)
                                {
                                    var newGroup = existingGroup.Clone(groupSet);
                                    newGroup.Description = groupDescription;
                                    currentGroup = newGroup.WriteGroup();
                                }
                                else
                                {
                                    currentGroup = existingGroup;
                                }
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


        public static void ProducePluginsTxt(LoadOrdersViewModel viewModel, string? outputFileName = null)
        {
            if (viewModel == null || viewModel.Items == null || !viewModel.Items.Any())
            {
                throw new ArgumentException("The viewModel or its Items collection cannot be null or empty.");
            }

            var sb = new StringBuilder();

            // Retrieve necessary information for the header
            var groupSetName = AggLoadInfo.Instance.ActiveGroupSet.GroupSetName ?? "Default_GroupSet";
            var loadOutName = AggLoadInfo.Instance.ActiveLoadOut.Name ?? "Default_Profile";
            var dateTimeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var defaultFileName = $"Plugins_{groupSetName}_{loadOutName}.txt";
            var pluginsFilePath = outputFileName ?? Path.Combine(FileManager.GameLocalAppDataFolder, defaultFileName);

            // Custom header with the actual filename
            sb.AppendLine($"# {defaultFileName} produced by ZeeOgre's LoadOutManager using Group Set {groupSetName} and profile {loadOutName} on {dateTimeNow}");
            sb.AppendLine("##----------------------------------------------------------------------------------------------------------------------------------##");

            // Process each item in the viewModel
            foreach (var item in viewModel.Items)
            {
                AppendItemToStringBuilder(item, sb, isRoot: true);
            }

            // Custom footer
            sb.AppendLine();
            sb.AppendLine($"# End of {defaultFileName}");

            // Write to file
            File.WriteAllText(pluginsFilePath, sb.ToString());
        }

        private static void AppendItemToStringBuilder(LoadOrderItemViewModel item, StringBuilder sb, bool isRoot = false)
        {
            // Print active plugins with a prefix '*'
            if (item.EntityType == EntityType.Plugin)
            {
                sb.AppendLine(item.IsActive ? $"*{item.DisplayName}" : item.DisplayName);
            }

            // Process children if any
            if (item.Children != null && item.Children.Any())
            {
                var plugins = item.Children.Where(c => c.EntityType == EntityType.Plugin);
                var groups = item.Children.Where(c => c.EntityType == EntityType.Group);

                // Append plugins first
                foreach (var plugin in plugins)
                {
                    AppendItemToStringBuilder(plugin, sb);
                }

                // Append groups
                foreach (var group in groups)
                {
                    var groupObject = ZO.LoadOrderManager.EntityTypeHelper.GetUnderlyingObject(group) as ModGroup;
                    if (groupObject != null)
                    {
                        if (groupObject.GroupID <= 0)
                        {
                            continue;
                        }
                        // Skip appending the root group itself but process its children
                        if (groupObject.GroupID != 1)
                        {
                            sb.AppendLine();
                            sb.AppendLine(groupObject.ToPluginsString());
                        }
                        AppendItemToStringBuilder(group, sb);
                    }
                }
            }
        }



        //public static void ProducePluginsTxt(LoadOrdersViewModel viewModel, string? outputFileName = null)
        //{
        //    // Ensure the ViewModel's Items collection is not empty
        //    if (viewModel.Items == null || viewModel.Items.Count == 0)
        //    {
        //        Console.WriteLine("No items in the ViewModel to produce plugins.txt.");
        //        return;
        //    }

        //    // Retrieve the active GroupSetID
        //    var thisGroupSetID = AggLoadInfo.Instance.ActiveGroupSet.GroupSetID;

        //    // Check if the root group (GroupID = 1) has any plugins in the current GroupSet
        //    var rootGroupPluginCount = AggLoadInfo.Instance.GroupSetPlugins.Items
        //        .Count(gsp => gsp.groupSetID == thisGroupSetID && gsp.groupID == 1);

        //    // Determine if the root group should be displayed
        //    bool boolDisplayRoot = rootGroupPluginCount > 0;

        //    // Determine the profile name and output file path
        //    var groupSetName = AggLoadInfo.Instance.ActiveGroupSet.GroupSetName ?? "Default_GroupSet";
        //    var loadOutName = AggLoadInfo.Instance.ActiveLoadOut.Name ?? "Default_Profile";
        //    var dateTimeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        //    var defaultFileName = $"Plugins_{groupSetName}_{loadOutName}.txt";
        //    var pluginsFilePath = outputFileName ?? Path.Combine(FileManager.GameLocalAppDataFolder, defaultFileName);

        //    var sb = new StringBuilder();

        //    // Custom header with the actual filename
        //    sb.AppendLine($"# {defaultFileName} produced by ZeeOgre's LoadOutManager using Group Set {groupSetName} and profile {loadOutName} on {dateTimeNow}");
        //    sb.AppendLine();

        //    // Track added plugins and groups to avoid duplication and log errors
        //    var addedPlugins = new HashSet<string>();
        //    var printedGroups = new HashSet<long>();

        //    // Iterate through the ViewModel's Items collection
        //    foreach (var item in viewModel.Items)
        //    {
        //        // Check if the item is a group
        //        if (item.EntityType == EntityType.Group)
        //        {
        //            // Check if this is the root group (Default group) and whether to display it
        //            if (item.GroupID == 1 && !boolDisplayRoot)
        //            {
        //                continue; // Skip the Default group if it shouldn't be displayed
        //            }

        //            // Retrieve the group from AggLoadInfo using the GroupID
        //            var group = AggLoadInfo.Instance.Groups.FirstOrDefault(g => g.GroupID == item.GroupID);
        //            if (group != null)
        //            {
        //                // Add a blank line before each group line for readability
        //                sb.AppendLine();

        //                var groupString = group.ToPluginsString();
        //                if (boolDisplayRoot)
        //                {
        //                    groupString = $"#{groupString}";
        //                }

        //                // Check if the group ID is already in the printedGroups set
        //                if (!printedGroups.Contains((long)group.GroupID))
        //                {
        //                    // Print the group line and track it
        //                    sb.AppendLine(groupString);
        //                    printedGroups.Add((long)group.GroupID);
        //                }
        //            }
        //        }
        //        else if (item.EntityType == EntityType.Plugin)
        //        {
        //            // Print the plugin name, prefixing with * if active
        //            var pluginName = $"{(item.IsActive ? "*" : string.Empty)}{item.PluginData?.PluginName ?? string.Empty}";

        //            // Check if the plugin ID is already in the addedPlugins set
        //            if (item.PluginData != null && !addedPlugins.Contains(item.PluginData.PluginName))
        //            {
        //                // Add the plugin name to the StringBuilder and track it
        //                sb.AppendLine(pluginName);
        //                addedPlugins.Add(item.PluginData.PluginName);
        //            }
        //        }
        //    }

        //    // Custom footer
        //    sb.AppendLine();
        //    sb.AppendLine($"# Group Set {groupSetName} : LoadOut {loadOutName} : {dateTimeNow}");

        //    // Write the content to the output file
        //    File.WriteAllText(pluginsFilePath, sb.ToString());
        //    Console.WriteLine($"Plugins.txt successfully written to: {pluginsFilePath}");
        //}
    }
}
