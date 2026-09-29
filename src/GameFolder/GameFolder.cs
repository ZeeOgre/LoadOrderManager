using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;

namespace ZO.LoadOrderManager;

public sealed class GameFolder
{
    public long GameFolderID { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string GameRoot { get; set; } = string.Empty;
    public string DataFolder => Path.Combine(GameRoot, "Data");

    public override string ToString() => DisplayName;

    public static ObservableCollection<GameFolder> LoadAll()
    {
        var result = new ObservableCollection<GameFolder>();
        using var connection = DbManager.Instance.GetConnection();
        using var command = new SQLiteCommand(
            "SELECT GameFolderID, DisplayName, GameRoot FROM GameFolders ORDER BY DisplayName COLLATE NOCASE", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new GameFolder
            {
                GameFolderID = reader.GetInt64(0),
                DisplayName = reader.GetString(1),
                GameRoot = reader.GetString(2)
            });
        }
        return result;
    }

    public void Save()
    {
        DisplayName = DisplayName.Trim();
        GameRoot = Path.GetFullPath(GameRoot.Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        using var connection = DbManager.Instance.GetConnection();
        using var command = new SQLiteCommand(connection);
        command.CommandText = GameFolderID == 0
            ? @"INSERT INTO GameFolders (DisplayName, GameRoot) VALUES (@DisplayName, @GameRoot);
                SELECT last_insert_rowid();"
            : @"UPDATE GameFolders SET DisplayName = @DisplayName, GameRoot = @GameRoot
                WHERE GameFolderID = @GameFolderID; SELECT @GameFolderID;";
        command.Parameters.AddWithValue("@GameFolderID", GameFolderID);
        command.Parameters.AddWithValue("@DisplayName", DisplayName);
        command.Parameters.AddWithValue("@GameRoot", GameRoot);
        GameFolderID = Convert.ToInt64(command.ExecuteScalar());
    }

    public void Delete()
    {
        using var connection = DbManager.Instance.GetConnection();
        using var transaction = connection.BeginTransaction();
        using var command = new SQLiteCommand(@"
            DELETE FROM FileInfo WHERE GameFolderID = @GameFolderID;
            DELETE FROM GameFolders WHERE GameFolderID = @GameFolderID;", connection);
        command.Parameters.AddWithValue("@GameFolderID", GameFolderID);
        command.ExecuteNonQuery();
        transaction.Commit();
    }
}

public static class GameFolderContext
{
    private static readonly HashSet<long> PresentPluginIDs = new();
    public static GameFolder? Active { get; private set; }

    public static void Select(GameFolder folder)
    {
        Active = folder;
        Config.Instance.GameFolder = folder.GameRoot;
        RefreshPresence();
    }

    public static bool IsPresent(long pluginID) => Active != null && PresentPluginIDs.Contains(pluginID);

    public static void SetPresent(long pluginID, bool present)
    {
        if (present) PresentPluginIDs.Add(pluginID);
        else PresentPluginIDs.Remove(pluginID);
    }

    public static void RefreshPresence()
    {
        PresentPluginIDs.Clear();
        if (Active == null) return;
        using var connection = DbManager.Instance.GetConnection();
        using var command = new SQLiteCommand(@"
            SELECT DISTINCT PluginID FROM FileInfo
            WHERE GameFolderID = @GameFolderID AND PluginID IS NOT NULL AND (Flags & @PluginFlag) = @PluginFlag", connection);
        command.Parameters.AddWithValue("@GameFolderID", Active.GameFolderID);
        command.Parameters.AddWithValue("@PluginFlag", (long)FileFlags.Plugin);
        using var reader = command.ExecuteReader();
        while (reader.Read()) PresentPluginIDs.Add(reader.GetInt64(0));
    }
}
