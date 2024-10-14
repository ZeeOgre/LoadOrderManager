using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.SQLite;
using ZO.LoadOrderManager;

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

public class GroupSet : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    
    public long GroupSetID { get; set; }
    public string GroupSetName { get; set; } = string.Empty;
    private GroupFlags _groupSetFlags;
    public GroupFlags GroupSetFlags
    {
        get => _groupSetFlags;
        set
        {
            if (_groupSetFlags != value)
            {
                _groupSetFlags = value;
                //OnPropertyChanged(nameof(GroupSetFlags));
                //OnPropertyChanged(nameof(IsUninitialized));
                //OnPropertyChanged(nameof(IsDefaultGroup));
                //OnPropertyChanged(nameof(IsReadOnly));
                //OnPropertyChanged(nameof(IsFavorite));
                //OnPropertyChanged(nameof(IsReadyToLoad));
                //OnPropertyChanged(nameof(AreFilesLoaded));
            }
        }
    }


    //public ObservableCollection<ModGroup> ModGroups { get; set; } = new ObservableCollection<ModGroup>();
    //public ObservableCollection<LoadOut> LoadOuts { get; set; } = new ObservableCollection<LoadOut>();

    // Constructor to create a new GroupSet or load an existing one
    public GroupSet(long groupSetID, string groupSetName, GroupFlags groupSetFlags)
    {
        GroupSetID = groupSetID;
        GroupSetName = groupSetName;
        GroupSetFlags = groupSetFlags;

        // If this is an empty group set, mark it as ReadyToLoad and Uninitialized, but do not create database entries.
        if (GroupSetID == 0)
        {
            GroupSetName = "EmptyGroupSet";
            GroupSetFlags = GroupFlags.Uninitialized | GroupFlags.ReadyToLoad; // Mark as both uninitialized and ready to load
        }
    }

    // Default constructor
    public GroupSet() : this(0, "EmptyGroupSet", GroupFlags.Uninitialized | GroupFlags.ReadyToLoad) { }

    // SaveGroupSet method
    public void SaveGroupSet()
    {
        using var connection = DbManager.Instance.GetConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            using var command = new SQLiteCommand(connection);

            if (this.GroupSetID == 0) // New GroupSet
            {
                command.CommandText = @"
                    INSERT INTO GroupSets (GroupSetName, GroupSetFlags)
                    VALUES (@GroupSetName, @GroupSetFlags)
                    RETURNING GroupSetID;
                ";
                command.Parameters.AddWithValue("@GroupSetName", this.GroupSetName);
                command.Parameters.AddWithValue("@GroupSetFlags", (long)this.GroupSetFlags);
                this.GroupSetID = (long)command.ExecuteScalar();
            }
            else // Update existing GroupSet
            {
                command.CommandText = @"
                    UPDATE GroupSets
                    SET GroupSetName = @GroupSetName, GroupSetFlags = @GroupSetFlags
                    WHERE GroupSetID = @GroupSetID;
                ";
                command.Parameters.AddWithValue("@GroupSetName", this.GroupSetName);
                command.Parameters.AddWithValue("@GroupSetFlags", (long)this.GroupSetFlags);
                command.Parameters.AddWithValue("@GroupSetID", this.GroupSetID);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            App.LogDebug($"Error saving GroupSet: {ex.Message}");
            throw;
        }
    }

    // LoadGroupSet method
    public static GroupSet? LoadGroupSet(long groupSetID)
    {
        using var connection = DbManager.Instance.GetConnection();
        using var command = new SQLiteCommand(connection);

        command.CommandText = @"
            SELECT GroupSetID, GroupSetName, GroupSetFlags
            FROM GroupSets
            WHERE GroupSetID = @GroupSetID;
        ";
        command.Parameters.AddWithValue("@GroupSetID", groupSetID);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var groupSet = new GroupSet
            {
                GroupSetID = reader.GetInt64(reader.GetOrdinal("GroupSetID")),
                GroupSetName = reader.GetString(reader.GetOrdinal("GroupSetName")),
                GroupSetFlags = (GroupFlags)reader.GetInt64(reader.GetOrdinal("GroupSetFlags"))
            };

            // Load associated ModGroups
            // var modGroupsList = ModGroup.LoadModGroupsByGroupSet(groupSetID);
            //groupSet.ModGroups = new ObservableCollection<ModGroup>(modGroupsList);

            // Load associated LoadOuts
            //var loadOuts = GetAllLoadOuts(groupSetID);
            //groupSet.LoadOuts = new ObservableCollection<LoadOut>(loadOuts);

            return groupSet;
        }

        return null;
    }

    // CreateEmptyGroupSet method
    public static GroupSet CreateEmptyGroupSet()
    {
        using var connection = DbManager.Instance.GetConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            using var command = new SQLiteCommand(connection);
            command.CommandText = @"
                INSERT INTO GroupSets (GroupSetName, GroupSetFlags)
                VALUES (@GroupSetName, @GroupSetFlags)
                RETURNING GroupSetID;
            ";
            command.Parameters.AddWithValue("@GroupSetName", "EmptyGroupSet");
            command.Parameters.AddWithValue("@GroupSetFlags", (long)(GroupFlags.Uninitialized | GroupFlags.ReadyToLoad));

            long groupSetID = (long)command.ExecuteScalar();

            // Insert or ignore GroupID = 1 (Default Group)
            command.CommandText = @"
                INSERT OR IGNORE INTO GroupSetGroups (GroupID, GroupSetID, ParentID, Ordinal)
                VALUES (1, @GroupSetID, 0, 0);
            ";
            command.Parameters.AddWithValue("@GroupSetID", groupSetID);
            command.ExecuteNonQuery();

            // Insert or ignore GroupID = -997 (Uncategorized Group)
            command.CommandText = @"
                INSERT OR IGNORE INTO GroupSetGroups (GroupID, GroupSetID, ParentID, Ordinal)
                VALUES (-997, @GroupSetID, 1, 9997);
            ";
            command.ExecuteNonQuery();

            transaction.Commit();

            return new GroupSet(groupSetID, "EmptyGroupSet", GroupFlags.Uninitialized | GroupFlags.ReadyToLoad);
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    // GroupSetChanged method for notification and reloading
    public static event NotifyCollectionChangedEventHandler? GroupSetChanged;

    private static void OnGroupSetChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!((GroupSet)sender).IsUninitialized && ((GroupSet)sender).AreFilesLoaded)
        {
            GroupSetChanged?.Invoke(sender, e);
        }
    }

    // Flag checks
    public bool IsUninitialized
    {
        get => (GroupSetFlags & GroupFlags.Uninitialized) == GroupFlags.Uninitialized;
        set
        {
            if (value)
                GroupSetFlags |= GroupFlags.Uninitialized;
            else
                GroupSetFlags &= ~GroupFlags.Uninitialized;
            OnPropertyChanged(nameof(IsUninitialized));
        }
    }
    public bool IsDefaultGroup
    {
        get => (GroupSetFlags & GroupFlags.DefaultGroup) == GroupFlags.DefaultGroup;
        set
        {
            if (value)
                GroupSetFlags |= GroupFlags.DefaultGroup;
            else
                GroupSetFlags &= ~GroupFlags.DefaultGroup;
            OnPropertyChanged(nameof(IsDefaultGroup));
        }
    }
    public bool IsReadOnly
    {
        get => (GroupSetFlags & GroupFlags.ReadOnly) == GroupFlags.ReadOnly;
        set
        {
            if (value)
                GroupSetFlags |= GroupFlags.ReadOnly;
            else
                GroupSetFlags &= ~GroupFlags.ReadOnly;
            OnPropertyChanged(nameof(IsReadOnly));
        }
    }
    public bool IsFavorite
    {
        get => (GroupSetFlags & GroupFlags.Favorite) == GroupFlags.Favorite;
        set
        {
            if (value)
                GroupSetFlags |= GroupFlags.Favorite;
            else
                GroupSetFlags &= ~GroupFlags.Favorite;
            OnPropertyChanged(nameof(IsFavorite));
        }
    }
    public bool IsReadyToLoad
    {
        get => (GroupSetFlags & GroupFlags.ReadyToLoad) == GroupFlags.ReadyToLoad;
        set
        {
            if (value)
                GroupSetFlags |= GroupFlags.ReadyToLoad;
            else
                GroupSetFlags &= ~GroupFlags.ReadyToLoad;
            OnPropertyChanged(nameof(IsReadyToLoad));
        }
    }
    public bool AreFilesLoaded
    {
        get => (GroupSetFlags & GroupFlags.FilesLoaded) == GroupFlags.FilesLoaded;
        set
        {
            if (value)
                GroupSetFlags |= GroupFlags.FilesLoaded;
            else
                GroupSetFlags &= ~GroupFlags.FilesLoaded;
            OnPropertyChanged(nameof(AreFilesLoaded));
        }
    }


    // Clone method
    public GroupSet Clone()
    {
        var clonedGroupSet = new GroupSet(this.GroupSetID, this.GroupSetName, this.GroupSetFlags);
        //foreach (var modgroup in this.modgroups)
        //{
        //    clonedgroupset.modgroups.add(modgroup.clone());
        //}
        return clonedGroupSet;
    }

    // Equality comparison
    public override bool Equals(object obj)
    {
    if (obj is GroupSet otherGroupSet)
        {
            return this.GroupSetID == otherGroupSet.GroupSetID ||
                   this.GroupSetName == otherGroupSet.GroupSetName;
        }

        return false;
    }

    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            int hash = 17;
            hash = hash * 23 + GroupSetID.GetHashCode();
            hash = hash * 23 + (GroupSetName?.GetHashCode() ?? 0);
            return hash;
        }
    }





    // Dummy LoadOut methods - replace these with actual implementations
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
                GroupSetID = reader.GetInt64(reader.GetOrdinal("GroupSetID")),
                IsFavorite = reader.GetBoolean(reader.GetOrdinal("IsFavorite")),
            };
            loadOut.LoadEnabledPlugins();
            loadOuts.Add(loadOut);
        }
        return loadOuts;
    }


    // DeleteRecord method
    public static void DeleteRecord(long groupSetID)
    {
        using var connection = DbManager.Instance.GetConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var groupSet = LoadGroupSet(groupSetID);
            if (groupSet == null)
            {
                throw new InvalidOperationException($"GroupSet with ID {groupSetID} not found.");
            }

            // Check if GroupSet is read-only, favorite, or default group
            if (groupSet.IsReadOnly)
            {
                throw new InvalidOperationException("Cannot delete a read-only GroupSet.");
            }
            if (groupSet.IsFavorite)
            {
                throw new InvalidOperationException("Cannot delete a favorite GroupSet.");
            }
            if (groupSet.IsDefaultGroup)
            {
                throw new InvalidOperationException("Cannot delete the default GroupSet.");
            }

            using var command = new SQLiteCommand(connection);

            // Delete from GroupSetGroups
            command.CommandText = "DELETE FROM GroupSetGroups WHERE GroupSetID = @GroupSetID;";
            command.Parameters.AddWithValue("@GroupSetID", groupSetID);
            command.ExecuteNonQuery();

            // Delete from GroupSetPlugins
            command.CommandText = "DELETE FROM GroupSetPlugins WHERE GroupSetID = @GroupSetID;";
            command.ExecuteNonQuery();

            // Delete from LoadOutProfiles
            command.CommandText = "DELETE FROM LoadOutProfiles WHERE GroupSetID = @GroupSetID;";
            command.ExecuteNonQuery();

            // Delete from GroupSets
            command.CommandText = "DELETE FROM GroupSets WHERE GroupSetID = @GroupSetID;";
            command.ExecuteNonQuery();

            transaction.Commit();

            // Refresh AggLoadInfo after deletion
            if (AggLoadInfo.Instance.ActiveGroupSet?.GroupSetID == groupSetID)
            {
                AggLoadInfo.Instance.ActiveGroupSet = AggLoadInfo.GroupSets.FirstOrDefault(gs => gs.IsFavorite) ?? AggLoadInfo.GroupSets.FirstOrDefault();
            }
            AggLoadInfo.Instance.RefreshAllData();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            App.LogDebug($"Error deleting GroupSet: {ex.Message}");
            throw;
        }
    }


    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
