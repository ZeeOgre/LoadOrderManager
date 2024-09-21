using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public int GroupCount => Groups?.Count ?? 0;
        public int ActiveLoadOutID { get; set; } = 1;

        public int? ActiveGroupSetID()
        {
            var activeLoadOut = LoadOuts.FirstOrDefault(lo => lo.ProfileID == ActiveLoadOutID);
            return activeLoadOut?.GroupSet?.GroupSetID;
        }

        private AggLoadInfo() { }

        public AggLoadInfo(LoadOut loadOut)
        {
            UpdateFromLoadOut(loadOut);
        }

        // Constructor to take a LoadOut and a GroupSet as arguments
        public AggLoadInfo(LoadOut loadOut, GroupSet groupSet)
        {
            UpdateFromLoadOut(loadOut);
            Groups.Clear();
            foreach (var modGroup in groupSet.ModGroups)
            {
                Groups.Add(modGroup);
            }
        }

        public void UpdateFromLoadOut(LoadOut loadOut)
        {
            // Update the singleton's state based on the selected LoadOut
            this.Plugins = new ObservableCollection<Plugin>(loadOut.Plugins.Select(pvm => pvm.Plugin));
            this.Groups = loadOut.GroupSet.ModGroups;
            this.LoadOuts = new ObservableCollection<LoadOut> { loadOut };
            // Update other properties as needed
        }

        public void LoadFromPluginsTxt(string filePath = null)
        {
            FileManager.ParsePluginsTxt(this, filePath);
        }







        public void InitFromDatabase()
        {
            bool shouldInitialize = !_initialized;

            if (!shouldInitialize)
            {
                var groupSet = GroupSet.LoadGroupSet(ActiveLoadOutID);
                if (groupSet != null)
                {
                    if ((groupSet.GroupSetFlags & (GroupFlags.DefaultGroup | GroupFlags.ReadOnly | GroupFlags.FilesLoaded | GroupFlags.ReadyToLoad)) != 0)
                    {
                        // Abort if any of these flags are set
                        return;
                    }
                }
            }

            lock (_lock)
            {
                if (!shouldInitialize) return;

                InitializationManager.StartInitialization("Database");

                Plugins.Clear();
                Groups.Clear();
                //LoadOuts.Clear();

                using var connection = DbManager.Instance.GetConnection();
                using (var command = new SQLiteCommand(@"
                        SELECT *
                        FROM vwPluginGrpUnion
                        WHERE GroupSetID IN (@GroupSetID, 1)", connection)) // Include default ID (1)
                {
                    command.Parameters.AddWithValue("@GroupSetID",this.ActiveGroupSetID());
                    using (var reader = command.ExecuteReader())
                    {
                        var pluginDict = new Dictionary<int, Plugin>();
                        var groupDict = new Dictionary<int, ModGroup>();

                        while (reader.Read())
                        {
                            var pluginID = reader.IsDBNull(reader.GetOrdinal("PluginID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("PluginID"));
                            var groupID = reader.GetInt32(reader.GetOrdinal("GroupID"));
                            //var profileID = reader.IsDBNull(reader.GetOrdinal("ProfileID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("ProfileID"));
                            var profileID = ActiveLoadOutID;

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

                                plugin.Files = new FileInfo().LoadFilesByPlugin(plugin.PluginID);

                                pluginDict[pluginID.Value] = plugin;
                                Plugins.Add(plugin);
                            }

                            if (!groupDict.ContainsKey(groupID))
                            {
                                var modGroup = new ModGroup
                                {
                                    GroupID = groupID,
                                    GroupName = reader.GetString(reader.GetOrdinal("GroupName")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("GroupDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("GroupDescription")),
                                    ParentID = reader.IsDBNull(reader.GetOrdinal("ParentID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("ParentID")),
                                    GroupSetID = this.ActiveGroupSetID(),
                                    Plugins = new ObservableCollection<Plugin>()
                                };
                                groupDict[groupID] = modGroup;
                                Groups.Add(modGroup);
                            }

                            if (pluginID.HasValue && groupDict.ContainsKey(groupID))
                            {
                                var plugin = pluginDict[pluginID.Value];
                                var modGroup = groupDict[groupID];
                                modGroup.Plugins.Add(plugin);
                            }

                            // Since ProfileID is now an int, we don't need to check for HasValue
                            var loadOut = LoadOuts.FirstOrDefault(l => l.ProfileID == profileID);
                            if (loadOut != null && pluginID.HasValue)
                            {
                                var plugin = pluginDict[pluginID.Value];
                                loadOut.Plugins.Add(new PluginViewModel(plugin, loadOut));
                            }
                        }
                    }
                }

                if (!LoadOuts.Any())
                {
                    var nextLoadOutID = GetNextAvailableLoadOutID();
                    ActiveLoadOutID = nextLoadOutID;
                    var newLoadOut = new LoadOut
                    {
                        ProfileID = nextLoadOutID,
                        Name = $"New LoadOut_{nextLoadOutID}",
                        GroupSet = null // or set to 1 if you want to use the default GroupSet
                    };
                    LoadOuts.Add(newLoadOut);
                }
                else
                {
                    var activeLoadOut = LoadOuts.First();
                    // Ensure ReadyToLoad flag is set
                    activeLoadOut.GroupSet.GroupSetFlags |= GroupFlags.ReadyToLoad;
                }

                App.LogDebug("Init From Database Complete");
                _initialized = true;

                SaveToDatabase();
            }
        }

        private int GetNextAvailableLoadOutID()
        {
            using var connection = DbManager.Instance.GetConnection();
            using (var command = new SQLiteCommand("SELECT IFNULL(MAX(ProfileID), 0) + 1 FROM LoadOutProfiles", connection))
            {
                var result = command.ExecuteScalar();
                return Convert.ToInt32(result);
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

        public void LoadFiles(string pluginsFile = null, string contentCatalog = null, bool scanGameFolder = false)
        {
            App.LogDebug("FileManager: LoadOut is initialized and in the ready to load state. Proceeding with initialization...");

            // Pass the current AggLoadInfo instance to ParsePluginsTxt
            FileManager.ParsePluginsTxt(this, pluginsFile);

            // Parse the content catalog if provided
            FileManager.ParseContentCatalogTxt(contentCatalog);

            // Scan the game directory for strays if requested
            if (scanGameFolder)
            {
                FileManager.ScanGameDirectoryForStrays();
            }

            // Mark the loadout as complete
            FileManager.MarkLoadOutComplete(this);
        }
    }

}
