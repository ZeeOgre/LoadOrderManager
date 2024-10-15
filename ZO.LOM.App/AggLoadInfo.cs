using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
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
        public static ObservableCollection<GroupSet> GroupSets
        {
            get => _cachedGroupSets;
            set => _cachedGroupSets = value;
        }

        public GroupSetGroupCollection GroupSetGroups { get; set; } = new GroupSetGroupCollection();
        public GroupSetPluginCollection GroupSetPlugins { get; 
            set; } = new GroupSetPluginCollection();
        public ProfilePluginCollection ProfilePlugins { get; set; } = new ProfilePluginCollection();
        private bool _isRefreshing = false;
        private bool _refreshPending = false;
        private readonly object _eventLock = new object();
        private bool isDirty;
        public void SetDirty(bool value)
        {
            lock (_refreshLock)
            {
                isDirty = value;
            }
        }
        
        public bool IsDirty
        {
            get
            {
                lock (_refreshLock)
                {
                    return isDirty;
                }
            }
        }

        public ICommand JumpToRecordCommand { get; }
        private bool _initialized = false;
        private static readonly object _initLock = new object();
        private static readonly object _refreshLock = new object();
        private bool _isUpdatingGroupSet = false;

        public event EventHandler? DataRefreshed;
        private void InitializeEventHandlers()
        {
            // Core property change
            PropertyChanged += AggLoadInfo_PropertyChanged;

            // Underlying collection changed
            //Plugins.CollectionChanged += CommonCollectionChangedHandler;
            //Groups.CollectionChanged += CommonCollectionChangedHandler;
            LoadOuts.CollectionChanged += CommonCollectionChangedHandler;
            GroupSets.CollectionChanged += CommonCollectionChangedHandler;
            GroupSetGroups.Items.CollectionChanged += CommonCollectionChangedHandler;
            GroupSetPlugins.Items.CollectionChanged += CommonCollectionChangedHandler;
            ProfilePlugins.Items.CollectionChanged += CommonCollectionChangedHandler;
        }

        private void CommonCollectionChangedHandler(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender == null || InitializationManager.IsAnyInitializing()) return;

            lock (_refreshLock)
            {
                if (_isRefreshing) return; // Prevent recursive calls
                _isRefreshing = true;

                try
                {
                    // Set the dirty flag (if needed)
                    SetDirty(true);

                    // Raise the DataRefreshed event
                    RaiseDataRefreshed();
                }
                finally
                {
                    _isRefreshing = false;
                }
            }
        }

        private LoadOut _activeLoadOut;
        public LoadOut ActiveLoadOut
        {
            get => _activeLoadOut;
            set
            {
                if (_activeLoadOut != value)
                {
                    _activeLoadOut = value;
                    OnPropertyChanged(); // Notify only if the value has changed
                }
            }
        }
        private GroupSet _activeGroupSet;
        public GroupSet ActiveGroupSet
        {
            get => _activeGroupSet;
            set
            {
                if (_activeGroupSet != value)
                {
                    _activeGroupSet = value ?? throw new ArgumentNullException(nameof(value));

                    //if (_activeGroupSet != null && _activeGroupSet != _cachedGroupSet1 && _initialized)
                    //{
                    //    LoadGroupSetState(); // Trigger group set state loading only when switching
                    //}
                    HandleActiveGroupSetChange();
                    OnPropertyChanged();
                }
            }
        }

        private void HandleActiveGroupSetChange()
        {
            if (_activeGroupSet != null && _activeGroupSet != _cachedGroupSet1 && _initialized)
            {
                LoadGroupSetState();
            }

            if (_activeLoadOut != null)
            {
                var newLoadOut = GetLoadOutForGroupSet(_activeGroupSet);

                // Avoid setting ActiveLoadOut unnecessarily to prevent triggering property changes
                if (newLoadOut != ActiveLoadOut)
                {
                    ActiveLoadOut = newLoadOut;
                }
            }
        }

        private void RaiseDataRefreshed()
        {
            lock (_eventLock)
            {
                if (_refreshPending) return;  // Prevent multiple triggers
                _refreshPending = true;

                // Schedule the actual refresh in a short delay
                Task.Delay(100).ContinueWith(_ =>
                {
                    lock (_eventLock)
                    {
                        _refreshPending = false;
                        DataRefreshed?.Invoke(this, EventArgs.Empty);
                    }
                });
            }
        }



        




        private string? GetPropertyName(object sender)
        {
            var properties = GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.GetValue(this) == sender)
                {
                    return property.Name;
                }
            }
            return null;
        }


        public void PerformLockedAction(Action action)
        {
            lock (_refreshLock)
            {
                action.Invoke();
            }
        }

        private static ObservableCollection<GroupSet>? _cachedGroupSets;

        public ObservableCollection<GroupSet> GetGroupSets()
        {
            if (_cachedGroupSets == null)
            {
                _cachedGroupSets = LoadGroupSetsFromDatabase();
            }
            return _cachedGroupSets;
        }

        private static GroupSet? _cachedGroupSet1; // Make this static

        public static GroupSet? GetCachedGroupSet1()
        {
            if (_cachedGroupSet1 == null)
            {
                _cachedGroupSet1 = GroupSet.LoadGroupSet(1);  // Load GroupSet1 from the database
            }
            return _cachedGroupSet1;
        }





        private AggLoadInfo()
        {
            
            InitializeEventHandlers();
        }

        public AggLoadInfo(long groupSetID)
        {
            JumpToRecordCommand = new RelayCommand<string>(JumpToRecord);

            if (groupSetID == 0)
            {
                ActiveGroupSet = new GroupSet();
                PropertyChanged = null;
            }
            else
            {
                ActiveGroupSet = GroupSet.LoadGroupSet(groupSetID) ?? throw new InvalidOperationException("GroupSet not found.");
                InitFromDatabase();
                InitializeEventHandlers();

            }
        }

        private void AggLoadInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ActiveLoadOut))
            {
                if (ActiveLoadOut != null)
                {
                    ActiveLoadOut.enabledPlugins = LoadEnabledPlugins(ActiveLoadOut.ProfileID);
                }
            }
            else if (e.PropertyName == nameof(ActiveGroupSet))
            {
                if (ActiveGroupSet != null)
                {
                    ActiveLoadOut = GetLoadOutForGroupSet(ActiveGroupSet);

                }
            }
        }

        

        public void InitFromDatabase()
        {
            
            if (ActiveGroupSet.GroupSetID == 0) return;

            lock (_initLock)
            {
                if (_initialized) return;

                Plugins.Clear();
                Groups.Clear();
                LoadOuts.Clear();

                if (_activeLoadOut != null && !LoadOuts.Contains(_activeLoadOut))
                {
                    LoadOuts.Add(_activeLoadOut);
                }

                if (_activeGroupSet != null && !GroupSets.Contains(_activeGroupSet) && !GroupSets.Any(gs => gs.GroupSetID == _activeGroupSet.GroupSetID))
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
            lock (_initLock)
            {
                lock (_refreshLock)
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
                        WHERE GroupSetID = @GroupSetID OR GroupSetID = 1);", connection);
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
                            var cachedGroup1 = Groups.FirstOrDefault(g => g.GroupID == 1 && g.GroupSetID == 1);
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
                    SELECT DISTINCT ProfileID, ProfileName, GroupSetID, isFavorite
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
                            var isFavorite = reader2.GetInt64(3) == 1;

                            var loadOut = new LoadOut
                            {
                                ProfileID = profileID,
                                GroupSetID = groupSetID,
                                  IsFavorite = isFavorite,    
                                Name = name,
                                enabledPlugins = LoadEnabledPlugins(profileID)
                            };

                            loadOuts.Add(loadOut);
                        }

                        transaction.Commit();
                        // Assign the loaded load-outs to the appropriate property
                        LoadOuts = loadOuts;

                        var matchingActiveLoadOut = LoadOuts.FirstOrDefault(l => l.ProfileID == ActiveLoadOut?.ProfileID);
                        if (matchingActiveLoadOut != null)
                        {
                            ActiveLoadOut = matchingActiveLoadOut; // Sync ActiveLoadOut
                        }



                        // Create and populate Group -997 with unassigned plugins
                        CreateAndPopulateGroup997(connection, pluginDict);

                        _initialized = true;

                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }


        private void CreateAndPopulateGroup997(SQLiteConnection conn, Dictionary<long, Plugin> pluginDict)
        {
            // Step 1: Start a transaction to handle everything in one go
            using var transaction = conn.BeginTransaction();

            var sqlCommandText = @"
            -- Insert unassigned plugins into GroupSetPlugins for GroupID -997
            INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal)
            SELECT @GroupSetIDz, -997, PluginID, ROW_NUMBER() OVER (ORDER BY PluginID)
            FROM Plugins p
            WHERE NOT EXISTS (
                -- Ensure the plugin is not already assigned to any group in the current GroupSet
                SELECT 1 FROM GroupSetPlugins gsp
                WHERE gsp.GroupSetID = @GroupSetIDz OR gsp.GroupID IN (1, -998, -999)
                AND gsp.PluginID = p.PluginID
            )
            RETURNING PluginID;
            ";

    //-- Step 1: Insert all unassigned plugins into GroupSetPlugins for GroupID -997
    //INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal)
    //SELECT @GroupSetIDz, -997, PluginID, ROW_NUMBER() OVER (ORDER BY PluginID)
    //FROM Plugins
    //WHERE PluginID IN (
    //    SELECT DISTINCT PluginID
    //    FROM vwPluginGrpUnion
    //    WHERE GroupSetID != @GroupSetIDz
    //    AND GroupID NOT IN (-998, -999, 1)
    //)
    //RETURNING PluginID;
    //";

            // Execute the SQL and capture the results (PluginIDs of inserted plugins)
            using var command = new SQLiteCommand(sqlCommandText, conn);
            command.Parameters.AddWithValue("@GroupSetIDz", ActiveGroupSet.GroupSetID);

            using var reader = command.ExecuteReader();

            // Step 2: Build the unassignedGroup (-997) using the PluginIDs returned from the insert
            var unassignedGroup = Groups.FirstOrDefault(g => g.GroupID == -997);
            if (unassignedGroup == null)
            {
                unassignedGroup = new ModGroup(-997, "Unassigned", "Plugins not assigned to any group", 1, 9997, ActiveGroupSet.GroupSetID);
                Groups.Add(unassignedGroup);
            }

            unassignedGroup.Plugins.Clear();

            while (reader.Read())
            {
                var pluginID = reader.GetInt64(0); // Capture PluginID from the returned result
                if (pluginDict.TryGetValue(pluginID, out var plugin))
                {
                    unassignedGroup.Plugins.Add(plugin);
                    plugin.GroupOrdinal = unassignedGroup.Plugins.Count;
                }
            }

            // Commit the transaction after all steps are completed
            transaction.Commit();

            // Step 3: Call RefreshMetaData to update the object model with the latest data

            RefreshMetadataFromDB();
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
            if (_isUpdatingGroupSet)
            {
                return; // Prevent reentrancy within the same thread
            }

            _isUpdatingGroupSet = true; // Indicate that the method is running

            try
            {
                // Check if the changed group set is the current ActiveGroupSet
                if (e.NewItems?.Contains(ActiveGroupSet) == true)
                {
                    // Refresh the ActiveGroupSet using the LoadGroupSet method
                    ActiveGroupSet = GroupSet.LoadGroupSet(ActiveGroupSet.GroupSetID);
                }

                _cachedGroupSets = GetGroupSets();
            }
            finally
            {
                _isUpdatingGroupSet = false; // Reset the flag
                RaiseDataRefreshed();
            }
        }

        public void UpdatePlugin(Plugin plugin)
        {
            lock (_refreshLock)
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
        }

        public void UpdateModGroup(ModGroup group)
        {
            lock (_refreshLock)
            {
                // Check for existing entry in GroupSetGroups for this group
                var existingGroupSetGroup = GroupSetGroups.Items.FirstOrDefault(gsg => gsg.groupID == (long)group.GroupID);

                // Handle the update based on existing GroupSet
                if (existingGroupSetGroup != default)
                {
                    // If the ParentID changes, we remove the old entry and add a new one
                    if (existingGroupSetGroup.parentID != group.ParentID)
                    {
                        GroupSetGroups.Items.Remove(existingGroupSetGroup);

                        // Add the new entry with updated ParentID and Ordinal
                        GroupSetGroups.Items.Add(((long)group.GroupID, (long)ActiveGroupSet.GroupSetID, (long?)group.ParentID, (long)group.Ordinal));
                    }
                    else if (existingGroupSetGroup.Ordinal != group.Ordinal)
                    {
                        // Update Ordinal if only the order has changed
                        var updatedGroupSetGroup = (existingGroupSetGroup.groupID, existingGroupSetGroup.groupSetID, existingGroupSetGroup.parentID, (long)group.Ordinal);
                        GroupSetGroups.Items[GroupSetGroups.Items.IndexOf(existingGroupSetGroup)] = updatedGroupSetGroup;
                    }
                }
                else
                {
                    // If no entry exists, insert new entry
                    GroupSetGroups.Items.Add(((long)group.GroupID, (long)ActiveGroupSet.GroupSetID, (long?)group.ParentID, (long)group.Ordinal));
                }

                // Finally, save the group changes
                group.WriteGroup();
            }
        }

        public void UpdateLoadOut(LoadOut loadOut)
        {
            lock (_refreshLock)
            {
                // Check for existing entry in LoadOuts for this loadOut
                var existingLoadOut = LoadOuts.FirstOrDefault(lo => lo.GroupSetID == (long)loadOut.GroupSetID);

                // Handle the update based on existing LoadOut
                if (existingLoadOut != default)
                {
                    // If the GroupSetID changes, we remove the old entry and add a new one
                    if (existingLoadOut.GroupSetID != loadOut.GroupSetID)
                    {
                        LoadOuts.Remove(existingLoadOut);

                        // Add the new entry with updated GroupSetID
                        LoadOuts.Add(loadOut);
                    }
                }
                else
                {
                    // If no entry exists, insert new entry
                    LoadOuts.Add(loadOut);
                }

                // Finally, save the loadOut changes
                loadOut.WriteProfile();
            }
        }

        public LoadOut GetLoadOutForGroupSet(GroupSet groupSet)
        {
            // Find the favorite loadout for the specific GroupSet
            var favoriteLoadOut = AggLoadInfo.Instance.LoadOuts
                .FirstOrDefault(l => l.IsFavorite && l.GroupSetID == groupSet.GroupSetID);

            if (favoriteLoadOut != null)
            {
                return favoriteLoadOut;
            }

            // Try to find the default loadout for the specific GroupSet
            var defaultLoadOut = AggLoadInfo.Instance.LoadOuts
                .FirstOrDefault(l => l.Name == "(Default)" && l.GroupSetID == groupSet.GroupSetID);

            if (defaultLoadOut != null)
            {
                return defaultLoadOut;
            }

            // Try to find the first loadout in the specific GroupSet
            var firstLoadOut = AggLoadInfo.Instance.LoadOuts
                .FirstOrDefault(l => l.GroupSetID == groupSet.GroupSetID);

            if (firstLoadOut != null)
            {
                return firstLoadOut;
            }

            // Create a new loadout if none exists
            return new LoadOut(groupSet) { Name = "(Default)" };
        }


        public void Save()
        {
            if (ActiveGroupSet == null)
            {
                throw new InvalidOperationException("ActiveGroupSet is not set.");
            }


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


            //RefreshMetadataFromDB();
            //    using var connection = DbManager.Instance.GetConnection();
            //    using var transaction = connection.BeginTransaction();
            ////try
            //{

            //    // Reload GroupSetGroups, GroupSetPlugins, and ProfilePlugins
            //    GroupSetGroups.LoadGroupSetGroups(ActiveGroupSet.GroupSetID, connection);
            //    GroupSetPlugins.LoadGroupSetPlugins(ActiveGroupSet.GroupSetID, connection);
            //    ProfilePlugins.LoadProfilePlugins(ActiveGroupSet.GroupSetID, connection);
            //    transaction.Commit();

            //}
            //catch (Exception)
            //{
            //    // Rollback transaction in case of an error
            //    transaction.Rollback();
            //    throw;
            //}
        }

        public void RefreshMetadataFromDB(SQLiteConnection? connection = null)
        {
            using var conn = connection ?? DbManager.Instance.GetConnection();
            GroupSetGroups.LoadGroupSetGroups(ActiveGroupSet.GroupSetID, conn);
            GroupSetPlugins.LoadGroupSetPlugins(ActiveGroupSet.GroupSetID, conn);
            ProfilePlugins.LoadProfilePlugins(ActiveGroupSet.GroupSetID, conn);

            foreach (var plugin in Plugins)
            {
                var matchingGSP = GroupSetPlugins.Items
                    .FirstOrDefault(gsp => gsp.groupSetID == ActiveGroupSet.GroupSetID && gsp.pluginID == plugin.PluginID);

                if (matchingGSP == default)
                {
                    plugin.GroupID = matchingGSP.groupID;
                    plugin.GroupOrdinal = matchingGSP.Ordinal;
                }
            }

            // Update Groups' ParentID and Ordinal based on GroupSetGroups
            foreach (var group in Groups)
            {
                var matchingGSG = GroupSetGroups.Items
                    .FirstOrDefault(gsg => gsg.groupSetID == ActiveGroupSet.GroupSetID && gsg.groupID == group.GroupID);

                if (matchingGSG == default)
                {
                    group.ParentID = matchingGSG.parentID;
                    group.Ordinal = matchingGSG.Ordinal;
                }
            }

            // Update LoadOuts by enabling appropriate plugins based on ProfilePlugins
            foreach (var loadout in LoadOuts)
            {
                var enabledPlugins = ProfilePlugins.Items
                    .Where(pp => pp.ProfileID == loadout.ProfileID)
                    .Select(pp => pp.PluginID)
                    .ToList();

                
            }
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
            //OnPropertyChanged(nameof(Groups));
            //OnPropertyChanged(nameof(Plugins));
            //OnPropertyChanged(nameof(LoadOuts));
            //OnPropertyChanged(nameof(GroupSets));

            // Raise the DataRefreshed event
            RaiseDataRefreshed();
        }

        
        public static long GetNextPluginOrdinal(long groupID, long? groupSetID = null)
        {
            // Use Instance.GroupSetID if groupSetID is null
            var effectiveGroupSetID = groupSetID ?? Instance.ActiveGroupSet.GroupSetID;

            var maxOrdinal = Instance.GroupSetPlugins.Items
                .Where(item => item.groupID == groupID && item.groupSetID == effectiveGroupSetID)
                .Select(item => item.Ordinal)
                .DefaultIfEmpty(0)
                .Max();

            return maxOrdinal + 1;
        }

        public static long GetNextGroupOrdinal(long groupID, long? groupSetID = null)
        {
            // Use Instance.GroupSetID if groupSetID is null
            var effectiveGroupSetID = groupSetID ?? Instance.ActiveGroupSet.GroupSetID;

            var maxOrdinal = Instance.GroupSetGroups.Items
                .Where(item => item.groupSetID == effectiveGroupSetID && item.groupID == groupID)
                .Max(item => (long?)item.Ordinal) ?? 0;

            return maxOrdinal + 1;
        }

        public static List<ModGroup> GetAllGroupChildren(long groupID)
        {
            return Instance.GroupSetGroups.Items
                .Where(gsg => gsg.parentID == groupID)
                .Select(gsg => Instance.Groups.FirstOrDefault(g => g.GroupID == gsg.groupID))
                .Where(group => group != null) // Exclude null groups
                .ToList();
        }

        public static ModGroup? GetGroupNeighbor(long groupID, long ordinal, bool up)
        {
            long targetOrdinal = up ? ordinal - 1 : ordinal + 1;

            return Instance.GroupSetGroups.Items
                .Where(gsg => gsg.parentID == groupID && gsg.Ordinal == targetOrdinal)
                .Select(gsg => Instance.Groups.FirstOrDefault(g => g.GroupID == gsg.groupID))
                .Where(group => group != null) // Exclude null groups
                .FirstOrDefault();
        }

        public static List<Plugin> GetAllPluginChildren(long groupID)
        {
            return Instance.GroupSetPlugins.Items
                .Where(gsp => gsp.groupID == groupID)
                .Select(gsp => Instance.Plugins.FirstOrDefault(p => p.PluginID == gsp.pluginID))
                .Where(plugin => plugin != null) // Exclude null plugins
                .ToList();
        }
        
        public static Plugin? GetPluginNeighbor(long groupID, long ordinal, bool up)
        {
            // Determine the target ordinal based on the direction
            long targetOrdinal = up ? ordinal - 1 : ordinal + 1;

            // Find the neighbor plugin
            return Instance.GroupSetPlugins.Items
                .Where(gsp => gsp.groupID == groupID && gsp.Ordinal == targetOrdinal)
                .Select(gsp => Instance.Plugins.FirstOrDefault(p => p.PluginID == gsp.pluginID))
                .FirstOrDefault();
        }

        public static List<LoadOrderItemViewModel> GetAllSiblings(object item)
        {
            List<LoadOrderItemViewModel> siblings = new List<LoadOrderItemViewModel>();

            if (item is LoadOrderItemViewModel viewModel)
            {
                if (viewModel.EntityType == EntityType.Plugin)
                {
                    // Get all siblings for a Plugin
                    var groupID = viewModel.GroupID;
                    siblings.AddRange(GetAllPluginChildren(groupID).Select(p => new LoadOrderItemViewModel(p)));
                }
                else if (viewModel.EntityType == EntityType.Group)
                {
                    // Get all siblings for a Group
                    var parentID = viewModel.ParentID;
                    siblings.AddRange(GetAllGroupChildren((long)parentID).Select(g => new LoadOrderItemViewModel(g)));
                }
            }

            return siblings;
        }




        private void JumpToRecord(string recordID)
        {
            if (long.TryParse(recordID, out long groupSetID))
            {
                var groupSet = GroupSets.FirstOrDefault(gs => gs.GroupSetID == groupSetID);
                if (groupSet != null)
                {
                    ActiveGroupSet = groupSet;
                }
                else
                {
                    // Handle case where GroupSet is not found
                    MessageBox.Show("GroupSet not found.");
                }
            }
            else
            {
                // Handle invalid input
                MessageBox.Show("Invalid GroupSet ID.");
            }
        }

        public static LoadOrderItemViewModel? GetNeighbor(LoadOrderItemViewModel item, bool up)
        {
            if (!item.Ordinal.HasValue)
            {
                return null;
            }

            long targetOrdinal = up ? (long)item.Ordinal - 1 : (long)item.Ordinal + 1;

            // If moving up, ensure the ordinal is not below 1 (invalid)
            if (up && targetOrdinal < 1)
            {
                return null;
            }

            if (item.EntityType == EntityType.Plugin)
            {
                // Look for a neighbor plugin in the same group
                var neighborPlugin = Instance.GroupSetPlugins.Items
                    .Where(gsp => gsp.groupID == item.GroupID && gsp.Ordinal == targetOrdinal)
                    .Select(gsp => Instance.Plugins.FirstOrDefault(p => p.PluginID == gsp.pluginID))
                    .Where(plugin => plugin != null)
                    .Select(plugin => new LoadOrderItemViewModel(plugin))
                    .FirstOrDefault();

                return neighborPlugin;
            }
            else if (item.EntityType == EntityType.Group)
            {
                // Look for a neighbor group with the matching target ordinal
                var neighborGroup = Instance.GroupSetGroups.Items
                    .Where(gsg => gsg.parentID == item.ParentID && gsg.Ordinal == targetOrdinal) // Ensure it shares the same parent
                    .Select(gsg => Instance.Groups.FirstOrDefault(g => g.GroupID == gsg.groupID))
                    .Where(group => group != null)
                    .Select(group => new LoadOrderItemViewModel(group))
                    .FirstOrDefault();

                return neighborGroup;
            }

            return null;
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (InitializationManager.IsAnyInitializing()) return;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AggLoadInfo NavigateCachedGroupSets(string direction)
        {
            if (_cachedGroupSets == null || !_cachedGroupSets.Any()) return null;

            int currentIndex = _cachedGroupSets.IndexOf(ActiveGroupSet);
            if (currentIndex == -1) return null;

            GroupSet? targetGroupSet = direction switch
            {
                "Next" => currentIndex + 1 < _cachedGroupSets.Count ? _cachedGroupSets[currentIndex + 1] : null,
                "Previous" => currentIndex - 1 >= 0 ? _cachedGroupSets[currentIndex - 1] : null,
                "First" => _cachedGroupSets.FirstOrDefault(),
                "Last" => _cachedGroupSets.LastOrDefault(),
                _ => null
            };

            if (targetGroupSet != null)
            {
                ActiveGroupSet = targetGroupSet;
                Instance.RefreshAllData();
                return new AggLoadInfo(targetGroupSet.GroupSetID);
            }
            return null;//by construction this should never be able to happen
        }

        public void DeleteGroupSet(GroupSet groupSet)
        {
            if (groupSet == null) throw new ArgumentNullException(nameof(groupSet));

            if (groupSet.IsReadOnly || groupSet.IsFavorite || groupSet.IsDefaultGroup)
            {
                throw new InvalidOperationException("Cannot delete a GroupSet that is ReadOnly, Favorite, or Default.");
            }

            var gsID = groupSet.GroupSetID;

            GroupSet.DeleteRecord(gsID);

            if (ActiveGroupSet == groupSet)
            {
                var newGroupSet = GroupSets.FirstOrDefault(gs => gs.IsFavorite) ?? GroupSets.FirstOrDefault();
                ActiveGroupSet = newGroupSet;
            }
            else
            {
                RefreshAllData();
            }

            if (Instance.ActiveGroupSet == groupSet)
            {
                var newGroupSet = GroupSets.FirstOrDefault(gs => gs.IsFavorite) ?? GroupSets.FirstOrDefault();
                Instance.ActiveGroupSet = newGroupSet;
            }
            else
            {
                Instance.RefreshAllData();
            }




        }


        public void PopulateLoadOrders(LoadOrdersViewModel viewModel, GroupSet groupSet, LoadOut loadOut, bool suppress997, bool isCached = false)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var isCachedGroupSet = groupSet == GetCachedGroupSet1();

                // Prevent regular updates on the cached GroupSet
                if (!isCached && isCachedGroupSet)
                {
                    return;
                }

                // Clear existing items only for the current view model
                viewModel.Items.Clear();
                viewModel.SelectedGroupSet = groupSet;
                viewModel.SelectedLoadOut = loadOut;
                viewModel.Suppress997 = suppress997;
                viewModel.IsCached = isCached;

                var activeGroupSetID = groupSet.GroupSetID;

                // Add items to the view model
                var sortedGroups = GetSortedGroups(activeGroupSetID);
                var groupDictionary = new Dictionary<long, LoadOrderItemViewModel>();

                foreach (var group in sortedGroups)
                {
                    if (group.GroupID == -997 && suppress997)
                        continue;

                    var groupItem = new LoadOrderItemViewModel(group);
                    groupDictionary[(long)group.GroupID] = groupItem;

                    // Handle parent-child relationships
                    if (group.ParentID.HasValue && groupDictionary.ContainsKey(group.ParentID.Value))
                    {
                        groupDictionary[group.ParentID.Value].Children.Add(groupItem);
                    }
                    else
                    {
                        // No need to invoke dispatcher again, already on UI thread
                        viewModel.Items.Add(groupItem);
                    }

                    // Add plugins to each group
                    AddPluginsToGroup(groupItem, activeGroupSetID, loadOut, suppress997);
                }
            });
        }



        public List<ModGroup> GetSortedGroups(long groupSetID)
        {
            var groupSetGroups = AggLoadInfo.Instance.GroupSetGroups.Items
                .Where(g => g.groupSetID == groupSetID)
                .Select(g => new { g.groupID, g.parentID, g.Ordinal })
                .OrderBy(g => g.parentID)  // Sorting by parentID first
                .ThenBy(g => g.Ordinal)    // Then by Ordinal within each parent
                .ToList();

            // Convert the groupSetGroups into actual ModGroup instances
            var sortedGroups = groupSetGroups
                .Select(g => ModGroup.LoadModGroup(g.groupID, groupSetID)) // LoadModGroup ensures correct loading
                .Where(g => g != null) // Exclude any null results
                .ToList();

            return sortedGroups;
        }


        public void AddPluginsToGroup(LoadOrderItemViewModel groupItem, long groupSetID, LoadOut loadOut, bool suppress997)
        {
            // This method should add the plugins for the given group
            var groupID = groupItem.GroupID;

            // Fetch the plugins for the given group and groupSetID
            var plugins = GroupSetPlugins.Items
                .Where(gsp => gsp.groupID == groupID && gsp.groupSetID == groupSetID)
                .Select(gsp => Plugins.FirstOrDefault(p => p.PluginID == gsp.pluginID))
                .Where(plugin => plugin != null) // Ensure we don't include null plugins
                .OrderBy(plugin => plugin.GroupOrdinal) // Sort plugins by ordinal
                .ToList();

            foreach (var plugin in plugins)
            {
                var pluginItem = new LoadOrderItemViewModel(plugin)
                {
                    IsActive = loadOut.enabledPlugins.Contains(plugin.PluginID) // Set IsActive based on enabledPlugins collection
                };

                // Optionally handle suppression of Group -997
                if (groupID == -997 && suppress997)
                    continue;

                groupItem.Children.Add(pluginItem); // Add plugin to group’s children
            }
        }



    }
}
   

