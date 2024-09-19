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

        public void UpdateFromLoadOut(LoadOut loadOut)
        {
            // Update the singleton's state based on the selected LoadOut
            this.Plugins = new ObservableCollection<Plugin>(loadOut.Plugins.Select(pvm => pvm.Plugin));
            this.Groups = loadOut.GroupSet.ModGroups;
            this.LoadOuts = new ObservableCollection<LoadOut> { loadOut };
            // Update other properties as needed
        }

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
                using (var command = new SQLiteCommand(@"
    SELECT 
        p.PluginID, 
        p.PluginName, 
        p.Description, 
        p.Achievements, 
        p.DTStamp, 
        p.Version, 
        p.State, 
        p.GroupID AS PluginGroupID, 
        p.GroupOrdinal, 
        g.GroupID AS GroupID,  
        g.GroupName AS GroupName,      
        g.Description AS GroupDescription, 
        g.ParentID, 
        g.Ordinal AS GroupGroupOrdinal, 
        pp.ProfileID, 
        e.BethesdaID, 
        e.NexusID, 
        l.GroupSetID, 
        gs.GroupSetName 
    FROM 
        vwPluginGrpUnion", connection))
                using (var reader = command.ExecuteReader())
                {
                    var pluginDict = new Dictionary<int, Plugin>();
                    var groupDict = new Dictionary<int, ModGroup>();

                    while (reader.Read())
                    {
                        var pluginID = reader.IsDBNull(reader.GetOrdinal("PluginID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("PluginID"));
                        var groupID = reader.GetInt32(reader.GetOrdinal("GroupID"));
                        var profileID = reader.IsDBNull(reader.GetOrdinal("ProfileID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("ProfileID"));

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
                                State = reader.IsDBNull(reader.GetOrdinal("State")) ? (ModState)0 : (ModState)reader.GetInt32(reader.GetOrdinal("State")),
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

                        // Associate Plugin with LoadOut
                        if (profileID.HasValue)
                        {
                            var loadOut = LoadOuts.FirstOrDefault(l => l.ProfileID == profileID.Value);
                            if (loadOut != null && pluginID.HasValue)
                            {
                                var plugin = pluginDict[pluginID.Value];
                                loadOut.Plugins.Add(new PluginViewModel(plugin, true)); // Assuming the plugin is enabled
                            }
                        }
                    }
                }
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
                    using (var command = new SQLiteCommand(@"
                        SELECT 
                            p.PluginID, 
                            p.PluginName, 
                            p.Description, 
                            p.Achievements, 
                            p.DTStamp, 
                            p.Version, 
                            p.GroupID AS PluginGroupID, 
                            p.GroupOrdinal, 
                            g.GroupID AS GroupID,  
                            g.GroupName AS GroupName,      
                            g.Description AS GroupDescription, 
                            g.ParentID, 
                            g.Ordinal AS GroupGroupOrdinal, 
                            pp.ProfileID, 
                            e.BethesdaID, 
                            e.NexusID 
                        FROM 
                            Plugins p 
                        LEFT JOIN 
                            ModGroups g ON p.GroupID = g.GroupID 
                        LEFT JOIN 
                            ProfilePlugins pp ON p.PluginID = pp.PluginID 
                        LEFT JOIN 
                            ExternalIDs e ON p.PluginID = e.PluginID 
                        UNION 
                        SELECT 
                            NULL AS PluginID, 
                            NULL AS PluginName, 
                            NULL AS Description, 
                            NULL AS Achievements, 
                            NULL AS DTStamp, 
                            NULL AS Version, 
                            g.GroupID AS PluginGroupID, 
                            NULL AS GroupOrdinal, 
                            g.GroupID AS GroupID,    
                            g.GroupName AS GroupName,      
                            g.Description AS GroupDescription, 
                            g.ParentID, 
                            g.Ordinal AS GroupGroupOrdinal, 
                            NULL AS ProfileID, 
                            NULL AS BethesdaID, 
                            NULL AS NexusID 
                        FROM 
                            ModGroups g", connection))
                    using (var reader = command.ExecuteReader())
                    {
                        var pluginDict = new Dictionary<int, Plugin>();
                        var groupDict = new Dictionary<int, ModGroup>();

                        while (reader.Read())
                        {
                            var pluginID = reader.IsDBNull(reader.GetOrdinal("PluginID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("PluginID"));
                            var groupID = reader.GetInt32(reader.GetOrdinal("GroupID"));
                            var profileID = reader.IsDBNull(reader.GetOrdinal("ProfileID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("ProfileID"));

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

                            // Associate Plugin with LoadOut
                            if (profileID.HasValue)
                            {
                                var loadOut = LoadOuts.FirstOrDefault(l => l.ProfileID == profileID.Value);
                                if (loadOut != null && pluginID.HasValue)
                                {
                                    var plugin = pluginDict[pluginID.Value];
                                    loadOut.Plugins.Add(new PluginViewModel(plugin, loadOut)); // Assuming the plugin is enabled
                                }
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

        // Clone method
        public AggLoadInfo Clone()
        {
            return new AggLoadInfo
            {
                Plugins = new ObservableCollection<Plugin>(this.Plugins),
                Groups = new ObservableCollection<ModGroup>(this.Groups),
                LoadOuts = new ObservableCollection<LoadOut>(this.LoadOuts)
            };
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
