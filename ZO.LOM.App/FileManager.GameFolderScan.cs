using System.IO;
using System.Windows;

namespace ZO.LoadOrderManager
{
    partial class FileManager
    {

        public static void ScanGameDirectoryForStrays(bool fullScan = true)
        {
            _ = Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await FileManager.ScanGameDirectoryForStraysAsync(fullScan);
            });
        }

        public static async Task ScanGameDirectoryForStraysAsync(bool fullScan = true)
        {
            App.LogDebug("Scan Game Directory For Strays");
            var gameFolder = FileManager.GameFolder;
            var dataFolder = Path.Combine(gameFolder, "data");
            var pluginFiles = Directory.GetFiles(dataFolder, "*.esp")
                .Concat(Directory.GetFiles(dataFolder, "*.esm"))
                .ToList();

            // Log start of scan and set the initial warning message
            LoadOrderWindow.Instance.LOWVM.SetWarning(fullScan
                ? "Full Scan Selected, this may take several minutes as the game folder has VERY large files..."
                : "Quick Scan Selected, starting scan...");

            // Load all known FileInfo objects with the GameFolder flag set
            var knownGameFolderFiles = ZO.LoadOrderManager.FileInfo.GetAllFiles()
                .ToDictionary(f => f.Filename, StringComparer.OrdinalIgnoreCase);

            // Dictionary to track the highest ordinal for each group
            var groupOrdinalTracker = new Dictionary<long, long>();

            // Precompute the ordinal for the -997 group
            long groupID = -997; // Unassigned group
            long groupSetID = 1; // Assign GroupSetID = 1 for Uncategorized group
            groupOrdinalTracker[groupID] = AggLoadInfo.GetNextPluginOrdinal(groupID, groupSetID);

            // HashSet to track processed filenames
            var processedFilenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int totalFiles = pluginFiles.Count;
            int currentFileIndex = 0;

            foreach (var pluginFile in pluginFiles)
            {
                currentFileIndex++;
                long progress = 1 + (98 * currentFileIndex / totalFiles);
                string pluginFileName = Path.GetFileName(pluginFile);
                LoadOrderWindow.Instance.LOWVM.SetWarning($"({currentFileIndex}/{totalFiles}) Adding file info for {pluginFileName}");

                var fileInfo = new System.IO.FileInfo(pluginFile);
                var pluginName = fileInfo.Name.ToLowerInvariant();

                // Skip if the filename has already been processed
                if (!processedFilenames.Add(pluginName))
                {
                    continue;
                }

                var dtStamp = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                string? newHash = null;
                if (fullScan) newHash = ZO.LoadOrderManager.FileInfo.ComputeHash(fileInfo.FullName);

                if (knownGameFolderFiles.TryGetValue(pluginName, out var existingFileInfo))
                {
                    var existingPlugin = AggLoadInfo.Instance.Plugins.FirstOrDefault(p => p.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
                    if (existingPlugin != null)
                    {
                        if (existingFileInfo.DTStamp == dtStamp && (!fullScan && newHash != String.Empty))
                        {
                            existingPlugin.DTStamp = dtStamp;
                            existingPlugin.State |= ModState.GameFolder;
                            _ = existingPlugin.WriteMod();

                            if (fullScan) existingFileInfo.HASH = newHash;
                            existingFileInfo.Flags = FileFlags.GameFolder;
                            existingFileInfo.AbsolutePath = fileInfo.FullName;
                            _ = ZO.LoadOrderManager.FileInfo.InsertFileInfo(existingFileInfo, existingPlugin.PluginID);
                        }
                        else
                        {
                            // DTStamp does not match, perform full update
                            existingPlugin.DTStamp = dtStamp;
                            existingPlugin.State |= ModState.GameFolder;
                            _ = existingPlugin.WriteMod();

                            existingFileInfo.DTStamp = dtStamp;
                            if (fullScan) existingFileInfo.HASH = newHash;
                            existingFileInfo.Flags = FileFlags.GameFolder;
                            existingFileInfo.AbsolutePath = fileInfo.FullName;
                            _ = ZO.LoadOrderManager.FileInfo.InsertFileInfo(existingFileInfo, existingPlugin.PluginID);
                        }

                        // Check for affiliated archives
                        AddAffiliatedFiles(fileInfo, existingPlugin.PluginID, fullScan);
                    }
                }
                else
                {
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
                    _ = newPlugin.WriteMod();
                    AggLoadInfo.Instance.Plugins.Add(newPlugin);

                    // Increment the ordinal for the group
                    groupOrdinalTracker[groupID]++;

                    // Insert new FileInfo record
                    var newFileInfo = new ZO.LoadOrderManager.FileInfo
                    {
                        Filename = pluginName,
                        DTStamp = dtStamp,
                        HASH = newHash,
                        Flags = FileFlags.GameFolder,
                        AbsolutePath = fileInfo.FullName
                    };
                    _ = ZO.LoadOrderManager.FileInfo.InsertFileInfo(newFileInfo, newPlugin.PluginID);

                    // Check for affiliated archives
                    AddAffiliatedFiles(fileInfo, newPlugin.PluginID, fullScan);
                }

                // Allow UI to update
                await Task.Delay(10);
            }

            LoadOrderWindow.Instance.LOWVM.ClearWarning(); // Clear the warning after scan completion
            App.LogDebug("Scan complete.");
        }

        private static void AddAffiliatedFiles(System.IO.FileInfo pluginFileInfo, long pluginId, bool fullScan)
        {
            var dataFolder = pluginFileInfo.DirectoryName;
            if (dataFolder == null) return;

            var baseFileName = Path.GetFileNameWithoutExtension(pluginFileInfo.Name);
            var ba2Files = Directory.GetFiles(dataFolder, $"{baseFileName}*.ba2");
            var iniFiles = Directory.GetFiles(dataFolder, $"{baseFileName}.ini");

            int totalAffiliatedFiles = ba2Files.Length + iniFiles.Length;
            int currentAffiliatedFileIndex = 0;

            foreach (var ba2File in ba2Files)
            {
                currentAffiliatedFileIndex++;
                string ba2FileName = Path.GetFileName(ba2File);
                LoadOrderWindow.Instance.LOWVM.SetWarning($"({currentAffiliatedFileIndex}/{totalAffiliatedFiles}) Adding affiliated file info for {ba2FileName}");

                string? newHash = null;
                if (fullScan) newHash = ZO.LoadOrderManager.FileInfo.ComputeHash(ba2File);

                var ba2FileInfo = new ZO.LoadOrderManager.FileInfo
                {
                    Filename = ba2FileName,
                    DTStamp = File.GetLastWriteTime(ba2File).ToString("yyyy-MM-dd HH:mm:ss"),
                    HASH = newHash,
                    Flags = FileFlags.IsArchive | FileFlags.GameFolder,
                    AbsolutePath = ba2File
                };
                _ = ZO.LoadOrderManager.FileInfo.InsertFileInfo(ba2FileInfo, pluginId);
            }

            foreach (var iniFile in iniFiles)
            {
                currentAffiliatedFileIndex++;
                string iniFileName = Path.GetFileName(iniFile);
                LoadOrderWindow.Instance.LOWVM.SetWarning($"({currentAffiliatedFileIndex}/{totalAffiliatedFiles}) Adding affiliated file info for {iniFileName} ");

                string? newHash = null;
                if (fullScan) newHash = ZO.LoadOrderManager.FileInfo.ComputeHash(iniFile);
                var iniFileInfo = new ZO.LoadOrderManager.FileInfo
                {
                    Filename = iniFileName,
                    DTStamp = File.GetLastWriteTime(iniFile).ToString("yyyy-MM-dd HH:mm:ss"),
                    HASH = newHash,
                    Flags = FileFlags.GameFolder,
                    AbsolutePath = iniFile
                };
                _ = ZO.LoadOrderManager.FileInfo.InsertFileInfo(iniFileInfo, pluginId);
            }
        }
    }
}
