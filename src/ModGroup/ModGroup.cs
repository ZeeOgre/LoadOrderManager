using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace ZO.LoadOrderManager
{
    public class ModGroup : INotifyPropertyChanged
    {
        // Core Properties
        public long? GroupID { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long? ParentID { get; set; }
        public long? Ordinal { get; set; }
        public long? GroupSetID { get; set; }
        public string DisplayName => $"{GroupName} | {Description}";

        // Path-to-root structure for hierarchy navigation
        public List<long> PathToRoot { get; private set; } = new List<long>();

        // PluginID HashSet for quick lookup and manipulation
        private HashSet<long> _pluginIDs = new HashSet<long>();
        public IReadOnlyCollection<long> PluginIDs => _pluginIDs;

        // Observable collection of plugins associated with the group
        private ObservableCollection<Plugin> _plugins = new ObservableCollection<Plugin>();
        public ObservableCollection<Plugin> Plugins
        {
            get => _plugins;
            set
            {
                if (_plugins != value)
                {
                    _plugins = value;
                    OnPropertyChanged(nameof(Plugins));
                }
            }
        }

        // Property for tracking if the group is a reserved/special group
        private bool _isReservedGroup;
        public bool IsReservedGroup
        {
            get => _isReservedGroup;
            private set
            {
                if (_isReservedGroup != value)
                {
                    _isReservedGroup = value;
                    OnPropertyChanged(nameof(IsReservedGroup));
                }
            }
        }

        // Event for property changes (WPF binding support)
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (InitializationManager.IsAnyInitializing()) return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Constructor
        public ModGroup() { }

        public ModGroup(long? groupID, string groupName, string description, long? parentID, long? ordinal, long? groupSetID)
        {
            GroupID = groupID;
            GroupName = groupName;
            Description = description;
            ParentID = parentID;
            Ordinal = ordinal;
            GroupSetID = groupSetID;
            InitializeReservedStatus();
        }


        public static ModGroup? LoadModGroup(long groupID, long groupSetID)
        {
            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);

            command.CommandText = @"
        SELECT GroupID, GroupName, GroupDescription, ParentID, Ordinal, GroupSetID
        FROM vwModGroups
        WHERE GroupID = @GroupID AND GroupSetID = @GroupSetID";

            _ = command.Parameters.AddWithValue("@GroupID", groupID);
            _ = command.Parameters.AddWithValue("@GroupSetID", groupSetID);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new ModGroup
                {
                    GroupID = reader.GetInt64(0),
                    GroupName = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2), // Handle nullable Description
                    ParentID = reader.IsDBNull(3) ? null : reader.GetInt64(3), // Handle nullable ParentID
                    Ordinal = reader.IsDBNull(4) ? null : reader.GetInt64(4), // Handle nullable Ordinal
                    GroupSetID = reader.GetInt64(5)
                };
            }
            return null;
        }

        public static List<ModGroup> LoadModGroupsByGroupSet(long groupSetID)
        {
            var modGroups = new List<ModGroup>();

            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);
            command.CommandText = @"
                SELECT GroupID, GroupName, GroupDescription, ParentID, Ordinal, GroupSetID
                FROM vwModGroups
                WHERE GroupSetID = @GroupSetID";

            _ = command.Parameters.AddWithValue("@GroupSetID", groupSetID);

            if (groupSetID == 1)
            {
                command.CommandText += " AND GroupID != -997";
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var modGroup = new ModGroup
                {
                    GroupID = reader.GetInt64(0),
                    GroupName = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2), // Handle nullable Description
                    ParentID = reader.IsDBNull(3) ? null : reader.GetInt64(3), // Handle nullable ParentID
                    Ordinal = reader.IsDBNull(4) ? null : reader.GetInt64(4), // Handle nullable Ordinal
                    GroupSetID = reader.GetInt64(5)
                };
                modGroups.Add(modGroup);
            }

            return modGroups;
        }



        // Initialization method to set the reserved status based on GroupID
        private void InitializeReservedStatus()
        {
            if (GroupID < 0)
            {
                switch (GroupID)
                {
                    case -999:
                        IsReservedGroup = true;
                        break;
                    case -998:
                    case -997:
                        IsReservedGroup = false;
                        break;
                    default:
                        IsReservedGroup = true;
                        break;
                }
            }
            else
            {
                IsReservedGroup = false;
            }
        }

        private static readonly HashSet<string> ReservedWords = new HashSet<string>
{
    "uncategorized",
    "default",
    "never_load",
    "unassigned",
    "neverload",
    "CoreGameFiles",
    "inactive",
};


        // Changing the group’s parent and ordinal
        public void ChangeGroup(long newParentId)
        {
            // Disallow reserved groups as parents
            if (newParentId < 0)
            {
                throw new InvalidOperationException("Cannot change to a reserved group as parent.");
            }

            // Retrieve the AggLoadInfo singleton instance
            _ = AggLoadInfo.Instance;

            using var connection = DbManager.Instance.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Step 1: Combine the move and sibling ordinal adjustment into one SQL query
                using var updateCommand = new SQLiteCommand(connection)
                {
                    CommandText = @"
            -- Move the group to the new parent and calculate the new ordinal
            UPDATE GroupSetGroups
            SET ParentID = @NewParentID, Ordinal = (
                SELECT COALESCE(MAX(Ordinal), 0) + 1
                FROM GroupSetGroups
                WHERE ParentID = @NewParentID AND GroupSetID = @GroupSetID
            )
            WHERE GroupSetID = @GroupSetID AND GroupID = @GroupID;

            -- Decrement the ordinals of the old siblings after moving the group
            UPDATE GroupSetGroups
            SET Ordinal = Ordinal - 1
            WHERE ParentID = @ParentID AND Ordinal > @OldOrdinal AND GroupSetID = @GroupSetID;"
                };

                // Add the shared parameters for both queries
                _ = updateCommand.Parameters.AddWithValue("@NewParentID", newParentId);
                _ = updateCommand.Parameters.AddWithValue("@GroupSetID", this.GroupSetID ?? 1);
                _ = updateCommand.Parameters.AddWithValue("@GroupID", this.GroupID ?? 1);
                _ = updateCommand.Parameters.AddWithValue("@ParentID", this.ParentID ?? (object)DBNull.Value);
                _ = updateCommand.Parameters.AddWithValue("@OldOrdinal", this.Ordinal ?? 1);

                // Execute the combined queries
                _ = updateCommand.ExecuteNonQuery();
                // Step 2: Commit the transaction
                transaction.Commit();

            }
            catch (Exception ex)
            {
                // Rollback in case of any errors
                transaction.Rollback();
                App.LogDebug($"ChangeGroup error: {ex.Message}");
                throw;
            }

            // Save the current group after all changes
            //this.WriteGroup();
            AggLoadInfo.Instance.RefreshMetadataFromDB();
        }





        public ModGroup Clone()
        {
            var clonedModGroup = new ModGroup
            {
                GroupID = this.GroupID,
                Ordinal = this.Ordinal,
                Description = this.Description,
                GroupName = this.GroupName,
                ParentID = this.ParentID,
                GroupSetID = this.GroupSetID,
                Plugins = new ObservableCollection<Plugin>(this.Plugins?.Select(p => p.Clone()) ?? Enumerable.Empty<Plugin>()),
            };
            return clonedModGroup;
        }

        public ModGroup Clone(GroupSet groupSet)
        {
            var aggLoadInfoInstance = AggLoadInfo.Instance;
            long maxOrdinal = aggLoadInfoInstance.GroupSetGroups.Items
                .Where(gsg => gsg.groupSetID == groupSet.GroupSetID)
                .Max(gsg => gsg.Ordinal) + 1;

            return new ModGroup
            {
                GroupID = this.GroupID,
                Ordinal = maxOrdinal,
                Description = this.Description,
                GroupName = this.GroupName,
                ParentID = null,
                GroupSetID = groupSet.GroupSetID,
                Plugins = new ObservableCollection<Plugin>(this.Plugins?.Select(p =>
                {
                    var clonedPlugin = p.Clone();
                    clonedPlugin.GroupSetID = groupSet.GroupSetID;
                    return clonedPlugin;
                }) ?? Enumerable.Empty<Plugin>())
            };
        }

        // This method now calculates the group path based on the in-memory structure
        public void CalculatePathToRootUsingCache()
        {
            if (PathToRoot.Any()) return;

            PathToRoot = new List<long>();
            var currentGroupID = this.GroupID ?? 0;
            var aggLoadInfoInstance = AggLoadInfo.Instance;

            while (currentGroupID != 1)
            {
                PathToRoot.Insert(0, currentGroupID);
                var parentGroupTuple = aggLoadInfoInstance.GroupSetGroups.Items
                    .FirstOrDefault(g => g.groupID == currentGroupID);

                if (parentGroupTuple == default || !parentGroupTuple.parentID.HasValue)
                {
                    break;
                }

                currentGroupID = parentGroupTuple.parentID.Value;
            }

            if (!PathToRoot.Contains(1))
            {
                PathToRoot.Insert(0, 1);
            }
        }

        public string ToPluginsString()
        {
            CalculatePathToRootUsingCache();

            if (PathToRoot == null || PathToRoot.Count == 0)
            {
                return "No path to root available.";
            }

            int depth = PathToRoot.Count;
            string hashMarks = new string('#', depth + 1);
            string groupName = this.GroupName;
            string groupDescription = this.Description;

            return string.IsNullOrEmpty(groupDescription)
                ? $"{hashMarks} {groupName}"
                : $"{hashMarks} {groupName} @@ {groupDescription}";
        }

        public ModGroup WriteGroup()
        {
            var existingMatch = this.FindMatchingModGroup();
            if (existingMatch != null)
            {
                this.GroupID = existingMatch.GroupID;
            }

            using var connection = DbManager.Instance.GetConnection();
            using var transaction = connection.BeginTransaction();
            using var command = new SQLiteCommand(connection);

            if (!this.GroupID.HasValue)
            {
                command.CommandText = @"
                    INSERT INTO ModGroups (GroupName, Description)
                    VALUES (@GroupName, @Description)
                    RETURNING GroupID;";
                _ = command.Parameters.AddWithValue("@GroupName", this.GroupName ?? (object)DBNull.Value);
                _ = command.Parameters.AddWithValue("@Description", this.Description ?? (object)DBNull.Value);

                try
                {
                    this.GroupID = Convert.ToInt64(command.ExecuteScalar());
                    App.LogDebug($"New ModGroup inserted INTO database: GroupID={this.GroupID}, GroupName={this.GroupName}, Description={this.Description}");
                }
                catch (SQLiteException ex)
                {
                    transaction.Rollback();
                    App.LogDebug($"Error inserting new ModGroup: {ex.Message}");
                    throw;
                }
            }
            else
            {
                command.CommandText = @"
                    UPDATE ModGroups
                    SET GroupName = COALESCE(@GroupName, GroupName),
                        Description = COALESCE(@Description, Description)
                    WHERE GroupID = @GroupID;";
                _ = command.Parameters.AddWithValue("@GroupID", this.GroupID);
                _ = command.Parameters.AddWithValue("@GroupName", this.GroupName ?? (object)DBNull.Value);
                _ = command.Parameters.AddWithValue("@Description", this.Description ?? (object)DBNull.Value);
                _ = command.ExecuteNonQuery();
                App.LogDebug($"ModGroup updated in database: {this.ToString()}");
            }

            // Write to GroupSetGroups
            command.CommandText = @"
                INSERT INTO GroupSetGroups (GroupID, GroupSetID, ParentID, Ordinal)
                VALUES (@GroupID, @GroupSetID, @ParentID, @Ordinal)
                ON CONFLICT(GroupID, GroupSetID) DO UPDATE SET
                    ParentID = COALESCE(@ParentID, ParentID),
                    Ordinal = COALESCE(@Ordinal, Ordinal);";
            command.Parameters.Clear();
            _ = command.Parameters.AddWithValue("@GroupID", this.GroupID);
            _ = command.Parameters.AddWithValue("@GroupSetID", this.GroupSetID ?? (object)DBNull.Value);
            _ = command.Parameters.AddWithValue("@ParentID", this.ParentID ?? (object)DBNull.Value);
            _ = command.Parameters.AddWithValue("@Ordinal", this.Ordinal);
            _ = command.ExecuteNonQuery();

            transaction.Commit();
            App.LogDebug($"Transaction committed successfully.");

            return this;
        }

        public override string ToString()
        {
            return $"{GroupName} (ID: {GroupID}, ParentID: {ParentID}, Ordinal: {Ordinal}, GroupSetID: {GroupSetID})";
        }

        public string ToYAMLObject()
        {
            var serializer = new SerializerBuilder().Build();
            return serializer.Serialize(this);
        }

        private string NormalizeString(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Regex.Replace(input, @"[^a-zA-Z0-9]", "").Trim().ToLowerInvariant();
        }

        public static ModGroup GetModGroupById(long groupId)
        {
            return AggLoadInfo.Instance.Groups
                .FirstOrDefault(g => g.GroupID == groupId) ?? throw new InvalidOperationException("Group not found");
        }

        public ModGroup? FindMatchingModGroup()
        {
            string normalizedGroupName = NormalizeString(GroupName);
            string normalizedDescription = NormalizeString(Description);

            var matchingGroup = ReservedWords.FirstOrDefault(word => normalizedGroupName.Contains(word, StringComparison.OrdinalIgnoreCase) || normalizedDescription.Contains(word, StringComparison.OrdinalIgnoreCase));
            if (matchingGroup != null)
            {
                return LoadModGroup(this.GroupID ?? 0, 1);
            }

            return null;
        }

        public override bool Equals(object obj)
        {
            if (obj is ModGroup other)
            {
                return this.GroupID == other.GroupID && this.GroupSetID == other.GroupSetID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + GroupID.GetHashCode();
                hash = hash * 23 + GroupSetID.GetHashCode();
                return hash;
            }
        }

        public void SwapLocations(ModGroup other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            _ = ((long)this.GroupID!, (long)this.GroupSetID!, this.ParentID, (long)this.Ordinal!);
            _ = ((long)other.GroupID!, (long)other.GroupSetID!, other.ParentID, (long)other.Ordinal!);

            var tempGroupSetID = this.GroupSetID;
            var tempParentID = this.ParentID;
            var tempOrdinal = this.Ordinal;

            this.GroupSetID = other.GroupSetID;
            this.ParentID = other.ParentID;
            this.Ordinal = other.Ordinal;

            other.GroupSetID = tempGroupSetID;
            other.ParentID = tempParentID;
            other.Ordinal = tempOrdinal;


            _ = this.WriteGroup();
            _ = other.WriteGroup();
            AggLoadInfo.Instance.RefreshMetadataFromDB();
        }
    }
}
