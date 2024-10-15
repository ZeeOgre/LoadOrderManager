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
                await FileManager.ScanGameDirectoryForStraysAsync(true);
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

            var loadingWindow = new LoadingWindow
            {
                Owner = Application.Current.MainWindow // Set the owner to the main window
            };
            loadingWindow.ShowInForeground();
            _ = loadingWindow.Activate(); // Ensure the window is brought to the foreground
            if (fullScan)
            {
                loadingWindow.UpdateProgress(1, "Full Scan Selected, this may take several minutes as the game folder has VERY large files...");
            }
            else
            {
                loadingWindow.UpdateProgress(1, "Quick Scan Selected, starting scan...");
            }

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
                loadingWindow.UpdateProgress(progress, $"Processing {Path.GetFileName(pluginFile)}...");

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

            loadingWindow.UpdateProgress(100, "Scan complete.");
            loadingWindow.Close();
        }

        private static void AddAffiliatedFiles(System.IO.FileInfo pluginFileInfo, long pluginId, bool fullScan)
        {
            var dataFolder = pluginFileInfo.DirectoryName;
            if (dataFolder == null) return;

            var baseFileName = Path.GetFileNameWithoutExtension(pluginFileInfo.Name);
            var ba2Files = Directory.GetFiles(dataFolder, $"{baseFileName}*.ba2");
            var iniFiles = Directory.GetFiles(dataFolder, $"{baseFileName}.ini");

            foreach (var ba2File in ba2Files)
            {

                string? newHash = null;
                if (fullScan) newHash = ZO.LoadOrderManager.FileInfo.ComputeHash(ba2File);

                var ba2FileInfo = new ZO.LoadOrderManager.FileInfo
                {
                    Filename = Path.GetFileName(ba2File),
                    DTStamp = File.GetLastWriteTime(ba2File).ToString("yyyy-MM-dd HH:mm:ss"),
                    HASH = newHash,
                    Flags = FileFlags.IsArchive | FileFlags.GameFolder,
                    AbsolutePath = ba2File
                };
                _ = ZO.LoadOrderManager.FileInfo.InsertFileInfo(ba2FileInfo, pluginId);
            }

            foreach (var iniFile in iniFiles)
            {
                string? newHash = null;
                if (fullScan) newHash = ZO.LoadOrderManager.FileInfo.ComputeHash(iniFile);
                var iniFileInfo = new ZO.LoadOrderManager.FileInfo
                {
                    Filename = Path.GetFileName(iniFile),
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
