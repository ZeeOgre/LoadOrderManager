using System.IO;

namespace ZO.LoadOrderManager
{
    public static partial class FileManager
    {
        public static void ScanGameDirectoryForStrays()
        {
            App.LogDebug("Scan Game Directory For Strays");
            var gameFolder = FileManager.GameFolder;
            var dataFolder = Path.Combine(gameFolder, "data");
            var pluginFiles = Directory.GetFiles(dataFolder, "*.esp")
                .Concat(Directory.GetFiles(dataFolder, "*.esm"))
                .ToList();

            // Dictionary to track the highest ordinal for each group
            var groupOrdinalTracker = new Dictionary<int, int>();

            foreach (var pluginFile in pluginFiles)
            {
                var fileInfo = new System.IO.FileInfo(pluginFile);
                var pluginName = fileInfo.Name.ToLowerInvariant();
                var dtStamp = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");

                var existingPlugin = AggLoadInfo.Instance.Plugins.FirstOrDefault(p => p.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase));

                if (existingPlugin != null)
                {
                    // Update DTStamp for the existing plugin
                    existingPlugin.DTStamp = dtStamp;
                    existingPlugin.WriteMod();

                    // Update FileInfo record
                    var existingFileInfo = existingPlugin.Files.FirstOrDefault(f => f.Filename.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
                    if (existingFileInfo != null)
                    {
                        existingFileInfo.DTStamp = dtStamp;
                        existingFileInfo.HASH = ZO.LoadOrderManager.FileInfo.ComputeHash(fileInfo.FullName);
                        existingFileInfo.IsArchive = false;
                        ZO.LoadOrderManager.FileInfo.InsertFileInfo(existingFileInfo, existingPlugin.PluginID);
                    }
                }
                else
                {
                    // Determine the group ID for the new plugin
                    int groupId = -997; // Unassigned group

                    // Fetch or initialize the ordinal for the group
                    if (!groupOrdinalTracker.ContainsKey(groupId))
                    {
                        groupOrdinalTracker[groupId] = DbManager.GetNextOrdinal(EntityType.Plugin, groupId);
                    }

                    // Create a new Plugin object
                    var newPlugin = new Plugin
                    {
                        PluginName = pluginName,
                        DTStamp = dtStamp,
                        GroupID = groupId,
                        GroupOrdinal = groupOrdinalTracker[groupId] // Assign the next ordinal
                    };
                    newPlugin.WriteMod();
                    AggLoadInfo.Instance.Plugins.Add(newPlugin);

                    // Increment the ordinal for the group
                    groupOrdinalTracker[groupId]++;

                    // Insert new FileInfo record
                    var newFileInfo = new ZO.LoadOrderManager.FileInfo
                    {
                        Filename = pluginName,
                        DTStamp = dtStamp,
                        HASH = ZO.LoadOrderManager.FileInfo.ComputeHash(fileInfo.FullName),
                        IsArchive = false
                    };
                    ZO.LoadOrderManager.FileInfo.InsertFileInfo(newFileInfo, newPlugin.PluginID);
                }
            }
        }
    }
}
