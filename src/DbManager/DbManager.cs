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
                EnsureConfigSchemaCompatibility();
                EnsureGameFolderSchema();

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

        /// <summary>
        /// Adds configuration columns introduced after the original database release.
        /// This runs before Config.LoadFromDatabase so an existing user database can
        /// be read without being deleted or replaced.
        /// </summary>
        public void EnsureConfigSchemaCompatibility()
        {
            using var connection = GetConnection();
            using var command = new SQLiteCommand(connection);
            command.CommandText = "PRAGMA table_info(Config);";

            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    columns.Add(reader.GetString(1));
                }
            }

            if (columns.Count == 0)
            {
                return;
            }

            var missingColumns = new (string Name, string Definition)[]
            {
                ("ProfileID", "INTEGER"),
                ("IsActive", "INTEGER NOT NULL DEFAULT 1"),
                ("StartupGameFolderID", "INTEGER"),
                ("RememberLastGameFolder", "INTEGER NOT NULL DEFAULT 1"),
                ("AutoScanGameFolder", "INTEGER NOT NULL DEFAULT 1"),
                ("AutoScanModRepoFolder", "INTEGER NOT NULL DEFAULT 0"),
                ("LootExePath", "TEXT"),
                ("NexusExportFile", "TEXT"),
                ("MO2ExportFile", "TEXT"),
                ("WebServicePort", "INTEGER NOT NULL DEFAULT 23306"),
                ("PluginWarning", "INTEGER NOT NULL DEFAULT 1"),
                ("ShowDiff", "INTEGER NOT NULL DEFAULT 1")
            };

            using var transaction = connection.BeginTransaction();
            foreach (var column in missingColumns)
            {
                if (columns.Contains(column.Name))
                {
                    continue;
                }

                command.CommandText = $"ALTER TABLE Config ADD COLUMN {column.Name} {column.Definition};";
                command.ExecuteNonQuery();
            }

            // SQLite cannot add a PRIMARY KEY through ALTER TABLE. Existing Config rows
            // receive their stable row ID as the profile identifier instead.
            command.CommandText = "UPDATE Config SET ProfileID = rowid WHERE ProfileID IS NULL;";
            command.ExecuteNonQuery();
            transaction.Commit();
        }

        private void EnsureGameFolderSchema()
        {
            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            using var command = new SQLiteCommand(connection);
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS GameFolders (
                    GameFolderID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                    DisplayName TEXT NOT NULL,
                    GameRoot TEXT NOT NULL COLLATE NOCASE UNIQUE
                );";
            command.ExecuteNonQuery();

            command.CommandText = "PRAGMA table_info(FileInfo);";
            var hasGameFolderID = false;
            using (var reader = command.ExecuteReader())
                while (reader.Read()) hasGameFolderID |= string.Equals(reader.GetString(1), "GameFolderID", StringComparison.OrdinalIgnoreCase);

            if (!hasGameFolderID)
            {
                command.CommandText = @"
                    DROP TRIGGER IF EXISTS trgInsteadOfInsert_vwLoadOuts;
                    DROP TRIGGER IF EXISTS trgInsteadOfInsert_vwModGroups;
                    DROP TRIGGER IF EXISTS trgInsteadOfInsert_vwPluginFiles;
                    DROP TRIGGER IF EXISTS trgInsteadOfInsert_vwPlugins;
                    DROP TRIGGER IF EXISTS trgInsteadOfUpdate_vwLoadOuts;
                    DROP TRIGGER IF EXISTS trgInsteadOfUpdate_vwModGroups;
                    DROP TRIGGER IF EXISTS fki_FileInfo_PluginID_Plugins_PluginID;
                    DROP TRIGGER IF EXISTS fku_FileInfo_PluginID_Plugins_PluginID;
                    DROP VIEW IF EXISTS vwPluginFiles;
                    ALTER TABLE FileInfo RENAME TO FileInfo_Legacy;
                    CREATE TABLE FileInfo (
                        FileID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                        PluginID INTEGER REFERENCES Plugins(PluginID) ON DELETE CASCADE,
                        GameFolderID INTEGER REFERENCES GameFolders(GameFolderID) ON DELETE CASCADE,
                        Filename TEXT NOT NULL COLLATE NOCASE,
                        RelativePath TEXT, AbsolutePath TEXT, ModManagerFolderPath TEXT,
                        DTStamp TEXT NOT NULL, HASH TEXT, Flags INTEGER, FileContent BLOB,
                        UNIQUE(GameFolderID, Filename)
                    );
                    INSERT INTO FileInfo (FileID, PluginID, Filename, RelativePath, AbsolutePath, ModManagerFolderPath, DTStamp, HASH, Flags, FileContent)
                    SELECT FileID, PluginID, Filename, RelativePath, AbsolutePath, ModManagerFolderPath, DTStamp, HASH, Flags, FileContent FROM FileInfo_Legacy;
                    DROP TABLE FileInfo_Legacy;";
                command.ExecuteNonQuery();
            }

            var legacyRoot = Config.Instance.GameFolder;
            if (!string.IsNullOrWhiteSpace(legacyRoot) && Directory.Exists(Path.Combine(legacyRoot, "Data")))
            {
                command.CommandText = @"
                    INSERT INTO GameFolders (DisplayName, GameRoot) VALUES (@DisplayName, @GameRoot)
                    ON CONFLICT(GameRoot) DO NOTHING;";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@DisplayName", "<DEFAULT>");
                command.Parameters.AddWithValue("@GameRoot", Path.GetFullPath(legacyRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                command.ExecuteNonQuery();
            }

            command.CommandText = @"
                DROP VIEW IF EXISTS vwPluginFiles;
                CREATE VIEW vwPluginFiles AS
                SELECT fi.FileID, p.PluginID, p.PluginName, fi.Filename, fi.RelativePath,
                       fi.DTStamp, fi.HASH, fi.Flags, fi.AbsolutePath, fi.GameFolderID
                FROM Plugins p JOIN FileInfo fi ON p.PluginID = fi.PluginID;";
            command.Parameters.Clear();
            command.ExecuteNonQuery();
            transaction.Commit();
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


        public static void CompactFileInfoTable()
        {
            using var connection = Instance.GetConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Create a temporary table with the same structure as FileInfo
                using (var createTempTableCommand = new SQLiteCommand(
                    "CREATE TABLE IF NOT EXISTS TempFileInfo AS SELECT * FROM FileInfo;", connection))
                {
                    createTempTableCommand.ExecuteNonQuery();
                }

                // Clear the main FileInfo table
                using (var deleteFromFileInfoCommand = new SQLiteCommand("DELETE FROM FileInfo;", connection))
                {
                    deleteFromFileInfoCommand.ExecuteNonQuery();
                }

                // Reset the autoincrement counter
                using (var resetAutoIncrementCommand = new SQLiteCommand("DELETE FROM sqlite_sequence WHERE name='FileInfo';", connection))
                {
                    resetAutoIncrementCommand.ExecuteNonQuery();
                }

                // Insert data back into FileInfo with new sequential FileIDs
                using (var insertFromTempCommand = new SQLiteCommand(
                    @"INSERT INTO FileInfo (PluginID, Filename, RelativePath, AbsolutePath, ModManagerFolderPath, DTStamp, HASH, Flags, FileContent)
                      SELECT PluginID, Filename, RelativePath, AbsolutePath, ModManagerFolderPath, DTStamp, HASH, Flags, FileContent
                      FROM TempFileInfo;", connection))
                {
                    insertFromTempCommand.ExecuteNonQuery();
                }

                // Drop the temporary table
                using (var dropTempTableCommand = new SQLiteCommand("DROP TABLE IF EXISTS TempFileInfo;", connection))
                {
                    dropTempTableCommand.ExecuteNonQuery();
                }

                transaction.Commit();
                App.LogDebug("FileInfo table compacted successfully.");
            }
            catch (Exception ex)
            {
                App.LogDebug($"Error compacting FileInfo table: {ex.Message}");
                transaction.Rollback();
            }
        }

        public static void CompactExternalIDsTable()
        {
            using var connection = Instance.GetConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Create a temporary table with the same structure as ExternalIDs
                using (var createTempTableCommand = new SQLiteCommand(
                    "CREATE TABLE IF NOT EXISTS TempExternalIDs AS SELECT * FROM ExternalIDs;", connection))
                {
                    createTempTableCommand.ExecuteNonQuery();
                }

                // Clear the main ExternalIDs table
                using (var deleteFromExternalIDsCommand = new SQLiteCommand("DELETE FROM ExternalIDs;", connection))
                {
                    deleteFromExternalIDsCommand.ExecuteNonQuery();
                }

                // Reset the autoincrement counter
                using (var resetAutoIncrementCommand = new SQLiteCommand("DELETE FROM sqlite_sequence WHERE name='ExternalIDs';", connection))
                {
                    resetAutoIncrementCommand.ExecuteNonQuery();
                }

                // Insert data back into ExternalIDs with new sequential ExternalIDs
                using (var insertFromTempCommand = new SQLiteCommand(
                    @"INSERT INTO ExternalIDs (PluginID, BethesdaID, NexusID)
              SELECT PluginID, BethesdaID, NexusID
              FROM TempExternalIDs;", connection))
                {
                    insertFromTempCommand.ExecuteNonQuery();
                }

                // Drop the temporary table
                using (var dropTempTableCommand = new SQLiteCommand("DROP TABLE IF EXISTS TempExternalIDs;", connection))
                {
                    dropTempTableCommand.ExecuteNonQuery();
                }

                transaction.Commit();
                App.LogDebug("ExternalIDs table compacted successfully.");
            }
            catch (Exception ex)
            {
                App.LogDebug($"Error compacting ExternalIDs table: {ex.Message}");
                transaction.Rollback();
            }
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
