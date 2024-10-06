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
        public bool IsFavorite { get; set; }

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

        public ObservableHashSet<long> LoadEnabledPlugins(SQLiteConnection? connection = null, SQLiteTransaction? transaction = null)
        {
            var _enabledPlugins = new ObservableHashSet<long>();

            bool localConnection = false;
            if (connection == null)
            {
                connection = DbManager.Instance.GetConnection();
                localConnection = true;
            }

            using var command = new SQLiteCommand(connection);
            if (transaction != null)
            {
                command.Transaction = transaction;
            }
            command.CommandText = "SELECT PluginID FROM ProfilePlugins WHERE ProfileID = @ProfileID";
            command.Parameters.AddWithValue("@ProfileID", this.ProfileID);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                _enabledPlugins.Add(reader.GetInt64(reader.GetOrdinal("PluginID")));
            }

            if (localConnection)
            {
                connection.Close();
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
                    command.Transaction = transaction;
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
                                GroupSetID = groupSetID,
                                IsFavorite = reader.GetInt64(reader.GetOrdinal("IsFavorite")) == 1
                            };
                        }
                        else
                        {
                            throw new InvalidOperationException($"LoadOut with ID {loadOutID} not found.");
                        }
                    }
                }

                // Load ProfilePlugins
                loadOut.enabledPlugins = loadOut.LoadEnabledPlugins(connection, transaction);

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
                            INSERT OR REPLACE INTO LoadOutProfiles (ProfileID, ProfileName, GroupSetID, IsFavorite)
                            VALUES (@ProfileID, @ProfileName, @GroupSetID, @IsFavorite)";
                    command.Parameters.AddWithValue("@ProfileID", this.ProfileID);
                    command.Parameters.AddWithValue("@ProfileName", this.Name);
                    command.Parameters.AddWithValue("@GroupSetID", this.GroupSetID);
                    command.Parameters.AddWithValue("@IsFavorite", this.IsFavorite ? 1 : 0);
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

        public static void SetPluginEnabled(long profileID, long pluginID, bool isEnabled, AggLoadInfo? aggLoadInfo = null)
        {
            // Use the provided AggLoadInfo or default to the singleton instance
            aggLoadInfo ??= AggLoadInfo.Instance;

            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);

            if (isEnabled)
            {
                // Insert the plugin into the ProfilePlugins table if enabled
                command.CommandText = @"
                INSERT OR IGNORE INTO ProfilePlugins (ProfileID, PluginID)
                VALUES (@ProfileID, @PluginID)";
            }
            else
            {
                // Delete the plugin from ProfilePlugins if disabled
                command.CommandText = @"
                DELETE FROM ProfilePlugins
                WHERE ProfileID = @ProfileID AND PluginID = @PluginID";
            }
            command.Parameters.AddWithValue("@ProfileID", profileID);
            command.Parameters.AddWithValue("@PluginID", pluginID);
            command.ExecuteNonQuery();
            Debug.WriteLine($"Insert/Update {pluginID} on profile {profileID}");

            // Now update the in-memory LoadOut object in AggLoadInfo
            var loadOut = aggLoadInfo.LoadOuts.FirstOrDefault(l => l.ProfileID == profileID);
            if (loadOut != null)
            {
                if (isEnabled)
                {
                    // Add the plugin if it's being enabled
                    loadOut.enabledPlugins.Add(pluginID);
                }
                else
                {
                    // Remove the plugin if it's being disabled
                    loadOut.enabledPlugins.Remove(pluginID);
                }
            }

            // Update the ProfilePlugins collection in AggLoadInfo
            var profilePlugins = aggLoadInfo.ProfilePlugins.Items;

            if (isEnabled)
            {
                // Add the plugin to ProfilePlugins
                if (!profilePlugins.Contains((profileID, pluginID)))
                {
                    profilePlugins.Add((profileID, pluginID));
                    Debug.WriteLine($"Added PluginID {pluginID} to ProfilePlugins (ProfileID = {profileID})");
                }
            }
            else
            {
                // Remove the plugin from ProfilePlugins
                var existingPlugin = profilePlugins.FirstOrDefault(pp => pp.ProfileID == profileID && pp.PluginID == pluginID);
                if (existingPlugin != default)
                {
                    profilePlugins.Remove(existingPlugin);
                    Debug.WriteLine($"Removed PluginID {pluginID} from ProfilePlugins (ProfileID = {profileID})");
                }
            }
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
