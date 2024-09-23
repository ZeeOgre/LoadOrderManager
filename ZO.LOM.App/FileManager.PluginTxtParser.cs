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
            // Use the provided AggLoadInfo or the current singleton instance
            var aggLoadInfo = incomingAggLoadInfo ?? AggLoadInfo.Instance;

            // Use the provided plugins file path or the default path
            var pluginsFilePath = pluginsFile ?? FileManager.PluginsFile;

            // Use AggLoadInfo to retrieve the default root group (GroupID 1)
            var defaultModGroup = aggLoadInfo.Groups.FirstOrDefault(g => g.GroupID == 1);
            if (defaultModGroup == null)
            {
#if WINDOWS
                App.LogDebug("Error: Default ModGroup (ID = 1) not found in AggLoadInfo.");
#endif
                throw new Exception("Default ModGroup not found in AggLoadInfo.");
            }

            // Use the ActiveLoadOutID property to get the active loadout // these must be set in the AggLoadInfo object before trying to use this
            var loadOut = aggLoadInfo.ActiveLoadOut;
            var groupSet = aggLoadInfo.ActiveGroupSet;

            var enabledPlugins = new HashSet<int>(); // Using HashSet for faster lookups
            ModGroup currentGroup = defaultModGroup;

            // Dictionary to track parent-child relationships by level
            var groupParentMapping = new Dictionary<int, ModGroup>
                {
                    { 0, defaultModGroup } // The top-level root group is at level 0
                };

            // Dictionary to track ordinals for each parent group (for groups)
            var groupOrdinalTracker = new Dictionary<int, int>
                {
                    { 1, 1 }
                };

            // Dictionary to track ordinals for each group (for plugins)
            var pluginOrdinalTracker = new Dictionary<int, int>();

            // Consolidate initialization of group and plugin ordinals
            foreach (var group in aggLoadInfo.Groups)
            {
                var maxGroupOrdinal = DbManager.GetNextOrdinal(EntityType.Group, group.GroupID ?? 0);
                var maxPluginOrdinal = DbManager.GetNextOrdinal(EntityType.Plugin, group.GroupID ?? 0);
                groupOrdinalTracker[group.GroupID ?? 0] = maxGroupOrdinal; // Start from maxGroupOrdinal
                pluginOrdinalTracker[group.GroupID ?? 0] = maxPluginOrdinal; // Start from maxPluginOrdinal
            }

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
                        int level = line.TakeWhile(c => c == '#').Count(); // Count the # symbols to determine group level
                        string groupInfo = line.Substring(level).Trim();
                        string[] groupParts = groupInfo.Split(new[] { "@@" }, StringSplitOptions.None);
                        string groupName = groupParts.Length > 0 ? groupParts[0].Trim() : string.Empty;
                        string groupDescription = groupParts.Length > 1 ? groupParts[1].Trim() : string.Empty;

                        // Determine the parent group based on the current level
                        ModGroup parentGroup;
                        if (level == 3)
                        {
                            // For level 3, the parent group is always the root group (GroupID 1)
                            parentGroup = aggLoadInfo.Groups.FirstOrDefault(g => g.GroupID == 1) ?? throw new Exception("Parent group not found.");
                        }
                        else
                        {
                            parentGroup = groupParentMapping[level - 1];
                        }

                        if (parentGroup == null)
                        {
#if WINDOWS
                            App.LogDebug($"Error: No parent group found for level {level - 1}. Skipping group {groupName}.");
#endif
                            continue;
                        }

                        // Fetch or initialize the ordinal for the parent group
                        if (!groupOrdinalTracker.ContainsKey(parentGroup.GroupID ?? 0))
                        {
                            groupOrdinalTracker[parentGroup.GroupID ?? 0] = 1; // Initialize ordinal for this parent group
                        }

                        // Check for existing group by name
                        var existingGroup = aggLoadInfo.Groups.FirstOrDefault(g => g.GroupName == groupName);
                        if (existingGroup != null)
                        {
                            // Check if the group is a member of GroupSet 1 and is not read-only

                            if (existingGroup.GroupSetID == groupSet.GroupSetID)
                            {
                                // Update existing group
                                existingGroup.Description = groupDescription;
                                currentGroup = existingGroup;
                            }
                            else
                            {
                                // Clone the existing group and update the GroupSetID
                                var newGroup = existingGroup.Clone(groupSet);
                                newGroup.Description = groupDescription;

                                // Write the new group to the database
                                var completeGroup = newGroup.WriteGroup();
                                if (completeGroup != null)
                                {
                                    groupParentMapping[level] = completeGroup; // Set this group as the parent for the next level
                                    currentGroup = completeGroup;

                                    // Increment the ordinal for this parent group
                                    groupOrdinalTracker[parentGroup.GroupID ?? 0]++;

                                    // Reset pluginOrdinal for the new group
                                    if (!pluginOrdinalTracker.ContainsKey(currentGroup.GroupID ?? 0))
                                    {
                                        pluginOrdinalTracker[currentGroup.GroupID ?? 0] = 1; // Initialize ordinal for this new group
                                    }
#if WINDOWS
                                    App.LogDebug($"Successfully added group: {newGroup.GroupName} to group {parentGroup.GroupName} with ordinal {newGroup.Ordinal}.");
#endif
                                    // Add the new group to AggLoadInfo
                                    //aggLoadInfo.Groups.Add(completeGroup);
                                }
                                else
                                {
#if WINDOWS
                                    App.LogDebug($"Error: Failed to write group: {groupName}.");
#endif
                                }
                            }

                        }
                        else
                        {
                            // Create and write the new group to the database with the incremented ordinal
                            var newGroup = new ModGroup(parentGroup, groupDescription, groupName, groupSet.GroupSetID, groupOrdinalTracker[parentGroup.GroupID ?? 0]);
                     

                            // Write the new group to the database
                            var completeGroup = newGroup.WriteGroup();
                            if (completeGroup != null)
                            {
                                groupParentMapping[level] = completeGroup; // Set this group as the parent for the next level
                                currentGroup = completeGroup;

                                // Increment the ordinal for this parent group
                                groupOrdinalTracker[parentGroup.GroupID ?? 0]++;

                                // Reset pluginOrdinal for the new group
                                if (!pluginOrdinalTracker.ContainsKey(currentGroup.GroupID ?? 0))
                                {
                                    pluginOrdinalTracker[currentGroup.GroupID ?? 0] = 1; // Initialize ordinal for this new group
                                }
#if WINDOWS
                                App.LogDebug($"Successfully added group: {newGroup.GroupName} to group {parentGroup.GroupName} with ordinal {newGroup.Ordinal}.");
#endif
                                // Add the new group to AggLoadInfo
                                //aggLoadInfo.Groups.Add(completeGroup);
                            }
                            else
                            {
#if WINDOWS
                                App.LogDebug($"Error: Failed to write group: {groupName}.");
#endif
                            }
                        }

                        continue;
                    }

                    // Handle plugin lines (e.g., *pluginName.esm)
                    bool isEnabled = line.StartsWith("*");
                    string pluginName = line.TrimStart('*').Trim();

                    // Check for existing plugin by name
                    var existingPlugin = aggLoadInfo.Plugins.FirstOrDefault(p => p.PluginName == pluginName);
                    if (existingPlugin != null)
                    {
                        // Update existing plugin
                        existingPlugin.GroupID = currentGroup.GroupID;
                        existingPlugin.GroupOrdinal = pluginOrdinalTracker[currentGroup.GroupID ?? 0]; // Set the ordinal for the plugin
                    }
                    else
                    {
                        var plugin = new Plugin
                        {
                            PluginName = pluginName,
                            GroupID = currentGroup.GroupID,
                            GroupOrdinal = pluginOrdinalTracker[currentGroup.GroupID ?? 0] // Set the ordinal for the plugin
                        };

                        // Write the plugin to the database
                        plugin.WriteMod();

                        // Increment the ordinal for the current group
                        pluginOrdinalTracker[currentGroup.GroupID ?? 0]++;

                        App.LogDebug($"Successfully added plugin: {plugin.PluginName} to group {currentGroup.GroupName} with Ordinal {plugin.GroupOrdinal}.");
                        var completePlugin = Plugin.LoadPlugin(modName: plugin.PluginName);
                        if (completePlugin != null)
                        {
                            loadOut.Plugins.Add(new PluginViewModel(completePlugin, loadOut)); // Call the constructor with parameters

                            if (isEnabled)
                            {
                                enabledPlugins.Add(completePlugin.PluginID); // Mark the plugin as enabled
#if WINDOWS
                                App.LogDebug($"Plugin {completePlugin.PluginName} is enabled.");
#endif
                            }

                            // Add the new plugin to AggLoadInfo
                            aggLoadInfo.Plugins.Add(completePlugin);
                        }
                    }
                }

                // Save the loadout profile and get the ProfileID
                var profileID = loadOut.WriteProfile();

                // Ensure the LoadOut is updated in AggLoadInfo.Instance.LoadOuts
                var existingLoadOut = aggLoadInfo.LoadOuts.FirstOrDefault(l => l.ProfileID == loadOut.ProfileID);
                if (existingLoadOut != null)
                {
                    aggLoadInfo.LoadOuts.Remove(existingLoadOut);
                }
                aggLoadInfo.LoadOuts.Add(loadOut);

            }
            catch (Exception ex)
            {
#if WINDOWS
                App.LogDebug($"Error parsing plugins file: {ex.Message}");
#endif
            }

            return aggLoadInfo;
        }

        public static void ProducePluginsTxt(LoadOut incomingLoadOut, string? outputFileName = null)
        {
            var loadOut = incomingLoadOut;
            var profileName = loadOut.Name;
            var dateTimeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var defaultFileName = $"profile_{profileName}.txt";
            var fileName = outputFileName ?? Path.Combine(FileManager.AppDataFolder, defaultFileName);
            var sb = new StringBuilder();

            // Header
            sb.AppendLine($"# Plugin.txt produced by ZeeOgre's LoadOutManager using profile {profileName} on {dateTimeNow}");
            sb.AppendLine();

            // Groups and Plugins
            var groups = AggLoadInfo.Instance.Groups
                .Where(g => g.GroupID > 1) // Exclude reserved groups and root
                .OrderBy(g => g.Ordinal)
                .ToList();

            foreach (var group in groups)
            {
                // Append group header
                sb.AppendLine(group.ToPluginsString());
                //sb.AppendLine();

                var pluginsInGroup = loadOut.Plugins
                    .Where(p => p.Plugin.GroupID == group.GroupID)
                    .OrderBy(p => p.Plugin.GroupOrdinal)
                    .ToList();

                foreach (var pluginViewModel in pluginsInGroup)
                {
                    var pluginLine = pluginViewModel.IsEnabled ? "*" + pluginViewModel.Plugin.PluginName : pluginViewModel.Plugin.PluginName;
                    sb.AppendLine(pluginLine);
                }

                sb.AppendLine();
            }

            // Summary
            sb.AppendLine($"# {profileName} : {dateTimeNow}");

            // Write to file
            File.WriteAllText(fileName, sb.ToString());
#if WINDOWS
            App.LogDebug($"Plugins file '{fileName}' created successfully.");
#endif
        }
    }


}