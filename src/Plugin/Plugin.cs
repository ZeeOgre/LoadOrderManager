using Newtonsoft.Json.Linq;
using System.Data.SQLite;
using System.IO;
using YamlDotNet.Serialization;

namespace ZO.LoadOrderManager
{
    [Flags]
    public enum ModState
    {
        None = 0,
        GameFolder = 1 << 0,
        Bethesda = 1 << 1,
        Nexus = 1 << 2,
        ModManager = 1 << 3
    }

    public class Plugin
    {
        public long PluginID { get; set; }
        public string PluginName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Achievements { get; set; } = false;
        public List<FileInfo> Files { get; set; }
        public string DTStamp { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public ModState State { get; set; } // Add the State property
        public string BethesdaID { get; set; } = string.Empty;
        public string NexusID { get; set; } = string.Empty;
        public long? GroupID { get; set; } = 1; // Default group ID
        public long? GroupOrdinal { get; set; }
        public long? GroupSetID { get; set; }

        public Plugin()
        {
            Files = new List<FileInfo>();
            if (Files.Count == 0)
            {
                Files.Add(new FileInfo(PluginName));
            }
        }
        public override string ToString()
        {
            return $"PluginID: {PluginID}, Name: {PluginName}, Description: {Description}, Achievements: {Achievements}, " +
                   $"DTStamp: {DTStamp}, Version: {Version}, GroupID: {GroupID}, GroupOrdinal: {GroupOrdinal}, " +
                   $"BethesdaID: {BethesdaID}, NexusID: {NexusID} GroupSetID: {GroupSetID}";
        }

        public string ToPluginsString(bool isEnabled)
        {
            return isEnabled ? $"*{PluginName}" : PluginName;
        }

        public string ToCatalogString()
        {
            var jsonObject = new JObject
            {
                ["AchievementSafe"] = Achievements.ToString().ToLower(),
                ["Files"] = new JArray(Files.Select(f => f.ToString())), // Assuming FileInfo has a meaningful ToString()
                ["FilesSize"] = "0", // Placeholder as we don't capture file size
                ["Timestamp"] = new DateTimeOffset(DateTime.Parse(DTStamp)).ToUnixTimeSeconds(),
                ["Title"] = Description,
                ["Version"] = $"{new DateTimeOffset(DateTime.Parse(DTStamp)).ToUnixTimeSeconds()}.{Version}"
            };

            var catalogObject = new JObject
            {
                [BethesdaID] = jsonObject
            };

            return catalogObject.ToString();
        }

        public string ToYAMLObject()
        {
            var serializer = new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .Build();

            return serializer.Serialize(this);
        }
        public void EnsureFilesList()
        {
            if (Files == null)
            {
                Files = new List<FileInfo>();
            }

            // Ensure the PluginName isn't null or empty before adding it to Files
            if (!string.IsNullOrWhiteSpace(PluginName) &&
                !Files.Any(f => f.Filename.Equals(PluginName, StringComparison.OrdinalIgnoreCase)))
            {
                Files.Add(new FileInfo(PluginName));
            }

            // Remove any empty or invalid FileInfo entries
            _ = Files.RemoveAll(f => string.IsNullOrWhiteSpace(f.Filename));

            // Remove duplicate FileInfo entries based on Filename while malongaining order
            var seenFilenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Files = Files.Where(f => seenFilenames.Add(f.Filename)).ToList();
        }



        public static Plugin? LoadPlugin(long? modID = null, string? modName = null, long? groupSetID = null)
        {
            if (modID == null && modName == null)
            {
                throw new ArgumentException("Either modID or modName must be provided.");
            }

            Plugin? plugin = null;

            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);

            // Construct the SQL query based on the presence of groupSetID
            if (groupSetID.HasValue)
            {
                command.CommandText = @"
            SELECT * 
            FROM vwPlugins 
            WHERE (PluginID = @modID OR PluginName = @modName) 
            AND (GroupSetID = @groupSetID OR GroupID < 0)
            LIMIT 1";
                _ = command.Parameters.AddWithValue("@groupSetID", groupSetID.Value);
            }
            else
            {
                command.CommandText = @"
            SELECT * 
            FROM vwPlugins 
            WHERE (PluginID = @modID OR PluginName = @modName)
            LIMIT 1";
            }

            _ = command.Parameters.AddWithValue("@modID", (object?)modID ?? DBNull.Value);
            _ = command.Parameters.AddWithValue("@modName", (object?)modName ?? DBNull.Value);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                // Populate the Plugin object with base details
                plugin = new Plugin
                {
                    PluginID = reader.GetInt64(reader.GetOrdinal("PluginID")),
                    PluginName = reader.GetString(reader.GetOrdinal("PluginName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    Achievements = reader.GetBoolean(reader.GetOrdinal("Achievements")),
                    DTStamp = reader.IsDBNull(reader.GetOrdinal("DTStamp")) ? string.Empty : reader.GetString(reader.GetOrdinal("DTStamp")),
                    Version = reader.IsDBNull(reader.GetOrdinal("Version")) ? string.Empty : reader.GetString(reader.GetOrdinal("Version")),
                    State = reader.IsDBNull(reader.GetOrdinal("State")) ? 0 : (ModState)reader.GetInt64(reader.GetOrdinal("State")),
                    BethesdaID = reader.IsDBNull(reader.GetOrdinal("BethesdaID")) ? null : reader.GetString(reader.GetOrdinal("BethesdaID")),
                    NexusID = reader.IsDBNull(reader.GetOrdinal("NexusID")) ? null : reader.GetString(reader.GetOrdinal("NexusID")),
                    Files = new List<FileInfo>()
                };

                // If groupSetID is provided, populate group-related fields
                if (groupSetID.HasValue)
                {
                    plugin.GroupID = reader.IsDBNull(reader.GetOrdinal("GroupID")) ? null : reader.GetInt64(reader.GetOrdinal("GroupID"));
                    plugin.GroupOrdinal = reader.IsDBNull(reader.GetOrdinal("GroupOrdinal")) ? null : reader.GetInt64(reader.GetOrdinal("GroupOrdinal"));
                    plugin.GroupSetID = reader.IsDBNull(reader.GetOrdinal("GroupSetID")) ? null : reader.GetInt64(reader.GetOrdinal("GroupSetID"));
                }

                // EnsureFilesList will populate the Files property with relevant file details.
                plugin.EnsureFilesList();
            }

            return plugin;
        }



        public Plugin(string bethesdaID, JObject pluginData)
        {
            BethesdaID = bethesdaID;

            if (pluginData != null)
            {
                Achievements = bool.TryParse(pluginData["AchievementSafe"]?.ToString(), out bool achievements) ? achievements : false;
                Description = pluginData["Title"]?.ToString() ?? string.Empty;
                Files = new List<FileInfo>();

                string versionData = pluginData["Version"]?.ToString() ?? string.Empty;
                string[] versionParts = versionData.Split('.');

                if (versionParts.Length >= 2 && long.TryParse(versionParts[0], out long timestamp))
                {
                    DTStamp = DateTimeOffset.FromUnixTimeSeconds(timestamp).ToString("yyyy-MM-dd HH:mm:ss");
                    Version = timestamp > 0 && versionParts[1].All(c => c >= 32 && c <= 126) ? versionParts[1] : string.Empty;
                }
                else
                {
                    DTStamp = DateTimeOffset.FromUnixTimeSeconds(0).ToString("yyyy-MM-dd HH:mm:ss");
                    Version = string.Empty;
                }

                foreach (var file in pluginData["Files"] ?? Enumerable.Empty<JToken>())
                {
                    string filePath = file.ToString();
                    string relativePath = Path.GetDirectoryName(filePath) ?? string.Empty;

                    Files.Add(new FileInfo
                    {
                        Filename = filePath,
                        RelativePath = relativePath,
                        Flags = FileFlags.None,
                        DTStamp = this.DTStamp,
                    });
                }
            }
            else
            {
                throw new ArgumentException($"No data found for BethesdaID: {bethesdaID}");
            }
        }

        public Plugin(System.IO.FileInfo file, bool? fullScan = false)
        {
            var dataPath = Path.Combine(Config.Instance.GameFolder ?? string.Empty, "data");
            var relativePath = Path.GetRelativePath(dataPath, file.FullName);

            PluginName = file.Name.ToLowerInvariant(); // Normalize case before storing
            Description = PluginName;
            string? newHash = null;
            if (fullScan == true) newHash = FileInfo.ComputeHash(file.FullName);
            Files = new List<FileInfo>
            {

                new FileInfo
                {
                    Filename = file.Name,
                    RelativePath = relativePath,
                    DTStamp = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    HASH = newHash,
                    Flags = FileFlags.None
                }
            };
        }

        public Plugin(long pluginID, string pluginName, string description, bool achievements, string dtStamp, string version, string bethesdaID, string nexusID, long groupID, long groupOrdinal, long groupSetID, List<FileInfo> files)
        {
            PluginID = pluginID;
            PluginName = pluginName.ToLowerInvariant(); // Normalize case before storing
            Description = description;
            Achievements = achievements;
            DTStamp = dtStamp;
            Version = version;
            BethesdaID = bethesdaID;
            NexusID = nexusID;
            GroupID = groupID;
            GroupOrdinal = groupOrdinal;
            GroupSetID = groupSetID;
            Files = files ?? new List<FileInfo>();
        }

        public Plugin Clone()
        {
            var clonedPlugin = new Plugin
            {
                PluginID = this.PluginID,
                PluginName = this.PluginName,
                Description = this.Description,
                Achievements = this.Achievements,
                DTStamp = this.DTStamp,
                Version = this.Version,
                BethesdaID = this.BethesdaID,
                NexusID = this.NexusID,
                GroupID = this.GroupID,
                GroupOrdinal = this.GroupOrdinal,
                GroupSetID = this.GroupSetID,
                Files = this.Files.Select(file => new FileInfo
                {
                    FileID = file.FileID,
                    Filename = file.Filename,
                    RelativePath = file.RelativePath,
                    DTStamp = file.DTStamp,
                    HASH = file.HASH,
                    Flags = file.Flags
                }).ToList()
            };

            return clonedPlugin;
        }

        public Plugin WriteMod()
        {
            //App.LogDebug($"Plugin.WriteMod: Writing plugin: {PluginName}");
            EnsureFilesList(); // Ensure the Files list is not empty before beginning transaction

            PluginName = PluginName.ToLowerInvariant(); // Normalize case before inserting

            using var connection = DbManager.Instance.GetConnection();
            //App.LogDebug($"WriteMod Begin Transaction");
            using var transaction = connection.BeginTransaction();

            try
            {
                // Special handling for GroupID < 0
                if (this.GroupID < 0 && GroupID != -997)
                {
                    this.GroupSetID = 1;
                }

                using var command = new SQLiteCommand(connection);
                // Check if the plugin already exists in vwPlugins
                command.CommandText = @"
                        SELECT DISTINCT PluginID, BethesdaID, NexusID, State
                        FROM vwPlugins
                        WHERE LOWER(PluginName) = @PluginName";
                _ = command.Parameters.AddWithValue("@PluginName", this.PluginName);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        this.PluginID = reader.GetInt64(0);

                        // Use fetched values only if the class properties are null or default
                        this.BethesdaID = !string.IsNullOrEmpty(this.BethesdaID) ? this.BethesdaID : (reader.IsDBNull(1) ? string.Empty : reader.GetString(1));
                        this.NexusID = !string.IsNullOrEmpty(this.NexusID) ? this.NexusID : (reader.IsDBNull(2) ? string.Empty : reader.GetString(2));

                        if (!reader.IsDBNull(3))
                        {
                            ModState existingState = (ModState)reader.GetInt64(3);
                            ModState newState = this.State | existingState; // Merge the current state with the new state using bitwise OR

                            // Validate the new state to ensure no conflicting conditions
                            if (newState.HasFlag(ModState.None) && newState != ModState.None)
                            {
                                newState = ModState.None; // If None is set, ensure no other flags are set
                            }

                            this.State = newState;
                        }
                        // Retrieve the existing State value from the database and merge with the new value
                        if (!reader.IsDBNull(3))
                        {
                            ModState existingState = (ModState)reader.GetInt64(3);
                            this.State |= existingState; // Merge the current state with the new state using bitwise OR
                        }

                        App.LogDebug($"Plugin already exists: {this.PluginName} (ID: {this.PluginID})");
                    }
                }

                // Insert or update the Plugin based on existence
                if (this.PluginID == 0)
                {
                    // Insert new plugin into Plugins table
                    command.CommandText = @"
                            INSERT INTO Plugins (PluginName, Description, Achievements, DTStamp, Version, State)
                            VALUES (LOWER(@PluginName), @Description, @Achievements, @DTStamp, @Version, @State)
                            RETURNING PluginID;";
                    command.Parameters.Clear();

                    _ = command.Parameters.AddWithValue("@PluginName", this.PluginName);
                    _ = command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(this.Description) ? DBNull.Value : this.Description);
                    _ = command.Parameters.AddWithValue("@Achievements", this.Achievements);
                    _ = command.Parameters.AddWithValue("@DTStamp", string.IsNullOrEmpty(this.DTStamp) ? DBNull.Value : this.DTStamp);
                    _ = command.Parameters.AddWithValue("@Version", string.IsNullOrEmpty(this.Version) ? DBNull.Value : this.Version);
                    _ = command.Parameters.AddWithValue("@State", (long)this.State);
                    this.PluginID = Convert.ToInt64(command.ExecuteScalar());
                }
                else
                {
                    // Update existing plugin in Plugins table
                    command.CommandText = @"
                            UPDATE Plugins
                            SET Description = COALESCE(@Description, Description), 
                                Achievements = @Achievements, 
                                DTStamp = COALESCE(@DTStamp, DTStamp), 
                                Version = COALESCE(@Version, Version), 
                                State = @State
                            WHERE PluginID = @PluginID;";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(this.Description) ? DBNull.Value : this.Description);
                    _ = command.Parameters.AddWithValue("@Achievements", this.Achievements);
                    _ = command.Parameters.AddWithValue("@DTStamp", string.IsNullOrEmpty(this.DTStamp) ? DBNull.Value : this.DTStamp);
                    _ = command.Parameters.AddWithValue("@Version", string.IsNullOrEmpty(this.Version) ? DBNull.Value : this.Version);
                    _ = command.Parameters.AddWithValue("@State", (long)this.State);
                    _ = command.Parameters.AddWithValue("@PluginID", this.PluginID);

                    _ = command.ExecuteNonQuery();
                }

                // Calculate GroupOrdinal if not set
                long effectiveGroupSetID = (this.GroupSetID == 0 || this.GroupSetID == null) ? 1 : (long)this.GroupSetID;
                if (!this.GroupOrdinal.HasValue)
                {
                    command.CommandText = @"
                            SELECT COALESCE(MAX(Ordinal), 0) + 1
                            FROM GroupSetPlugins
                            WHERE GroupSetID = @GroupSetID AND GroupID = @GroupID";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@GroupSetID", effectiveGroupSetID);
                    _ = command.Parameters.AddWithValue("@GroupID", this.GroupID ?? (object)DBNull.Value);

                    this.GroupOrdinal = Convert.ToInt64(command.ExecuteScalar());
                }

                command.CommandText = @"
                        INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal)
                        VALUES (@GroupSetID, @GroupID, @PluginID, @Ordinal)
                        ON CONFLICT(GroupSetID, PluginID) DO UPDATE 
                        SET GroupID = EXCLUDED.GroupID, 
                            Ordinal = COALESCE(EXCLUDED.Ordinal, Ordinal);";

                command.Parameters.Clear();
                _ = command.Parameters.AddWithValue("@GroupSetID", effectiveGroupSetID);
                _ = command.Parameters.AddWithValue("@GroupID", this.GroupID ?? (object)DBNull.Value);
                _ = command.Parameters.AddWithValue("@PluginID", this.PluginID);
                _ = command.Parameters.AddWithValue("@Ordinal", this.GroupOrdinal);

                _ = command.ExecuteNonQuery();

                // Insert or update ExternalIDs table
                command.CommandText = @"
                        INSERT INTO ExternalIDs (PluginID, BethesdaID, NexusID)
                        VALUES (@PluginID, @BethesdaID, @NexusID)
                        ON CONFLICT(PluginID) DO UPDATE 
                        SET BethesdaID = CASE WHEN @BethesdaID IS NOT NULL THEN @BethesdaID ELSE BethesdaID END, 
                            NexusID = CASE WHEN @NexusID IS NOT NULL THEN @NexusID ELSE NexusID END";

                command.Parameters.Clear();
                _ = command.Parameters.AddWithValue("@PluginID", this.PluginID);
                _ = command.Parameters.AddWithValue("@BethesdaID", string.IsNullOrEmpty(this.BethesdaID) ? DBNull.Value : this.BethesdaID);
                _ = command.Parameters.AddWithValue("@NexusID", string.IsNullOrEmpty(this.NexusID) ? DBNull.Value : this.NexusID);

                _ = command.ExecuteNonQuery();

                // Insert or update FileInfo table
                foreach (var file in this.Files)
                {
                    command.CommandText = @"
                            INSERT INTO FileInfo (PluginID, Filename, RelativePath, DTStamp, HASH, Flags)
                            VALUES (@PluginID, @Filename, @RelativePath, @DTStamp, @HASH, @Flags)
                            ON CONFLICT(Filename) DO UPDATE 
                            SET RelativePath = COALESCE(excluded.RelativePath, FileInfo.RelativePath), 
                                DTStamp = COALESCE(excluded.DTStamp, FileInfo.DTStamp), 
                                HASH = COALESCE(excluded.HASH, FileInfo.HASH), 
                                Flags = excluded.Flags;";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@PluginID", this.PluginID);
                    _ = command.Parameters.AddWithValue("@Filename", string.IsNullOrEmpty(file.Filename) ? DBNull.Value : file.Filename);
                    _ = command.Parameters.AddWithValue("@RelativePath", string.IsNullOrEmpty(file.RelativePath) ? DBNull.Value : file.RelativePath);
                    _ = command.Parameters.AddWithValue("@DTStamp", string.IsNullOrEmpty(file.DTStamp) ? DBNull.Value : file.DTStamp);
                    _ = command.Parameters.AddWithValue("@HASH", string.IsNullOrEmpty(file.HASH) ? DBNull.Value : file.HASH);
                    _ = command.Parameters.AddWithValue("@Flags", file.Flags);

                    _ = command.ExecuteNonQuery();
                }

                transaction.Commit(); // Commit the transaction after all operations

            }
            catch (Exception ex)
            {
                App.LogDebug($"Plugin.WriteMod: Error writing plugin: {ex.Message}");
                transaction.Rollback(); // Rollback the transaction in case of an error
                throw;
            }

            return this;
        }

        public void ChangeGroup(long newGroupId, long? newGroupSetId = null)
        {
            // If groupSetID is null, use the ActiveGroupSet.GroupSetID from the singleton
            var activeGroupSetID = newGroupSetId ?? AggLoadInfo.Instance.ActiveGroupSet.GroupSetID;

            //// Adjust ordinals in the in-memory collection first
            //var groupPlugins = AggLoadInfo.Instance.GroupSetPlugins.Items
            //    .Where(gsp => gsp.groupID == this.GroupID && gsp.groupSetID == activeGroupSetID && gsp.Ordinal > this.GroupOrdinal)
            //    .OrderBy(gsp => gsp.Ordinal)
            //    .ToList();

            //// Update the in-memory ordinals by shifting them down by 1
            //foreach (var plugin in groupPlugins)
            //{
            //    var updatedPlugin = (plugin.groupSetID, plugin.groupID, plugin.pluginID, plugin.Ordinal - 1);

            //    // Remove the old entry and add the updated one
            //    AggLoadInfo.Instance.GroupSetPlugins.Items.Remove(plugin);
            //    AggLoadInfo.Instance.GroupSetPlugins.Items.Add(updatedPlugin);
            //}

            using var connection = DbManager.Instance.GetConnection();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Combine the move and sibling ordinal adjustment into one SQL query
                using var updateCommand = new SQLiteCommand(connection)
                {
                    CommandText = @"
                -- Move the plugin to the new group and calculate the new ordinal
                UPDATE GroupSetPlugins
                SET GroupID = @NewGroupID, GroupSetID = @NewGroupSetID, Ordinal = (
                    SELECT COALESCE(MAX(Ordinal), 0) + 1
                    FROM GroupSetPlugins
                    WHERE GroupID = @NewGroupID AND GroupSetID = @NewGroupSetID
                )
                WHERE PluginID = @PluginID AND GroupID = @OldGroupID AND GroupSetID = @OldGroupSetID;

                -- Decrement the ordinals of the old siblings after moving the plugin
                UPDATE GroupSetPlugins
                SET Ordinal = Ordinal - 1
                WHERE GroupID = @OldGroupID AND GroupSetID = @OldGroupSetID AND Ordinal > @OldOrdinal;"
                };

                // Add the shared parameters for both queries
                updateCommand.Parameters.AddWithValue("@NewGroupID", newGroupId);
                updateCommand.Parameters.AddWithValue("@NewGroupSetID", activeGroupSetID);
                updateCommand.Parameters.AddWithValue("@PluginID", this.PluginID);
                updateCommand.Parameters.AddWithValue("@OldGroupID", this.GroupID);
                updateCommand.Parameters.AddWithValue("@OldGroupSetID", this.GroupSetID);
                updateCommand.Parameters.AddWithValue("@OldOrdinal", this.GroupOrdinal);

                // Execute the combined queries
                updateCommand.ExecuteNonQuery();

                // Commit the transaction
                transaction.Commit();
            }
            catch (Exception ex)
            {
                // Rollback in case of any errors
                transaction.Rollback();
#if WINDOWS
                App.LogDebug($"Plugin.ChangeGroup: Error changing group: {ex.Message}");
#endif
                throw;
            }

            // Update the in-memory object
            this.GroupID = newGroupId;
            this.GroupSetID = newGroupSetId;
            this.GroupOrdinal = null; // The new ordinal will be set by the database

            AggLoadInfo.Instance.RefreshMetadataFromDB();

        }


        public void AddModToGroup(ModGroup newGroup, AggLoadInfo? aggLoadInfo = null)
        {
            aggLoadInfo ??= AggLoadInfo.Instance;


            using var connection = DbManager.Instance.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Insert the plugin into the new group with the ordinal set to max + 1
                using var insertCommand = new SQLiteCommand(connection)
                {
                    CommandText = @"
                INSERT INTO GroupSetPlugins (GroupID, GroupSetID, PluginID, Ordinal)
                VALUES (@NewGroupID, @NewGroupSetID, @PluginID, (
                    SELECT COALESCE(MAX(Ordinal), 0) + 1
                    FROM GroupSetPlugins
                    WHERE GroupID = @NewGroupID AND GroupSetID = @NewGroupSetID
                ))
                RETURNING Ordinal;"
                };

                insertCommand.Parameters.AddWithValue("@NewGroupID", newGroup.GroupID);
                insertCommand.Parameters.AddWithValue("@NewGroupSetID", aggLoadInfo.ActiveGroupSet.GroupSetID);
                insertCommand.Parameters.AddWithValue("@PluginID", this.PluginID);

                this.GroupOrdinal = (long)insertCommand.ExecuteScalar();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
#if WINDOWS
                App.LogDebug($"Plugin.AddModToGroup: Error adding mod to group: {ex.Message}");
#endif
                throw;
            }
            AggLoadInfo.Instance.RefreshMetadataFromDB();


            // Update the in-memory object
            this.GroupID = newGroup.GroupID;
            this.GroupSetID = aggLoadInfo.ActiveGroupSet.GroupSetID;

            aggLoadInfo.Groups.FirstOrDefault(g => g.GroupID == newGroup.GroupID)?.Plugins.Add(this);
            newGroup.Plugins.Add(this);

        }

        public void RemoveModFromGroup(ModGroup oldGroup, AggLoadInfo? aggLoadInfo = null)
        {
            aggLoadInfo ??= AggLoadInfo.Instance;

            using var connection = DbManager.Instance.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Remove the plugin from the group
                using var deleteCommand = new SQLiteCommand(connection)
                {
                    CommandText = @"
                DELETE FROM GroupSetPlugins
                WHERE PluginID = @PluginID AND GroupID = @OldGroupID AND GroupSetID = @OldGroupSetID;

                -- Decrement the ordinals of the old siblings after removing the plugin
                UPDATE GroupSetPlugins
                SET Ordinal = Ordinal - 1
                WHERE GroupID = @OldGroupID AND GroupSetID = @OldGroupSetID AND Ordinal > @OldOrdinal;"
                };

                deleteCommand.Parameters.AddWithValue("@PluginID", this.PluginID);
                deleteCommand.Parameters.AddWithValue("@OldGroupID", oldGroup.GroupID);
                deleteCommand.Parameters.AddWithValue("@OldGroupSetID", aggLoadInfo.ActiveGroupSet.GroupSetID);
                deleteCommand.Parameters.AddWithValue("@OldOrdinal", this.GroupOrdinal);

                deleteCommand.ExecuteNonQuery();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
#if WINDOWS
                App.LogDebug($"Plugin.RemoveModFromGroup: Error removing mod from group: {ex.Message}");
#endif
                throw;
            }

            aggLoadInfo.RefreshMetadataFromDB();

            // Update the in-memory object
            this.GroupID = null;
            this.GroupSetID = null;
            this.GroupOrdinal = null;
            aggLoadInfo.Groups.FirstOrDefault(g => g.GroupID == oldGroup.GroupID)?.Plugins.Remove(this);
            oldGroup.Plugins.Remove(this);

        }


        public void SwapLocations(Plugin other)
        {
            // Ensure that GroupSetID, GroupID, and GroupOrdinal are not null
            if (this.GroupSetID == null || this.GroupID == null || this.GroupOrdinal == null ||
                other.GroupSetID == null || other.GroupID == null || other.GroupOrdinal == null)
            {
                throw new InvalidOperationException("GroupSetID, GroupID, and GroupOrdinal must not be null for either plugin.");
            }

            // Capture the current tuples before swapping
            _ = ((long)this.GroupSetID, (long)this.GroupID, this.PluginID, (long)this.GroupOrdinal);
            _ = ((long)other.GroupSetID, (long)other.GroupID, other.PluginID, (long)other.GroupOrdinal);

            // Swap the locations
            var tempGroupID = this.GroupID;
            var tempGroupSetID = this.GroupSetID;
            var tempGroupOrdinal = this.GroupOrdinal;

            this.GroupID = other.GroupID;
            this.GroupSetID = other.GroupSetID;
            this.GroupOrdinal = other.GroupOrdinal;

            other.GroupID = tempGroupID;
            other.GroupSetID = tempGroupSetID;
            other.GroupOrdinal = tempGroupOrdinal;


            // Write the changes to the database
            _ = this.WriteMod();
            _ = other.WriteMod();
            AggLoadInfo.Instance.RefreshMetadataFromDB();
        }

        public override bool Equals(object obj)
        {
            if (obj is Plugin otherPlugin)
            {
                // Compare Plugin properties here, e.g., PluginID, PluginName, etc.
                return this.PluginID == otherPlugin.PluginID && this.PluginName == otherPlugin.PluginName;
            }
            else if (obj is PluginViewModel otherViewModel)
            {
                return this.Equals(otherViewModel.Plugin);
            }
            return false;
        }


        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + PluginID.GetHashCode();
                hash = hash * 23 + PluginName.GetHashCode();
                hash = hash * 23 + (BethesdaID?.GetHashCode() ?? 0);
                hash = hash * 23 + (NexusID?.GetHashCode() ?? 0);
                return hash;
            }
        }

    }



}
