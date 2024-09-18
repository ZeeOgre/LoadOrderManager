using Newtonsoft.Json.Linq;
using System.Data.SQLite;
using System.IO;
using YamlDotNet.Serialization;
using System.ComponentModel;

namespace ZO.LoadOrderManager
{
    public class Plugin
    {
        public int PluginID { get; set; }
        public string PluginName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Achievements { get; set; } = false;
        public List<FileInfo> Files { get; set; }
        public string DTStamp { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string BethesdaID { get; set; } = string.Empty;
        public string NexusID { get; set; } = string.Empty;
        public int? GroupID { get; set; } = 1; // Default group ID
        public int? GroupOrdinal { get; set; }

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
                   $"BethesdaID: {BethesdaID}, NexusID: {NexusID}";
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
            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(new
            {
                PluginName,
                Achievements,
                Description,
                DTStamp,
                BethesdaID,
                NexusID,
                Version,
                GroupID,
                GroupOrdinal
            });

            return yaml;
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

            // Remove duplicate FileInfo entries based on Filename while maintaining order
            var seenFilenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Files = Files.Where(f => seenFilenames.Add(f.Filename)).ToList();
        }



        public static Plugin? LoadPlugin(string pluginName)
        {
            pluginName = pluginName.ToLowerInvariant(); // Normalize case before querying

            using var connection = DbManager.Instance.GetConnection();
            
            using var command = new SQLiteCommand("SELECT * FROM Plugins WHERE LOWER(PluginName) = @PluginName", connection);
            command.Parameters.AddWithValue("@PluginName", pluginName);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Plugin
                {
                    PluginID = reader.GetInt32(reader.GetOrdinal("PluginID")),
                    PluginName = reader.GetString(reader.GetOrdinal("PluginName")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    Achievements = reader.GetBoolean(reader.GetOrdinal("Achievements")),
                    DTStamp = reader.GetString(reader.GetOrdinal("DTStamp")),
                    Version = reader.GetString(reader.GetOrdinal("Version")),
                    GroupID = reader.GetInt32(reader.GetOrdinal("GroupID")),
                    GroupOrdinal = reader.GetInt32(reader.GetOrdinal("GroupOrdinal"))
                };
            }
            return null;
        }

        public static Plugin? LoadPlugin(int modID)
        {
            using var connection = DbManager.Instance.GetConnection();

            using var command = new SQLiteCommand("SELECT * FROM vwPlugins WHERE PluginID = @ModID", connection);
            command.Parameters.AddWithValue("@ModID", modID);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var plugin = new Plugin
                {
                    PluginID = reader.GetInt32(reader.GetOrdinal("PluginID")),
                    PluginName = reader.GetString(reader.GetOrdinal("PluginName")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    Achievements = reader.GetInt32(reader.GetOrdinal("Achievements")) == 1,
                    DTStamp = reader.GetString(reader.GetOrdinal("DTStamp")),
                    Version = reader.GetString(reader.GetOrdinal("Version")),
                    BethesdaID = reader.GetString(reader.GetOrdinal("BethesdaID")),
                    NexusID = reader.GetString(reader.GetOrdinal("NexusID")),
                    GroupID = reader.IsDBNull(reader.GetOrdinal("GroupID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("GroupID")),
                    GroupOrdinal = reader.IsDBNull(reader.GetOrdinal("GroupOrdinal")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("GroupOrdinal")),
                    Files = new List<FileInfo>()
                };

                // Load FileInfo records
                using (var fileCommand = new SQLiteCommand("SELECT * FROM vwModFiles WHERE PluginID = @ModID", connection))
                {
                    fileCommand.Parameters.AddWithValue("@ModID", modID);
                    using var fileReader = fileCommand.ExecuteReader();
                    while (fileReader.Read())
                    {
                        plugin.Files.Add(new FileInfo
                        {
                            Filename = fileReader.GetString(fileReader.GetOrdinal("Filename")),
                            RelativePath = fileReader.GetString(fileReader.GetOrdinal("RelativePath")),
                            DTStamp = fileReader.GetString(fileReader.GetOrdinal("DTStamp")),
                            HASH = fileReader.GetString(fileReader.GetOrdinal("HASH")),
                            IsArchive = fileReader.GetInt32(fileReader.GetOrdinal("IsArchive")) == 1
                        });
                    }
                }

                return plugin;
            }
            return null;
        }










        public Plugin(string fileName, int groupId, int ordinal)
        {
            PluginName = fileName.ToLowerInvariant(); // Normalize case before storing
            Description = PluginName;
            GroupID = groupId;
            GroupOrdinal = ordinal;
            Files = new List<FileInfo>
            {
                new FileInfo
                {
                    Filename = fileName,
                    DTStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    IsArchive = false
                }
            };
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

        public Plugin(int pluginID, string pluginName, string description, bool achievements, string dtStamp, string version, string bethesdaID, string nexusID, int groupID, int groupOrdinal, List<FileInfo> files)
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
    


    public void WriteMod()
        {
            App.LogDebug($"Plugin.WriteMod: Writing plugin: {PluginName}");
            EnsureFilesList(); // Ensure the Files list is not empty

            PluginName = PluginName.ToLowerInvariant(); // Normalize case before inserting

            using var connection = DbManager.Instance.GetConnection();
            

            App.LogDebug($"WriteMod Begin Transaction");
            using var transaction = connection.BeginTransaction();

            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    // Clear parameters before setting them for a new command
                    command.Parameters.Clear();

                    if (this.PluginID == 0)
                    {
                        // Check if the plugin already exists and retrieve GroupID
                        command.CommandText = "SELECT PluginID, GroupID FROM Plugins WHERE LOWER(PluginName) = @PluginName";
                        command.Parameters.AddWithValue("@PluginName", this.PluginName.ToLowerInvariant());
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                this.PluginID = reader.GetInt32(0);
                                this.GroupID = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
                                App.LogDebug($"Plugin already exists: {this.PluginName} (ID: {this.PluginID}, GroupID: {this.GroupID})");
                            }
                        }
                    }

                    if (this.PluginID == 0 || (this.GroupID != -998 && this.GroupID != -999))
                    {
                        if (this.PluginID == 0)
                        {
                            // Insert new plugin
                            command.CommandText = @"
                                INSERT INTO Plugins (PluginName, Description, Achievements, DTStamp, Version, GroupID, GroupOrdinal)
                                VALUES (LOWER(@PluginName), @Description, @Achievements, @DTStamp, @Version, @GroupID, @GroupOrdinal)
                                RETURNING PluginID;";
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@PluginName", this.PluginName);
                            command.Parameters.AddWithValue("@Description", this.Description);
                            command.Parameters.AddWithValue("@Achievements", this.Achievements);
                            command.Parameters.AddWithValue("@DTStamp", this.DTStamp);
                            command.Parameters.AddWithValue("@Version", this.Version);
                            command.Parameters.AddWithValue("@GroupID", this.GroupID);
                            command.Parameters.AddWithValue("@GroupOrdinal", this.GroupOrdinal);
                            this.PluginID = Convert.ToInt32(command.ExecuteScalar());
                            App.LogDebug($"Inserting New Plugin: {this.PluginName} (ID: {this.PluginID}, GroupID: {this.GroupID})");
                        }
                        else
                        {
                            // Update existing plugin
                            command.CommandText = @"
                            UPDATE Plugins
                            SET PluginName = LOWER(@PluginName),
                                Description = COALESCE(@Description, Description),
                                Achievements = COALESCE(@Achievements, Achievements),
                                DTStamp = COALESCE(@DTStamp, DTStamp),
                                Version = COALESCE(@Version, Version),
                                GroupID = COALESCE(@GroupID, GroupID),
                                GroupOrdinal = @GroupOrdinal
                            WHERE PluginID = @PluginID";
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@PluginName", this.PluginName);
                            command.Parameters.AddWithValue("@Description", this.Description);
                            command.Parameters.AddWithValue("@Achievements", this.Achievements);
                            command.Parameters.AddWithValue("@DTStamp", this.DTStamp);
                            command.Parameters.AddWithValue("@Version", this.Version);
                            command.Parameters.AddWithValue("@GroupID", this.GroupID);
                            command.Parameters.AddWithValue("@GroupOrdinal", this.GroupOrdinal);
                            command.Parameters.AddWithValue("@PluginID", this.PluginID);
                            command.ExecuteNonQuery();

                            App.LogDebug($"Updating Plugin: {this.PluginName} (ID: {this.PluginID}, GroupID: {this.GroupID})");
                        }
                    }
                    else
                    {
                        App.LogDebug($"Skipping update for plugin: {this.PluginName} (ID: {this.PluginID}) due to GroupID: {this.GroupID}");
                    }

                    // Insert or update into ExternalIDs table using INSERT OR REPLACE
                    if (!string.IsNullOrEmpty(this.BethesdaID) || !string.IsNullOrEmpty(this.NexusID))
                    {
                        command.CommandText = @"
                            INSERT OR REPLACE INTO ExternalIDs (PluginID, BethesdaID, NexusID)
                            VALUES (@PluginID, COALESCE(@BethesdaID, (SELECT BethesdaID FROM ExternalIDs WHERE PluginID = @PluginID)), COALESCE(@NexusID, (SELECT NexusID FROM ExternalIDs WHERE PluginID = @PluginID)))";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@PluginID", this.PluginID);
                        command.Parameters.AddWithValue("@BethesdaID", string.IsNullOrEmpty(this.BethesdaID) ? (object)DBNull.Value : this.BethesdaID);
                        command.Parameters.AddWithValue("@NexusID", string.IsNullOrEmpty(this.NexusID) ? (object)DBNull.Value : this.NexusID);
                        command.ExecuteNonQuery();
                        App.LogDebug($"ExternalIDs inserted or updated in database: PluginID={this.PluginID}, BethesdaID={this.BethesdaID}, NexusID={this.NexusID}");
                    }

                    App.LogDebug($"WriteMod Commit Transaction");
                    transaction.Commit();

                    // Ensure files are handled correctly
                    foreach (var file in Files)
                    {
                        FileInfo.InsertFileInfo(file, this.PluginID); // Assume InsertFileInfo is a static method in FileInfo class
                    }
                }
            }
            catch (Exception ex)
            {
                App.LogDebug($"Error writing plugin to database: {ex.Message}");
                transaction.Rollback();
                throw;
            }
        }

        public void ChangeGroup(int newGroupId)
        {
            // Prevent adding items to the group with GroupID -999 (Bethesda Core)
            if (newGroupId == -999)
            {
                throw new InvalidOperationException("Cannot add items to the Bethesda Core group.");
            }

            // Retrieve the AggLoadInfo singleton instance
            var aggLoadInfoInstance = AggLoadInfo.Instance;

            // Step 1: Find all siblings with a GroupOrdinal greater than the current GroupOrdinal and decrement their GroupOrdinal
            var currentGroup = aggLoadInfoInstance.Groups.FirstOrDefault(g => g.GroupID == this.GroupID);
            if (currentGroup != null)
            {
                var siblings = currentGroup.Plugins?.Where(p => p.GroupOrdinal > this.GroupOrdinal).ToList();
                if (siblings != null)
                {
                    foreach (var sibling in siblings)
                    {
                        sibling.GroupOrdinal--;
                    }
                }
            }

            // Step 2: Find the maximum GroupOrdinal value among the new group's children
            int maxOrdinal = 0;
            var targetGroup = aggLoadInfoInstance.Groups.FirstOrDefault(g => g.GroupID == newGroupId);
            if (targetGroup != null)
            {
                maxOrdinal = targetGroup.Plugins?.Max(p => p.GroupOrdinal) ?? 0;
            }

            // Step 3: Update the GroupID and set the GroupOrdinal to the maximum GroupOrdinal value plus one
            this.GroupID = newGroupId;
            this.GroupOrdinal = maxOrdinal + 1;
        }

    }
    public class PluginViewModel : INotifyPropertyChanged
    {
        private Plugin _plugin;
        private bool _isEnabled;

        public Plugin Plugin
        {
            get => _plugin;
            set
            {
                if (_plugin != value)
                {
                    _plugin = value;
                    OnPropertyChanged(nameof(Plugin));
                }
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public PluginViewModel()
        {
            _plugin = new Plugin();
            _isEnabled = false;
        }

        public PluginViewModel(Plugin plugin, bool isEnabled)
        {
            _plugin = plugin;
            _isEnabled = isEnabled;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

       



    }

}