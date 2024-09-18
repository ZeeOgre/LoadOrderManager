using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZO.LoadOrderManager
{
    public class AggLoadInfo
    {
        private static readonly Lazy<AggLoadInfo> instance = new Lazy<AggLoadInfo>(() => new AggLoadInfo());
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        public static AggLoadInfo Instance => instance.Value;

        public ObservableCollection<Plugin> Plugins { get; set; } = new ObservableCollection<Plugin>();
        public ObservableCollection<ModGroup> Groups { get; set; } = new ObservableCollection<ModGroup>();
        public ObservableCollection<LoadOut> LoadOuts { get; set; } = new ObservableCollection<LoadOut>();

        private AggLoadInfo() { }

        public int GroupCount => Groups?.Count ?? 0;

        public void LoadFromPluginsTxt(string filePath)
        {
            filePath ??= FileManager.PluginsFile;
            FileManager.ParsePluginsTxt(filePath);
        }

        public void InitFromDatabase(bool? forceRefresh = null)
        {
            bool shouldInitialize = forceRefresh == true || !_initialized;

            if (!shouldInitialize) return;

            lock (_lock)
            {
                if (!shouldInitialize) return;

                // Ensure the database is initialized using InitializationManager
                InitializationManager.StartInitialization("Database");

                // Clear existing data to avoid duplicates
                Plugins.Clear();
                Groups.Clear();
                LoadOuts.Clear();

                using var connection = DbManager.Instance.GetConnection();

                App.LogDebug("Init From Database");

                try
                {
                    // Load LoadOuts
                    using (var command = new SQLiteCommand("SELECT DISTINCT ProfileID, ProfileName FROM vwLoadOuts", connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var profileID = reader.GetInt32(reader.GetOrdinal("ProfileID"));
                            var loadOut = LoadOuts.FirstOrDefault(l => l.ProfileID == profileID) ?? new LoadOut
                            {
                                ProfileID = profileID,
                                Name = reader.GetString(reader.GetOrdinal("ProfileName")),
                                Plugins = new ObservableCollection<PluginViewModel>()
                            };
                            LoadOuts.Add(loadOut);
                        }
                    }

                    // Load Plugins and Groups
                    using (var command = new SQLiteCommand("SELECT * FROM vwPluginGrpUnion", connection))
                    using (var reader = command.ExecuteReader())
                    {
                        var pluginDict = new Dictionary<int, Plugin>();
                        var groupDict = new Dictionary<int, ModGroup>();

                        while (reader.Read())
                        {
                            var pluginID = reader.IsDBNull(reader.GetOrdinal("PluginID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("PluginID"));
                            var groupID = reader.GetInt32(reader.GetOrdinal("GroupID"));

                            // Process Plugin
                            if (pluginID.HasValue && !pluginDict.ContainsKey(pluginID.Value))
                            {
                                var plugin = new Plugin
                                {
                                    PluginID = pluginID.Value,
                                    PluginName = reader.IsDBNull(reader.GetOrdinal("PluginName")) ? string.Empty : reader.GetString(reader.GetOrdinal("PluginName")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                                    Achievements = !reader.IsDBNull(reader.GetOrdinal("Achievements")) && reader.GetInt64(reader.GetOrdinal("Achievements")) != 0,
                                    DTStamp = reader.IsDBNull(reader.GetOrdinal("DTStamp")) ? string.Empty : reader.GetString(reader.GetOrdinal("DTStamp")),
                                    Version = reader.IsDBNull(reader.GetOrdinal("Version")) ? string.Empty : reader.GetString(reader.GetOrdinal("Version")),
                                    BethesdaID = reader.IsDBNull(reader.GetOrdinal("BethesdaID")) ? string.Empty : reader.GetString(reader.GetOrdinal("BethesdaID")),
                                    NexusID = reader.IsDBNull(reader.GetOrdinal("NexusID")) ? string.Empty : reader.GetString(reader.GetOrdinal("NexusID")),
                                    GroupID = reader.IsDBNull(reader.GetOrdinal("PluginGroupID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("PluginGroupID")),
                                    GroupOrdinal = reader.IsDBNull(reader.GetOrdinal("GroupOrdinal")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("GroupOrdinal"))
                                };

                                // Load FileInfo objects for the plugin
                                plugin.Files = new FileInfo().LoadFilesByPlugin(plugin.PluginID);

                                pluginDict[pluginID.Value] = plugin;
                                Plugins.Add(plugin);
                            }

                            // Process Group
                            if (!groupDict.ContainsKey(groupID))
                            {
                                var modGroup = new ModGroup
                                {
                                    GroupID = groupID,
                                    GroupName = reader.GetString(reader.GetOrdinal("GroupName")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("GroupDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("GroupDescription")),
                                    ParentID = reader.IsDBNull(reader.GetOrdinal("ParentID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("ParentID")),
                                    Plugins = new ObservableCollection<Plugin>()
                                };
                                groupDict[groupID] = modGroup;
                                Groups.Add(modGroup);
                            }

                            // Associate Plugin with Group
                            if (pluginID.HasValue && groupDict.ContainsKey(groupID))
                            {
                                var plugin = pluginDict[pluginID.Value];
                                var modGroup = groupDict[groupID];
                                modGroup.Plugins.Add(plugin);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    App.LogDebug($"Error in InitFromDatabase: {ex.Message}");
                    throw;
                }

                App.LogDebug("Init From Database Complete");
                _initialized = true;
                SaveToDatabase();
            }
        }

        public void SaveToDatabase()
        {
            //bool textLoggingEnabled = bool.TryParse(ConfigurationManager.AppSettings["TextLogging"], out bool result) && result;
            bool textLoggingEnabled = false;
            if (textLoggingEnabled)
            {
                SaveToDatabaseWithBackup();
            }
            else
            {
                PerformSaveToDatabase();
            }
        }

        private void PerformSaveToDatabase()
        {
            var pluginsCopy = Plugins.ToList(); // Create a copy of the Plugins collection
            var groupsCopy = Groups.ToList();   // Create a copy of the Groups collection
            var loadOutsCopy = LoadOuts.ToList(); // Create a copy of the LoadOuts collection

            foreach (var plugin in pluginsCopy)
            {
                plugin.WriteMod();
            }

            foreach (var group in groupsCopy)
            {
                group.WriteGroup();
            }

            foreach (var loadout in loadOutsCopy)
            {
                loadout.WriteProfile();
            }
        }

        public void SaveToDatabaseWithBackup(string? preSaveBackupPath = null, string? postSaveBackupPath = null)
        {
            // Create a backup before saving to the database if not provided
            preSaveBackupPath ??= DbManager.Instance.BackupDB();

            // Proceed with the save operation
            PerformSaveToDatabase();

            // Create a backup after saving to the database if not provided
            postSaveBackupPath ??= DbManager.Instance.BackupDB();

            if (!string.IsNullOrEmpty(preSaveBackupPath) && !string.IsNullOrEmpty(postSaveBackupPath))
            {
                // Compute hashes for comparison
                string preSaveHash = FileInfo.ComputeHash(preSaveBackupPath);
                string postSaveHash = FileInfo.ComputeHash(postSaveBackupPath);

                if (preSaveHash == postSaveHash)
                {
                    Console.WriteLine("The database state is unchanged after the save operation.");
                }
                else
                {
                    Console.WriteLine("The database state has changed after the save operation.");
                }
            }
        }
    }

}
