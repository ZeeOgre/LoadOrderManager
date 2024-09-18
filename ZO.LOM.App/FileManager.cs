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
        //public static string PluginsFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "starfield", "plugins.txt");
        //public static string ContentCatalogFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "starfield", "ContentCatalog.txt");
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

                    // Attempt to load from the database
                    App.LogDebug("FileManager: Attempting to load from the database...");
                    AggLoadInfo.Instance.InitFromDatabase();
                    App.LogDebug("FileManager: Database load completed.");

                    // Check if we are working from an empty set
                    App.LogDebug("FileManager: Checking if working from an empty set...");
                    if (AggLoadInfo.Instance.Plugins.Count == 9 && (AggLoadInfo.Instance.Groups.Count == 4 && AggLoadInfo.Instance.LoadOuts.Count == 1))
                    {
                        App.LogDebug("FileManager: Working from an empty set. Loading plugins and content catalog...");

                        // Load plugins from plugins.txt
                        App.LogDebug($"FileManager: Loading plugins from {PluginsFile}...");
                        ParsePluginsTxt(PluginsFile);
                        AggLoadInfo.Instance.SaveToDatabase();
                        App.LogDebug("FileManager: Plugins loaded.");

                        // Load content catalog
                        App.LogDebug($"FileManager: Loading content catalog from {ContentCatalogFile}...");
                        ParseContentCatalogTxt(ContentCatalogFile);
                        AggLoadInfo.Instance.SaveToDatabase();
                        App.LogDebug("FileManager: Content catalog loaded.");

                        // Scan game directory for stray files
                        App.LogDebug("FileManager: Scanning game directory for stray files...");
                        ScanGameDirectoryForStrays();
                        AggLoadInfo.Instance.SaveToDatabase();
                        App.LogDebug("FileManager: Game directory scan completed.");
                    }
                    else if (AggLoadInfo.Instance.Plugins.Count > 9 && AggLoadInfo.Instance.Groups.Count >= 4 && AggLoadInfo.Instance.LoadOuts.Count >= 1)
                    {
                        App.LogDebug("FileManager: Valid data loaded from the database. Proceeding with initialization...");
                    }
                    else
                    {
                        App.LogDebug("FileManager: Invalid data state detected. Shutting down application...");
                        if (Application.Current != null)
                        {
                            Application.Current.Shutdown();
                        }
                        else
                        {
                            _ = MessageBox.Show("Application is not initialized properly.");
                        }
                    }

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
