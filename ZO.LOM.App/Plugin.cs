using Newtonsoft.Json.Linq;
using System.Data.SQLite;
using System.IO;
using YamlDotNet.Serialization;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

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
            Files.RemoveAll(f => string.IsNullOrWhiteSpace(f.Filename));

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
                command.Parameters.AddWithValue("@groupSetID", groupSetID.Value);
            }
            else
            {
                command.CommandText = @"
            SELECT * 
            FROM vwPlugins 
            WHERE (PluginID = @modID OR PluginName = @modName)
            LIMIT 1";
            }

            command.Parameters.AddWithValue("@modID", (object?)modID ?? DBNull.Value);
            command.Parameters.AddWithValue("@modName", (object?)modName ?? DBNull.Value);

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
                    DTStamp = reader.GetString(reader.GetOrdinal("DTStamp")),
                    Version = reader.GetString(reader.GetOrdinal("Version")),
                    State = reader.IsDBNull(reader.GetOrdinal("State")) ? 0 : (ModState)reader.GetInt64(reader.GetOrdinal("State")),
                    BethesdaID = reader.IsDBNull(reader.GetOrdinal("BethesdaID")) ? null : reader.GetString(reader.GetOrdinal("BethesdaID")),
                    NexusID = reader.IsDBNull(reader.GetOrdinal("NexusID")) ? null : reader.GetString(reader.GetOrdinal("NexusID")),
                    Files = new List<FileInfo>()
                };

                // If groupSetID is provided, populate group-related fields
                if (groupSetID.HasValue)
                {
                    plugin.GroupID = reader.IsDBNull(reader.GetOrdinal("GroupID")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("GroupID"));
                    plugin.GroupOrdinal = reader.IsDBNull(reader.GetOrdinal("GroupOrdinal")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("GroupOrdinal"));
                    plugin.GroupSetID = reader.IsDBNull(reader.GetOrdinal("GroupSetID")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("GroupSetID"));
                }

                // EnsureFilesList will populate the Files property with relevant file details.
                plugin.EnsureFilesList();
            }

            return plugin;
        }

        //public Plugin(string fileName, long groupId, long ordinal)
        //{
        //    PluginName = fileName.ToLowerInvariant(); // Normalize case before storing
        //    Description = PluginName;
        //    GroupID = groupId;
        //    GroupOrdinal = ordinal;
        //    GroupSetID = 1; // Default to GroupSetID 1  
        //    Files = new List<FileInfo>
        //    {
        //        new FileInfo
        //        {
        //            Filename = fileName,
        //            DTStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        //            IsArchive = false
        //        }
        //    };
        //}

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
                        IsArchive = false,
                        DTStamp = this.DTStamp,
                    });
                }
            }
            else
            {
                throw new ArgumentException($"No data found for BethesdaID: {bethesdaID}");
            }
        }

        public Plugin(System.IO.FileInfo file)
        {
            var dataPath = Path.Combine(Config.Instance.GameFolder ?? string.Empty, "data");
            var relativePath = Path.GetRelativePath(dataPath, file.FullName);

            PluginName = file.Name.ToLowerInvariant(); // Normalize case before storing
            Description = PluginName;
            Files = new List<FileInfo>
            {
                new FileInfo
                {
                    Filename = file.Name,
                    RelativePath = relativePath,
                    DTStamp = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    HASH = FileInfo.ComputeHash(file.FullName),
                    IsArchive = false
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
                    IsArchive = file.IsArchive
                }).ToList()
            };

            return clonedPlugin;
        }

        public Plugin WriteMod()
        {
            App.LogDebug($"Plugin.WriteMod: Writing plugin: {PluginName}");
            EnsureFilesList(); // Ensure the Files list is not empty before beginning transaction

            PluginName = PluginName.ToLowerInvariant(); // Normalize case before inserting

            using var connection = DbManager.Instance.GetConnection();
            App.LogDebug($"WriteMod Begin Transaction");
            using var transaction = connection.BeginTransaction();

            try
            {

                // Special handling for GroupID < 0
                if (this.GroupID < 0)
                {
                    this.GroupSetID = 1;
                }

                using (var command = new SQLiteCommand(connection))
                {
                    // Check if the plugin already exists in vwPlugins within the given GroupSet
                    command.CommandText = @"
                SELECT PluginID, GroupID, GroupOrdinal, GroupSetID, BethesdaID, NexusID, State
                FROM vwPlugins
                WHERE LOWER(PluginName) = @PluginName AND GroupSetID = @GroupSetID";
                    command.Parameters.AddWithValue("@PluginName", this.PluginName);
                    command.Parameters.AddWithValue("@GroupSetID", this.GroupSetID ?? (object)DBNull.Value);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            this.PluginID = reader.GetInt64(0);

                            // Use fetched values only if the class properties are null or default
                            this.GroupID = this.GroupID ?? (reader.IsDBNull(1) ? (long?)null : reader.GetInt64(1));
                            this.GroupOrdinal = this.GroupOrdinal ?? (reader.IsDBNull(2) ? (long?)null : (long?)reader.GetInt64(2));
                            this.GroupSetID = this.GroupSetID ?? (reader.IsDBNull(3) ? (long?)null : reader.GetInt64(3));

                            // For string properties, use empty string as fallback
                            this.BethesdaID = !string.IsNullOrEmpty(this.BethesdaID) ? this.BethesdaID : (reader.IsDBNull(4) ? string.Empty : reader.GetString(4));
                            this.NexusID = !string.IsNullOrEmpty(this.NexusID) ? this.NexusID : (reader.IsDBNull(5) ? string.Empty : reader.GetString(5));

                            // Retrieve the existing State value from the database and merge with the new value
                            if (!reader.IsDBNull(6))
                            {
                                ModState existingState = (ModState)reader.GetInt64(6);
                                this.State |= existingState; // Merge the current state with the new state using bitwise OR
                            }

                            App.LogDebug($"Plugin already exists: {this.PluginName} (ID: {this.PluginID}, GroupSetID: {this.GroupSetID})");
                        }
                    }
                    // Insert or update the Plugin based on existence
                    if (this.PluginID == 0 && GroupID != -999)
                    {
                        // Insert new plugin INTO Plugins table
                        command.CommandText = @"
                    INSERT INTO Plugins (PluginName, Description, Achievements, DTStamp, Version, State)
                    VALUES (LOWER(@PluginName), @Description, @Achievements, @DTStamp, @Version, @State)
                    RETURNING PluginID;";
                        command.Parameters.Clear();

                        command.Parameters.AddWithValue("@PluginName", this.PluginName);
                        command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(this.Description) ? (object)DBNull.Value : this.Description);
                        command.Parameters.AddWithValue("@Achievements", this.Achievements);
                        command.Parameters.AddWithValue("@DTStamp", string.IsNullOrEmpty(this.DTStamp) ? (object)DBNull.Value : this.DTStamp);
                        command.Parameters.AddWithValue("@Version", string.IsNullOrEmpty(this.Version) ? (object)DBNull.Value : this.Version);
                        command.Parameters.AddWithValue("@State", (long)this.State);
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
                    WHERE PluginID = @PluginID";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(this.Description) ? (object)DBNull.Value : this.Description);
                        command.Parameters.AddWithValue("@Achievements", this.Achievements);
                        command.Parameters.AddWithValue("@DTStamp", string.IsNullOrEmpty(this.DTStamp) ? (object)DBNull.Value : this.DTStamp);
                        command.Parameters.AddWithValue("@Version", string.IsNullOrEmpty(this.Version) ? (object)DBNull.Value : this.Version);
                        command.Parameters.AddWithValue("@State", (long)this.State);
                        command.Parameters.AddWithValue("@PluginID", this.PluginID);

                        command.ExecuteNonQuery();
                    }
                    // Insert or update ExternalIDs table
                    command.CommandText = @"
                        INSERT INTO ExternalIDs (PluginID, BethesdaID, NexusID)
                        VALUES (@PluginID, @BethesdaID, @NexusID)
                        ON CONFLICT(PluginID) DO UPDATE 
                        SET BethesdaID = CASE WHEN @BethesdaID IS NOT NULL THEN @BethesdaID ELSE BethesdaID END, 
                            NexusID = CASE WHEN @NexusID IS NOT NULL THEN @NexusID ELSE NexusID END";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@PluginID", this.PluginID);
                    command.Parameters.AddWithValue("@BethesdaID", string.IsNullOrEmpty(this.BethesdaID) ? (object)DBNull.Value : this.BethesdaID);
                    command.Parameters.AddWithValue("@NexusID", string.IsNullOrEmpty(this.NexusID) ? (object)DBNull.Value : this.NexusID);

                    command.ExecuteNonQuery();

                    // Insert or update FileInfo table
                    foreach (var file in this.Files)
                    {
                        command.CommandText = @"
                        INSERT INTO FileInfo (PluginID, Filename, RelativePath, DTStamp, HASH, IsArchive)
                        VALUES (@PluginID, @Filename, @RelativePath, @DTStamp, @HASH, @IsArchive)
                        ON CONFLICT(Filename) DO UPDATE 
                        SET RelativePath = COALESCE(excluded.RelativePath, FileInfo.RelativePath), 
                            DTStamp = COALESCE(excluded.DTStamp, FileInfo.DTStamp), 
                            HASH = COALESCE(excluded.HASH, FileInfo.HASH), 
                            IsArchive = excluded.IsArchive";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@PluginID", this.PluginID);
                        command.Parameters.AddWithValue("@Filename", string.IsNullOrEmpty(file.Filename) ? (object)DBNull.Value : file.Filename);
                        command.Parameters.AddWithValue("@RelativePath", string.IsNullOrEmpty(file.RelativePath) ? (object)DBNull.Value : file.RelativePath);
                        command.Parameters.AddWithValue("@DTStamp", string.IsNullOrEmpty(file.DTStamp) ? (object)DBNull.Value : file.DTStamp);
                        command.Parameters.AddWithValue("@HASH", string.IsNullOrEmpty(file.HASH) ? (object)DBNull.Value : file.HASH);
                        command.Parameters.AddWithValue("@IsArchive", file.IsArchive);

                        command.ExecuteNonQuery();
                    }

                    // Insert or update GroupSetPlugins table
                    command.CommandText = @"
                    INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal)
                    VALUES (@GroupSetID, @GroupID, @PluginID, @Ordinal)
                    ON CONFLICT(GroupSetID, GroupID, PluginID) DO UPDATE 
                    SET Ordinal = COALESCE(@Ordinal, Ordinal)";

                    command.Parameters.Clear();

                    // Handle the special case where GroupID is -997
                    long effectiveGroupSetID = (this.GroupID == -997) ? 1 : (long)(this.GroupSetID ?? 1);


                    command.Parameters.AddWithValue("@GroupSetID", effectiveGroupSetID);
                    command.Parameters.AddWithValue("@GroupID", this.GroupID ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PluginID", this.PluginID);
                    command.Parameters.AddWithValue("@Ordinal", this.GroupOrdinal ?? (object)DBNull.Value);

                    command.ExecuteNonQuery();

                    transaction.Commit(); // Commit the transaction after all operations
                }
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
            using (var connection = DbManager.Instance.GetConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Delete the existing entry in GroupSetPlugins where GroupID and GroupSetID match the old values
                        using (var deleteCommand = new SQLiteCommand(connection))
                        {
                            deleteCommand.CommandText = @"
                                DELETE FROM GroupSetPlugins
                                WHERE PluginID = @PluginID AND GroupID = @OldGroupID AND GroupSetID = @OldGroupSetID;";
                            deleteCommand.Parameters.AddWithValue("@PluginID", this.PluginID);
                            deleteCommand.Parameters.AddWithValue("@OldGroupID", this.GroupID ?? (object)DBNull.Value);
                            deleteCommand.Parameters.AddWithValue("@OldGroupSetID", this.GroupSetID ?? (object)DBNull.Value);
                            deleteCommand.ExecuteNonQuery();
                        }

                        // Update the GroupID and GroupSetID properties
                        this.GroupID = newGroupId;
                        this.GroupSetID = newGroupSetId;

                        // Insert the new entry in GroupSetPlugins with calculated Ordinal
                        using (var insertCommand = new SQLiteCommand(connection))
                        {
                            insertCommand.CommandText = @"
                                INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal)
                                VALUES (@GroupSetID, @GroupID, @PluginID, 
                                    (SELECT COALESCE(MAX(Ordinal), 1) + 1 FROM GroupSetPlugins WHERE GroupSetID = @GroupSetID AND GroupID = @GroupID));";
                            insertCommand.Parameters.AddWithValue("@GroupSetID", this.GroupSetID ?? (object)DBNull.Value);
                            insertCommand.Parameters.AddWithValue("@GroupID", this.GroupID ?? (object)DBNull.Value);
                            insertCommand.Parameters.AddWithValue("@PluginID", this.PluginID);
                            insertCommand.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        App.LogDebug($"Plugin.ChangeGroup: Error changing group: {ex.Message}");
                        throw;
                    }
                }
            }
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