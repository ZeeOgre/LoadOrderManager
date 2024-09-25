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

        public static void ProducePluginsTxt(LoadOut incomingLoadOut, string? outputFileName = null)
        {
            var loadOut = incomingLoadOut;
            var profileName = loadOut.Name;
            var dateTimeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var defaultFileName = $"profile_{profileName}.txt";
            var fileName = outputFileName ?? Path.Combine(FileManager.AppDataFolder, defaultFileName);
            var sb = new StringBuilder();

            sb.AppendLine($"# Plugin.txt produced by ZeeOgre's LoadOutManager using profile {profileName} on {dateTimeNow}");
            sb.AppendLine();

            var groups = AggLoadInfo.Instance.Groups
                .Where(g => g.GroupID > 1)
                .OrderBy(g => g.Ordinal)
                .ToList();

            foreach (var group in groups)
            {
                sb.AppendLine(group.ToPluginsString());

                var pluginsInGroup = AggLoadInfo.Instance.Plugins
                    .Where(p => p.GroupID == group.GroupID)
                    .OrderBy(p => p.GroupOrdinal)
                    .ToList();

                foreach (var plugin in pluginsInGroup)
                {
                    var pluginLine = loadOut.IsPluginEnabled(plugin.PluginID) ? "*" + plugin.PluginName : plugin.PluginName;
                    sb.AppendLine(pluginLine);
                }

                sb.AppendLine();
            }

            sb.AppendLine($"# {profileName} : {dateTimeNow}");
            File.WriteAllText(fileName, sb.ToString());
        }
    }


}