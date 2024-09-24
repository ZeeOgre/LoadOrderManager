using System.Collections.ObjectModel;
using System.Data.SQLite;

namespace ZO.LoadOrderManager
{
    public class LoadOut
    {
        public long ProfileID { get; set; }
        public required string Name { get; set; }
        public HashSet<long> enabledPlugins;

        // Add GroupSet property
        public GroupSet GroupSet { get; set; }

        // Default constructor
        public LoadOut()
        {
            enabledPlugins = new HashSet<long>();

            // Load GroupSet with ID 2 or initialize with a new GroupSet if not found
            GroupSet = GroupSet.LoadGroupSet(2) ?? GroupSet.CreateEmptyGroupSet();
            if (GroupSet.GroupSetID != 2)
            {
                GroupSet.GroupSetName = $"{Name}_Groupset_{GroupSet.GroupSetID}";
            }
        }

        // Parameterized constructor
        public LoadOut(GroupSet groupSet)
        {
            enabledPlugins = new HashSet<long>();

            // Use the provided GroupSet
            GroupSet = groupSet;
        }

        public void LoadPlugins(IEnumerable<Plugin> plugins)
        {
            foreach (var plugin in plugins)
            {
                enabledPlugins.Add(plugin.PluginID);
            }
        }

        public static LoadOut Load(long loadOutID)
        {
            LogDebug("Loading profile from database");
            using var connection = DbManager.Instance.GetConnection();

            LogDebug("LoadOut Begin Transaction");
            using var transaction = connection.BeginTransaction();
            try
            {
                LoadOut loadOut = null!;

                // Load LoadOutProfiles
                using (var command = new SQLiteCommand("SELECT * FROM LoadOutProfiles WHERE ProfileID = @ProfileID", connection))
                {
                    command.Parameters.AddWithValue("@ProfileID", loadOutID);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var groupSetID = reader.GetInt64(reader.GetOrdinal("GroupSetID"));
                            var groupSet = GroupSet.LoadGroupSet(groupSetID) ?? GroupSet.LoadGroupSet(1) ?? GroupSet.CreateEmptyGroupSet();

                            loadOut = new LoadOut
                            {
                                ProfileID = reader.GetInt64(reader.GetOrdinal("ProfileID")),
                                Name = reader.GetString(reader.GetOrdinal("ProfileName")),
                                GroupSet = groupSet
                            };

                            // If a new GroupSet was created, set its name
                            if (groupSet.GroupSetID == 0)
                            {
                                groupSet.GroupSetName = $"{loadOut.Name}_GroupSet_{groupSet.GroupSetID}";
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"LoadOut with ID {loadOutID} not found.");
                        }
                    }
                }

                // Load ProfilePlugins
                using (var command = new SQLiteCommand("SELECT PluginID FROM ProfilePlugins WHERE ProfileID = @ProfileID", connection))
                {
                    command.Parameters.AddWithValue("@ProfileID", loadOutID);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var pluginID = reader.GetInt64(reader.GetOrdinal("PluginID"));
                            loadOut.enabledPlugins.Add(pluginID);
                        }
                    }
                }

                // Commit the transaction
                LogDebug("LoadOut Commit Transaction");
                transaction.Commit();
                return loadOut;
            }
            catch (Exception ex)
            {
                LogDebug("Error loading profile from database", ex.ToString());
                transaction.Rollback();
                throw;
            }
        }

        public long WriteProfile()
        {
            LogDebug("Writing profile to database");
            using var connection = DbManager.Instance.GetConnection();

            LogDebug("LoadOut Begin Transaction");
            using var transaction = connection.BeginTransaction();
            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    LogDebug($"Updating LoadOutProfiles table for {this.Name}");
                    // Update the LoadOutProfiles table
                    command.CommandText = @"
                                INSERT OR REPLACE INTO LoadOutProfiles (ProfileID, ProfileName, GroupSetID)
                                VALUES (@ProfileID, @ProfileName, @GroupSetID)";
                    command.Parameters.AddWithValue("@ProfileID", this.ProfileID);
                    command.Parameters.AddWithValue("@ProfileName", this.Name);
                    command.Parameters.AddWithValue("@GroupSetID", this.GroupSet.GroupSetID);
                    command.ExecuteNonQuery();

                    // Insert or replace ProfilePlugins entries
                    command.CommandText = @"
                                INSERT OR REPLACE INTO ProfilePlugins (ProfileID, PluginID)
                                VALUES (@ProfileID, @PluginID)";
                    foreach (var pluginID in this.enabledPlugins)
                    {
                        LogDebug($"Updating ProfilePlugins table for {pluginID}");
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@ProfileID", this.ProfileID);
                        command.Parameters.AddWithValue("@PluginID", pluginID);
                        command.ExecuteNonQuery();
                    }

                    // Remove any ProfilePlugins entries not in ActivePlugins
                    var activePluginsList = string.Join(",", this.enabledPlugins);
                    LogDebug($"Removing ProfilePlugins entries not in ActivePlugins: {activePluginsList}");
                    command.CommandText = $@"
                                DELETE FROM ProfilePlugins
                                WHERE ProfileID = @ProfileID AND PluginID NOT IN ({activePluginsList})";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@ProfileID", this.ProfileID);
                    command.ExecuteNonQuery();
                }

                // Commit the transaction
                LogDebug("LoadOut Commit Transaction");
                transaction.Commit();
                return this.ProfileID;
            }
            catch (Exception ex)
            {
                LogDebug("Error writing profile to database", ex.ToString());
                transaction.Rollback();
                throw;
            }
        }

        public static void SetPluginEnabled(long profileID, long pluginID, bool isEnabled)
        {
            using var connection = DbManager.Instance.GetConnection();

            using var command = new SQLiteCommand(connection);
            if (isEnabled)
            {
                command.CommandText = @"
                            INSERT OR IGNORE INTO ProfilePlugins (ProfileID, PluginID)
                            VALUES (@ProfileID, @PluginID)";
            }
            else
            {
                command.CommandText = @"
                            DELETE FROM ProfilePlugins
                            WHERE ProfileID = @ProfileID AND PluginID = @PluginID";
            }
            command.Parameters.AddWithValue("@ProfileID", profileID);
            command.Parameters.AddWithValue("@PluginID", pluginID);
            command.ExecuteNonQuery();
        }

        public static IEnumerable<long> GetActivePlugins(long profileId)
        {
            var loadOut = AggLoadInfo.Instance.LoadOuts.FirstOrDefault(l => l.ProfileID == profileId);
            if (loadOut != null)
            {
                return loadOut.enabledPlugins;
            }
            return Enumerable.Empty<long>();
        }

        public void UpdateEnabledPlugins(IEnumerable<long> pluginIDs)
        {
            enabledPlugins.Clear();
            foreach (var pluginID in pluginIDs)
            {
                enabledPlugins.Add(pluginID);
            }
        }

        public bool IsPluginEnabled(long pluginID)
        {
            return enabledPlugins.Contains(pluginID);
        }

        private static void LogDebug(string message, string? details = null)
        {
            if (OperatingSystem.IsWindows())
            {
                App.LogDebug(message, details);
            }
        }
    }
}
