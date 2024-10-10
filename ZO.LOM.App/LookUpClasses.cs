using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;

namespace ZO.LoadOrderManager
{
    public class GroupSetGroupCollection
    {
        public ObservableCollection<(long groupID, long groupSetID, long? parentID, long Ordinal)> Items { get; set; }

        public GroupSetGroupCollection()
        {
            Items = new ObservableCollection<(long, long, long?, long)>();
        }

        // Load data from the database for a specific GroupSetID
        public void LoadGroupSetGroups(long groupSetID, SQLiteConnection connection)
        {
            try
            {
                Items.Clear();

                using var command = new SQLiteCommand(connection);
                command.CommandText = @"
                        SELECT GroupID, GroupSetID, ParentID, Ordinal
                        FROM GroupSetGroups
                        WHERE GroupSetID = @GroupSetID OR GroupSetID = 1";
                command.Parameters.AddWithValue("@GroupSetID", groupSetID);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var groupID = reader.GetInt64(0);
                    var grpSetID = reader.GetInt64(1);
                    var parentID = reader.IsDBNull(2) ? (long?)null : reader.GetInt64(2);
                    var ordinal = reader.GetInt64(3);

                    Items.Add((groupID, grpSetID, parentID, ordinal));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading GroupSetGroups: {ex.Message}");
            }
        }

        // Write or update an entry in the database
        public void WriteGroupSetGroup(long groupID, long groupSetID, long? parentID, long ordinal, SQLiteConnection connection)
        {
            try
            {
                using var command = new SQLiteCommand(connection);
                command.CommandText = @"
                        INSERT INTO GroupSetGroups (GroupID, GroupSetID, ParentID, Ordinal)
                        VALUES (@GroupID, @GroupSetID, @ParentID, @Ordinal)
                        ON CONFLICT(GroupID, GroupSetID) DO UPDATE 
                        SET ParentID = COALESCE(@ParentID, ParentID),
                            Ordinal = COALESCE(@Ordinal, Ordinal);";

                command.Parameters.AddWithValue("@GroupID", groupID);
                command.Parameters.AddWithValue("@GroupSetID", groupSetID);
                command.Parameters.AddWithValue("@ParentID", (object?)parentID ?? DBNull.Value);
                command.Parameters.AddWithValue("@Ordinal", ordinal);

                command.ExecuteNonQuery();
                Console.WriteLine($"GroupSetGroup written to database: GroupID = {groupID}, GroupSetID = {groupSetID}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing GroupSetGroup: {ex.Message}");
            }
        }
    }

    public class GroupSetPluginCollection
    {
        public ObservableCollection<(long groupSetID, long groupID, long pluginID, long Ordinal)> Items { get; set; }

        public GroupSetPluginCollection()
        {
            Items = new ObservableCollection<(long, long, long, long)>();
        }

        // Load data from the database for a specific GroupSetID
        public void LoadGroupSetPlugins(long groupSetID, SQLiteConnection connection)
        {
            try
            {
                Items.Clear();

                using var command = new SQLiteCommand(connection);
                command.CommandText = @"
                        SELECT GroupSetID, GroupID, PluginID, Ordinal
                        FROM GroupSetPlugins
                        WHERE GroupSetID = @GroupSetID OR GroupSetID = 1";
                command.Parameters.AddWithValue("@GroupSetID", groupSetID);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var grpSetID = reader.GetInt64(0);
                    var groupID = reader.GetInt64(1);
                    var pluginID = reader.GetInt64(2);
                    var ordinal = reader.GetInt64(3);

                    Items.Add((grpSetID, groupID, pluginID, ordinal));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading GroupSetPlugins: {ex.Message}");
            }
        }

        // Write or update an entry in the database
        public void WriteGroupSetPlugin(long groupSetID, long groupID, long pluginID, long ordinal, SQLiteConnection connection)
        {
            try
            {
                using var command = new SQLiteCommand(connection);
                command.CommandText = @"
                        INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal)
                        VALUES (@GroupSetID, @GroupID, @PluginID, @Ordinal)
                        ON CONFLICT(GroupSetID, GroupID, PluginID) DO UPDATE 
                        SET Ordinal = COALESCE(@Ordinal, Ordinal);";

                command.Parameters.AddWithValue("@GroupSetID", groupSetID);
                command.Parameters.AddWithValue("@GroupID", groupID);
                command.Parameters.AddWithValue("@PluginID", pluginID);
                command.Parameters.AddWithValue("@Ordinal", ordinal);

                command.ExecuteNonQuery();
                Console.WriteLine($"GroupSetPlugin written to database: GroupSetID = {groupSetID}, GroupID = {groupID}, PluginID = {pluginID}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing GroupSetPlugin: {ex.Message}");
            }
        }
    }

    public class ProfilePluginCollection : IEnumerable<(long ProfileID, long PluginID)>
    {
        public ObservableHashSet<(long ProfileID, long PluginID)> Items { get; set; } = new ObservableHashSet<(long ProfileID, long PluginID)>();

        public ProfilePluginCollection() { }

        // Load data from the database for all profiles associated with a specific GroupSetID
        public void LoadProfilePlugins(long groupSetID, SQLiteConnection connection)
        {
            try
            {
                Items.Clear();

                using var command = new SQLiteCommand(connection);
                command.CommandText = @"
                        SELECT pp.ProfileID, pp.PluginID
                        FROM ProfilePlugins pp
                        INNER JOIN LoadOutProfiles lp ON pp.ProfileID = lp.ProfileID
                        WHERE lp.GroupSetID = @GroupSetID OR GroupSetID = 1";
                command.Parameters.AddWithValue("@GroupSetID", groupSetID);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var profileID = reader.GetInt64(0);
                    var pluginID = reader.GetInt64(1);

                    Items.Add((profileID, pluginID));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading ProfilePlugins: {ex.Message}");
            }
        }

        // Write or update an entry in the database
        public void WriteProfilePlugin(long profileID, long pluginID, SQLiteConnection connection)
        {
            try
            {
                using var command = new SQLiteCommand(connection);
                command.CommandText = @"
                        INSERT INTO ProfilePlugins (ProfileID, PluginID)
                        VALUES (@ProfileID, @PluginID)
                        ON CONFLICT(ProfileID, PluginID) DO NOTHING;"; // No updates on conflict

                command.Parameters.AddWithValue("@ProfileID", profileID);
                command.Parameters.AddWithValue("@PluginID", pluginID);

                command.ExecuteNonQuery();
                Console.WriteLine($"ProfilePlugin written to database: ProfileID = {profileID}, PluginID = {pluginID}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing ProfilePlugin: {ex.Message}");
            }
        }

        // Implement IEnumerable to allow foreach iteration
        public IEnumerator<(long ProfileID, long PluginID)> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
