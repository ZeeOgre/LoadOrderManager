using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ZO.LoadOrderManager
{
    public static partial class FileManager
    {
        public static LoadOut ParsePluginsTxt(string pluginsFile, LoadOut? incomingLoadOut = null)
        {
            // Use AggLoadInfo to retrieve the default root group (GroupID 1)
            var defaultModGroup = AggLoadInfo.Instance.Groups.FirstOrDefault(g => g.GroupID == 1);
            if (defaultModGroup == null)
            {
#if WINDOWS
                App.LogDebug("Error: Default ModGroup (ID = 1) not found in AggLoadInfo.");
#endif
                throw new Exception("Default ModGroup not found in AggLoadInfo.");
            }

            var loadOut = incomingLoadOut ?? AggLoadInfo.Instance.LoadOuts.First(l => l.ProfileID == 1);
            
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
            foreach (var group in AggLoadInfo.Instance.Groups)
            {
                var maxGroupOrdinal = DbManager.GetNextOrdinal(EntityType.Group, group.GroupID);
                var maxPluginOrdinal = DbManager.GetNextOrdinal(EntityType.Plugin, group.GroupID);
                groupOrdinalTracker[group.GroupID] = maxGroupOrdinal; // Start from maxGroupOrdinal
                pluginOrdinalTracker[group.GroupID] = maxPluginOrdinal; // Start from maxPluginOrdinal
            }

            try
            {
                var lines = File.ReadAllLines(pluginsFile);

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
                            parentGroup = AggLoadInfo.Instance.Groups.FirstOrDefault(g => g.GroupID == 1);
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
                        if (!groupOrdinalTracker.ContainsKey(parentGroup.GroupID))
                        {
                            groupOrdinalTracker[parentGroup.GroupID] = 1; // Initialize ordinal for this parent group
                        }

                        // Check for existing group by name
                        var existingGroup = AggLoadInfo.Instance.Groups.FirstOrDefault(g => g.GroupName == groupName);
                        if (existingGroup != null)
                        {
                            // Update existing group
                            existingGroup.Description = groupDescription;
                            currentGroup = existingGroup;
                        }
                        else
                        {
                            // Create and write the new group to the database with the incremented ordinal
                            var newGroup = new ModGroup(parentGroup, groupDescription, groupName)
                            {
                                Ordinal = groupOrdinalTracker[parentGroup.GroupID] // Assign ordinal
                            };

                            // Write the new group to the database
                            var completeGroup = newGroup.WriteGroup();
                            if (completeGroup != null)
                            {
                                groupParentMapping[level] = completeGroup; // Set this group as the parent for the next level
                                currentGroup = completeGroup;

                                // Increment the ordinal for this parent group
                                groupOrdinalTracker[parentGroup.GroupID]++;

                                // Reset pluginOrdinal for the new group
                                if (!pluginOrdinalTracker.ContainsKey(currentGroup.GroupID))
                                {
                                    pluginOrdinalTracker[currentGroup.GroupID] = 1; // Initialize ordinal for this new group
                                }
#if WINDOWS
                                App.LogDebug($"Successfully added group: {newGroup.GroupName} to group {parentGroup.GroupName} with ordinal {newGroup.Ordinal}.");
#endif
                                // Add the new group to AggLoadInfo
                                //AggLoadInfo.Instance.Groups.Add(completeGroup);
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
                    var existingPlugin = AggLoadInfo.Instance.Plugins.FirstOrDefault(p => p.PluginName == pluginName);
                    if (existingPlugin != null)
                    {
                        // Update existing plugin
                        existingPlugin.GroupID = currentGroup.GroupID;
                        existingPlugin.GroupOrdinal = pluginOrdinalTracker[currentGroup.GroupID]; // Set the ordinal for the plugin
                    }
                    else
                    {
                        var plugin = new Plugin
                        {
                            PluginName = pluginName,
                            GroupID = currentGroup.GroupID,
                            GroupOrdinal = pluginOrdinalTracker[currentGroup.GroupID] // Set the ordinal for the plugin
                        };

                        // Write the plugin to the database
                        plugin.WriteMod();

                        // Increment the ordinal for the current group
                        pluginOrdinalTracker[currentGroup.GroupID]++;
#if WINDOWS
                        App.LogDebug($"Successfully added plugin: {plugin.PluginName} to group {currentGroup.GroupName} with Ordinal {plugin.GroupOrdinal}.");
#endif

                        var completePlugin = Plugin.LoadPlugin(plugin.PluginName);
                        if (completePlugin != null)
                        {
                            loadOut.Plugins.Add(new PluginViewModel
                            {
                                Plugin = completePlugin,
                                IsEnabled = isEnabled // Set the IsEnabled property based on the isEnabled variable
                            });

                            if (isEnabled)
                            {
                                enabledPlugins.Add(completePlugin.PluginID); // Mark the plugin as enabled
#if WINDOWS
                                App.LogDebug($"Plugin {completePlugin.PluginName} is enabled.");
#endif
                            }

                            // Add the new plugin to AggLoadInfo
                            AggLoadInfo.Instance.Plugins.Add(completePlugin);
                        }
                    }
                }

                // Update LoadOut plugins with the correct state
                //loadOut.Plugins.Clear();
                //foreach (int pluginID in enabledPlugins)
                //{
                //    var completePlugin = Plugin.LoadPlugin(pluginID); // Pass pluginID as an integer
                //    loadOut.Plugins.Add(new PluginViewModel
                //    {
                //        Plugin = completePlugin,
                //        IsEnabled = true // Ensure the plugin is enabled
                //    });
                //}

                // Save the loadout profile
                loadOut.WriteProfile();

                // Ensure the LoadOut is updated in AggLoadInfo.Instance.LoadOuts
                var existingLoadOut = AggLoadInfo.Instance.LoadOuts.FirstOrDefault(l => l.ProfileID == loadOut.ProfileID);
                if (existingLoadOut != null)
                {
                    AggLoadInfo.Instance.LoadOuts.Remove(existingLoadOut);
                }
                AggLoadInfo.Instance.LoadOuts.Add(loadOut);

            }
            catch (Exception ex)
            {
#if WINDOWS
                App.LogDebug($"Error parsing plugins file: {ex.Message}");
#endif
            }

            return loadOut;
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
            sb.AppendLine();

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