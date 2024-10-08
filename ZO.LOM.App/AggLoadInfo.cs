using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        private static readonly Lazy<AggLoadInfo> _instance = new Lazy<AggLoadInfo>(() => new AggLoadInfo());
        public static AggLoadInfo Instance => _instance.Value;

        public ObservableCollection<Plugin> Plugins { get; set; } = new ObservableCollection<Plugin>();
        public ObservableCollection<ModGroup> Groups { get; set; } = new ObservableCollection<ModGroup>();
        public ObservableCollection<LoadOut> LoadOuts { get; set; } = new ObservableCollection<LoadOut>();
        public ObservableCollection<GroupSet> GroupSets { get; set; } = new ObservableCollection<GroupSet>();

        public GroupSetGroupCollection GroupSetGroups { get; set; } = new GroupSetGroupCollection();
        public GroupSetPluginCollection GroupSetPlugins { get; set; } = new GroupSetPluginCollection();
        public ProfilePluginCollection ProfilePlugins { get; set; } = new ProfilePluginCollection();
        
        public event EventHandler? DataRefreshed;

        public LoadOut ActiveLoadOut
        {
            get => _activeLoadOut;
            set
            {
                if (_activeLoadOut != value)
                {
                    _activeLoadOut = value;
                    
                    if (_activeLoadOut != null)
                    {
                        _activeLoadOut.enabledPlugins = LoadEnabledPlugins(_activeLoadOut.ProfileID);
                    }
                }
            }
        }

        private LoadOut _activeLoadOut;

        public GroupSet ActiveGroupSet
        {
            get => _activeGroupSet;
            set
            {
                if (_activeGroupSet != value)
                {
                    _activeGroupSet = value ?? throw new ArgumentNullException(nameof(value));
                    OnPropertyChanged(nameof(ActiveGroupSet));
                    //// Only trigger InitFromDatabase if initialization is complete
                    //if (_initialized)
                    //{
                    //    InitFromDatabase();
                    //}
                    // Check if the ActiveGroupSet has changed and reload data if necessary
                    if (_activeGroupSet != null && _activeGroupSet != _cachedGroupSet1 && _initialized)
                    {
                        LoadGroupSetState();
                    }

                    //OnPropertyChanged();
                }
            }
        }

        private GroupSet _activeGroupSet;
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        private ObservableCollection<GroupSet>? _cachedGroupSets;
      
        public ObservableCollection<GroupSet> GetGroupSets()
        {
            if (_cachedGroupSets == null)
            {
                _cachedGroupSets = LoadGroupSetsFromDatabase();
            }
            return _cachedGroupSets;
        }

        private GroupSet? _cachedGroupSet1;

        public GroupSet? GetCachedGroupSet1()
        {
            return _cachedGroupSet1;
        }

        private AggLoadInfo()
        {
            PropertyChanged = null;
            GroupSet.GroupSetChanged += OnGroupSetChanged;
        }

        public AggLoadInfo(long groupSetID)
        {
            if (groupSetID == 0)
            {
                ActiveGroupSet = new GroupSet();
                PropertyChanged = null;
            }
            else
            {
                ActiveGroupSet = GroupSet.LoadGroupSet(groupSetID) ?? throw new InvalidOperationException("GroupSet not found.");
                InitFromDatabase();

            }
        }

        public void InitFromDatabase()
        {
            if (_initialized) return;
            if (ActiveGroupSet.GroupSetID == 0) return;

            lock (_lock)
            {
                if (_initialized) return;

                Plugins.Clear();
                Groups.Clear();
                LoadOuts.Clear();

                //if (_activeLoadOut != null && !LoadOuts.Contains(_activeLoadOut))
                //{
                //    LoadOuts.Add(_activeLoadOut);
                //}

                if (_activeGroupSet != null && !GroupSets.Contains(_activeGroupSet))
                {
                    GroupSets.Add(_activeGroupSet);
                }

                GroupSetGroups.Items.Clear();
                GroupSetPlugins.Items.Clear();
                ProfilePlugins.Items.Clear();

                GroupSets = GetGroupSets();
                InitializationManager.ReportProgress(25, "GroupSets loaded from database");

                if (_cachedGroupSet1 == null)
                {
                    _cachedGroupSet1 = GroupSet.LoadGroupSet(1);
                    if (_cachedGroupSet1 == null)
                    {
                        App.LogDebug("Failed to load GroupSet 1.");
                        return;
                    }
                    App.LogDebug("GroupSet 1 loaded into cache.");
                }
                InitializationManager.ReportProgress(35, "GroupSet 1 cached");

               
                LoadGroupSetState();
                RefreshMetadataFromDB();

                _initialized = true;
                InitializationManager.ReportProgress(75, "GroupSet state fully loaded");
            }
        }

        private void LoadGroupSetState()
        {
            using var connection = DbManager.Instance.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Your existing logic to load group set state
                // ...

                // First command and reader for vwPluginGrpUnion
                using var command1 = new SQLiteCommand(@"
                    SELECT DISTINCT * FROM (
                        SELECT *
                        FROM vwPluginGrpUnion
                        WHERE GroupSetID = @GroupSetID);", connection);
                command1.Parameters.AddWithValue("@GroupSetID", ActiveGroupSet.GroupSetID);

                Console.WriteLine($"GroupSetID: {ActiveGroupSet.GroupSetID}");
                Console.WriteLine($"Executing query: {command1.CommandText} with GroupSetID = {ActiveGroupSet.GroupSetID}");

                using var reader1 = command1.ExecuteReader();
                var pluginDict = new Dictionary<long, Plugin>();
                var groupDict = new Dictionary<long, ModGroup>();

                while (reader1.Read())
                {
                    LoadPluginFromReader(reader1, pluginDict);
                    LoadGroupFromReader(reader1, groupDict);
                }

                if (!groupDict.ContainsKey(1))
                {
                    var cachedGroup1 = _cachedGroupSet1?.ModGroups.FirstOrDefault(g => g.GroupID == 1);
                    if (cachedGroup1 != null)
                    {
                        var clonedGroup = cachedGroup1.Clone();
                        Groups.Add(clonedGroup);
                        groupDict[1] = clonedGroup;
                    }
                }

                foreach (var plugin in pluginDict.Values)
                {
                    if (plugin.GroupID.HasValue && groupDict.ContainsKey(plugin.GroupID.Value))
                    {
                        var modGroup = groupDict[plugin.GroupID.Value];
                        if (!modGroup.Plugins.Contains(plugin))
                        {
                            modGroup.Plugins.Add(plugin);
                        }
                    }
                }

                // Second command and reader for vwLoadOuts
                using var command2 = new SQLiteCommand(@"
                    SELECT DISTINCT ProfileID, ProfileName, GroupSetID
                    FROM vwLoadOuts
                    WHERE GroupSetID = @GroupSetID;", connection);
                command2.Parameters.AddWithValue("@GroupSetID", ActiveGroupSet.GroupSetID);

                using var reader2 = command2.ExecuteReader();
                var loadOuts = new ObservableCollection<LoadOut>();

                while (reader2.Read())
                {
                    var profileID = reader2.GetInt64(0);
                    var name = reader2.GetString(1);
                    var groupSetID = reader2.GetInt64(2);

                    var loadOut = new LoadOut
                    {
                        ProfileID = profileID,
                        GroupSetID = groupSetID,
                        Name = name,
                        enabledPlugins = LoadEnabledPlugins(profileID)
                    };

                    loadOuts.Add(loadOut);
                }

                transaction.Commit();
                // Assign the loaded load-outs to the appropriate property
                LoadOuts = loadOuts;

                // Create and populate Group -997 with unassigned plugins
                CreateAndPopulateGroup997(connection, pluginDict);


                

            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        private void CreateAndPopulateGroup997(SQLiteConnection conn, Dictionary<long, Plugin> pluginDict)
        {
            var unassignedPlugins = new List<Plugin>();
            var unassignedPluginDict = new Dictionary<long, Plugin>(); // Correct scope

            using var command = new SQLiteCommand(@"
                SELECT DISTINCT * FROM (
                    SELECT *
                    FROM vwPluginGrpUnion
                    WHERE GroupSetID != @GroupSetIDz
                    AND GroupID NOT IN (-998, -999, 1));", conn);

            command.Parameters.AddWithValue("@GroupSetIDz", ActiveGroupSet.GroupSetID); // Corrected parameter

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var plugin = new Plugin();
                LoadPluginFromReader(reader, unassignedPluginDict);

            }

            reader.Close(); // Close the reader after reading the data

            // Populate unassignedPlugins list
            foreach (var plugin in unassignedPluginDict.Values)
            {
                plugin.GroupSetID = ActiveGroupSet.GroupSetID;
                unassignedPlugins.Add(plugin);
            }

            var unassignedGroup = Groups.FirstOrDefault(g => g.GroupID == -997);
            if (unassignedGroup == null)
            {
                unassignedGroup = new ModGroup(-997, "Unassigned", "Plugins not assigned to any group",1,9997,ActiveGroupSet.GroupSetID);
                Groups.Add(unassignedGroup);
            }

            unassignedGroup.Plugins.Clear();
            foreach (var plugin in unassignedPlugins)
            {
                unassignedGroup.Plugins.Add(plugin);
                plugin.GroupOrdinal = unassignedGroup.Plugins.Count;
                plugin.WriteMod();
            }
        }

        private ObservableHashSet<long> LoadEnabledPlugins(long profileID, SQLiteConnection? connection = null)
        {
            using var conn = connection ?? DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(@"
                SELECT PluginID
                FROM ProfilePlugins
                WHERE ProfileID = @ProfileID;", conn);
            command.Parameters.AddWithValue("@ProfileID", profileID);

            using var reader = command.ExecuteReader();
            var enabledPlugins = new ObservableHashSet<long>();

            while (reader.Read())
            {
                enabledPlugins.Add(reader.GetInt64(0));
            }

            return enabledPlugins;
        }

        // Associate plugins with groups based on the data reader
        public void AssociatePluginsWithGroups(SQLiteDataReader reader, Dictionary<long, Plugin> pluginDict, Dictionary<long, ModGroup> groupDict)
        {
            if (reader.IsDBNull(reader.GetOrdinal("PluginID")) || reader.IsDBNull(reader.GetOrdinal("GroupID")))
            {
                return;
            }

            var pluginID = reader.GetInt64(reader.GetOrdinal("PluginID"));
            var groupID = reader.GetInt64(reader.GetOrdinal("GroupID"));

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

        private void LoadPluginFromReader(SQLiteDataReader reader, Dictionary<long, Plugin> pluginDict)
        {
            if (reader.IsDBNull(reader.GetOrdinal("PluginID")))
            {
                return;
            }

            var pluginID = reader.GetInt64(reader.GetOrdinal("PluginID"));

            if (!pluginDict.ContainsKey(pluginID))
            {
                var plugin = new Plugin
                {
                    PluginID = pluginID,
                    PluginName = reader.GetString(reader.GetOrdinal("PluginName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("PluginDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("PluginDescription")),
                    Achievements = reader.IsDBNull(reader.GetOrdinal("Achievements")) ? false : reader.GetInt64(reader.GetOrdinal("Achievements")) == 1,
                    DTStamp = reader.IsDBNull(reader.GetOrdinal("DTStamp")) ? string.Empty : reader.GetString(reader.GetOrdinal("DTStamp")),
                    Version = reader.IsDBNull(reader.GetOrdinal("Version")) ? string.Empty : reader.GetString(reader.GetOrdinal("Version")),
                    State = reader.IsDBNull(reader.GetOrdinal("State")) ? ModState.None : (ModState)reader.GetInt64(reader.GetOrdinal("State")),
                    BethesdaID = reader.IsDBNull(reader.GetOrdinal("BethesdaID")) ? string.Empty : reader.GetString(reader.GetOrdinal("BethesdaID")),
                    NexusID = reader.IsDBNull(reader.GetOrdinal("NexusID")) ? string.Empty : reader.GetString(reader.GetOrdinal("NexusID")),
                    GroupID = reader.IsDBNull(reader.GetOrdinal("GroupID")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("GroupID")),
                    GroupOrdinal = reader.IsDBNull(reader.GetOrdinal("GroupOrdinal")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("GroupOrdinal")),
                    GroupSetID = reader.IsDBNull(reader.GetOrdinal("GroupSetID")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("GroupSetID"))
                };

                Plugins.Add(plugin);
                pluginDict[pluginID] = plugin;
            }
        }

        private void LoadGroupFromReader(SQLiteDataReader reader, Dictionary<long, ModGroup> groupDict)
        {
            var groupID = reader.GetInt64(reader.GetOrdinal("GroupID"));

            if (!groupDict.ContainsKey(groupID))
            {
                var modGroup = new ModGroup
                {
                    GroupID = groupID,
                    GroupName = reader.GetString(reader.GetOrdinal("GroupName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("GroupDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("GroupDescription")),
                    ParentID = reader.IsDBNull(reader.GetOrdinal("ParentID")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("ParentID")),
                    Ordinal = reader.IsDBNull(reader.GetOrdinal("Ordinal")) ? 0 : reader.GetInt64(reader.GetOrdinal("Ordinal")),
                    GroupSetID = ActiveGroupSet.GroupSetID,
                    Plugins = new ObservableCollection<Plugin>()
                };

                Groups.Add(modGroup);
                groupDict[groupID] = modGroup;
            }
        }

        public static ObservableCollection<GroupSet> LoadGroupSetsFromDatabase(SQLiteConnection? connection = null)
        {
            using var conn = connection ?? DbManager.Instance.GetConnection();
            var groupSets = new ObservableCollection<GroupSet>();

            using var command = new SQLiteCommand("SELECT GroupSetID, GroupSetName, GroupSetFlags FROM GroupSets", conn);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var groupSet = new GroupSet(
                    reader.GetInt64(reader.GetOrdinal("GroupSetID")),
                    reader.GetString(reader.GetOrdinal("GroupSetName")),
                    (GroupFlags)reader.GetInt32(reader.GetOrdinal("GroupSetFlags"))
                );

                groupSets.Add(groupSet);
            }

            return groupSets;
        }

        public AggLoadInfo Clone()
        {
            return new AggLoadInfo
            {
                Plugins = new ObservableCollection<Plugin>(this.Plugins),
                Groups = new ObservableCollection<ModGroup>(this.Groups),
                LoadOuts = new ObservableCollection<LoadOut>(this.LoadOuts),
                GroupSetGroups = new GroupSetGroupCollection { Items = new ObservableCollection<(long, long, long?, long)>(this.GroupSetGroups.Items) },
                GroupSetPlugins = new GroupSetPluginCollection { Items = new ObservableCollection<(long, long, long, long)>(this.GroupSetPlugins.Items) },
                ProfilePlugins = new ProfilePluginCollection { Items = new ObservableHashSet<(long ProfileID, long PluginID)>() },
                ActiveLoadOut = this.ActiveLoadOut,
                ActiveGroupSet = this.ActiveGroupSet
            };
        }

        private void OnGroupSetChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Check if the changed group set is the current ActiveGroupSet
            if (e.NewItems?.Contains(ActiveGroupSet) == true)
            {
                // Refresh the ActiveGroupSet using the LoadGroupSet method
                ActiveGroupSet = GroupSet.LoadGroupSet(ActiveGroupSet.GroupSetID);
            }

            _cachedGroupSets = GetGroupSets();
        }

        public void UpdatePlugin(Plugin plugin)
        {
            // Check for existing entry in GroupSetPlugins for this plugin
            var existingGroupSetPlugin = GroupSetPlugins.Items.FirstOrDefault(gsp => gsp.pluginID == plugin.PluginID);

            // Handle the update based on existing GroupSet
            if (existingGroupSetPlugin != default)
            {
                // If the GroupID changes, we remove the old entry and add a new one
                if (existingGroupSetPlugin.groupID != plugin.GroupID)
                {
                    GroupSetPlugins.Items.Remove(existingGroupSetPlugin);

                    // Add the new entry with updated GroupID and Ordinal
                    GroupSetPlugins.Items.Add((ActiveGroupSet.GroupSetID, plugin.GroupID ?? -1, plugin.PluginID, plugin.GroupOrdinal ?? 1));
                }
                else if (existingGroupSetPlugin.Ordinal != plugin.GroupOrdinal)
                {
                    // Update Ordinal if only the order has changed
                    existingGroupSetPlugin = (existingGroupSetPlugin.groupSetID, existingGroupSetPlugin.groupID, existingGroupSetPlugin.pluginID, plugin.GroupOrdinal ?? 1);
                }
            }
            else
            {
                // If no entry exists, insert new entry
                GroupSetPlugins.Items.Add((ActiveGroupSet.GroupSetID, plugin.GroupID ?? -1, plugin.PluginID, plugin.GroupOrdinal ?? 1));
            }

            // Finally, save the plugin changes
            plugin.WriteMod();
        }

        public void UpdateModGroup(ModGroup group)
        {

        }

        public void UpdateLoadOut(LoadOut loadOut)
        {

        }   


        public void Save()
        {
            if (ActiveGroupSet == null)
            {
                throw new InvalidOperationException("ActiveGroupSet is not set.");
            }

            using var connection = DbManager.Instance.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Save GroupSet
                ActiveGroupSet.SaveGroupSet();

                // Save Groups
                foreach (var group in Groups.Where(g => g.GroupSetID == ActiveGroupSet.GroupSetID))
                {
                    group.WriteGroup();
                }

                // Save Plugins
                foreach (var plugin in Plugins.Where(p => p.GroupSetID == ActiveGroupSet.GroupSetID))
                {
                    plugin.WriteMod();
                }

                // Save LoadOuts
                foreach (var loadOut in LoadOuts.Where(l => l.GroupSetID == ActiveGroupSet.GroupSetID))
                {
                    loadOut.WriteProfile();
                }

                // Commit transaction
                transaction.Commit();

                // Reload GroupSetGroups, GroupSetPlugins, and ProfilePlugins
                GroupSetGroups.LoadGroupSetGroups(ActiveGroupSet.GroupSetID, connection);
                GroupSetPlugins.LoadGroupSetPlugins(ActiveGroupSet.GroupSetID, connection);
                ProfilePlugins.LoadProfilePlugins(ActiveGroupSet.GroupSetID, connection);
            }
            catch (Exception)
            {
                // Rollback transaction in case of an error
                transaction.Rollback();
                throw;
            }
        }

        public void RefreshMetadataFromDB(SQLiteConnection? connection = null)
        {
            using var conn = connection ?? DbManager.Instance.GetConnection();
            GroupSetGroups.LoadGroupSetGroups(ActiveGroupSet.GroupSetID, conn);
            GroupSetPlugins.LoadGroupSetPlugins(ActiveGroupSet.GroupSetID, conn);
            ProfilePlugins.LoadProfilePlugins(ActiveGroupSet.GroupSetID, conn);
        }

        public void RefreshAllData()
        {
            // Backup the cachedGroupSet1
            var backupCachedGroupSet1 = _cachedGroupSet1;

            // Perform the refresh operation
            LoadGroupSetState();
            RefreshMetadataFromDB();

            // Restore the cachedGroupSet1
            _cachedGroupSet1 = backupCachedGroupSet1;

            // Notify that the data has been refreshed
            OnPropertyChanged(nameof(Groups));
            OnPropertyChanged(nameof(Plugins));
            OnPropertyChanged(nameof(LoadOuts));
            OnPropertyChanged(nameof(GroupSets));

            // Raise the DataRefreshed event
            DataRefreshed?.Invoke(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (InitializationManager.IsAnyInitializing()) return;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        
    }
}
