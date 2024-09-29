using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Diagnostics;

namespace ZO.LoadOrderManager
{
    public class LoadOut
    {
        public long ProfileID { get; set; }
        public required string Name { get; set; }
        public ObservableHashSet<long> enabledPlugins { get; set; } = new ObservableHashSet<long>();

        //private GroupSet? _groupSet;
        public long GroupSetID { get; set; }

        //public GroupSet GroupSet
        //{
        //    get
        //    {
        //        if (_groupSet == null)
        //        {
        //            _groupSet = GroupSet.LoadGroupSet(GroupSetID) ?? GroupSet.CreateEmptyGroupSet();
        //            if (_groupSet.GroupSetID == 0)
        //            {
        //                _groupSet.GroupSetName = $"{Name}_GroupSet_{_groupSet.GroupSetID}";
        //            }
        //        }
        //        return _groupSet;
        //    }
        //    set
        //    {
        //        _groupSet = value;
        //        GroupSetID = value.GroupSetID;
        //    }
        //}

        // Default constructor
        public LoadOut()
        {
            enabledPlugins = new ObservableHashSet<long>();
        }

        // Parameterized constructor
        public LoadOut(GroupSet groupSet)
        {
            enabledPlugins = new ObservableHashSet<long>();
            GroupSetID = groupSet.GroupSetID;
            
        }

        public void LoadPlugins(IEnumerable<Plugin> plugins)
        {
            foreach (var plugin in plugins)
            {
                enabledPlugins.Add(plugin.PluginID);
            }
        }

        public ObservableHashSet<long> LoadEnabledPlugins()
        {
            var _enabledPlugins = new ObservableHashSet<long>();

            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);
            command.CommandText = "SELECT PluginID FROM ProfilePlugins WHERE ProfileID = @ProfileID";
            command.Parameters.AddWithValue("@ProfileID", this.ProfileID);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                _enabledPlugins.Add(reader.GetInt64(reader.GetOrdinal("PluginID")));
            }

            return _enabledPlugins;
        }

        public static LoadOut Load(long loadOutID)
        {
            App.LogDebug("Loading profile from database");
            using var connection = DbManager.Instance.GetConnection();

            App.LogDebug("LoadOut Begin Transaction");
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

                            loadOut = new LoadOut
                            {
                                ProfileID = reader.GetInt64(reader.GetOrdinal("ProfileID")),
                                Name = reader.GetString(reader.GetOrdinal("ProfileName")),
                                GroupSetID = groupSetID
                            };
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
                App.LogDebug("LoadOut Commit Transaction");
                transaction.Commit();
                return loadOut;
            }
            catch (Exception ex)
            {
                App.LogDebug("Error loading profile from database", ex.ToString());
                transaction.Rollback();
                throw;
            }
        }

        public long WriteProfile()
        {
            App.LogDebug("Writing profile to database");
            using var connection = DbManager.Instance.GetConnection();

            App.LogDebug("LoadOut Begin Transaction");
            using var transaction = connection.BeginTransaction();
            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    App.LogDebug($"Updating LoadOutProfiles table for {this.Name}");
                    // Update the LoadOutProfiles table
                    command.CommandText = @"
                            INSERT OR REPLACE INTO LoadOutProfiles (ProfileID, ProfileName, GroupSetID)
                            VALUES (@ProfileID, @ProfileName, @GroupSetID)";
                    command.Parameters.AddWithValue("@ProfileID", this.ProfileID);
                    command.Parameters.AddWithValue("@ProfileName", this.Name);
                    command.Parameters.AddWithValue("@GroupSetID", this.GroupSetID);
                    command.ExecuteNonQuery();

                    // Insert or replace ProfilePlugins entries
                    command.CommandText = @"
                            INSERT OR IGNORE INTO ProfilePlugins (ProfileID, PluginID)
                            VALUES (@ProfileID, @PluginID)";
                    foreach (var pluginID in this.enabledPlugins)
                    {
                        App.LogDebug($"Updating ProfilePlugins table for {pluginID}");
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@ProfileID", this.ProfileID);
                        command.Parameters.AddWithValue("@PluginID", pluginID);
                        command.ExecuteNonQuery();
                    }

                    // Remove any ProfilePlugins entries not in ActivePlugins
                    var activePluginsList = string.Join(",", this.enabledPlugins);
                    App.LogDebug($"Removing ProfilePlugins entries not in ActivePlugins: {activePluginsList}");
                    command.CommandText = $@"
                            DELETE FROM ProfilePlugins
                            WHERE ProfileID = @ProfileID AND PluginID NOT IN ({activePluginsList})";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@ProfileID", this.ProfileID);
                    command.ExecuteNonQuery();
                }

                // Commit the transaction
                App.LogDebug("LoadOut Commit Transaction");
                transaction.Commit();
                return this.ProfileID;
            }
            catch (Exception ex)
            {
                App.LogDebug("Error writing profile to database", ex.ToString());
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
            Debug.WriteLine($"Insert/Update {pluginID} on profile {profileID}");
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
            WriteProfile();
        }

        public bool IsPluginEnabled(long pluginID)
        {
            return enabledPlugins.Contains(pluginID);
        }


        public override bool Equals(object obj)
        {
            if (obj is LoadOut other)
            {
                return ProfileID == other.ProfileID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return ProfileID.GetHashCode();
        }



    }
}
