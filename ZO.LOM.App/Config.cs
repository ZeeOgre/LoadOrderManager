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
        public List<FileInfo> MonitoredFiles { get; set; } = new List<FileInfo>();

        /// <summary>
        /// Gets the singleton instance of the Config class.
        /// </summary>
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

        public string? GameFolder { get; set; }
        public bool AutoCheckForUpdates { get; set; } = true;

        public void UpdateFrom(Config other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            // Assuming these are the properties you want to update
            this.GameFolder = other.GameFolder;
            this.AutoCheckForUpdates = other.AutoCheckForUpdates;
            this.MonitoredFiles = other.MonitoredFiles;
            // Add other properties as needed
        }

        /// <summary>
        /// Initializes the configuration.
        /// </summary>
        public static void Initialize()
        {
            //MessageBox.Show($"Regular Init: Config file path: {configFilePath}\nDB file path: {dbFilePath}", "Initialization Paths");

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
            //MessageBox.Show($"Special Blank Init:Config file path: {configFilePath}\nDB file path: {dbFilePath}", "Initialization Paths");

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

        /// <summary>
        /// Loads the configuration from a YAML file.
        /// </summary>
        /// <returns>The loaded configuration.</returns>
        public static Config LoadFromYaml()
        {
            return LoadFromYaml(configFilePath);
        }

        /// <summary>
        /// Saves the configuration to a YAML file.
        /// </summary>
        public static void SaveToYaml()
        {
            SaveToYaml(configFilePath);
        }

        /// <summary>
        /// Loads the configuration from a specified YAML file.
        /// </summary>
        /// <param name="filePath">The path to the YAML file.</param>
        /// <returns>The loaded configuration.</returns>
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

        /// <summary>
        /// Saves the configuration to a specified YAML file.
        /// </summary>
        /// <param name="filePath">The path to the YAML file.</param>
        public static void SaveToYaml(string filePath)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(Instance);

            File.WriteAllText(filePath, yaml);
        }

        /// <summary>
        /// Loads the configuration from the database.
        /// </summary>
        /// <returns>The loaded configuration.</returns>
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
                        AutoCheckForUpdates = Convert.ToBoolean(reader["AutoCheckForUpdates"])

                    };
                }
            }
            return _instance;
        }

        /// <summary>
        /// Saves the configuration to the database.
        /// </summary>
        public static void SaveToDatabase()
        {
            var config = Instance;

            using var connection = DbManager.Instance.GetConnection();
            
            App.LogDebug($"Config Begin Transaction");
            using var transaction = connection.BeginTransaction();
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = "DELETE FROM Config";
                _ = command.ExecuteNonQuery();

                command.CommandText = @"
                                INSERT INTO Config (
                                    GameFolder,
                                    AutoCheckForUpdates
                                ) VALUES (
                                    @GameFolder,
                                    @AutoCheckForUpdates
                                )";

                _ = command.Parameters.AddWithValue("@GameFolder", config.GameFolder ?? (object)DBNull.Value);
                _ = command.Parameters.AddWithValue("@AutoCheckForUpdates", config.AutoCheckForUpdates ? 1 : 0);

                _ = command.ExecuteNonQuery();
            }
            App.LogDebug($"Config  Commit Transaction");
            transaction.Commit();
        }
    }
}