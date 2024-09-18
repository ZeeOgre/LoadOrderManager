using System.Collections.ObjectModel;
using System.Data.SQLite;
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
            return new ModGroup
            {
                GroupID = this.GroupID,
                Ordinal = this.Ordinal,
                Description = this.Description,
                GroupName = this.GroupName,
                ParentID = this.ParentID,
                Plugins = new ObservableCollection<Plugin>(this.Plugins.Select(p => p.Clone())) // Assuming Plugin has a Clone method
            };
        }

        public override string ToString()
        {
            return $"GroupID: {GroupID}, GroupName: {GroupName}, Description: {Description}, Ordinal: {Ordinal}, ParentID: {ParentID}";
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
            // Normalize GroupName and Description by replacing non-alphanumeric characters with spaces
            string normalizedGroupName = NormalizeString(GroupName);
            string normalizedDescription = NormalizeString(Description);

            // Check for reserved words in normalized GroupName or Description
            var matchingGroup = ReservedWords.FirstOrDefault(word => normalizedGroupName.Contains(word, StringComparison.OrdinalIgnoreCase) || normalizedDescription.Contains(word, StringComparison.OrdinalIgnoreCase));
            if (GroupID == -999 || GroupID == -998 || GroupID == -997 || GroupID == 1 || matchingGroup != null)
            {
                // Normalize the GroupName and Description of each ModGroup in the collection
                var matchingModGroup = AggLoadInfo.Instance.Groups
                    .FirstOrDefault(g => g.GroupID == GroupID || (NormalizeString(g.GroupName).Replace(" ", "").Contains(normalizedGroupName, StringComparison.OrdinalIgnoreCase) || NormalizeString(g.Description).Replace(" ", "").Contains(normalizedDescription, StringComparison.OrdinalIgnoreCase)));

                if (matchingModGroup != null)
                {
                    App.LogDebug($"Returning existing group: GroupID={matchingModGroup.GroupID}, GroupName={matchingModGroup.GroupName}, Description={matchingModGroup.Description}");
                    return matchingModGroup;
                }
            }

            using var connection = DbManager.Instance.GetConnection();
            

            using var command = new SQLiteCommand(connection);

            if (this.GroupID == 0)
            {
                // Check if a group with the same name and description already exists
                command.CommandText = "SELECT GroupID FROM ModGroups WHERE GroupName = @GroupName AND Description = @Description";
                command.Parameters.AddWithValue("@GroupName", this.GroupName);
                command.Parameters.AddWithValue("@Description", this.Description);
                var existingGroupID = command.ExecuteScalar();
                if (existingGroupID != null)
                {
                    App.LogDebug($"Group already exists: {this.GroupName} (ID: {existingGroupID})");
                    this.GroupID = Convert.ToInt32(existingGroupID);
                    return AggLoadInfo.Instance.Groups.FirstOrDefault(g => g.GroupID == this.GroupID);
                }

                // Prepare insert command for new group (GroupID is auto-generated)
                command.CommandText = @"
                    INSERT INTO ModGroups (Ordinal, Description, GroupName, ParentID)
                    VALUES (@Ordinal, @Description, @GroupName, @ParentID)
                    RETURNING GroupID;";  // Use RETURNING clause to get the GroupID

                // Add parameters
                command.Parameters.AddWithValue("@Ordinal", this.Ordinal);
                command.Parameters.AddWithValue("@Description", this.Description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@GroupName", this.GroupName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ParentID", this.ParentID ?? (object)DBNull.Value);
               
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
                    WHERE GroupID = @GroupID";

                // Add parameters
                command.Parameters.AddWithValue("@GroupID", this.GroupID);
                command.Parameters.AddWithValue("@Ordinal", this.Ordinal);
                command.Parameters.AddWithValue("@Description", this.Description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@GroupName", this.GroupName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ParentID", this.ParentID ?? (object)DBNull.Value);

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
                return new ModGroup
                {
                    GroupID = reader.GetInt32(reader.GetOrdinal("GroupID")),
                    Ordinal = reader.IsDBNull(reader.GetOrdinal("Ordinal")) ? null : reader.GetInt32(reader.GetOrdinal("Ordinal")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    GroupName = reader.IsDBNull(reader.GetOrdinal("GroupName")) ? null : reader.GetString(reader.GetOrdinal("GroupName")),
                    ParentID = reader.IsDBNull(reader.GetOrdinal("ParentID")) ? null : reader.GetInt32(reader.GetOrdinal("ParentID"))
                };
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

        // Method to load ModGroup by GroupID



    }
}
