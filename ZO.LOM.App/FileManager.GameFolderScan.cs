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
            var groupOrdinalTracker = new Dictionary<long, long>();

            // HashSet to track processed filenames
            var processedFilenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var pluginFile in pluginFiles)
            {
                var fileInfo = new System.IO.FileInfo(pluginFile);
                var pluginName = fileInfo.Name.ToLowerInvariant();

                // Skip if the filename has already been processed
                if (!processedFilenames.Add(pluginName))
                {
                    continue;
                }

                var dtStamp = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");

                var existingPlugin = AggLoadInfo.Instance.Plugins.FirstOrDefault(p => p.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase));

                if (existingPlugin != null)
                {
                    // Update DTStamp and set the installed flag for the existing plugin
                    existingPlugin.DTStamp = dtStamp;
                    existingPlugin.State |= ModState.GameFolder; // Set the installed flag
                    existingPlugin.WriteMod();

                    // Update FileInfo record
                    var existingFileInfo = existingPlugin.Files.FirstOrDefault(f => f.Filename.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
                    if (existingFileInfo != null)
                    {
                        existingFileInfo.DTStamp = dtStamp;
                        existingFileInfo.HASH = ZO.LoadOrderManager.FileInfo.ComputeHash(fileInfo.FullName);
                        existingFileInfo.Flags = FileFlags.GameFolder;
                        existingFileInfo.AbsolutePath = fileInfo.FullName;
                        ZO.LoadOrderManager.FileInfo.InsertFileInfo(existingFileInfo, existingPlugin.PluginID);

                        // Check for affiliated archives
                        AddAffiliatedFiles(fileInfo, existingPlugin.PluginID);
                    }
                }
                else
                {
                    // Determine the group ID for the new plugin
                    long groupID = -997; // Unassigned group
                    long groupSetID = 1; // Assign GroupSetID = 1 for Uncategorized group

                    // Fetch or initialize the ordinal for the group
                    if (!groupOrdinalTracker.ContainsKey(groupID))
                    {
                        groupOrdinalTracker[groupID] = AggLoadInfo.GetNextPluginOrdinal(groupID, groupSetID);
                    }

                    // Create a new Plugin object
                    var newPlugin = new Plugin
                    {
                        PluginName = pluginName,
                        DTStamp = dtStamp,
                        GroupID = groupID,
                        GroupSetID = groupSetID,
                        GroupOrdinal = groupOrdinalTracker[groupID], // Assign the next ordinal
                        State = ModState.GameFolder // Set the installed flag
                    };
                    newPlugin.WriteMod();
                    AggLoadInfo.Instance.Plugins.Add(newPlugin);

                    // Increment the ordinal for the group
                    groupOrdinalTracker[groupID]++;

                    // Insert new FileInfo record
                    var newFileInfo = new ZO.LoadOrderManager.FileInfo
                    {
                        Filename = pluginName,
                        DTStamp = dtStamp,
                        HASH = ZO.LoadOrderManager.FileInfo.ComputeHash(fileInfo.FullName),
                        Flags = FileFlags.GameFolder,
                        AbsolutePath = fileInfo.FullName
                    };
                    ZO.LoadOrderManager.FileInfo.InsertFileInfo(newFileInfo, newPlugin.PluginID);

                    // Check for affiliated archives
                    AddAffiliatedFiles(fileInfo, newPlugin.PluginID);
                }
            }
        }

        private static void AddAffiliatedFiles(System.IO.FileInfo pluginFileInfo, long pluginId)
        {
            var dataFolder = pluginFileInfo.DirectoryName;
            if (dataFolder == null) return;

            var baseFileName = Path.GetFileNameWithoutExtension(pluginFileInfo.Name);
            var ba2Files = Directory.GetFiles(dataFolder, $"{baseFileName}*.ba2");
            var iniFiles = Directory.GetFiles(dataFolder, $"{baseFileName}.ini");

            foreach (var ba2File in ba2Files)
            {
                var ba2FileInfo = new ZO.LoadOrderManager.FileInfo
                {
                    Filename = Path.GetFileName(ba2File),
                    DTStamp = File.GetLastWriteTime(ba2File).ToString("yyyy-MM-dd HH:mm:ss"),
                    HASH = ZO.LoadOrderManager.FileInfo.ComputeHash(ba2File),
                    Flags = FileFlags.IsArchive | FileFlags.GameFolder,
                    AbsolutePath = ba2File
                };
                ZO.LoadOrderManager.FileInfo.InsertFileInfo(ba2FileInfo, pluginId);
            }

            foreach (var iniFile in iniFiles)
            {
                var iniFileInfo = new ZO.LoadOrderManager.FileInfo
                {
                    Filename = Path.GetFileName(iniFile),
                    DTStamp = File.GetLastWriteTime(iniFile).ToString("yyyy-MM-dd HH:mm:ss"),
                    HASH = ZO.LoadOrderManager.FileInfo.ComputeHash(iniFile),
                    Flags = FileFlags.GameFolder,
                    AbsolutePath = iniFile
                };
                ZO.LoadOrderManager.FileInfo.InsertFileInfo(iniFileInfo, pluginId);
            }
        }
    }
}
