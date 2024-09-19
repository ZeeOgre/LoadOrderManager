using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ZO.LoadOrderManager
{

[Flags]
    public enum GroupFlags
    {
        None = 0,
        DefaultGroup = 1 << 0, // 1
        ReadOnly = 1 << 1,     // 2
        Hidden = 1 << 2,       // 4
        Archived = 1 << 3,     // 8
        Favorite = 1 << 4,     // 16
        // Add other characteristics as needed
    }


    public class GroupSet
    {
        public int GroupSetID { get; set; }
        public string GroupSetName { get; set; }
        public GroupFlags GroupSetFlags { get; set; }
        public ObservableCollection<ModGroup> ModGroups { get; set; } = new ObservableCollection<ModGroup>();

        public GroupSet(int groupSetID, string groupSetName, GroupFlags groupSetFlags)
        {
            GroupSetID = groupSetID;
            GroupSetName = groupSetName;
            GroupSetFlags = groupSetFlags;

            // Ensure the default GroupSet has the correct flags
            if (GroupSetID == 1)
            {
                if (GroupSetFlags != (GroupFlags.DefaultGroup | GroupFlags.ReadOnly))
                {
                    throw new ArgumentException("GroupSet 1 must have flags set to DefaultGroup and ReadOnly.");
                }
            }
            else
            {
                // Include the default GroupSet in all other GroupSets
                var defaultGroupSet = LoadGroupSet(1);
                if (defaultGroupSet != null)
                {
                    Merge(defaultGroupSet);
                }
            }
        }

        public void AddModGroup(ModGroup modGroup)
        {
            if (ModGroups.Any(mg => mg.GroupID == modGroup.GroupID))
            {
                throw new InvalidOperationException("ModGroup already exists in this GroupSet.");
            }
            ModGroups.Add(modGroup);
        }

        public bool IsDefaultGroup => (GroupSetFlags & GroupFlags.DefaultGroup) == GroupFlags.DefaultGroup;
        public bool IsReadOnly => (GroupSetFlags & GroupFlags.ReadOnly) == GroupFlags.ReadOnly;
        public bool IsHidden => (GroupSetFlags & GroupFlags.Hidden) == GroupFlags.Hidden;
        public bool IsArchived => (GroupSetFlags & GroupFlags.Archived) == GroupFlags.Archived;
        public bool IsFavorite => (GroupSetFlags & GroupFlags.Favorite) == GroupFlags.Favorite;

        // Method to clone the GroupSet
        public GroupSet Clone()
        {
            var clonedGroupSet = new GroupSet(this.GroupSetID, this.GroupSetName, this.GroupSetFlags);
            foreach (var modGroup in this.ModGroups)
            {
                // Pass the current GroupSet instance to the ModGroup.Clone method
                clonedGroupSet.ModGroups.Add(modGroup.Clone(clonedGroupSet));
            }
            return clonedGroupSet;
        }

        // Method to merge another GroupSet into this one
        public void Merge(GroupSet otherGroupSet)
        {
            foreach (var modGroup in otherGroupSet.ModGroups)
            {
                if (!this.ModGroups.Any(mg => mg.GroupID == modGroup.GroupID))
                {
                    // Pass the current GroupSet instance to the ModGroup.Clone method
                    this.ModGroups.Add(modGroup.Clone(this));
                }
            }
        }

        // Method to load GroupSet from the database
        public static GroupSet? LoadGroupSet(int groupSetID)
        {
            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);
            command.CommandText = "SELECT * FROM GroupSets WHERE GroupSetID = @GroupSetID";
            command.Parameters.AddWithValue("@GroupSetID", groupSetID);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var groupSet = new GroupSet(
                    reader.GetInt32(reader.GetOrdinal("GroupSetID")),
                    reader.GetString(reader.GetOrdinal("GroupSetName")),
                    (GroupFlags)reader.GetInt32(reader.GetOrdinal("GroupSetFlags"))
                );

                var modGroupsList = ModGroup.LoadModGroupsByGroupSet(groupSetID);
                groupSet.ModGroups = new ObservableCollection<ModGroup>(modGroupsList);
                return groupSet;
            }
            return null;
        }

        // Method to save GroupSet to the database
        public void SaveGroupSet()
        {
            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);

            // Check if the GroupSet already exists
            command.CommandText = "SELECT GroupSetFlags FROM GroupSets WHERE GroupSetID = @GroupSetID";
            command.Parameters.AddWithValue("@GroupSetID", GroupSetID);

            var existingFlags = command.ExecuteScalar();
            if (existingFlags != null)
            {
                // Compare existing flags with new flags
                var existingGroupSetFlags = (GroupFlags)(int)existingFlags;
                if (existingGroupSetFlags != GroupSetFlags)
                {
                    // Handle flag conflict (e.g., log a warning, throw an exception, or merge flags)
                    throw new InvalidOperationException($"GroupSet with ID {GroupSetID} already exists with different flags.");
                }
            }

            // Prepare insert or replace command
            command.CommandText = @"
                INSERT OR REPLACE INTO GroupSets (GroupSetID, GroupSetName, GroupSetFlags)
                VALUES (@GroupSetID, @GroupSetName, @GroupSetFlags)";
            command.Parameters.AddWithValue("@GroupSetName", GroupSetName);
            command.Parameters.AddWithValue("@GroupSetFlags", (int)GroupSetFlags);

            command.ExecuteNonQuery();

            // Save ModGroups
            foreach (var modGroup in ModGroups)
            {
                modGroup.WriteGroup();
            }
        }
    }


    public class GroupSetViewModel : INotifyPropertyChanged
    {
        private GroupSet _groupSet;

        public GroupSetViewModel(GroupSet groupSet)
        {
            _groupSet = groupSet;
            ModGroups = new ObservableCollection<ModGroupViewModel>(
                groupSet.ModGroups.Select(mg => new ModGroupViewModel(mg))
            );
        }

        public int GroupSetID
        {
            get => _groupSet.GroupSetID;
            set
            {
                if (_groupSet.GroupSetID != value)
                {
                    _groupSet.GroupSetID = value;
                    OnPropertyChanged();
                }
            }
        }

        public string GroupSetName
        {
            get => _groupSet.GroupSetName;
            set
            {
                if (_groupSet.GroupSetName != value)
                {
                    _groupSet.GroupSetName = value;
                    OnPropertyChanged();
                }
            }
        }

        public GroupFlags GroupSetFlags
        {
            get => _groupSet.GroupSetFlags;
            set
            {
                if (_groupSet.GroupSetFlags != value)
                {
                    _groupSet.GroupSetFlags = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<ModGroupViewModel> ModGroups { get; }

        public ICommand AddModGroupCommand => new RelayCommand(AddModGroup);

        private void AddModGroup()
        {
            var newModGroup = new ModGroup();
            _groupSet.AddModGroup(newModGroup);
            ModGroups.Add(new ModGroupViewModel(newModGroup));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ModGroupViewModel : INotifyPropertyChanged
    {
        private ModGroup _modGroup;

        public ModGroupViewModel(ModGroup modGroup)
        {
            _modGroup = modGroup;
        }

        public int GroupID
        {
            get => _modGroup.GroupID;
            set
            {
                if (_modGroup.GroupID != value)
                {
                    _modGroup.GroupID = value;
                    OnPropertyChanged();
                }
            }
        }

        public string GroupName
        {
            get => _modGroup.GroupName;
            set
            {
                if (_modGroup.GroupName != value)
                {
                    _modGroup.GroupName = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<Plugin> Plugins => _modGroup.Plugins;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


}
