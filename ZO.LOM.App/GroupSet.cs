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

        public GroupSet(string loadoutName)
        {
            GroupSetName = $"{loadoutName}_";
            GroupSetFlags = GroupFlags.Uninitialized;
            ModGroups = new ObservableCollection<ModGroup>();

            // Insert INTO database, get the GroupSetID, and update GroupSetName in one query
            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);
            command.CommandText = @"
                    INSERT INTO GroupSet (GroupSetName, GroupSetFlags)
                    VALUES (@GroupSetName, @GroupSetFlags)
                    RETURNING GroupSetID;
                ";
            command.Parameters.AddWithValue("@GroupSetName", GroupSetName);
            command.Parameters.AddWithValue("@GroupSetFlags", (long)GroupSetFlags);
            GroupSetID = Convert.ToInt64(command.ExecuteScalar());

            // Update GroupSetName to include GroupSetID
            GroupSetName = $"{loadoutName}_{GroupSetID}";
            command.CommandText = @"
                    UPDATE GroupSet
                    SET GroupSetName = @UpdatedGroupSetName
                    WHERE GroupSetID = @GroupSetID;
                ";
            command.Parameters.AddWithValue("@UpdatedGroupSetName", GroupSetName);
            command.Parameters.AddWithValue("@GroupSetID", GroupSetID);
            command.ExecuteNonQuery();
        }

        public void AddModGroup(ModGroup modGroup)
        {
            if (ModGroups.Any(mg => mg.GroupID == modGroup.GroupID))
            {
                throw new InvalidOperationException("ModGroup already exists in this GroupSet.");
            }

            // Set the GroupSetID of the modGroup to the current GroupSetID
            modGroup.GroupSetID = this.GroupSetID;

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
                INSERT INTO GroupSet (GroupSetName, GroupSetFlags)
                VALUES (@GroupSetName, @GroupSetFlags)
                RETURNING GroupSetID;
            ";
            command.Parameters.AddWithValue("@GroupSetName", "EmptyGroupSet");
            command.Parameters.AddWithValue("@GroupSetFlags", (long)GroupFlags.Uninitialized);

            long groupSetID = (long)command.ExecuteScalar();
            
            return new GroupSet(groupSetID, "EmptyGroupSet", GroupFlags.Uninitialized);

        }
    }


    //public class GroupSetViewModel : INotifyPropertyChanged
    //{
    //    private GroupSet _groupSet;

    //    public GroupSetViewModel(GroupSet groupSet)
    //    {
    //        _groupSet = groupSet;
    //        ModGroups = new ObservableCollection<ModGroupViewModel>(
    //            groupSet.ModGroups.Select(mg => new ModGroupViewModel(mg))
    //        );
    //    }

    //    public long groupSetID
    //    {
    //        get => _groupSet.GroupSetID;
    //        set
    //        {
    //            if (_groupSet.GroupSetID != value)
    //            {
    //                _groupSet.GroupSetID = value;
    //                OnPropertyChanged();
    //            }
    //        }
    //    }

    //    public string GroupSetName
    //    {
    //        get => _groupSet.GroupSetName;
    //        set
    //        {
    //            if (_groupSet.GroupSetName != value)
    //            {
    //                _groupSet.GroupSetName = value;
    //                OnPropertyChanged();
    //            }
    //        }
    //    }

    //    public GroupFlags GroupSetFlags
    //    {
    //        get => _groupSet.GroupSetFlags;
    //        set
    //        {
    //            if (_groupSet.GroupSetFlags != value)
    //            {
    //                _groupSet.GroupSetFlags = value;
    //                OnPropertyChanged();
    //            }
    //        }
    //    }

    //    public ObservableCollection<ModGroupViewModel> ModGroups { get; }

    //    public ICommand AddModGroupCommand => new RelayCommand<object?>(AddModGroup);
    //    public ICommand CloneModGroupCommand => new RelayCommand<ModGroupViewModel>(CloneModGroup);

    //    private void CloneModGroup(ModGroupViewModel modGroupViewModel)
    //    {
    //        var clonedModGroup = modGroupViewModel.ModGroup.Clone();
    //        _groupSet.AddModGroup(clonedModGroup);
    //        ModGroups.Add(new ModGroupViewModel(clonedModGroup));
    //    }

    //    private void AddModGroup(object? parameter)
    //    {
    //        var newModGroup = new ModGroup
    //        {
    //            GroupSetID = _groupSet.GroupSetID // Set the GroupSetID of the new ModGroup
    //        };
    //        _groupSet.AddModGroup(newModGroup);
    //        ModGroups.Add(new ModGroupViewModel(newModGroup));
    //    }

    //    public event PropertyChangedEventHandler? PropertyChanged = delegate { };

    //    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    //    {
    //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    //    }
    //}

    public class GroupSetViewModel : INotifyPropertyChanged
    {
        private GroupSet _groupSet;
        public ObservableCollection<ModGroupViewModel> ModGroups { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public GroupSetViewModel(GroupSet groupSet)
        {
            _groupSet = groupSet;

            // Initialize ModGroups with existing groups from the current GroupSet
            ModGroups = new ObservableCollection<ModGroupViewModel>(
                groupSet.ModGroups.Select(mg => new ModGroupViewModel(mg))
            );

            // Add common groups from cached GroupSet 1
            AddCommonGroupsFromCachedGroupSet1();

            OnPropertyChanged(nameof(ModGroups));
        }

        // Method to add common groups from the cached GroupSet 1
        private void AddCommonGroupsFromCachedGroupSet1()
        {
            var cachedGroupSet1 = AggLoadInfo.Instance.GetCachedGroupSet1();
            if (cachedGroupSet1 == null)
            {
                App.LogDebug("GroupSet 1 not found in cache. Cannot add common groups.");
                return;
            }

            // Iterate through all groups in cached GroupSet 1
            foreach (var commonGroup in cachedGroupSet1.ModGroups)
            {
                if (commonGroup.GroupID < 0 && !ModGroups.Any(g => g.GroupID == commonGroup.GroupID))
                {
                    // Add the common group to both the ModGroups collection and the underlying group set
                    ModGroups.Add(new ModGroupViewModel(commonGroup));
                    _groupSet.ModGroups.Add(commonGroup);
                }
            }
        }

        // Other existing methods and properties...

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"GroupSetViewModel: {_groupSet.GroupSetName} with {ModGroups.Count} groups.";
        }
    }




}
