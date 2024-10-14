using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;
using System.Windows;

namespace ZO.LoadOrderManager
{
    public static partial class FileManager
    {
        public static readonly string PluginsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "starfield", "plugins.txt");
        public static readonly string ContentCatalogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "starfield", "contentcatalog.txt");
        public static string AppDataFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZeeOgre", "LoadOrderManager");
        public static string GameFolder => Config.Instance.GameFolder;
        public static string GameDocsFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Starfield");
        public static string GameSaveFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Starfield", "Saves");
        public static string GameLocalAppDataFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "starfield");

        private static bool _initialized = false;
        private static readonly object _lock = new object();

        public static void Initialize()
        {
            if (_initialized)
            {
                App.LogDebug("FileManager: Already initialized.");
                return;
            }

            lock (_lock)
            {
                if (_initialized)
                {
                    App.LogDebug("FileManager: Already initialized inside lock.");
                    return;
                }

                try
                {
                    InitializationManager.StartInitialization(nameof(FileManager));
                    App.LogDebug("FileManager: Starting initialization...");
                    FileMonitor.InitializeAllMonitors();
                    InitializeFavoriteGroupSetAndLoadOut();
                    //// Retrieve the singleton GroupSet and LoadOut from the database
                    //var singletonGroupSet = GroupSet.LoadGroupSet(2);
                    //var singletonLoadOut = LoadOut.Load(2);

                    //AggLoadInfo.Instance.ActiveLoadOut = singletonLoadOut;
                    //AggLoadInfo.Instance.ActiveGroupSet = singletonGroupSet;

                    // Load data from the database INTO the AggLoadInfo instance
                    AggLoadInfo.Instance.InitFromDatabase();
                    FileMonitor.InitializeAllMonitors();
                    // Check if files have already been loaded
                    if (AggLoadInfo.Instance.ActiveGroupSet.AreFilesLoaded)
                    {
                        App.LogDebug("FileManager: Files have already been loaded. Skipping file initialization.");
                        _initialized = true;
                        return;
                    }

                    // Update the flags to indicate the singleton is ready to load
                    AggLoadInfo.Instance.ActiveGroupSet.GroupSetFlags |= GroupFlags.ReadyToLoad;
                    AggLoadInfo.Instance.ActiveGroupSet.SaveGroupSet();

                    FileManager.ParsePluginsTxt(AggLoadInfo.Instance, PluginsFile);
                    InitializationManager.ReportProgress(83, "Plugins parsed");

                    FileManager.ParseContentCatalogTxt();
                    InitializationManager.ReportProgress(84, "Content catalog parsed");


                    //FileManager.ScanGameDirectoryForStraysAsync();
                    FileManager.ScanGameDirectoryForStrays(true);

                    FileManager.MarkLoadOutComplete(AggLoadInfo.Instance);
                    InitializationManager.ReportProgress(85, "LoadOut marked complete");


                    

                    _initialized = true;
                    App.LogDebug("FileManager: Initialization completed successfully.");
                }
                catch (Exception ex)
                {
                    App.LogDebug($"FileManager: Exception during initialization: {ex.Message}");
                    throw;
                }
                finally
                {
                    InitializationManager.EndInitialization(nameof(FileManager));
                }
            }
        }

        public static GroupSet? FindFavoriteGroupSet()
        {
            var groupSets = AggLoadInfo.LoadGroupSetsFromDatabase();
            AggLoadInfo.GroupSets = new ObservableCollection<GroupSet>(groupSets);
            return groupSets.FirstOrDefault(gs => gs.IsFavorite);
        }

        public static LoadOut? FindFavoriteLoadOut(GroupSet groupSet)
        {
            var loadOuts = GroupSet.GetAllLoadOuts(groupSet.GroupSetID);
            AggLoadInfo.Instance.LoadOuts = new ObservableCollection<LoadOut>(loadOuts);
            return loadOuts.FirstOrDefault(lo => lo.IsFavorite);
        }

        public static void InitializeFavoriteGroupSetAndLoadOut()
        {
            var favoriteGroupSet = FindFavoriteGroupSet();
            var favoriteLoadOut = favoriteGroupSet != null ? FindFavoriteLoadOut(favoriteGroupSet) : null;

            if (favoriteGroupSet == null || favoriteLoadOut == null)
            {
                // Default to GroupSet and LoadOut with IDs 2
                favoriteGroupSet = GroupSet.LoadGroupSet(2);
                favoriteLoadOut = LoadOut.Load(2);
            }

            AggLoadInfo.Instance.ActiveGroupSet = favoriteGroupSet;
            AggLoadInfo.Instance.ActiveLoadOut = favoriteLoadOut;
            
        }

        public static void MarkLoadOutComplete(AggLoadInfo aggLoadInfo)
        {
            var activeGroupSet = aggLoadInfo.ActiveGroupSet;
            if (activeGroupSet != null)
            {
                activeGroupSet.GroupSetFlags |= GroupFlags.FilesLoaded;
                activeGroupSet.GroupSetFlags &= ~GroupFlags.ReadyToLoad;
                activeGroupSet.SaveGroupSet(); // Save the updated GroupSet to the database
            }
        }

        public static List<ZO.LoadOrderManager.FileInfo> LoadFilesByPlugin(long pluginID, SQLiteConnection connection)
        {
            var files = new List<ZO.LoadOrderManager.FileInfo>();

            using (var command = new SQLiteCommand("SELECT FileID, Filename, RelativePath, DTStamp, HASH, Flags FROM vwPluginFiles WHERE PluginID = @PluginID", connection))
            {
                _ = command.Parameters.AddWithValue("@PluginID", pluginID);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var file = new ZO.LoadOrderManager.FileInfo
                    {
                        FileID = reader.GetInt64(reader.GetOrdinal("FileID")),
                        Filename = reader.GetString(reader.GetOrdinal("Filename")),
                        RelativePath = reader.IsDBNull(reader.GetOrdinal("RelativePath")) ? null : reader.GetString(reader.GetOrdinal("RelativePath")),
                        DTStamp = reader.GetString(reader.GetOrdinal("DTStamp")),
                        HASH = reader.IsDBNull(reader.GetOrdinal("HASH")) ? null : reader.GetString(reader.GetOrdinal("HASH")),
                        Flags = (FileFlags)reader.GetInt64(reader.GetOrdinal("Flags"))
                    };
                    files.Add(file);
                }
            }

            return files;
        }
    }
}
