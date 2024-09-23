using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ZO.LoadOrderManager
{
    public class AggLoadInfo : INotifyPropertyChanged
    {
        // Singleton Instance with Lazy Initialization
        private static readonly Lazy<AggLoadInfo> _instance = new Lazy<AggLoadInfo>(() => new AggLoadInfo());
        public static AggLoadInfo Instance => _instance.Value;

        // Observable Collections for core entities
        public ObservableCollection<Plugin> Plugins { get; set; } = new ObservableCollection<Plugin>();
        public ObservableCollection<ModGroup> Groups { get; set; } = new ObservableCollection<ModGroup>();
        public ObservableCollection<LoadOut> LoadOuts { get; set; } = new ObservableCollection<LoadOut>();

        // Collections for GroupSet Mappings and Relationships
        public GroupSetGroupCollection GroupSetGroups { get; set; } = new GroupSetGroupCollection();
        public GroupSetPluginCollection GroupSetPlugins { get; set; } = new GroupSetPluginCollection();
        public ProfilePluginCollection ProfilePlugins { get; set; } = new ProfilePluginCollection();

        // Current active LoadOut and GroupSet
        public LoadOut ActiveLoadOut { get; set; }
        public GroupSet ActiveGroupSet { get; set; }

        // Boolean flag to indicate initialization state
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        // Private constructor for Singleton pattern
        private AggLoadInfo() { }

        // Method to initialize from the database
        public void InitFromDatabase()
        {
            // Only proceed if not already initialized
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                Plugins.Clear();
                Groups.Clear();
                LoadOuts.Clear();
                GroupSetGroups.Items.Clear();
                GroupSetPlugins.Items.Clear();
                ProfilePlugins.Items.Clear();

                // Load data from the database into the respective collections
                using var connection = DbManager.Instance.GetConnection();
                LoadGroupSetState(connection);

                // Mark as initialized
                _initialized = true;
            }
        }

        // Load GroupSet data including plugins, groups, and profiles
        private void LoadGroupSetState(SQLiteConnection connection)
        {
            // Load core entities from vwPluginGrpUnion based on GroupSetID
            using var command = new SQLiteCommand(@"
                SELECT * 
                FROM vwPluginGrpUnion
                WHERE GroupSetID = @GroupSetID", connection);
            command.Parameters.AddWithValue("@GroupSetID", ActiveGroupSet.GroupSetID);

            using var reader = command.ExecuteReader();
            var pluginDict = new Dictionary<int, Plugin>();
            var groupDict = new Dictionary<int, ModGroup>();

            while (reader.Read())
            {
                // Load Plugins
                LoadPluginFromReader(reader, pluginDict);

                // Load Groups
                LoadGroupFromReader(reader, groupDict);

                // Associate Plugins with Groups
                AssociatePluginsWithGroups(reader, pluginDict, groupDict);
            }

            // Load GroupSetGroups, GroupSetPlugins, and ProfilePlugins for Active GroupSet
            GroupSetGroups.LoadGroupSetGroups(ActiveGroupSet.GroupSetID, connection);
            GroupSetPlugins.LoadGroupSetPlugins(ActiveGroupSet.GroupSetID, connection);
            ProfilePlugins.LoadProfilePlugins(ActiveGroupSet.GroupSetID, connection);
        }

        // Load a plugin from the data reader
        private void LoadPluginFromReader(SQLiteDataReader reader, Dictionary<int, Plugin> pluginDict)
        {
            var pluginID = reader.GetInt32(reader.GetOrdinal("PluginID"));
            if (!pluginDict.ContainsKey(pluginID))
            {
                var plugin = new Plugin
                {
                    PluginID = pluginID,
                    PluginName = reader.GetString(reader.GetOrdinal("PluginName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                    Achievements = reader.GetBoolean(reader.GetOrdinal("Achievements")),
                    DTStamp = reader.GetString(reader.GetOrdinal("DTStamp")),
                    Version = reader.GetString(reader.GetOrdinal("Version")),
                    State = reader.IsDBNull(reader.GetOrdinal("State")) ? ModState.None : (ModState)reader.GetInt32(reader.GetOrdinal("State")),
                    BethesdaID = reader.IsDBNull(reader.GetOrdinal("BethesdaID")) ? string.Empty : reader.GetString(reader.GetOrdinal("BethesdaID")),
                    NexusID = reader.IsDBNull(reader.GetOrdinal("NexusID")) ? string.Empty : reader.GetString(reader.GetOrdinal("NexusID")),
                    GroupID = reader.IsDBNull(reader.GetOrdinal("PluginGroupID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("PluginGroupID")),
                    GroupOrdinal = reader.IsDBNull(reader.GetOrdinal("GroupOrdinal")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("GroupOrdinal")),
                };

                Plugins.Add(plugin);
                pluginDict[pluginID] = plugin;
            }
        }

        // Load a group from the data reader
        private void LoadGroupFromReader(SQLiteDataReader reader, Dictionary<int, ModGroup> groupDict)
        {
            var groupID = reader.GetInt32(reader.GetOrdinal("GroupID"));
            if (!groupDict.ContainsKey(groupID))
            {
                var modGroup = new ModGroup
                {
                    GroupID = groupID,
                    GroupName = reader.GetString(reader.GetOrdinal("GroupName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("GroupDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("GroupDescription")),
                    ParentID = reader.IsDBNull(reader.GetOrdinal("ParentID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("ParentID")),
                    GroupSetID = ActiveGroupSet.GroupSetID,
                    Plugins = new ObservableCollection<Plugin>()
                };

                Groups.Add(modGroup);
                groupDict[groupID] = modGroup;
            }
        }

        // Associate plugins with groups based on the data reader
        private void AssociatePluginsWithGroups(SQLiteDataReader reader, Dictionary<int, Plugin> pluginDict, Dictionary<int, ModGroup> groupDict)
        {
            var pluginID = reader.GetInt32(reader.GetOrdinal("PluginID"));
            var groupID = reader.GetInt32(reader.GetOrdinal("GroupID"));

            if (pluginDict.ContainsKey(pluginID) && groupDict.ContainsKey(groupID))
            {
                var plugin = pluginDict[pluginID];
                var modGroup = groupDict[groupID];
                if (!modGroup.Plugins.Contains(plugin))
                {
                    modGroup.Plugins.Add(plugin);
                }
            }
        }

        // Clone method to create a copy of the current AggLoadInfo
        public AggLoadInfo Clone()
        {
            return new AggLoadInfo
            {
                Plugins = new ObservableCollection<Plugin>(this.Plugins),
                Groups = new ObservableCollection<ModGroup>(this.Groups),
                LoadOuts = new ObservableCollection<LoadOut>(this.LoadOuts),
                GroupSetGroups = new GroupSetGroupCollection { Items = new ObservableCollection<(int, int, int?, int)>(this.GroupSetGroups.Items) },
                GroupSetPlugins = new GroupSetPluginCollection { Items = new ObservableCollection<(int, int, int, int)>(this.GroupSetPlugins.Items) },
                ProfilePlugins = new ProfilePluginCollection { Items = new ObservableCollection<(int, int)>(this.ProfilePlugins.Items) },
                ActiveLoadOut = this.ActiveLoadOut,
                ActiveGroupSet = this.ActiveGroupSet
            };
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
