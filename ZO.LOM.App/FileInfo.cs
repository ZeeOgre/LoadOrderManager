using System.Data.SQLite;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Windows;

namespace ZO.LoadOrderManager
{
    public class FileInfo
    {
        public int FileID { get; set; }
        public string Filename { get; set; }
        public string? RelativePath { get; set; }
        public string DTStamp { get; set; }
        public string? HASH { get; set; }
        public bool IsArchive { get; set; }

        public FileInfo()
        {
            Filename = string.Empty; // Initialize with default value
            DTStamp = DateTime.Now.ToString("o"); // Initialize with current timestamp
        }

        public FileInfo(string filename)
        {
            Filename = filename.ToLowerInvariant();
            RelativePath = null;
            DTStamp = DateTime.Now.ToString("o");
            HASH = null;
            IsArchive = false;
        }

        public FileInfo(System.IO.FileInfo fileInfo, string gameFolderPath)
        {
            Filename = fileInfo.Name.ToLowerInvariant();
            RelativePath = Path.GetRelativePath(Path.Combine(gameFolderPath, "data"), fileInfo.FullName);
            DTStamp = fileInfo.LastWriteTime.ToString("o");
            HASH = ComputeHash(fileInfo.FullName);
            IsArchive = CheckIfArchive(fileInfo.Name);
        }

        private bool CheckIfArchive(string filename)
        {
            string extension = Path.GetExtension(filename).ToLowerInvariant();
            return extension == ".rar" || extension == ".zip" || extension == ".7z" || extension == ".ba2";
        }

        public static string ComputeHash(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = System.IO.File.OpenRead(filePath);
            var hashBytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        public override string ToString()
        {
            return $"FileID: {FileID}, Filename: {Filename}, RelativePath: {RelativePath}, DTStamp: {DTStamp}, HASH: {HASH}, IsArchive: {IsArchive}";
        }

        public List<FileInfo> LoadFilesByPlugin(long pluginId)
        {
            var fileInfos = new List<FileInfo>();

            using var connection = DbManager.Instance.GetConnection();
            

            using var command = new SQLiteCommand(
                "SELECT FileID, Filename, RelativePath, DTStamp, HASH, IsArchive " +
                "FROM vwPluginFiles WHERE PluginID = @PluginID", connection);

            _ = command.Parameters.AddWithValue("@PluginID", pluginId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var fileInfo = new FileInfo
                {
                    FileID = reader.GetInt32(0),
                    Filename = reader.GetString(1),
                    RelativePath = reader.IsDBNull(2) ? null : reader.GetString(2),
                    DTStamp = reader.GetString(3),
                    HASH = reader.IsDBNull(4) ? null : reader.GetString(4),
                    IsArchive = reader.GetInt64(5) == 1
                };
                fileInfos.Add(fileInfo);
            }

            return fileInfos;
        }

        public static void InsertFileInfo(FileInfo fileInfo, long pluginId)
        {
            using var connection = DbManager.Instance.GetConnection();
            
            App.LogDebug($"Fileinfo Begin Transaction");
            using var transaction = connection.BeginTransaction();

            try
            {
                using var command = new SQLiteCommand(connection);
                if (fileInfo.FileID == 0)
                {
                    command.CommandText = @"
                    INSERT INTO FileInfo (PluginID, Filename, RelativePath, DTStamp, HASH, IsArchive)
                    VALUES (@PluginID, @Filename, @RelativePath, @DTStamp, @HASH, @IsArchive)
                    RETURNING FileID";

                    command.Parameters.AddWithValue("@PluginID", pluginId);
                    command.Parameters.AddWithValue("@Filename", fileInfo.Filename);
                    command.Parameters.AddWithValue("@RelativePath", fileInfo.RelativePath ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@DTStamp", fileInfo.DTStamp);
                    command.Parameters.AddWithValue("@HASH", fileInfo.HASH ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@IsArchive", fileInfo.IsArchive);

                    fileInfo.FileID = Convert.ToInt32(command.ExecuteScalar());
                }
                else
                {
                    command.CommandText = @"
                    UPDATE FileInfo
                    SET PluginID = @PluginID,
                        Filename = @Filename,
                        RelativePath = @RelativePath,
                        DTStamp = COALESCE(@DTStamp, DTStamp),
                        HASH = COALESCE(@HASH, HASH),
                        IsArchive = @IsArchive
                    WHERE FileID = @FileID";

                    command.Parameters.AddWithValue("@FileID", fileInfo.FileID);
                    command.Parameters.AddWithValue("@PluginID", pluginId);
                    command.Parameters.AddWithValue("@Filename", fileInfo.Filename);
                    command.Parameters.AddWithValue("@RelativePath", fileInfo.RelativePath ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@DTStamp", fileInfo.DTStamp);
                    command.Parameters.AddWithValue("@HASH", fileInfo.HASH ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@IsArchive", fileInfo.IsArchive);

                    command.ExecuteNonQuery();
                }

                App.LogDebug($"Fileinfo Commit Transaction");
                transaction.Commit();
            }
            catch (Exception ex)
            {
                App.LogDebug($"Error inserting/updating file info: {ex.Message}");
                transaction.Rollback();
                throw;
            }
        }
    }
}

