using System.Configuration;
using System.Data.SQLite;
using System.IO;
using System.Windows;

namespace ZO.LoadOrderManager
{
    public class DbManager
    {
        private static readonly Lazy<DbManager> _instance = new Lazy<DbManager>(() => new DbManager());
        private static readonly string localAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZeeOgre", "LoadOrderManager");
        public static readonly string dbFilePath = Path.Combine(localAppDataPath, "LoadOrderManager.db");
        private static string ConnectionString;
        private static readonly bool textLoggingEnabled;

        private static bool _initialized = false;
        private static readonly object _lock = new object();

        static DbManager()
        {
            ConnectionString = $"Data Source={dbFilePath};Version=3;";

            // Initialize text logging status
            textLoggingEnabled = bool.TryParse(ConfigurationManager.AppSettings["TextLogging"], out bool result) && result;

            if (textLoggingEnabled)
            {
                // Write the connection string to a text file
                string connStringFilePath = Path.Combine(localAppDataPath, "connstring.txt");
                File.WriteAllText(connStringFilePath, ConnectionString);
            }
        }

        private DbManager() { }

        public static DbManager Instance => _instance.Value;

        public void Initialize()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                // Verify local app data files before any database operations
                Config.VerifyLocalAppDataFiles();

                bool dbExists = File.Exists(dbFilePath) && new System.IO.FileInfo(dbFilePath).Length > 0;
                App.LogDebug($"Database file path: {dbFilePath}");
                App.LogDebug($"Database exists: {dbExists}");

                using var connection = GetConnection();

                if (IsConfigTableEmpty() || !IsDatabaseInitialized())
                {
                    var config = Config.LoadFromYaml();
                    if (IsSampleOrInvalidData(config))
                    {
                        App.LogDebug($"Loaded sample data from YAML: {config}");
                        bool settingsSaved = LaunchSettingsWindow(SettingsLaunchSource.DatabaseInitialization);

                        config = Config.LoadFromYaml();
                        if (IsSampleOrInvalidData(config))
                        {
                            App.LogDebug("Configuration data is still invalid after settings window.");

                            // Wrap message box in Dispatcher
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                var resultRetry = MessageBox.Show("Configuration data is invalid. Would you like to retry?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Error);

                                if (resultRetry == MessageBoxResult.Yes)
                                {
                                    settingsSaved = LaunchSettingsWindow(SettingsLaunchSource.DatabaseInitialization);
                                }
                                else
                                {
                                    App.LogDebug("User chose not to retry. Shutting down application.");
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        Application.Current.Shutdown();
                                    });

                                    return;
                                }
                            });
                        }

                        if (!settingsSaved)
                        {
                            App.LogDebug("Settings were not saved. Shutting down application.");

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Application.Current.Shutdown();
                            });

                            return;
                        }
                    }
                    SetInitializationStatus(true);
                    Config.SaveToDatabase();
                }
                else
                {
                    App.LogDebug("Loading configuration from database.");
                    _ = Config.LoadFromDatabase();
                }

                _initialized = true;
            }
        }


        public static bool IsSampleOrInvalidData(Config config)
        {
            return config.GameFolder == "<<GAME ROOT FOLDER>>" ||
                   !Directory.Exists(config.GameFolder);
        }

        public bool LaunchSettingsWindow(SettingsLaunchSource source)
        {
            bool? result = null;

            // Ensure the settings window is launched on the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                var settingsWindow = new SettingsWindow(source);
                result = settingsWindow.ShowDialog();
            });

            App.LogDebug($"SettingsWindow launched.");
            return result == true;
        }


        public SQLiteConnection GetConnection()
        {
            var connection = new SQLiteConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        private bool IsConfigTableEmpty()
        {
            using var connection = GetConnection();
            using var command = new SQLiteCommand("SELECT COUNT(*) FROM Config", connection);
            return Convert.ToInt64(command.ExecuteScalar()) == 0;
        }

        public bool IsDatabaseInitialized()
        {
            using var connection = GetConnection();

            // Consolidated command to check for tables and initialization status
            using var command = new SQLiteCommand(@"
                    SELECT 
                        (SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='InitializationStatus') AS InitStatusExists,
                        (SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Config') AS ConfigTableExists,
                        (SELECT IsInitialized FROM InitializationStatus LIMIT 1) AS IsInitialized,
                        (SELECT COUNT(*) FROM Config) AS ConfigCount
                ", connection);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                bool initStatusExists = reader.GetInt64(0) > 0;
                bool configTableExists = reader.GetInt64(1) > 0;
                bool isInitialized = reader.IsDBNull(2) ? false : Convert.ToBoolean(reader.GetInt64(2));
                long configCount = reader.GetInt64(3);

                return initStatusExists && configTableExists && isInitialized && configCount > 0;
            }

            return false;
        }

        public static long GetNextID(string tableName)
        {
            string idField;

            if (tableName == "ModGroups")
            {
                idField = "GroupId";
            }
            else if (tableName == "Plugins")
            {
                idField = "PluginId";
            }
            else
            {
                // Handle the case when the tableName is neither "ModGroups" nor "Plugins"
                throw new ArgumentException("Invalid tableName specified.");
            }

            using var connection = Instance.GetConnection();

            // Consolidated command to get max ID and auto-increment value
            using var command = new SQLiteCommand($@"
                    SELECT 
                        (SELECT MAX({idField}) +1 FROM {tableName}) AS MaxId
                ", connection);
            _ = command.Parameters.AddWithValue("@tableName", tableName);
            return Convert.ToInt64(command.ExecuteScalar());
        }

        public static long GetNextOrdinal(EntityType type, long groupId, long groupSetId)
        {
            // Abort if groupId or groupSetId is 0
            if (groupId == 0 || groupSetId == 0)
            {
                return 1;
            }

            // Force groupSetID to 1 if groupId is less than 0
            if (groupId < 0)
            {
                groupSetId = 1;
            }

            string query;


            // Regular case with GroupSetID
            query = type switch
            {
                EntityType.Plugin => "SELECT MAX(GroupOrdinal) + 1 FROM vwPlugins WHERE GroupID = @GroupID AND GroupSetID = @GroupSetID AND GroupID != -999",
                EntityType.Group => "SELECT MAX(Ordinal) + 1 FROM vwModGroups WHERE ParentID = @GroupID AND GroupSetID = @GroupSetID AND GroupID != -999",
                _ => throw new ArgumentException("Invalid type specified.")
            };

            using var connection = Instance.GetConnection();
            using var command = new SQLiteCommand(query, connection);
            _ = command.Parameters.AddWithValue("@GroupID", groupId);
            if (groupId != -999)
            {
                _ = command.Parameters.AddWithValue("@GroupSetID", groupSetId);
            }
            var result = command.ExecuteScalar();

            // Check for DBNull or invalid result
            if (result == DBNull.Value || Convert.ToInt64(result) <= 1)
            {
                return 1;
            }
            else
            {
                return Convert.ToInt64(result);
            }
        }

        public void SetInitializationStatus(bool status)
        {
            using var connection = GetConnection();
            using var command = new SQLiteCommand("INSERT OR REPLACE INTO InitializationStatus (Id, IsInitialized, InitializationTime) VALUES (1, @IsInitialized, @InitializationTime)", connection);
            _ = command.Parameters.AddWithValue("@IsInitialized", status ? 1 : 0);
            _ = command.Parameters.AddWithValue("@InitializationTime", DateTime.UtcNow);
            _ = command.ExecuteNonQuery();
            App.LogDebug($"Database marked as initialized: {status}");
        }

        public static void FlushDB()
        {
            using var connection = Instance.GetConnection();

            using var transaction = connection.BeginTransaction();
            try
            {
                App.LogDebug($"WriteMod Begin Transaction");
                transaction.Commit();

                using var vacuumCommand = new SQLiteCommand("VACUUM;", connection);
                _ = vacuumCommand.ExecuteNonQuery();

                using var reindexCommand = new SQLiteCommand("REINDEX;", connection);
                _ = reindexCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                App.LogDebug($"Error during FlushDB: {ex.Message}");
                transaction.Rollback();
            }
            finally
            {
                connection.Close();
            }

            SQLiteConnection.ClearAllPools();
        }

        public string BackupDB(bool Force = false, string? backupFilePath = null)
        {
            if (!Force && !textLoggingEnabled)
            {
                return string.Empty;
            }

            if (backupFilePath == null)
            {
                string backupFileName = $"LOM_BACKUP_{DateTime.Now:yyMMddHHmmss}.db";
                backupFilePath = Path.Combine(localAppDataPath, backupFileName);
            }

            using (var source = Instance.GetConnection())
            using (var destination = new SQLiteConnection($"Data Source={backupFilePath};Version=3;"))
            {
                destination.Open();
                source.BackupDatabase(destination, "main", "main", -1, null, 0);
            }

            return backupFilePath;
        }
    }
}
