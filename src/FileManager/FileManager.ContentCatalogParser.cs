using Newtonsoft.Json.Linq;
using System.IO;

namespace ZO.LoadOrderManager
{
    public static partial class FileManager
    {
        public static void ParseContentCatalogTxt(string? contentCatalogFile = null, long? groupSetID = null)
        {
            App.LogDebug("Parse Content Catalog Txt");
            if (string.IsNullOrEmpty(contentCatalogFile))
            {
                contentCatalogFile = ContentCatalogFile;
            }

            groupSetID ??= 1; // Set groupSetID to 1 if it is null

            try
            {
                var content = File.ReadAllText(contentCatalogFile);
                var json = JObject.Parse(content);

                // Initialize the ordinal for the Uncategorized group
                long groupID = -997; // Uncategorized group
                long groupOrdinal = DbManager.GetNextOrdinal(EntityType.Plugin, groupID, groupSetID.Value);
                foreach (var property in json.Properties())
                {
                    if (property.Name.StartsWith("TM_"))
                    {
                        App.LogDebug($"Parsing property: {property.Name}");
                        var pluginData = property.Value;
                        var pluginName = pluginData["Files"]?
                            .FirstOrDefault(file => file.ToString().ToLowerInvariant().EndsWith(".esm") || file.ToString().ToLowerInvariant().EndsWith(".esp"))?.ToString();

                        App.LogDebug($"Plugin name: {pluginName}");
                        string bethesdaID = (property.Name.Substring(3)).ToLowerInvariant();

                        var existingPlugin = AggLoadInfo.Instance.Plugins?.FirstOrDefault(p => string.Equals(p.PluginName, pluginName, StringComparison.OrdinalIgnoreCase));

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

                        // Create FileInfo objects for each file in the Files array, ensuring no duplicates
                        var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var files = pluginData["Files"]?.Select(f => f.ToString())
                            .Where(f => fileNames.Add(f)) // Add to HashSet and filter out duplicates
                            .Select(f => new FileInfo(f))
                            .ToList() ?? new List<FileInfo>();

                        if (existingPlugin != null)
                        {
                            // Update existing plugin
                            existingPlugin.Achievements = pluginData["AchievementSafe"]?.ToString()?.ToLower() == "true";
                            existingPlugin.Description = pluginData["Title"]?.ToString()?.Trim() ?? string.Empty;
                            existingPlugin.DTStamp = dtStamp;
                            existingPlugin.Version = version;
                            existingPlugin.BethesdaID = bethesdaID;
                            existingPlugin.Files = files;
                            existingPlugin.State |= ModState.Bethesda; // Set the Bethesda flag
                            _ = existingPlugin.WriteMod();
                        }
                        else
                        {
                            // Add new plugin
                            var newPlugin = new Plugin
                            {
                                PluginName = pluginName,
                                Achievements = pluginData["AchievementSafe"]?.ToString()?.ToLower() == "true",
                                Description = pluginData["Title"]?.ToString()?.Trim() ?? string.Empty,
                                DTStamp = dtStamp,
                                BethesdaID = bethesdaID,
                                Version = version,
                                GroupID = -997, // Assign to Uncategorized group by default -997
                                GroupSetID = groupSetID.Value, // Use the provided or default groupSetID
                                Files = files,
                                State = ModState.Bethesda, // Set the Bethesda flag,
                                GroupOrdinal = groupOrdinal
                            };
                            _ = newPlugin.WriteMod();
                            AggLoadInfo.Instance.Plugins?.Add(newPlugin); // Add the new plugin to the singleton instance
                        }
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                App.LogDebug($"Content catalog file not found: {contentCatalogFile}. Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                App.LogDebug($"An error occurred while parsing the content catalog file: {ex.Message}");
            }
        }
    }
}
