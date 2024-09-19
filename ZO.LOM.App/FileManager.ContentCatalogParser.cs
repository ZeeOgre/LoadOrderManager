using Newtonsoft.Json.Linq;
using System.IO;

namespace ZO.LoadOrderManager
{
    public static partial class FileManager
    {
        public static void ParseContentCatalogTxt(string? contentCatalogFile = null)
        {
            App.LogDebug("Parse Content Catalog Txt");
            if (string.IsNullOrEmpty(contentCatalogFile))
            {
                contentCatalogFile = ContentCatalogFile;
            }

            var content = File.ReadAllText(contentCatalogFile);
            var json = JObject.Parse(content);

            // Initialize the ordinal for the Uncategorized group
            int groupId = -997; // Uncategorized group
            int groupOrdinal = DbManager.GetNextOrdinal(EntityType.Plugin, groupId);

            foreach (var property in json.Properties())
            {
                if (property.Name.StartsWith("TM_"))
                {
                    App.LogDebug($"Parsing property: {property.Name}");
                    var pluginData = property.Value;
                    var pluginName = (pluginData["Files"]?.First?.ToString().ToLowerInvariant());
                    App.LogDebug($"Plugin name: {pluginName}");
                    string bethesdaID = (property.Name.Substring(3)).ToLowerInvariant();

                    if (string.IsNullOrEmpty(pluginName))
                    {
                        continue;
                    }

                    var existingPlugin = AggLoadInfo.Instance.Plugins?.FirstOrDefault(p => p.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase));

                    // Extract and process the version string
                    var versionString = pluginData["Version"]?.ToString();
                    var versionParts = versionString?.Split('.') ?? Array.Empty<string>();
                    string dtStamp = DateTime.Now.ToString("o"); // Use ISO 8601 format
                    string version = string.Empty;

                    if (versionParts.Length > 0 && long.TryParse(versionParts[0], out long dtStampLong))
                    {
                        var dtStampDateTime = DateTimeOffset.FromUnixTimeSeconds(dtStampLong).DateTime;
                        if (dtStampDateTime > DateTime.Now)
                        {
                            dtStamp = DateTime.Now.ToString("o"); // Use ISO 8601 format
                        }
                        else
                        {
                            dtStamp = dtStampDateTime.ToString("o"); // Use ISO 8601 format
                        }

                        version = string.Join('.', versionParts.Skip(1));
                        if (version.Any(c => c < 32 || c > 126)) // Check for non-ASCII printable characters
                        {
                            version = string.Empty;
                        }
                    }

                    // Create FileInfo objects for each file in the Files array
                    var files = pluginData["Files"]?.Select(f => new FileInfo(f.ToString())).ToList() ?? new List<FileInfo>();

                    if (existingPlugin != null)
                    {
                        // Update existing plugin
                        existingPlugin.Achievements = pluginData["AchievementSafe"]?.ToString()?.ToLower() == "true";
                        existingPlugin.Description = pluginData["Title"]?.ToString().Trim();
                        existingPlugin.DTStamp = dtStamp;
                        existingPlugin.Version = version;
                        existingPlugin.BethesdaID = bethesdaID;
                        existingPlugin.Files = files;
                        existingPlugin.State |= ModState.Bethesda; // Set the Bethesda flag
                        existingPlugin.WriteMod();
                    }
                    else
                    {
                        // Add new plugin
                        var newPlugin = new Plugin
                        {
                            PluginName = pluginName,
                            Achievements = pluginData["AchievementSafe"]?.ToString()?.ToLower() == "true",
                            Description = pluginData["Title"]?.ToString().Trim(),
                            DTStamp = dtStamp,
                            BethesdaID = bethesdaID,
                            Version = version,
                            GroupID = groupId, // Assign to Uncategorized group by default
                            GroupOrdinal = groupOrdinal, // Assign the next ordinal
                            Files = files,
                            State = ModState.Bethesda // Set the Bethesda flag
                        };
                        newPlugin.WriteMod();
                        AggLoadInfo.Instance.Plugins.Add(newPlugin); // Add the new plugin to the singleton instance

                        // Increment the ordinal for the group
                        groupOrdinal++;
                    }
                }
            }
        }
    }
}
