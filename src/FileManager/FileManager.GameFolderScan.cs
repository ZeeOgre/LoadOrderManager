using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Formats.Tar;
using System.Data.SQLite;

namespace ZO.LoadOrderManager
{
    partial class FileManager
    {
        private static bool _quiet = false;
        private static int _scanInProgress;

        //public static void ScanGameDirectoryForStrays(bool fullScan = true, long? groupSetID = null)
        //{
        //    _ = Application.Current.Dispatcher.InvokeAsync(async () =>
        //    {
        //        await FileManager.ScanGameDirectoryForStraysAsync(fullScan);
        //    });
        //}

        public static void MWMessage(string message, bool isUpdateStatus)
        {

            if (_quiet) return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (isUpdateStatus)
                {
                    LoadOrderWindow.Instance.LOWVM.UpdateStatus(message);
                }
                else
                {
                    LoadOrderWindow.Instance.LOWVM.SetWarning(message);
                }
            });
        }

        public static void MWClear()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LoadOrderWindow.Instance.LOWVM.ClearWarning();
            });
        }


        //public static async Task ScanGameDirectoryForStraysAsync(bool fullScan = true, long? groupSetID = null)
       public static void ScanGameDirectoryForStrays(bool fullScan = true, long? groupSetID = null, bool quiet = false)
        {
            if (Interlocked.Exchange(ref _scanInProgress, 1) != 0)
            {
                App.LogDebug("Skipped duplicate GameFolder scan request because a scan is already active.");
                return;
            }

            try
            {
            _quiet = quiet;
            MWMessage("Clearing all known file information", false); 
            if (GameFolderContext.Active == null) throw new InvalidOperationException("No game folder is selected.");
            var activeGameFolder = GameFolderContext.Active;
            if (InitializationManager.IsAnyInitializing()) InitializationManager.ReportScanContext(activeGameFolder);
            RemoveMissingGameFolderFiles(activeGameFolder.GameFolderID);
            
            MWMessage("Scanning Game Folder and computing hashes, please wait",true);
            App.LogDebug("Scan Game Directory For Strays");
            var dataFolder = activeGameFolder.DataFolder;
            var pluginFiles = Directory.GetFiles(dataFolder, "*.esp")
                .Concat(Directory.GetFiles(dataFolder, "*.esm"))
                .ToList();

            // Log start of scan and set the initial warning message
            MWMessage(fullScan
                ? "Full Scan Selected, this may take several minutes as the game folder has VERY large files..."
                : "Quick Scan Selected, starting scan...",false);


            MWMessage(fullScan 
                ? "Full Scan Selected, this may take several minutes as the game folder has VERY large files..."
                : "Quick Scan Selected, starting scan...",true);

            // Load all known FileInfo objects with the GameFolder flag set
            var knownGameFolderFiles = ZO.LoadOrderManager.FileInfo.GetAllFiles(activeGameFolder.GameFolderID)
    .GroupBy(f => f.Filename, StringComparer.OrdinalIgnoreCase)
    .Select(g => g.First())
    .ToDictionary(f => f.Filename, StringComparer.OrdinalIgnoreCase);

            // Dictionary to track the highest ordinal for each group
            var groupOrdinalTracker = new Dictionary<long, long>();

            // Precompute the ordinal for the -997 group
            long groupID = -997; // Unassigned group
            groupSetID = groupSetID ?? 1; // Assign GroupSetID = 1 for Uncategorized group only when the incoming value is null
            groupOrdinalTracker[groupID] = AggLoadInfo.GetNextPluginOrdinal(groupID, groupSetID);

            // HashSet to track processed filenames
            var processedFilenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int totalFiles = pluginFiles.Count;
            int currentFileIndex = 0;


            foreach (var pluginFile in pluginFiles)
            {
                currentFileIndex++;
                
                string pluginFileName = Path.GetFileName(pluginFile);
                MWMessage($"({currentFileIndex}/{totalFiles}) Adding file info for {pluginFileName}",false);

                long progress = (long)(100 * ((double)currentFileIndex / totalFiles));
                if (InitializationManager.IsAnyInitializing()) InitializationManager.ReportProgress(progress, $"({currentFileIndex}/{totalFiles}) Adding file info for {pluginFileName}");
                var fileInfo = new System.IO.FileInfo(pluginFile);
                var pluginName = fileInfo.Name.ToLowerInvariant();

                // Skip if the filename has already been processed
                if (!processedFilenames.Add(pluginName))
                {
                    continue;
                }

                var dtStamp = fileInfo.LastWriteTime.ToString("o");
                string? newHash = null;
        
                

                if (knownGameFolderFiles.TryGetValue(pluginName, out var existingFileInfo))
                {
                    
                    var existingPlugin = AggLoadInfo.Instance.Plugins.FirstOrDefault(p => p.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
                    if (existingPlugin != null)
                    {

                        existingPlugin.DTStamp = dtStamp;
                        existingPlugin.InGameFolder = true;
                        _ = existingPlugin.WriteMod();

                        ZO.LoadOrderManager.FileInfo.RefreshFingerprint(existingFileInfo, fileInfo, hashWhenChanged: true);
                        existingFileInfo.Flags |= FileFlags.GameFolder;
                        existingFileInfo.AbsolutePath = fileInfo.FullName;
                        existingFileInfo.RelativePath = Path.GetRelativePath(dataFolder, fileInfo.FullName);
                        existingFileInfo.GameFolderID = activeGameFolder.GameFolderID;
                        _ = ZO.LoadOrderManager.FileInfo.InsertFileInfo(existingFileInfo, existingPlugin.PluginID);
                       

                        // Check for affiliated archives
                        AddAffiliatedFiles(fileInfo, existingPlugin.PluginID, hashChangedFiles: true);
                        AggLoadInfo.Instance.UpdatePlugin(existingPlugin);
                    }
                }
                else
                {
                    newHash = ZO.LoadOrderManager.FileInfo.ComputeHash(fileInfo.FullName);
                    // Create a new Plugin object
                    var newPlugin = new Plugin
                    {
                        PluginName = pluginName,
                        DTStamp = dtStamp,
                        GroupID = groupID,
                        GroupSetID = groupSetID,
                        GroupOrdinal = groupOrdinalTracker[groupID], // Assign the next ordinal
                        InGameFolder = true // Set the installed flag
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
                        Flags = FileFlags.GameFolder | FileFlags.Plugin,
                        AbsolutePath = fileInfo.FullName,
                        RelativePath = Path.GetRelativePath(dataFolder, fileInfo.FullName),
                        GameFolderID = activeGameFolder.GameFolderID
                    };
                    newFileInfo.FileSize = fileInfo.Length;
                    newFileInfo.LastWriteTimeUtcTicks = fileInfo.LastWriteTimeUtc.Ticks;
                    //newFileInfo.Flags &= ~FileFlags.IsArchive;
                    _ = ZO.LoadOrderManager.FileInfo.InsertFileInfo(newFileInfo, newPlugin.PluginID);

                    // Check for affiliated archives
                    AddAffiliatedFiles(fileInfo, newPlugin.PluginID, hashChangedFiles: true);
                    
                }

                // Allow UI to update


                //await Task.Delay(10);

                _quiet = false;
            }

            GameFolderContext.RefreshPresence();

            Application.Current.Dispatcher.Invoke(() =>
            {
                LoadOrderWindow.Instance.LOWVM.UpdateStatus("Completed Filescan");
                MWClear();
                LoadOrderWindow.Instance.LOWVM.LoadOrders.RefreshData(); // Clear the warning after scan completion
            });

            //App.RestartDialog("Finished loading all files from the game folder. Please restart the application to see the changes.");

            App.LogDebug("Scan complete.");
            }
            finally
            {
                Interlocked.Exchange(ref _scanInProgress, 0);
            }
        }

        public static void AddAffiliatedFiles(System.IO.FileInfo pluginFileInfo, long pluginId, bool hashChangedFiles)
        {
            var dataFolder = pluginFileInfo.DirectoryName;
            if (dataFolder == null) return;

            var baseFileName = Path.GetFileNameWithoutExtension(pluginFileInfo.Name);
            var ba2Files = Directory.GetFiles(dataFolder, "*.ba2")
                .Where(path => Path.GetFileName(path).StartsWith($"{baseFileName} - ", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var iniFiles = Directory.GetFiles(dataFolder, $"{baseFileName}.ini");

            int totalAffiliatedFiles = ba2Files.Length + iniFiles.Length;
            int currentAffiliatedFileIndex = 0;

            foreach (var ba2File in ba2Files)
            {
                currentAffiliatedFileIndex++;
                string ba2FileName = Path.GetFileName(ba2File);
                MWMessage($"({currentAffiliatedFileIndex}/{totalAffiliatedFiles}) Adding affiliated file info for {ba2FileName}",false);

                var physicalFile = new System.IO.FileInfo(ba2File);
                var existing = ZO.LoadOrderManager.FileInfo.GetAllFiles(GameFolderContext.Active!.GameFolderID)
                    .FirstOrDefault(item => item.Filename.Equals(ba2FileName, StringComparison.OrdinalIgnoreCase));
                string? newHash = existing?.HASH;
                if (existing != null)
                {
                    ZO.LoadOrderManager.FileInfo.RefreshFingerprint(existing, physicalFile, hashChangedFiles);
                    newHash = existing.HASH;
                }
                else if (hashChangedFiles)
                    newHash = ZO.LoadOrderManager.FileInfo.ComputeHash(ba2File);

                var ba2FileInfo = new ZO.LoadOrderManager.FileInfo
                {
                    Filename = ba2FileName,
                    DTStamp = physicalFile.LastWriteTime.ToString("o"),
                    HASH = newHash,
                    Flags = FileFlags.IsArchive | FileFlags.GameFolder ,
                    AbsolutePath = ba2File,
                    RelativePath = Path.GetRelativePath(dataFolder, ba2File),
                    GameFolderID = GameFolderContext.Active!.GameFolderID,
                    FileSize = physicalFile.Length,
                    LastWriteTimeUtcTicks = physicalFile.LastWriteTimeUtc.Ticks
                };
                //newFileInfo.Flags &= ~FileFlags.IsPlugin;
                _ = ZO.LoadOrderManager.FileInfo.InsertFileInfo(ba2FileInfo, pluginId);
                //await Task.Delay(10);

            }

            foreach (var iniFile in iniFiles)
            {
                currentAffiliatedFileIndex++;
                string iniFileName = Path.GetFileName(iniFile);
                MWMessage($"({currentAffiliatedFileIndex}/{totalAffiliatedFiles}) Adding affiliated file info for {iniFileName}", false);

                var physicalFile = new System.IO.FileInfo(iniFile);
                var existing = ZO.LoadOrderManager.FileInfo.GetAllFiles(GameFolderContext.Active!.GameFolderID)
                    .FirstOrDefault(item => item.Filename.Equals(iniFileName, StringComparison.OrdinalIgnoreCase));
                string? newHash = existing?.HASH;
                if (existing != null)
                {
                    ZO.LoadOrderManager.FileInfo.RefreshFingerprint(existing, physicalFile, hashChangedFiles);
                    newHash = existing.HASH;
                }
                else if (hashChangedFiles)
                    newHash = ZO.LoadOrderManager.FileInfo.ComputeHash(iniFile);
                var iniFileInfo = new ZO.LoadOrderManager.FileInfo
                {
                    Filename = iniFileName,
                    DTStamp = physicalFile.LastWriteTime.ToString("o"),
                    HASH = newHash,
                    Flags = FileFlags.Config | FileFlags.GameFolder,
                    AbsolutePath = iniFile,
                    RelativePath = Path.GetRelativePath(dataFolder, iniFile),
                    GameFolderID = GameFolderContext.Active!.GameFolderID,
                    FileSize = physicalFile.Length,
                    LastWriteTimeUtcTicks = physicalFile.LastWriteTimeUtc.Ticks
                };
                _ = ZO.LoadOrderManager.FileInfo.InsertFileInfo(iniFileInfo, pluginId);
            }
        }

        public static void RemoveMissingGameFolderFiles(long gameFolderID)
        {
            var missingFileIds = ZO.LoadOrderManager.FileInfo.GetAllFiles(gameFolderID)
                .Where(file => string.IsNullOrWhiteSpace(file.AbsolutePath) || !File.Exists(file.AbsolutePath))
                .Select(file => file.FileID)
                .ToArray();

            if (missingFileIds.Length == 0)
            {
                return;
            }

            // Use the DbManager singleton to get the database connection
            using (var connection = DbManager.Instance.GetConnection())
            {

                string updateFileInfoSql = "DELETE FROM FileInfo WHERE GameFolderID = @GameFolderID AND FileID = @FileID";

                foreach (long fileId in missingFileIds)
                {
                    using var command = new SQLiteCommand(updateFileInfoSql, connection);
                    command.Parameters.AddWithValue("@GameFolderID", gameFolderID);
                    command.Parameters.AddWithValue("@FileID", fileId);
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
            GameFolderContext.RefreshPresence();
        }

        public static void ResetPluginStatesAndFileFlags(long gameFolderID) => RemoveMissingGameFolderFiles(gameFolderID);
    }
}
