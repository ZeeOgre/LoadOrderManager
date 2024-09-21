using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;


namespace ZO.LoadOrderManager
{
    public class ModGroup
    {
        public int GroupID { get; set; }
        public int? Ordinal { get; set; }
        public string? Description { get; set; }
        public string? GroupName { get; set; }
        public int? ParentID { get; set; }
        public int? GroupSetID { get; set; }
        public ObservableCollection<Plugin>? Plugins { get; set; } = new ObservableCollection<Plugin>();

        private static readonly HashSet<string> ReservedWords = new HashSet<string>
            {
                "uncategorized",
                "default",
                "never_load",
                "unassigned",
                "neverload",
                "CoreGameFiles"
            };

        public string DisplayName => $"{GroupName} | {Description}";

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
                Plugins = new ObservableCollection<Plugin>(this.Plugins?.Select(p => p.Clone()) ?? Enumerable.Empty<Plugin>())
            };
            return clonedModGroup;
        }


        public ModGroup Clone(string groupName)
        {
            return new ModGroup
            {
                GroupID = this.GroupID,
                Ordinal = this.Ordinal,
                Description = this.Description,
                GroupName = groupName,
                ParentID = this.ParentID,
                GroupSetID = this.GroupSetID,   
                Plugins = new ObservableCollection<Plugin>(this.Plugins?.Select(p => p.Clone()) ?? Enumerable.Empty<Plugin>())
            };
        }


        public ModGroup Clone(GroupSet groupSet)
        {
            return new ModGroup
            {
                GroupID = this.GroupID,
                Ordinal = this.Ordinal,
                Description = this.Description,
                GroupName = this.GroupName,
                ParentID = this.ParentID,
                GroupSetID = groupSet.GroupSetID,
                Plugins = new ObservableCollection<Plugin>(this.Plugins?.Select(p => p.Clone()) ?? Enumerable.Empty<Plugin>())
            };
        }


        public override string ToString()
        {
            return $"GroupID: {GroupID}, GroupName: {GroupName}, Description: {Description}, Ordinal: {Ordinal}, ParentID: {ParentID}, GroupSet: {GroupSetID}";
        }

        public string ToPluginsString()
        {
            int level = 0;
            var currentGroup = this;

            // Traverse up the hierarchy to determine the level
            while (currentGroup.ParentID != null && currentGroup.ParentID >= 1)
            {
                level++;
                currentGroup = ModGroup.LoadModGroup(currentGroup.ParentID.Value);
                if (currentGroup == null)
                {
                    break;
                }
            }

            // Create the appropriate number of # based on the level
            string hashMarks = new string('#', level + 2); // +2 to account for the base level

            return $"{hashMarks} {GroupName} @@ {Description}";
        }

        public string ToYAMLObject()
        {
            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(new
            {
                GroupID,
                GroupName,
                Description,
                Ordinal,
                ParentID
            });

            return yaml;
        }
        


        public ModGroup WriteGroup()
        {
            var existingMatch = this.FindMatchingModGroup();
            if (existingMatch != null)
            {
                return existingMatch;
            }


            using var connection = DbManager.Instance.GetConnection();
            

            using var command = new SQLiteCommand(connection);

            if (this.GroupID == 0)
            {
                // Check if a group with the same name and description already exists in the GroupSet
                command.CommandText = "SELECT GroupID FROM ModGroups WHERE GroupName = @GroupName AND Description = @Description AND GroupSetID = @GroupSetID";
                command.Parameters.AddWithValue("@GroupName", this.GroupName);
                command.Parameters.AddWithValue("@Description", this.Description);
                command.Parameters.AddWithValue("@GroupSetID", this.GroupSetID);
                var existingGroupID = command.ExecuteScalar();
                if (existingGroupID != null)
                {
                    App.LogDebug($"Group already exists: {this.GroupName} (ID: {existingGroupID}) (GroupSet: {GroupSetID}");
                    this.GroupID = Convert.ToInt32(existingGroupID);
                    return AggLoadInfo.Instance.Groups.FirstOrDefault(g => g.GroupID == this.GroupID);
                }

                // Prepare insert command for new group (GroupID is auto-generated)
                command.CommandText = @"
                    INSERT INTO ModGroups (Ordinal, Description, GroupName, ParentID, GroupSetID)
                    VALUES (@Ordinal, @Description, @GroupName, @ParentID, @GroupSetID)
                    RETURNING GroupID;";  // Use RETURNING clause to get the GroupID

                // Add parameters
                command.Parameters.AddWithValue("@Ordinal", this.Ordinal);
                command.Parameters.AddWithValue("@Description", this.Description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@GroupName", this.GroupName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ParentID", this.ParentID ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@GroupSetID", this.GroupSetID ?? (object)DBNull.Value);

                try
                {
                    // Execute command and retrieve the newly inserted GroupID
                    this.GroupID = Convert.ToInt32(command.ExecuteScalar());
                    App.LogDebug($"New ModGroup inserted into database: GroupID={this.GroupID}, GroupName={this.GroupName}, Description={this.Description}");
                    AggLoadInfo.Instance.Groups.Add(this);
                }
                catch (SQLiteException ex)
                {
                    App.LogDebug($"SQLiteException: {ex.Message}, ResultCode: {ex.ResultCode}");
                    if (ex.ResultCode == SQLiteErrorCode.Constraint)
                    {
                        App.LogDebug($"Constraint violation: {ex.Message}");
                        throw new InvalidOperationException("A group with the same name and description already exists.", ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else
            {
                // Prepare update command for existing group
                command.CommandText = @"
                    UPDATE ModGroups
                    SET Ordinal = @Ordinal, Description = COALESCE(@Description, ''), GroupName = COALESCE(@GroupName, ''), ParentID = @ParentID
                    WHERE GroupID = @GroupID AND GroupSetID =  @GroupSetID";

                // Add parameters
                command.Parameters.AddWithValue("@GroupID", this.GroupID);
                command.Parameters.AddWithValue("@Ordinal", this.Ordinal);
                command.Parameters.AddWithValue("@Description", this.Description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@GroupName", this.GroupName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ParentID", this.ParentID ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@GroupSetID", this.GroupSetID ?? (object)DBNull.Value);

                try
                {
                    // Execute update command
                    command.ExecuteNonQuery();
                    App.LogDebug($"ModGroup updated in database: {this.ToString()}");
                    var updatedGroup = LoadModGroup(this.GroupID);
                    if (updatedGroup != null)
                    {
                        // ...

                        var index = AggLoadInfo.Instance.Groups.ToList().FindIndex(g => g.GroupID == this.GroupID);
                        if (index >= 0)
                        {
                            AggLoadInfo.Instance.Groups[index] = updatedGroup;
                        }
                        else
                        {
                            AggLoadInfo.Instance.Groups.Add(updatedGroup);
                        }
                    }

                    return updatedGroup ?? this;
                }
                catch (SQLiteException ex)
                {
                    App.LogDebug($"SQLiteException: {ex.Message}, ResultCode: {ex.ResultCode}");
                    if (ex.ResultCode == SQLiteErrorCode.Constraint)
                    {
                        App.LogDebug($"Constraint violation: {ex.Message}");
                        throw new InvalidOperationException("A group with the same name and description already exists.", ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return this;
        }

        private string NormalizeString(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // Remove non-alphanumeric characters and trim whitespace
            return Regex.Replace(input, @"[^a-zA-Z0-9]", "").Trim();
        }

        // Constructor with parent group, description, and group name
        public ModGroup(ModGroup parentGroup, string description, string groupName)
        {
            ParentID = parentGroup.GroupID;
            Description = description;
            GroupName = groupName;
            Ordinal = null; // Ordinal is set to null initially
        }

        // Parameterless constructor
        public ModGroup()
        {
            // Default values can be set here if needed
        }

        // Method to load ModGroup by GroupID
        public static ModGroup? LoadModGroup(int groupId)
        {
            // Retrieve the AggLoadInfo singleton instance
            var aggLoadInfoInstance = ZO.LoadOrderManager.AggLoadInfo.Instance;

            // Search for the group in the Groups collection
            var modGroup = aggLoadInfoInstance.Groups.FirstOrDefault(g => g.GroupID == groupId);
            if (modGroup != null)
            {
                return modGroup;
            }

            // If not found in AggLoadInfo, query the database
            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);
            command.CommandText = "SELECT * FROM ModGroups WHERE GroupID = @GroupID";
            command.Parameters.AddWithValue("@GroupID", groupId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var newModGroup = new ModGroup
                {
                    GroupID = reader.GetInt32(reader.GetOrdinal("GroupID")),
                    Ordinal = reader.IsDBNull(reader.GetOrdinal("Ordinal")) ? null : reader.GetInt32(reader.GetOrdinal("Ordinal")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    GroupName = reader.IsDBNull(reader.GetOrdinal("GroupName")) ? null : reader.GetString(reader.GetOrdinal("GroupName")),
                    ParentID = reader.IsDBNull(reader.GetOrdinal("ParentID")) ? null : reader.GetInt32(reader.GetOrdinal("ParentID")),
                    Plugins = new ObservableCollection<Plugin>()
                };

                // Load plugins associated with this ModGroup
                command.CommandText = "SELECT * FROM Plugins WHERE GroupID = @GroupID";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@GroupID", groupId);

                using var pluginReader = command.ExecuteReader();
                while (pluginReader.Read())
                {
                    var plugin = new Plugin
                    {
                        PluginID = pluginReader.GetInt32(pluginReader.GetOrdinal("PluginID")),
                        PluginName = pluginReader.GetString(pluginReader.GetOrdinal("PluginName")),
                        Description = pluginReader.GetString(pluginReader.GetOrdinal("Description")),
                        Achievements = pluginReader.GetBoolean(pluginReader.GetOrdinal("Achievements")),
                        DTStamp = pluginReader.GetString(pluginReader.GetOrdinal("DTStamp")),
                        Version = pluginReader.GetString(pluginReader.GetOrdinal("Version")),
                        BethesdaID = pluginReader.GetString(pluginReader.GetOrdinal("BethesdaID")),
                        NexusID = pluginReader.GetString(pluginReader.GetOrdinal("NexusID")),
                        GroupID = pluginReader.GetInt32(pluginReader.GetOrdinal("GroupID")),
                        GroupOrdinal = pluginReader.GetInt32(pluginReader.GetOrdinal("GroupOrdinal")),
                        Files = new FileInfo().LoadFilesByPlugin(pluginReader.GetInt32(pluginReader.GetOrdinal("PluginID")))
                    };

                    newModGroup.Plugins.Add(plugin);
                }

                return newModGroup;
            }
            return null;
        }

        public void ChangeGroup(int newParentId)
        {
            // Disallow reserved groups as parents
            if (newParentId < 0)
            {
                throw new InvalidOperationException("Cannot change to a reserved group as parent.");
            }

            // Retrieve the AggLoadInfo singleton instance
            var aggLoadInfoInstance = AggLoadInfo.Instance;

            // Step 1: Find all siblings with an Ordinal greater than the current Ordinal and decrement their Ordinal
            var currentParentGroup = aggLoadInfoInstance.Groups.FirstOrDefault(g => g.GroupID == this.ParentID);
            if (currentParentGroup != null)
            {
                var siblings = currentParentGroup.Plugins?.Where(p => p.GroupOrdinal > this.Ordinal).ToList();
                if (siblings != null)
                {
                    foreach (var sibling in siblings)
                    {
                        sibling.GroupOrdinal--;
                    }
                }
            }

            // Step 2: Find the maximum Ordinal value among the new parent's children
            int maxOrdinal = 0;
            var targetGroup = aggLoadInfoInstance.Groups.FirstOrDefault(g => g.GroupID == newParentId);
            if (targetGroup != null)
            {
                maxOrdinal = targetGroup.Plugins?.Max(p => p.GroupOrdinal) ?? 0;
            }

            // Step 3: Update the ParentID and set the Ordinal to the maximum Ordinal value plus one
            this.ParentID = newParentId;
            this.Ordinal = maxOrdinal + 1;
        }

        public static List<ModGroup> LoadModGroupsByGroupSet(int groupSetID)
        {
            var modGroups = new List<ModGroup>();

            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);
            command.CommandText = "SELECT * FROM ModGroups WHERE GroupSetID = @GroupSetID";
            command.Parameters.AddWithValue("@GroupSetID", groupSetID);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var modGroup = new ModGroup
                {
                    GroupID = reader.GetInt32(reader.GetOrdinal("GroupID")),
                    Ordinal = reader.IsDBNull(reader.GetOrdinal("Ordinal")) ? null : reader.GetInt32(reader.GetOrdinal("Ordinal")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    GroupName = reader.IsDBNull(reader.GetOrdinal("GroupName")) ? null : reader.GetString(reader.GetOrdinal("GroupName")),
                    ParentID = reader.IsDBNull(reader.GetOrdinal("ParentID")) ? null : reader.GetInt32(reader.GetOrdinal("ParentID")),
                    Plugins = new ObservableCollection<Plugin>()
                };

                // Load plugins associated with this ModGroup
                command.CommandText = "SELECT * FROM Plugins WHERE GroupID = @GroupID";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@GroupID", modGroup.GroupID);

                using var pluginReader = command.ExecuteReader();
                while (pluginReader.Read())
                {
                    var plugin = new Plugin
                    {
                        PluginID = pluginReader.GetInt32(pluginReader.GetOrdinal("PluginID")),
                        PluginName = pluginReader.GetString(pluginReader.GetOrdinal("PluginName")),
                        Description = pluginReader.GetString(pluginReader.GetOrdinal("Description")),
                        Achievements = pluginReader.GetBoolean(pluginReader.GetOrdinal("Achievements")),
                        DTStamp = pluginReader.GetString(pluginReader.GetOrdinal("DTStamp")),
                        Version = pluginReader.GetString(pluginReader.GetOrdinal("Version")),
                        BethesdaID = pluginReader.GetString(pluginReader.GetOrdinal("BethesdaID")),
                        NexusID = pluginReader.GetString(pluginReader.GetOrdinal("NexusID")),
                        GroupID = pluginReader.GetInt32(pluginReader.GetOrdinal("GroupID")),
                        GroupOrdinal = pluginReader.GetInt32(pluginReader.GetOrdinal("GroupOrdinal")),
                        Files = new FileInfo().LoadFilesByPlugin(pluginReader.GetInt32(pluginReader.GetOrdinal("PluginID")))
                    };

                    modGroup.Plugins.Add(plugin);
                }

                modGroups.Add(modGroup);
            }

            return modGroups;
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
                int thisGroupSetID = this.GroupSetID ?? 0;

                // Load the GroupSet for the current GroupSetID
                GroupSet? groupSet = GroupSet.LoadGroupSet(thisGroupSetID);
                if (groupSet == null)
                {
                    // Handle the case where the GroupSet is not found
                    return null;
                }

                // Initialize GroupSetViewModel with the loaded GroupSet
                var groupSetViewModel = new GroupSetViewModel(groupSet);

                // Normalize the GroupName and Description of the calling ModGroup
                normalizedGroupName = normalizedGroupName.Replace(" ", "");
                normalizedDescription = normalizedDescription.Replace(" ", "");

                // Search for matching ModGroup within the same GroupSetID
                var matchingModGroupViewModel = groupSetViewModel.ModGroups
                    .FirstOrDefault(g => g.GroupID == this.GroupID ||
                                         NormalizeString(g.GroupName).Replace(" ", "").Contains(normalizedGroupName, StringComparison.OrdinalIgnoreCase) ||
                                         NormalizeString(g.ModGroup.Description).Replace(" ", "").Contains(normalizedDescription, StringComparison.OrdinalIgnoreCase));

                if (matchingModGroupViewModel != null)
                {
                    App.LogDebug($"Returning existing group: GroupID={matchingModGroupViewModel.GroupID}, GroupName={matchingModGroupViewModel.GroupName}, Description={matchingModGroupViewModel.ModGroup.Description}");
                    return matchingModGroupViewModel.ModGroup;
                }
            }

            return null;
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
            get => _modGroup.GroupName ?? string.Empty;
            set
            {
                if (_modGroup.GroupName != value)
                {
                    _modGroup.GroupName = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<Plugin> Plugins => _modGroup.Plugins ?? new ObservableCollection<Plugin>();

        public ModGroup ModGroup => _modGroup;

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
