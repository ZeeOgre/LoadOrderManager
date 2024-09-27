using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ZO.LoadOrderManager
{

    [Flags]
    public enum GroupFlags
    {
        Uninitialized = 0,
        DefaultGroup = 1,
        ReadOnly = 2,
        Favorite = 4,
        ReadyToLoad = 8,
        FilesLoaded = 16
    }


    public class GroupSet
    {
        public long GroupSetID { get; set; }
        public string GroupSetName { get; set; }
        public GroupFlags GroupSetFlags { get; set; }
        public ObservableCollection<ModGroup> ModGroups { get; set; } = new ObservableCollection<ModGroup>();
        public ObservableCollection<LoadOut> LoadOuts { get; set; } = new ObservableCollection<LoadOut>();

        public bool IsUninitialized => (GroupSetFlags & GroupFlags.DefaultGroup) == GroupFlags.Uninitialized;
        public bool IsDefaultGroup => (GroupSetFlags & GroupFlags.DefaultGroup) == GroupFlags.DefaultGroup;
        public bool IsReadOnly => (GroupSetFlags & GroupFlags.ReadOnly) == GroupFlags.ReadOnly;
        public bool IsFavorite => (GroupSetFlags & GroupFlags.Favorite) == GroupFlags.Favorite;
        public bool IsReadyToLoad => (GroupSetFlags & GroupFlags.ReadyToLoad) == GroupFlags.ReadyToLoad;
        public bool AreFilesLoaded => (GroupSetFlags & GroupFlags.FilesLoaded) == GroupFlags.FilesLoaded;



        public GroupSet(long groupSetID, string groupSetName, GroupFlags groupSetFlags)
        {
            GroupSetID = groupSetID;
            GroupSetName = groupSetName;
            GroupSetFlags = groupSetFlags;

            //if (IsDefaultGroup || GroupSetID == 1)
            //{
            //    // Load the default group set if applicable
            //    var defaultGroupSet = LoadGroupSet(1);
            //    if (defaultGroupSet == null)
            //    {
            //        throw new InvalidOperationException("Default GroupSet with ID 1 not found in the database.");
            //    }
            //    App.LogDebug("GroupSet 1 loaded as default.");
            //    return;
            //}

            if (IsReadOnly)
            {
                // Check if the read-only group set exists, or create it
                var readOnlyGroupSet = LoadGroupSet(groupSetID);
                if (readOnlyGroupSet == null)
                {
                    InsertReadOnlyGroupSet();
                    App.LogDebug("New ReadOnly GroupSet created and inserted INTO the database.");
                }
                else
                {
                    App.LogDebug("GroupSet with ReadOnly flag loaded.");
                }
            }
        }


        private void InsertReadOnlyGroupSet()
        {
            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);
            command.CommandText = @"
                INSERT INTO GroupSet (GroupSetID, GroupSetName, GroupSetFlags)
                VALUES (@GroupSetID, @GroupSetName, @GroupSetFlags)";
            command.Parameters.AddWithValue("@GroupSetID", GroupSetID);
            command.Parameters.AddWithValue("@GroupSetName", GroupSetName);
            command.Parameters.AddWithValue("@GroupSetFlags", (long)GroupSetFlags);

            command.ExecuteNonQuery();
        }


        public static IEnumerable<LoadOut> GetAllLoadOuts(long groupSetID)
        {
            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);
            command.CommandText = "SELECT * FROM LoadOutProfiles WHERE GroupSetID = @GroupSetID";
            command.Parameters.AddWithValue("@GroupSetID", groupSetID);

            using var reader = command.ExecuteReader();
            var loadOuts = new List<LoadOut>();
            while (reader.Read())
            {
                var loadOut = new LoadOut
                {
                    ProfileID = reader.GetInt64(reader.GetOrdinal("ProfileID")),
                    Name = reader.GetString(reader.GetOrdinal("ProfileName")),
                };
                loadOut.LoadEnabledPlugins();
                loadOuts.Add(loadOut);
            }
            return loadOuts;
        }


        public GroupSet()
        {
            GroupSetID = 0;
            GroupSetName = "EmptyGroupSet";
            GroupSetFlags = GroupFlags.Uninitialized;
            ModGroups = new ObservableCollection<ModGroup>();
            LoadOuts = new ObservableCollection<LoadOut>();
        }

        public void AddModGroup(ModGroup modGroup)
        {
            if (ModGroups.Any(mg => mg.GroupID == modGroup.GroupID))
            {
                throw new InvalidOperationException("ModGroup already exists in this GroupSet.");
            }

            // Set the GroupSetID of the modGroup to the current GroupSetID
            modGroup.GroupSetID = this.GroupSetID;
            modGroup.WriteGroup();
            ModGroups.Add(modGroup);
            
        }



        // Method to clone the GroupSet
        public GroupSet Clone()
        {
            var clonedGroupSet = new GroupSet(this.GroupSetID, this.GroupSetName, this.GroupSetFlags);
            foreach (var modGroup in this.ModGroups)
            {
                clonedGroupSet.ModGroups.Add(modGroup.Clone());
            }
            return clonedGroupSet;
        }

        // Method to merge another GroupSet INTO this one
        //public void Merge(GroupSet otherGroupSet)
        //{
        //    foreach (var modGroup in otherGroupSet.ModGroups)
        //    {
        //        if (!this.ModGroups.Any(mg => mg.GroupID == modGroup.GroupID))
        //        {
        //            this.ModGroups.Add(modGroup.Clone());
        //        }
        //    }
        //}

        // Method to load GroupSet from the database
        public static GroupSet? LoadGroupSet(long groupSetID)
        {
            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);
            command.CommandText = "SELECT * FROM GroupSets WHERE GroupSetID = @GroupSetID";
            command.Parameters.AddWithValue("@GroupSetID", groupSetID);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var groupSet = new GroupSet(
                    reader.GetInt64(reader.GetOrdinal("GroupSetID")),
                    reader.GetString(reader.GetOrdinal("GroupSetName")),
                    (GroupFlags)reader.GetInt64(reader.GetOrdinal("GroupSetFlags"))
                );

                var modGroupsList = ModGroup.LoadModGroupsByGroupSet(groupSetID);
                groupSet.ModGroups = new ObservableCollection<ModGroup>(modGroupsList);

                // Populate LoadOuts collection
                var loadOuts = GetAllLoadOuts(groupSetID);
                groupSet.LoadOuts = new ObservableCollection<LoadOut>(loadOuts);

                return groupSet;
            }
            return null;
        }

        // Method to save GroupSet to the database
        public GroupSet SaveGroupSet()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("Cannot update a ReadOnly GroupSet.");
            }

            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);

            // Check if the GroupSet already exists
            command.CommandText = "SELECT GroupSetFlags FROM GroupSets WHERE GroupSetID = @GroupSetID";
            command.Parameters.AddWithValue("@GroupSetID", GroupSetID);

            var existingFlags = command.ExecuteScalar();

            // Validate and cast existingFlags if it is not null or DBNull
            if (existingFlags != null && existingFlags != DBNull.Value)
            {
                // Correctly cast to long, then to GroupFlags
                var existingGroupSetFlags = (GroupFlags)(long)existingFlags;
                // Merge existing flags with the current flags
                GroupSetFlags |= existingGroupSetFlags;
            }

            // Prepare insert or replace command
            command.CommandText = @"
        INSERT OR REPLACE INTO GroupSets (GroupSetID, GroupSetName, GroupSetFlags)
        VALUES (@GroupSetID, @GroupSetName, @GroupSetFlags)";
            command.Parameters.AddWithValue("@GroupSetName", GroupSetName);
            command.Parameters.AddWithValue("@GroupSetFlags", (long)GroupSetFlags);

            command.ExecuteNonQuery();

            // Save ModGroups to the database
            foreach (var modGroup in ModGroups)
            {
                
               
                if (modGroup.GroupSetID != GroupSetID && modGroup.GroupID >= 0) 
                {
                    modGroup.GroupID = this.GroupSetID;
                    modGroup.WriteGroup();
                }
                
            }

            return this;
        }


        public static GroupSet CreateEmptyGroupSet()
        {
            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);
            command.CommandText = @"
                INSERT INTO GroupSets (GroupSetName, GroupSetFlags)
                VALUES (@GroupSetName, @GroupSetFlags)
                RETURNING GroupSetID;
            ";
            command.Parameters.AddWithValue("@GroupSetName", "EmptyGroupSet");
            command.Parameters.AddWithValue("@GroupSetFlags", (long)GroupFlags.Uninitialized);

            long groupSetID = (long)command.ExecuteScalar();
            
            return new GroupSet(groupSetID, "EmptyGroupSet", GroupFlags.Uninitialized);

        }
    }


    public class GroupSetViewModel : INotifyPropertyChanged
    {
        private GroupSet _groupSet;
        private AggLoadInfo _aggLoadInfo;

        public long GroupSetID { get; set; }
        public string GroupSetName { get; set; }
        public ObservableCollection<ModGroup> ModGroups => _aggLoadInfo.Groups;
        public ObservableCollection<LoadOut> LoadOuts => _aggLoadInfo.LoadOuts;
        public ObservableCollection<Plugin> Plugins => _aggLoadInfo.Plugins;

        public ICommand AddModGroupCommand { get; }
        public ICommand AddPluginCommand { get; }
        public ICommand AddLoadOutCommand { get; }
        public ICommand SaveCommand { get; }

        public GroupSetViewModel(long groupSetID)
        {
            // Initialize AggLoadInfo with the provided groupSetID
            _aggLoadInfo = new AggLoadInfo(groupSetID);

            // Initialize GroupSet based on AggLoadInfo
            _groupSet = _aggLoadInfo.ActiveGroupSet ?? new GroupSet();

            // Set properties
            GroupSetID = _groupSet.GroupSetID;
            GroupSetName = _groupSet.GroupSetName;

            // Initialize commands
            AddModGroupCommand = new RelayCommand(AddModGroup);
            AddPluginCommand = new RelayCommand(AddPlugin);
            AddLoadOutCommand = new RelayCommand(AddLoadOut);
            SaveCommand = new RelayCommand(_ => Save());
        }

        public void AddModGroup(object? parameter)
        {
            if (parameter is ModGroup modGroup)
            {
                _groupSet.AddModGroup(modGroup);
                OnPropertyChanged(nameof(ModGroups));
            }
        }

        private void AddPlugin(object? parameter)
        {
            // Implementation for adding a Plugin
        }

        private void AddLoadOut(object? parameter)
        {
            // Implementation for adding a LoadOut
        }

        private void Save()
        {
            _groupSet.SaveGroupSet();
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }



}
