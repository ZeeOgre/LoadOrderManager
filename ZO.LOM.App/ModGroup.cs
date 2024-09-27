using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
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
        public string DisplayName => $"{GroupName} ({GroupSetID}) | {Description}";

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
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // Constructor
        public ModGroup()
        {
        }

        public ModGroup(long? groupID, string groupName, string description, long? parentID, long? ordinal, long? groupSetID)
        {
            GroupID = groupID; // Set to 0 if null, assuming 0 is a placeholder for new entries
            GroupName = groupName;
            Description = description;
            ParentID = parentID;
            Ordinal = ordinal;
            GroupSetID = groupSetID;
            InitializeReservedStatus(); // Initialize reserved status based on GroupID
        }


        // Initialization method to set the reserved status based on GroupID
        private void InitializeReservedStatus()
        {
            if (GroupID < 0)
            {
                // Mark group as reserved if it's one of the special groups
                switch (GroupID)
                {
                    case -999: // Core game files, typically reserved
                        IsReservedGroup = true;
                        break;
                    case -998: // Never Load
                    case -997: // Uncategorized
                        IsReservedGroup = false;
                        break;
                    default: // Other negative IDs are reserved by default
                        IsReservedGroup = true;
                        break;
                }
            }
            else
            {
                IsReservedGroup = false; // Positive GroupIDs are user-defined, not reserved
            }
        }

        // Method to add a plugin to the group
        public void AddPlugin(Plugin plugin)
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));

            if (IsReservedGroup && GroupID != -998 && GroupID != -997)
            {
                throw new InvalidOperationException("Cannot add plugins to a reserved group.");
            }

            _plugins.Add(plugin);
            _pluginIDs.Add(plugin.PluginID);
            OnPropertyChanged(nameof(Plugins));
        }

        // Method to remove a plugin from the group
        public void RemovePlugin(Plugin plugin)
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));

            if (IsReservedGroup && GroupID != -998 && GroupID != -997)
            {
                throw new InvalidOperationException("Cannot remove plugins from a reserved group.");
            }

            _plugins.Remove(plugin);
            _pluginIDs.Remove(plugin.PluginID);
            OnPropertyChanged(nameof(Plugins));
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

        public void ChangeGroup(long newParentId)
        {
            // Disallow reserved groups as parents
            if (newParentId < 0)
            {
                throw new InvalidOperationException("Cannot change to a reserved group as parent.");
            }

            // Retrieve the AggLoadInfo singleton instance
            var aggLoadInfoInstance = AggLoadInfo.Instance;

            // Step 1: Decrement ordinals of sibling groups under the current parent
            var currentSiblings = this.GetGroupSiblingsFromAggLoad(aggLoadInfoInstance);
            foreach (var sibling in currentSiblings)
            {
                if (sibling.Ordinal > this.Ordinal)
                {
                    sibling.Ordinal--;
                }
            }

            // Step 2: Find the maximum ordinal of the new parent's children
            long maxOrdinal = 0;
            var newParentChildren = new ModGroup { GroupID = newParentId }.GetGroupChildrenFromAggLoad(aggLoadInfoInstance);
            if (newParentChildren.Any())
            {
                maxOrdinal = newParentChildren.Max(g => g.Ordinal) ?? 0;
            }

            // Step 3: Update the ParentID and GroupOrdinal of the group being moved
            this.ParentID = newParentId;
            this.Ordinal = maxOrdinal + 1;

            // Optional: Update AggLoadInfo instance if necessary
            // aggLoadInfoInstance.UpdateGroup(this); // This depends on the implementation of AggLoadInfo



        }


        public List<ModGroup> GetGroupChildrenFromAggLoad(AggLoadInfo? aggLoadInfoObject = null)
        {
            aggLoadInfoObject ??= AggLoadInfo.Instance;
            return aggLoadInfoObject.Groups.Where(g => g.ParentID == this.GroupID).ToList();
        }

        public List<ModGroup> GetGroupSiblingsFromAggLoad(AggLoadInfo? aggLoadInfoObject = null)
        {
            aggLoadInfoObject ??= AggLoadInfo.Instance;
            if (this.ParentID == null)
            {
                return new List<ModGroup>();
            }
            return aggLoadInfoObject.Groups.Where(g => g.ParentID == this.ParentID && g.GroupID != this.GroupID).ToList();
        }


        // Constructor for quick initialization from database view row
        public ModGroup(Dictionary<string, object> row)
        {
            GroupID = Convert.ToInt64(row["GroupID"]);
            GroupName = row["GroupName"].ToString();
            Description = row["GroupDescription"].ToString();
            ParentID = row["ParentID"] != DBNull.Value ? Convert.ToInt64(row["ParentID"]) : (long?)null;
            Ordinal = row["GroupOrdinal"] != DBNull.Value ? Convert.ToInt64(row["GroupOrdinal"]) : (long?)null;
            GroupSetID = row["GroupSetID"] != DBNull.Value ? Convert.ToInt64(row["GroupSetID"]) : (long?)null;
        }

        // Clone Constructor
        public ModGroup(ModGroup source)
        {
            GroupID = source.GroupID;
            GroupName = source.GroupName;
            Description = source.Description;
            ParentID = source.ParentID;
            Ordinal = source.Ordinal;
            GroupSetID = source.GroupSetID;
            IsReservedGroup = source.IsReservedGroup;
            PathToRoot = new List<long>(source.PathToRoot);
            _pluginIDs = new HashSet<long>(source._pluginIDs);
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

        public ModGroup Clone(string groupName)
        {
            return new ModGroup
            {
                GroupID = this.GroupID,
                Ordinal = GetMaxOrdinalForNewGroup(this.ParentID), // Set to max ordinal of siblings within the current GroupSet
                Description = this.Description,
                GroupName = groupName, // Set the new name
                ParentID = this.ParentID,
                GroupSetID = this.GroupSetID,
                Plugins = new ObservableCollection<Plugin>(this.Plugins?.Select(p => p.Clone()) ?? Enumerable.Empty<Plugin>()),
            };
        }

        public ModGroup Clone(GroupSet groupSet)
        {
            var newOrdinal = GetMaxOrdinalFromDatabase(null, groupSet.GroupSetID); // Get the max ordinal directly from the database

            return new ModGroup
            {
                GroupID = this.GroupID,
                Ordinal = newOrdinal, // Use the retrieved max ordinal
                Description = this.Description,
                GroupName = this.GroupName,
                ParentID = null, // Root level for new group set
                GroupSetID = groupSet.GroupSetID,
                Plugins = new ObservableCollection<Plugin>(this.Plugins?.Select(p =>
                {
                    var clonedPlugin = p.Clone();
                    clonedPlugin.GroupSetID = groupSet.GroupSetID;
                    return clonedPlugin;
                }) ?? Enumerable.Empty<Plugin>()),
            };
        }
        public static ModGroup? LoadModGroup(long groupID, long groupSetID)
        {
            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);
            if (groupID < 0)
            {
                // Reserved groups are not stored in the database
                groupSetID = 1;
            }
            // Query vwModGroups directly without unnecessary joins
            command.CommandText = @"
SELECT
    GroupID,
    GroupOrdinal,
    GroupName,
    GroupDescription,
    ParentID,
    GroupSetID,
    PluginID,
    PluginName,
    PluginDescription,
    Achievements,
    TimeStamp,
    Version,
    State,
    BethesdaID,
    NexusID,
    GroupOrdinal
FROM vwModGroups
WHERE GroupID = @GroupID AND GroupSetID = @GroupSetID
ORDER BY GroupOrdinal"; // Correct ordering based on the group

            command.Parameters.AddWithValue("@GroupID", groupID);
            command.Parameters.AddWithValue("@GroupSetID", groupSetID);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var newModGroup = new ModGroup
                {
                    GroupID = reader.GetInt64(reader.GetOrdinal("GroupID")),
                    Ordinal = reader.IsDBNull(reader.GetOrdinal("GroupOrdinal")) ? null : reader.GetInt64(reader.GetOrdinal("GroupOrdinal")),
                    Description = reader.IsDBNull(reader.GetOrdinal("GroupDescription")) ? null : reader.GetString(reader.GetOrdinal("GroupDescription")),
                    GroupName = reader.IsDBNull(reader.GetOrdinal("GroupName")) ? null : reader.GetString(reader.GetOrdinal("GroupName")),
                    ParentID = reader.IsDBNull(reader.GetOrdinal("ParentID")) ? null : reader.GetInt64(reader.GetOrdinal("ParentID")),
                    GroupSetID = reader.IsDBNull(reader.GetOrdinal("GroupSetID")) ? null : reader.GetInt64(reader.GetOrdinal("GroupSetID")),
                    Plugins = new ObservableCollection<Plugin>(),
                    PathToRoot = new List<long>() // Initialize the path to root list
                };

                // Populate the path to root
                long? currentParentID = newModGroup.ParentID;
                while (currentParentID != null)
                {
                    newModGroup.PathToRoot.Insert(0, currentParentID.Value);
                    currentParentID = GetParentIDFromDatabase(currentParentID.Value); // Utility method to get the parent ID
                }

                // Ensure the current group is included in the path to root
                newModGroup.PathToRoot.Add(newModGroup.GroupID ?? 0);

                // Load Plugin details (important for malongaining plugin order)
                do
                {
                    if (!reader.IsDBNull(reader.GetOrdinal("PluginID")))
                    {
                        var plugin = new Plugin
                        {
                            PluginID = reader.GetInt64(reader.GetOrdinal("PluginID")),
                            PluginName = reader.IsDBNull(reader.GetOrdinal("PluginName")) ? string.Empty : reader.GetString(reader.GetOrdinal("PluginName")),
                            Description = reader.IsDBNull(reader.GetOrdinal("PluginDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("PluginDescription")),
                            Achievements = reader.IsDBNull(reader.GetOrdinal("Achievements")) ? false : reader.GetInt64(reader.GetOrdinal("Achievements")) == 1,
                            DTStamp = reader.IsDBNull(reader.GetOrdinal("TimeStamp")) ? string.Empty : reader.GetString(reader.GetOrdinal("TimeStamp")),
                            Version = reader.IsDBNull(reader.GetOrdinal("Version")) ? string.Empty : reader.GetString(reader.GetOrdinal("Version")),
                            BethesdaID = reader.IsDBNull(reader.GetOrdinal("BethesdaID")) ? string.Empty : reader.GetString(reader.GetOrdinal("BethesdaID")),
                            NexusID = reader.IsDBNull(reader.GetOrdinal("NexusID")) ? string.Empty : reader.GetString(reader.GetOrdinal("NexusID")),
                            State = reader.IsDBNull(reader.GetOrdinal("State")) ? ModState.None : (ModState)reader.GetInt64(reader.GetOrdinal("State")),
                            GroupOrdinal = reader.IsDBNull(reader.GetOrdinal("GroupOrdinal")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("GroupOrdinal")),
                            GroupID = newModGroup.GroupID, // Assign GroupID to the Plugin
                            GroupSetID = newModGroup.GroupSetID // Assign GroupSetID to the Plugin
                        };
                        newModGroup.Plugins.Add(plugin);
                    }
                } while (reader.Read());

                return newModGroup;
            }

            return null;
        }

        public ModGroup WriteGroup()
        {
            // Check for an existing match based on GroupName, Description, ParentID, and GroupSetID.
            var existingMatch = this.FindMatchingModGroup();
            if (existingMatch != null)
            {
                return existingMatch; // Avoid re-insertion if match found
            }

            using var connection = DbManager.Instance.GetConnection();
            using var transaction = connection.BeginTransaction(); // Start a transaction to ensure atomic updates
            using var command = new SQLiteCommand(connection);

            // Step 1: Insert or Update the ModGroup
            if (!this.GroupID.HasValue) // If GroupID is null, it's a new entry
            {
                // Insert a new group
                command.CommandText = @"
        INSERT INTO ModGroups (GroupName, Description)
        VALUES (@GroupName, @Description)
        RETURNING GroupID;"; // Retrieve the new GroupID

                command.Parameters.AddWithValue("@GroupName", this.GroupName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Description", this.Description ?? (object)DBNull.Value);

                try
                {
                    this.GroupID = Convert.ToInt64(command.ExecuteScalar());
                    App.LogDebug($"New ModGroup inserted INTO database: GroupID={this.GroupID}, GroupName={this.GroupName}, Description={this.Description}");
                }
                catch (SQLiteException ex)
                {
                    transaction.Rollback();
                    App.LogDebug($"Error inserting new ModGroup: {ex.Message}");
                    throw; // Re-throw the exception after logging
                }
            }
            else
            {
                // Update an existing group
                command.CommandText = @"
        UPDATE ModGroups
        SET GroupName = COALESCE(@GroupName, GroupName),
            Description = COALESCE(@Description, Description)
        WHERE GroupID = @GroupID;";

                command.Parameters.AddWithValue("@GroupID", this.GroupID);
                command.Parameters.AddWithValue("@GroupName", this.GroupName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Description", this.Description ?? (object)DBNull.Value);

                command.ExecuteNonQuery();
                App.LogDebug($"ModGroup updated in database: {this.ToString()}");
            }

            // Step 2: Insert or Update the GroupSetGroups entry
            command.CommandText = @"
    INSERT INTO GroupSetGroups (GroupID, GroupSetID, ParentID, Ordinal)
    VALUES (@GroupID, @GroupSetID, @ParentID, @Ordinal)
    ON CONFLICT(GroupID, GroupSetID) DO UPDATE SET
        ParentID = COALESCE(@ParentID, ParentID),
        Ordinal = COALESCE(@Ordinal, Ordinal);";

            command.Parameters.Clear();
            command.Parameters.AddWithValue("@GroupID", this.GroupID);
            command.Parameters.AddWithValue("@GroupSetID", this.GroupSetID ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ParentID", this.ParentID ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Ordinal", this.Ordinal);

            command.ExecuteNonQuery();

            // Step 3: Sync Plugins in GroupSetPlugins Table
            //SyncPluginsWithDatabase(command);

            // Commit the transaction
            transaction.Commit();
            App.LogDebug($"Transaction committed successfully.");

            // Return the updated ModGroup object
            return this;
        }



        /// <summary>
        /// Builds and validates the path to root for the given group and parent.
        /// Ensures the path is valid and does not create circular references.
        /// </summary>
        /// <param name="newParentId">The ID of the new parent group.</param>
        /// <returns>A list representing the path to root.</returns>

        public List<long> BuildPathToRoot()
        {
            // Check if the path is already available in memory
            if (AggLoadInfo.Instance.GroupSetGroups.Items.Any(gsg => gsg.groupID == this.GroupID))
            {
                // Create a path list to store the parent-child relationship
                var pathToRoot = new List<long>();
                long? currentParentID = this.ParentID;

                // Traverse the in-memory GroupSetGroups collection to build the path to root
                while (currentParentID != null)
                {
                    // Find the parent group in the GroupSetGroups collection
                    var parentGroup = AggLoadInfo.Instance.GroupSetGroups.Items
                        .FirstOrDefault(gsg => gsg.groupID == currentParentID && gsg.groupSetID == this.GroupSetID);

                    if (parentGroup != default)
                    {
                        pathToRoot.Add(currentParentID.Value);
                        currentParentID = parentGroup.parentID; // Move to the next parent in the path
                    }
                    else
                    {
                        break; // If no parent is found, exit the loop
                    }
                }

                return pathToRoot;
            }

            // Fallback logic if the GroupID is not found in GroupSetGroups collection
            return new List<long> { this.GroupID ?? 0 };
        }


        private static long? GetParentIDFromDatabase(long groupID)
        {
            // This method should check the database for the parent ID of a given groupId
            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);

            command.CommandText = "SELECT ParentID FROM GroupSetGroups WHERE GroupID = @GroupID";
            command.Parameters.AddWithValue("@GroupID", groupID);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return reader.IsDBNull(reader.GetOrdinal("ParentID")) ? null : reader.GetInt64(reader.GetOrdinal("ParentID"));
            }
            return null;
        }

        private long GetMaxOrdinalForNewGroup(long? parentID)
        {
            // Use the GroupSetID of the current ModGroup, defaulting to 0 if null.
            var currentGroupSetID = this.GroupSetID ?? 0;

            // Try to get the max ordinal from the in-memory instance first
            var siblingGroups = AggLoadInfo.Instance?.Groups
                .Where(g => g.ParentID == parentID && g.GroupSetID == currentGroupSetID)
                .Select(g => g.Ordinal ?? 0)
                .DefaultIfEmpty(-1); // Using -1 to indicate an empty set

            var maxOrdinal = siblingGroups.Max();

            // If the max ordinal is -1, that means no siblings were found in the in-memory structure
            if (maxOrdinal == -1)
            {
                // Fall back to querying the database
                maxOrdinal = GetMaxOrdinalFromDatabase(parentID, currentGroupSetID);
            }

            return maxOrdinal + 1;
        }

        private long GetMaxOrdinalFromDatabase(long? parentID, long groupSetID)
        {
            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);

            command.CommandText = @"
    SELECT COALESCE(MAX(Ordinal), 0)
    FROM GroupSetGroups
    WHERE ParentID = @ParentID AND GroupSetID = @GroupSetID";
            command.Parameters.AddWithValue("@ParentID", (object?)parentID ?? DBNull.Value);
            command.Parameters.AddWithValue("@GroupSetID", groupSetID);

            return Convert.ToInt64(command.ExecuteScalar());
        }

        // Method to add a plugin to the group
        public void AddPluginID(long pluginID)
        {
            if (!_pluginIDs.Contains(pluginID))
            {
                _pluginIDs.Add(pluginID);
                OnPropertyChanged(nameof(PluginIDs));
            }
        }

        // Method to remove a plugin from the group
        public void RemovePluginID(long pluginID)
        {
            if (_pluginIDs.Contains(pluginID))
            {
                _pluginIDs.Remove(pluginID);
                OnPropertyChanged(nameof(PluginIDs));
            }
        }

        // Method to calculate and set path to root using only ParentID references
        public void CalculatePathToRoot(Dictionary<long, ModGroup> groupMap)
        {
            PathToRoot.Clear();
            var currentGroupID = this.GroupID;

            while (currentGroupID.HasValue && currentGroupID.Value != 0 && groupMap.ContainsKey(currentGroupID.Value))
            {
                PathToRoot.Add(currentGroupID.Value);
                currentGroupID = groupMap[currentGroupID.Value].ParentID ?? 0;
            }

            PathToRoot.Reverse(); // Reverse to get path from root to this group
        }

        // Method to get the parent group from the map using ParentID
        public ModGroup GetParentGroup(Dictionary<long, ModGroup> groupMap)
        {
            return ParentID.HasValue && groupMap.ContainsKey(ParentID.Value)
                ? groupMap[ParentID.Value]
                : null;
        }

        // Override ToString for better debugging and logging
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
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // Remove non-alphanumeric characters and trim whitespace
            return Regex.Replace(input, @"[^a-zA-Z0-9]", "").Trim().ToLowerInvariant();
        }

        public string ToPluginsString()
        {
            // If pathToRoot is empty or null, indicate no path is available
            if (PathToRoot == null || PathToRoot.Count == 0)
            {
                return "No path to root available.";
            }

            var sb = new StringBuilder();

            // Iterate through the pathToRoot to build the group hierarchy string
            for (int i = 0; i < PathToRoot.Count; i++)
            {
                var groupID = PathToRoot[i];

                // Find the group in AggLoadInfo or fall back to database lookup
                var group = AggLoadInfo.Instance?.Groups.FirstOrDefault(g => g.GroupID == groupID);

                if (group == null)
                {
                    // If the group is not found in memory, attempt to load it from the database
                    group = LoadModGroup(groupID, this.GroupSetID ?? 0);

                    if (group == null)
                    {
                        // If the group still cannot be found, skip this ID
                        continue;
                    }
                }

                // Calculate the indentation based on depth in hierarchy
                string indent = new string('#', i + 3); // #### for root children, ##### for grandchildren, etc.

                // Build the line with group details
                if (string.IsNullOrEmpty(group.Description))
                {
                    // If no description, just use the group name
                    sb.AppendLine($"{indent} {group.GroupName}");
                }
                else
                {
                    // Include group name and description
                    sb.AppendLine($"{indent} {group.GroupName} @@ {group.Description}");
                }
            }

            // Return the complete string representation of the group hierarchy
            return sb.ToString();
        }

        public static List<ModGroup> LoadModGroupsByGroupSet(long groupSetID)
        {
            var modGroups = new List<ModGroup>();

            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);
            command.CommandText = @"
                SELECT GroupID
                FROM GroupSetGroups
                WHERE GroupSetID = @GroupSetID OR GroupID < 0
                ORDER BY ParentID, Ordinal";
            command.Parameters.AddWithValue("@GroupSetID", groupSetID);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                long groupID = reader.GetInt64(reader.GetOrdinal("GroupID"));
                var modGroup = LoadModGroup(groupID, groupSetID);
                if (modGroup != null)
                {
                    modGroups.Add(modGroup);
                }
            }

            return modGroups;
        }
        private void SyncPluginsWithDatabase(SQLiteCommand command)
        {
            // Step 3: Sync Plugins in GroupSetPlugins Table

            // Step 3.1: Retrieve existing plugins in the database for this group and group set
            var pluginIDsInDb = new HashSet<long>();
            command.CommandText = @"
        SELECT PluginID
        FROM GroupSetPlugins
        WHERE (GroupID = @GroupID AND GroupSetID = @GroupSetID AND PluginID = @PluginID)
              OR (GroupID < 0 AND GroupID != -999 AND PluginID = @PluginID);";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@GroupID", this.GroupID);
            command.Parameters.AddWithValue("@GroupSetID", this.GroupSetID);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    pluginIDsInDb.Add(reader.GetInt64(0)); // Store existing PluginIDs in the database
                }
            }

            // Step 3.2: Add or update plugins in the database based on the local collection
            foreach (var plugin in Plugins)
            {
                if (pluginIDsInDb.Contains(plugin.PluginID))
                {
                    // Plugin already exists in the database, update its ordinal
                    command.CommandText = @"
                UPDATE GroupSetPlugins
                SET Ordinal = @Ordinal
                WHERE (GroupID = @GroupID AND GroupSetID = @GroupSetID AND PluginID = @PluginID)
                    OR (GroupID < 0 AND GroupID != -999 AND PluginID = @PluginID);";
                }
                else
                {
                    // Plugin does not exist, insert it
                    command.CommandText = @"
                INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal)
                VALUES (@GroupSetID, @GroupID, @PluginID, @Ordinal);";
                }

                command.Parameters.Clear();
                command.Parameters.AddWithValue("@GroupSetID", this.GroupSetID);
                command.Parameters.AddWithValue("@GroupID", this.GroupID);
                command.Parameters.AddWithValue("@PluginID", plugin.PluginID);
                command.Parameters.AddWithValue("@Ordinal", plugin.GroupOrdinal ?? 0);

                command.ExecuteNonQuery();
            }

            // Step 3.3: Remove any plugins from the database that are no longer in the local collection
            foreach (var pluginID in pluginIDsInDb)
            {
                if (!Plugins.Any(p => p.PluginID == pluginID))
                {
                    command.CommandText = @"
                DELETE FROM GroupSetPlugins
                WHERE GroupSetID = @GroupSetID AND GroupID = @GroupID AND PluginID = @PluginID;";

                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@GroupSetID", this.GroupSetID);
                    command.Parameters.AddWithValue("@GroupID", this.GroupID);
                    command.Parameters.AddWithValue("@PluginID", pluginID);

                    command.ExecuteNonQuery();
                }
            }
        }


        public ModGroup? FindMatchingModGroup()
        {
            // Normalize GroupName and Description by replacing non-alphanumeric characters with spaces
            string normalizedGroupName = NormalizeString(GroupName);
            string normalizedDescription = NormalizeString(Description);

            // Check for reserved words in normalized GroupName or Description
            var matchingGroup = ReservedWords.FirstOrDefault(word => normalizedGroupName.Contains(word, StringComparison.OrdinalIgnoreCase) || normalizedDescription.Contains(word, StringComparison.OrdinalIgnoreCase));

            if (matchingGroup != null)
            {
                // Assuming 'this' is the calling ModGroup object
                long thisGroupSetID = this.GroupSetID ?? 0;

                // If GroupID is less than 0, set GroupSetID to 1
                if (this.GroupID < 0)
                {
                    // Load the ModGroup directly with GroupID and GroupSetID 1
                    return LoadModGroup(this.GroupID ?? 0, 1);
                }

                // Load the GroupSet for the current GroupSetID
                GroupSet? groupSet = GroupSet.LoadGroupSet(thisGroupSetID);
                if (groupSet == null)
                {
                    // Handle the case where the GroupSet is not found
                    return null;
                }

                // Initialize GroupSetViewModel with the loaded GroupSet
                var groupSetViewModel = new GroupSetViewModel(groupSet.GroupSetID);

                // Normalize the GroupName and Description of the calling ModGroup
                normalizedGroupName = normalizedGroupName.Replace(" ", "").ToLowerInvariant();
                normalizedDescription = normalizedDescription.Replace(" ", "").ToLowerInvariant();

                // Method to search for matching ModGroup within a GroupSetViewModel
                ModGroupViewModel? FindMatchingModGroup(GroupSetViewModel viewModel, string name, string description)
                {
                    var matchingModGroup = viewModel.ModGroups.FirstOrDefault(g =>
                        g.GroupID == this.GroupID ||
                        SortingHelper.FuzzyCompareStrings(NormalizeString(g.GroupName).Replace(" ", "").ToLowerInvariant(), name) < 3);

                    return matchingModGroup != null ? new ModGroupViewModel(matchingModGroup) : null;
                }

                // Search for matching ModGroup within the same GroupSetID using fuzzy matching
                var matchingModGroupViewModel = FindMatchingModGroup(groupSetViewModel, normalizedGroupName, normalizedDescription);

                // If no match found in the current GroupSet, search in cached GroupSet 1
                if (matchingModGroupViewModel == null)
                {
                    var cachedGroupSet1 = AggLoadInfo.Instance.GetCachedGroupSet1();
                    if (cachedGroupSet1 != null)
                    {
                        var cachedGroupSetViewModel = new GroupSetViewModel(cachedGroupSet1.GroupSetID);
                        matchingModGroupViewModel = FindMatchingModGroup(cachedGroupSetViewModel, normalizedGroupName, normalizedDescription);
                    }
                }

                // If a matching group is found, return it
                if (matchingModGroupViewModel != null)
                {
                    App.LogDebug($"Returning existing group: GroupID={matchingModGroupViewModel.GroupID}, GroupName={matchingModGroupViewModel.GroupName}, Description={matchingModGroupViewModel.ModGroup.Description}");
                    return matchingModGroupViewModel.ModGroup;
                }

                // If no match is found, you can handle creating a new group or returning null
                App.LogDebug($"No matching group found for GroupName='{normalizedGroupName}', Description='{normalizedDescription}'.");
                return null;
            }

            // If no reserved word is found in the initial check, return null or handle accordingly
            App.LogDebug("No reserved word match found. Returning null.");
            return null;
        }


    }

    public class ModGroupViewModel : INotifyPropertyChanged
        {
            private ModGroup _modGroup;

            public ModGroupViewModel(ModGroup modGroup)
            {
                _modGroup = modGroup ?? throw new ArgumentNullException(nameof(modGroup));
            }

        public long GroupID
        {
            get => _modGroup.GroupID ?? 0;
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

            public string Description
            {
                get => _modGroup.Description;
                set
                {
                    if (_modGroup.Description != value)
                    {
                        _modGroup.Description = value;
                        OnPropertyChanged();
                    }
                }
            }

            public long? ParentID
            {
                get => _modGroup.ParentID;
                set
                {
                    if (_modGroup.ParentID != value)
                    {
                        _modGroup.ParentID = value;
                        OnPropertyChanged();
                    }
                }
            }

            public long? Ordinal
            {
                get => _modGroup.Ordinal;
                set
                {
                    if (_modGroup.Ordinal != value)
                    {
                        _modGroup.Ordinal = value;
                        OnPropertyChanged();
                    }
                }
            }

            public long? GroupSetID
            {
                get => _modGroup.GroupSetID;
                set
                {
                    if (_modGroup.GroupSetID != value)
                    {
                        _modGroup.GroupSetID = value;
                        OnPropertyChanged();
                    }
                }
            }

            // Expose the PluginIDs property as an ObservableCollection for data binding
            public ObservableCollection<long> PluginIDs => new ObservableCollection<long>(_modGroup.PluginIDs);

            // Commands for ViewModel actions
            public ICommand SaveCommand => new RelayCommand<object?>(SaveModGroup);
            public ICommand DeleteCommand => new RelayCommand<object?>(DeleteModGroup);

            private void SaveModGroup(object? parameter)
            {
                _modGroup.WriteGroup();
                OnPropertyChanged(nameof(GroupName));
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(ParentID));
                OnPropertyChanged(nameof(Ordinal));
                OnPropertyChanged(nameof(GroupSetID));
            }

            private void DeleteModGroup(object? parameter)
            {
                // Implement deletion logic as per your requirements
            }

            public ModGroup ModGroup => _modGroup;

            public event PropertyChangedEventHandler? PropertyChanged = delegate { };

            protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

}
