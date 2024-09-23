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

                    // Retrieve the singleton GroupSet and LoadOut from the database
                    App.LogDebug("FileManager: Retrieving singleton GroupSet and LoadOut from the database...");
                    var singletonGroupSet = GroupSet.LoadGroupSet(2);
                    if (singletonGroupSet == null)
                    {
                        throw new InvalidOperationException("FileManager: Failed to load singleton GroupSet from the database.");
                    }

                    var singletonLoadOut = LoadOut.Load(2);
                    if (singletonLoadOut == null)
                    {
                        throw new InvalidOperationException("FileManager: Failed to load singleton LoadOut from the database.");
                    }

                    AggLoadInfo.Instance.ActiveLoadOut = singletonLoadOut;
                    AggLoadInfo.Instance.ActiveGroupSet = singletonGroupSet;
                    App.LogDebug("FileManager: Singleton GroupSet and LoadOut retrieved successfully.");

                    // Load data from the database into the AggLoadInfo instance
                    App.LogDebug("FileManager: Attempting to load additional data from the database...");
                    AggLoadInfo.Instance.InitFromDatabase();
                    App.LogDebug("FileManager: Database load completed.");

                    // Update the flags to indicate the singleton is ready to load
                    App.LogDebug("FileManager: Updating singleton LoadOut flags to ReadyToLoad...");
                    singletonGroupSet.GroupSetFlags = GroupFlags.ReadyToLoad;
                    singletonGroupSet.SaveGroupSet();

                    App.LogDebug("FileManager: Singleton LoadOut is now ready to load. Proceeding with initialization...");
                    FileManager.ParsePluginsTxt(AggLoadInfo.Instance, PluginsFile);
                    FileManager.ParseContentCatalogTxt();
                    FileManager.ScanGameDirectoryForStrays();
                    FileManager.MarkLoadOutComplete(AggLoadInfo.Instance);

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

        public static void MarkLoadOutComplete(AggLoadInfo aggLoadInfo)
        {
            var activeGroupSet = aggLoadInfo.ActiveGroupSet;
            if (activeGroupSet != null)
            {
                activeGroupSet.GroupSetFlags |= GroupFlags.FilesLoaded;
                activeGroupSet.GroupSetFlags &= ~GroupFlags.ReadyToLoad;
            }
        }

        public static List<ZO.LoadOrderManager.FileInfo> LoadFilesByPlugin(int pluginID, SQLiteConnection connection)
        {
            var files = new List<ZO.LoadOrderManager.FileInfo>();

            using (var command = new SQLiteCommand("SELECT FileID, Filename, RelativePath, DTStamp, HASH, IsArchive FROM vwPluginFiles WHERE PluginID = @PluginID", connection))
            {
                _ = command.Parameters.AddWithValue("@PluginID", pluginID);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var file = new ZO.LoadOrderManager.FileInfo
                    {
                        FileID = reader.GetInt32(reader.GetOrdinal("FileID")),
                        Filename = reader.GetString(reader.GetOrdinal("Filename")),
                        RelativePath = reader.IsDBNull(reader.GetOrdinal("RelativePath")) ? null : reader.GetString(reader.GetOrdinal("RelativePath")),
                        DTStamp = reader.GetString(reader.GetOrdinal("DTStamp")),
                        HASH = reader.IsDBNull(reader.GetOrdinal("HASH")) ? null : reader.GetString(reader.GetOrdinal("HASH")),
                        IsArchive = reader.GetInt32(reader.GetOrdinal("IsArchive")) == 1
                    };
                    files.Add(file);
                }
            }

            return files;
        }
    }
}
