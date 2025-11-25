using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ZO.LoadOrderManager
{
    /// <summary>
    /// Configuration class for LoadOrderManager.App.
    /// </summary>
    public class Config
    {
        private static Config? _instance;
        private static readonly object _lock = new object();
        private static readonly string localAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZeeOgre", "LoadOrderManager");
        public static readonly string configFilePath = Path.Combine(localAppDataPath, "config.yaml");
        public static readonly string dbFilePath = Path.Combine(localAppDataPath, "LoadOrderManager.db");
        private static bool _isVerificationInProgress = false; // Flag to track verification

        // profile identifiers
        public int ProfileID { get; set; }
        public string ProfileName { get; set; } = "DefaultProfile";

        // active flag stored as INTEGER (0/1) in SQLite
        public bool IsActive { get; set; } = true;

        public List<FileInfo> MonitoredFiles { get; set; } = new List<FileInfo>();
        public bool DarkMode { get; set; } = true;
        public string? GameFolder { get; set; }
        public bool AutoScanGameFolder { get; set; } = true;
        public string? ModManagerRepoFolder { get; set; }
        public bool AutoScanModRepoFolder { get; set; } = false;
        public string? ModManagerExecutable { get; set; }
        public string? ModManagerArguments { get; set; }
        public bool AutoCheckForUpdates { get; set; } = true;
        public string? LootExePath { get; set; }
        public string? NexusExportFile { get; set; }
        public string? MO2ExportFile { get; set; }
        public int WebServicePort { get; set; } = 23306;
        public bool PluginWarning { get; set; } = true;
        public bool ShowDiff { get; set; } = true;

        public static Config Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Config();
                        }
                    }
                }
                return _instance;
            }
        }

        public void UpdateFrom(Config other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            this.GameFolder = other.GameFolder;
            this.AutoScanGameFolder = other.AutoScanGameFolder;
            this.ModManagerRepoFolder = other.ModManagerRepoFolder;
            this.AutoScanModRepoFolder = other.AutoScanModRepoFolder;
            this.ModManagerExecutable = other.ModManagerExecutable;
            this.ModManagerArguments = other.ModManagerArguments;
            this.AutoCheckForUpdates = other.AutoCheckForUpdates;
            this.LootExePath = other.LootExePath;
            this.NexusExportFile = other.NexusExportFile;
            this.MO2ExportFile = other.MO2ExportFile;
            this.WebServicePort = other.WebServicePort;
            this.PluginWarning = other.PluginWarning;
            this.ShowDiff = other.ShowDiff;
            this.DarkMode = other.DarkMode;
            this.MonitoredFiles = new List<FileInfo>(other.MonitoredFiles);

            // keep profile metadata in sync when copying
            this.ProfileID = other.ProfileID;
            this.ProfileName = other.ProfileName;
            this.IsActive = other.IsActive;
        }

        public static void Initialize()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        VerifyLocalAppDataFiles();

                        // Check DB schema but do not mutate it — you manage schema externally.     
                        try
                        {
                            EnsureProfileSchema();
                        }
                        catch (Exception ex)
                        {
                            App.LogDebug($"Error ensuring profile schema: {ex.Message}");
                        }

                        if (File.Exists(dbFilePath) && HasRowsInDatabase())
                        {
                            _ = LoadFromDatabase(); // loads active profile if present, otherwise first row
                            Instance.MonitoredFiles = FileInfo.GetMonitoredFiles();
                        }
                        else
                        {
                            _ = LoadFromYaml();
                        }
                    }
                }
            }
        }

        private static bool HasRowsInDatabase()
        {
            try
            {
                using var connection = DbManager.Instance.GetConnection();
                using var command = new SQLiteCommand("SELECT COUNT(*) FROM Config", connection);
                var rowCount = Convert.ToInt64(command.ExecuteScalar());
                return rowCount > 0;
            }
            catch (Exception ex)
            {
                App.LogDebug($"Error checking rows in database: {ex.Message}");
                return false;
            }
        }

        public static void InitializeNewInstance()
        {
            _instance = new Config();
        }

        public static void VerifyLocalAppDataFiles()
        {
            if (_isVerificationInProgress)
            {
                return; // Exit if verification is already in progress
            }

            _isVerificationInProgress = true;

            try
            {
                if (!Directory.Exists(localAppDataPath))
                {
                    _ = Directory.CreateDirectory(localAppDataPath);
                    return;
                }

                if (!File.Exists(dbFilePath))
                {
                    string sampleDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "LoadOrderManager.db");
                    if (File.Exists(sampleDbPath))
                    {
                        var result = MessageBox.Show("The database file is missing. Would you like to copy the sample data over?", "Database Missing", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            File.Copy(sampleDbPath, dbFilePath);
                            _ = MessageBox.Show("Sample data copied successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            _ = MessageBox.Show("Database file is missing. Please reinstall the application and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
                            return;
                        }
                    }
                    else
                    {
                        _ = MessageBox.Show("The database file is missing and no sample data is available. Please reinstall the application and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
                        return;
                    }
                }

                if (!File.Exists(configFilePath))
                {
                    string sampleConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "config.yaml");
                    if (File.Exists(sampleConfigPath))
                    {
                        File.Copy(sampleConfigPath, configFilePath);
                        _ = MessageBox.Show("Sample config copied successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        _ = MessageBox.Show("The config file is missing and no sample data is available. Please reinstall the application and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
                        return;
                    }
                }
            }
            finally
            {
                _isVerificationInProgress = false;
            }
        }

        public static Config LoadFromYaml()
        {
            return LoadFromYaml(configFilePath);
        }

        public static void SaveToYaml()
        {
            SaveToYaml(configFilePath);
        }

        public static Config LoadFromYaml(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Configuration file not found", filePath);
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            using var reader = new StreamReader(filePath);
            var config = deserializer.Deserialize<Config>(reader);

            lock (_lock)
            {
                _instance = config;
            }

            return _instance;
        }

        public static void SaveToYaml(string filePath)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(Instance);
            File.WriteAllText(filePath, yaml);
        }

        private static Config FromReader(SQLiteDataReader reader)
        {
            var cfg = new Config();

            cfg.ProfileID = reader["ProfileID"] != DBNull.Value ? Convert.ToInt32(reader["ProfileID"]) : 0;
            cfg.ProfileName = reader["ProfileName"] != DBNull.Value ? reader["ProfileName"].ToString() ?? "DefaultProfile" : "DefaultProfile";

            // treat NULL or non-1 as false
            cfg.IsActive = reader["IsActive"] != DBNull.Value && Convert.ToInt32(reader["IsActive"]) == 1;

            cfg.GameFolder = reader["GameFolder"] != DBNull.Value ? reader["GameFolder"].ToString() : null;
            cfg.AutoScanGameFolder = reader["AutoScanGameFolder"] != DBNull.Value && Convert.ToBoolean(reader["AutoScanGameFolder"]);
            cfg.ModManagerRepoFolder = reader["ModManagerRepoFolder"] != DBNull.Value ? reader["ModManagerRepoFolder"].ToString() : null;
            cfg.AutoScanModRepoFolder = reader["AutoScanModRepoFolder"] != DBNull.Value && Convert.ToBoolean(reader["AutoScanModRepoFolder"]);
            cfg.ModManagerExecutable = reader["ModManagerExecutable"] != DBNull.Value ? reader["ModManagerExecutable"].ToString() : null;
            cfg.ModManagerArguments = reader["ModManagerArguments"] != DBNull.Value ? reader["ModManagerArguments"].ToString() : null;
            cfg.AutoCheckForUpdates = reader["AutoCheckForUpdates"] != DBNull.Value && Convert.ToBoolean(reader["AutoCheckForUpdates"]);
            cfg.DarkMode = reader["DarkMode"] != DBNull.Value && Convert.ToBoolean(reader["DarkMode"]);
            cfg.LootExePath = reader["LootExePath"] != DBNull.Value ? reader["LootExePath"].ToString() : null;
            cfg.NexusExportFile = reader["NexusExportFile"] != DBNull.Value ? reader["NexusExportFile"].ToString() : null;
            cfg.MO2ExportFile = reader["MO2ExportFile"] != DBNull.Value ? reader["MO2ExportFile"].ToString() : null;
            cfg.WebServicePort = reader["WebServicePort"] != DBNull.Value ? Convert.ToInt32(reader["WebServicePort"]) : default(int);
            cfg.PluginWarning = reader["PluginWarning"] != DBNull.Value && Convert.ToBoolean(reader["PluginWarning"]);
            cfg.ShowDiff = reader["ShowDiff"] != DBNull.Value && Convert.ToBoolean(reader["ShowDiff"]);
            cfg.MonitoredFiles = FileInfo.GetMonitoredFiles();

            return cfg;
        }

        /// <summary>
        /// Load a profile by name. If profileName is null, prefer the active profile (IsActive=1),
        /// falling back to the first profile by ProfileID.
        /// </summary>
        public static Config? LoadFromDatabase(string? profileName = null)
        {
            if (profileName != null)
            {
                using (var connection = DbManager.Instance.GetConnection())
                using (var command = new SQLiteCommand("SELECT * FROM Config WHERE ProfileName = @p LIMIT 1", connection))
                {
                    command.Parameters.AddWithValue("@p", profileName);
                    using var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        _instance = FromReader(reader);
                        return _instance;
                    }
                }
            }
            else
            {
                // prefer active profile
                using (var connection = DbManager.Instance.GetConnection())
                using (var command = new SQLiteCommand("SELECT * FROM Config WHERE COALESCE(IsActive,0) = 1 ORDER BY ProfileID LIMIT 1", connection))
                {
                    using var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        _instance = FromReader(reader);
                        return _instance;
                    }
                }
            }

            // fallback to first available
            using (var connection = DbManager.Instance.GetConnection())
            using (var command = new SQLiteCommand("SELECT * FROM Config ORDER BY ProfileID LIMIT 1", connection))
            {
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    _instance = FromReader(reader);
                    return _instance;
                }
            }

            return _instance;
        }

        /// <summary>
        /// Returns all profiles (ordered by ProfileID) for SettingsWindow navigation.
        /// </summary>
        public static List<Config> LoadAllProfiles()
        {
            var list = new List<Config>();
            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand("SELECT * FROM Config ORDER BY ProfileID", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(FromReader(reader));
            }
            return list;
        }

        /// <summary>
        /// Create a new profile with given name. If setActive is true, it will be the only active profile.
        /// Returns ProfileID of new row (or existing ProfileID if name exists).
        /// </summary>
        public static int CreateProfile(string profileName, bool setActive = false)
        {
            if (string.IsNullOrWhiteSpace(profileName)) throw new ArgumentException("profileName required", nameof(profileName));

            using var connection = DbManager.Instance.GetConnection();
            using var transaction = connection.BeginTransaction();

            // check existing
            using (var check = new SQLiteCommand("SELECT ProfileID FROM Config WHERE ProfileName = @p LIMIT 1", connection, transaction))
            {
                check.Parameters.AddWithValue("@p", profileName);
                var existing = check.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                {
                    transaction.Commit();
                    return Convert.ToInt32(existing);
                }
            }

            if (setActive)
            {
                using var deactivate = new SQLiteCommand("UPDATE Config SET IsActive = 0 WHERE COALESCE(IsActive,0) = 1", connection, transaction);
                deactivate.ExecuteNonQuery();
            }

            using var insert = new SQLiteCommand(@"
                INSERT INTO Config (ProfileName, IsActive, GameFolder, AutoScanGameFolder, AutoCheckForUpdates, DarkMode, WebServicePort, PluginWarning, ShowDiff)
                VALUES (@ProfileName, @IsActive, @GameFolder, @AutoScanGameFolder, @AutoCheckForUpdates, @DarkMode, @WebServicePort, @PluginWarning, @ShowDiff);
            ", connection, transaction);

            insert.Parameters.AddWithValue("@ProfileName", profileName);
            insert.Parameters.AddWithValue("@IsActive", setActive ? 1 : 0);
            insert.Parameters.AddWithValue("@GameFolder", string.Empty);
            insert.Parameters.AddWithValue("@AutoScanGameFolder", 0);
            insert.Parameters.AddWithValue("@AutoCheckForUpdates", 1);
            insert.Parameters.AddWithValue("@DarkMode", 1);
            insert.Parameters.AddWithValue("@WebServicePort", 23306);
            insert.Parameters.AddWithValue("@PluginWarning", 1);
            insert.Parameters.AddWithValue("@ShowDiff", 1);

            insert.ExecuteNonQuery();

            // get last inserted id
            using var idCmd = new SQLiteCommand("SELECT last_insert_rowid()", connection, transaction);
            var newId = Convert.ToInt32(idCmd.ExecuteScalar());

            transaction.Commit();
            return newId;
        }

        public static void DeleteProfile(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) return;
            using var connection = DbManager.Instance.GetConnection();
            using var cmd = new SQLiteCommand("DELETE FROM Config WHERE ProfileName = @p", connection);
            cmd.Parameters.AddWithValue("@p", profileName);
            _ = cmd.ExecuteNonQuery();
        }

        public static void RenameProfile(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return;
            using var connection = DbManager.Instance.GetConnection();
            using var cmd = new SQLiteCommand("UPDATE Config SET ProfileName = @new WHERE ProfileName = @old", connection);
            cmd.Parameters.AddWithValue("@new", newName);
            cmd.Parameters.AddWithValue("@old", oldName);
            _ = cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Mark a profile active and mark all others inactive.
        /// </summary>
        public static void SetActiveProfile(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) return;
            using var connection = DbManager.Instance.GetConnection();
            using var transaction = connection.BeginTransaction();
            using var deactivate = new SQLiteCommand("UPDATE Config SET IsActive = 0 WHERE COALESCE(IsActive,0) = 1", connection, transaction);
            deactivate.ExecuteNonQuery();
            using var activate = new SQLiteCommand("UPDATE Config SET IsActive = 1 WHERE ProfileName = @p", connection, transaction);
            activate.Parameters.AddWithValue("@p", profileName);
            activate.ExecuteNonQuery();
            transaction.Commit();
        }

        public static void SaveToDatabase()
        {
            // Upsert by ProfileName (preserves existing other profiles)
            var config = Instance;
            using var connection = DbManager.Instance.GetConnection();
            using var transaction = connection.BeginTransaction();

            // Try update first; if no rows updated, insert
            using (var updateCmd = new SQLiteCommand(@"
                UPDATE Config SET
                    GameFolder = @GameFolder,
                    AutoScanGameFolder = @AutoScanGameFolder,
                    ModManagerRepoFolder = @ModManagerRepoFolder,
                    AutoScanModRepoFolder = @AutoScanModRepoFolder,
                    ModManagerExecutable = @ModManagerExecutable,
                    ModManagerArguments = @ModManagerArguments,
                    AutoCheckForUpdates = @AutoCheckForUpdates,
                    DarkMode = @DarkMode,
                    LootExePath = @LootExePath,
                    NexusExportFile = @NexusExportFile,
                    MO2EXPORTFILE = @MO2ExportFile,
                    WebServicePort = @WebServicePort,
                    PluginWarning = @PluginWarning,
                    ShowDiff = @ShowDiff,
                    IsActive = @IsActive
                WHERE ProfileName = @ProfileName;
            ", connection, transaction))
            {
                _ = updateCmd.Parameters.AddWithValue("@GameFolder", config.GameFolder ?? (object)DBNull.Value);
                _ = updateCmd.Parameters.AddWithValue("@AutoScanGameFolder", config.AutoScanGameFolder ? 1 : 0);
                _ = updateCmd.Parameters.AddWithValue("@ModManagerRepoFolder", config.ModManagerRepoFolder ?? (object)DBNull.Value);
                _ = updateCmd.Parameters.AddWithValue("@AutoScanModRepoFolder", config.AutoScanModRepoFolder ? 1 : 0);
                _ = updateCmd.Parameters.AddWithValue("@ModManagerExecutable", config.ModManagerExecutable ?? (object)DBNull.Value);
                _ = updateCmd.Parameters.AddWithValue("@ModManagerArguments", config.ModManagerArguments ?? (object)DBNull.Value);
                _ = updateCmd.Parameters.AddWithValue("@AutoCheckForUpdates", config.AutoCheckForUpdates ? 1 : 0);
                _ = updateCmd.Parameters.AddWithValue("@DarkMode", config.DarkMode ? 1 : 0);
                _ = updateCmd.Parameters.AddWithValue("@LootExePath", config.LootExePath ?? (object)DBNull.Value);
                _ = updateCmd.Parameters.AddWithValue("@NexusExportFile", config.NexusExportFile ?? (object)DBNull.Value);
                _ = updateCmd.Parameters.AddWithValue("@MO2ExportFile", config.MO2ExportFile ?? (object)DBNull.Value);
                _ = updateCmd.Parameters.AddWithValue("@WebServicePort", config.WebServicePort);
                _ = updateCmd.Parameters.AddWithValue("@PluginWarning", config.PluginWarning ? 1 : 0);
                _ = updateCmd.Parameters.AddWithValue("@ShowDiff", config.ShowDiff ? 1 : 0);
                _ = updateCmd.Parameters.AddWithValue("@IsActive", config.IsActive ? 1 : 0);
                _ = updateCmd.Parameters.AddWithValue("@ProfileName", config.ProfileName ?? "DefaultProfile");

                var rows = updateCmd.ExecuteNonQuery();
                if (rows == 0)
                {
                    // insert new profile row
                    using var insertCmd = new SQLiteCommand(@"
                        INSERT INTO Config (
                            ProfileName, IsActive, GameFolder, AutoScanGameFolder, AutoCheckForUpdates,
                            ModManagerRepoFolder, AutoScanModRepoFolder, ModManagerExecutable, ModManagerArguments,
                            LootExePath, NexusExportFile, MO2ExportFile, DarkMode, WebServicePort, PluginWarning, ShowDiff
                        ) VALUES (
                            @ProfileName, @IsActive, @GameFolder, @AutoScanGameFolder, @AutoCheckForUpdates,
                            @ModManagerRepoFolder, @AutoScanModRepoFolder, @ModManagerExecutable, @ModManagerArguments,
                            @LootExePath, @NexusExportFile, @MO2ExportFile, @DarkMode, @WebServicePort, @PluginWarning, @ShowDiff
                        );
                    ", connection, transaction);

                    _ = insertCmd.Parameters.AddWithValue("@ProfileName", config.ProfileName ?? "DefaultProfile");
                    _ = insertCmd.Parameters.AddWithValue("@IsActive", config.IsActive ? 1 : 0);
                    _ = insertCmd.Parameters.AddWithValue("@GameFolder", config.GameFolder ?? (object)DBNull.Value);
                    _ = insertCmd.Parameters.AddWithValue("@AutoScanGameFolder", config.AutoScanGameFolder ? 1 : 0);
                    _ = insertCmd.Parameters.AddWithValue("@AutoCheckForUpdates", config.AutoCheckForUpdates ? 1 : 0);
                    _ = insertCmd.Parameters.AddWithValue("@ModManagerRepoFolder", config.ModManagerRepoFolder ?? (object)DBNull.Value);
                    _ = insertCmd.Parameters.AddWithValue("@AutoScanModRepoFolder", config.AutoScanModRepoFolder ? 1 : 0);
                    _ = insertCmd.Parameters.AddWithValue("@ModManagerExecutable", config.ModManagerExecutable ?? (object)DBNull.Value);
                    _ = insertCmd.Parameters.AddWithValue("@ModManagerArguments", config.ModManagerArguments ?? (object)DBNull.Value);
                    _ = insertCmd.Parameters.AddWithValue("@LootExePath", config.LootExePath ?? (object)DBNull.Value);
                    _ = insertCmd.Parameters.AddWithValue("@NexusExportFile", config.NexusExportFile ?? (object)DBNull.Value);
                    _ = insertCmd.Parameters.AddWithValue("@MO2ExportFile", config.MO2ExportFile ?? (object)DBNull.Value);
                    _ = insertCmd.Parameters.AddWithValue("@DarkMode", config.DarkMode ? 1 : 0);
                    _ = insertCmd.Parameters.AddWithValue("@WebServicePort", config.WebServicePort);
                    _ = insertCmd.Parameters.AddWithValue("@PluginWarning", config.PluginWarning ? 1 : 0);
                    _ = insertCmd.Parameters.AddWithValue("@ShowDiff", config.ShowDiff ? 1 : 0);

                    insertCmd.ExecuteNonQuery();

                    // update ProfileID of in-memory instance
                    using var idCmd = new SQLiteCommand("SELECT last_insert_rowid()", connection, transaction);
                    config.ProfileID = Convert.ToInt32(idCmd.ExecuteScalar());
                }
            }

            transaction.Commit();
        }

        /// <summary>
        /// Inspects the Config table and logs missing columns. Schema changes are intentionally managed outside the application.
        /// This method will NOT perform ALTER/CREATE/DROP operations.
        /// </summary>
        private static void EnsureProfileSchema()
        {
            if (!File.Exists(dbFilePath)) return;

            try
            {
                using var connection = DbManager.Instance.GetConnection();

                using var checkCmd = new SQLiteCommand("PRAGMA table_info(Config);", connection);
                using var reader = checkCmd.ExecuteReader();
                var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                while (reader.Read())
                {
                    columns.Add(Convert.ToString(reader["name"]));
                }

                var missing = new List<string>();
                if (!columns.Contains("ProfileID")) missing.Add("ProfileID");
                if (!columns.Contains("ProfileName")) missing.Add("ProfileName");
                if (!columns.Contains("IsActive")) missing.Add("IsActive");

                if (missing.Count > 0)
                {
                    App.LogDebug($"Config table schema missing columns: {string.Join(", ", missing)}. Schema management is external; application will continue without modifying the database.");
                }
                else
                {
                    App.LogDebug("Config table schema contains ProfileID, ProfileName and IsActive — no action required.");
                }
            }
            catch (Exception ex)
            {
                App.LogDebug($"Error checking Config schema: {ex.Message}");
            }
        }
    }
}
