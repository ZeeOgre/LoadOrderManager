using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography.X509Certificates;
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
                        if (File.Exists(dbFilePath) && HasRowsInDatabase())
                        {
                            _ = LoadFromDatabase();
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

        public static Config? LoadFromDatabase()
        {
            using (var connection = DbManager.Instance.GetConnection())
            {
                using var command = new SQLiteCommand("SELECT * FROM Config", connection);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    _instance = new Config
                    {
                        GameFolder = reader["GameFolder"]?.ToString(),
                        AutoScanGameFolder = Convert.ToBoolean(reader["AutoScanGameFolder"]),
                        ModManagerRepoFolder = reader["ModManagerRepoFolder"]?.ToString(),
                        AutoScanModRepoFolder = Convert.ToBoolean(reader["AutoScanModRepoFolder"]),
                        ModManagerExecutable = reader["ModManagerExecutable"]?.ToString(),
                        ModManagerArguments = reader["ModManagerArguments"]?.ToString(),
                        AutoCheckForUpdates = Convert.ToBoolean(reader["AutoCheckForUpdates"]),
                        DarkMode = Convert.ToBoolean(reader["DarkMode"]),
                        LootExePath = reader["LootExePath"]?.ToString(),
                        NexusExportFile = reader["NexusExportFile"]?.ToString(),
                        MO2ExportFile = reader["MO2ExportFile"]?.ToString(),
                        WebServicePort = Convert.ToInt32(reader["WebServicePort"]),
                        PluginWarning = Convert.ToBoolean(reader["PluginWarning"]),
                        ShowDiff = Convert.ToBoolean(reader["ShowDiff"]),
                        MonitoredFiles = FileInfo.GetMonitoredFiles() // Ensure MonitoredFiles is loaded
                        
                    };
                }
            }
            return _instance;
        }

        public static void SaveToDatabase()
        {
            var config = Instance;

            using var connection = DbManager.Instance.GetConnection();
            using var transaction = connection.BeginTransaction();
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = "DELETE FROM Config";
                _ = command.ExecuteNonQuery();

                command.CommandText = @"
                    INSERT INTO Config (
                        GameFolder,
                        AutoScanGameFolder,
                        ModManagerRepoFolder,
                        AutoScanModRepoFolder,
                        ModManagerExecutable,
                        ModManagerArguments,
                        AutoCheckForUpdates,
                        DarkMode,
                        LootExePath,
                        NexusExportFile,
                        MO2ExportFile,
                        WebServicePort,
                        PluginWarning,
                        ShowDiff

    
                    ) VALUES (
                        @GameFolder,
                        @AutoScanGameFolder,
                        @ModManagerRepoFolder,
                        @AutoScanModRepoFolder,
                        @ModManagerExecutable,
                        @ModManagerArguments,
                        @AutoCheckForUpdates,
                        @DarkMode,
                        @LootExePath,
                        @NexusExportFile,
                        @MO2ExportFile,
                        @WebServicePort,
                        @PluginWarning,
                        @ShowDiff

                    )";

                _ = command.Parameters.AddWithValue("@GameFolder", config.GameFolder ?? (object)DBNull.Value);
                _ = command.Parameters.AddWithValue("@AutoScanGameFolder", config.AutoScanGameFolder ? 1 : 0);
                _ = command.Parameters.AddWithValue("@ModManagerRepoFolder", config.ModManagerRepoFolder ?? (object)DBNull.Value);
                _ = command.Parameters.AddWithValue("@AutoScanModRepoFolder", config.AutoScanModRepoFolder ? 1 : 0);
                _ = command.Parameters.AddWithValue("@ModManagerExecutable", config.ModManagerExecutable ?? (object)DBNull.Value);
                _ = command.Parameters.AddWithValue("@ModManagerArguments", config.ModManagerArguments ?? (object)DBNull.Value);
                _ = command.Parameters.AddWithValue("@AutoCheckForUpdates", config.AutoCheckForUpdates ? 1 : 0);
                _ = command.Parameters.AddWithValue("@DarkMode", config.DarkMode ? 1 : 0);
                _ = command.Parameters.AddWithValue("@LootExePath", config.LootExePath ?? (object)DBNull.Value);
                _ = command.Parameters.AddWithValue("@NexusExportFile", config.NexusExportFile ?? (object)DBNull.Value);
                _ = command.Parameters.AddWithValue("@MO2ExportFile", config.MO2ExportFile ?? (object)DBNull.Value);
                _ = command.Parameters.AddWithValue("@WebServicePort", config.WebServicePort);
                _ = command.Parameters.AddWithValue("@PluginWarning", config.PluginWarning ? 1 : 0);
                _ = command.Parameters.AddWithValue("@ShowDiff", config.ShowDiff ? 1 : 0);



                _ = command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
    }
}
