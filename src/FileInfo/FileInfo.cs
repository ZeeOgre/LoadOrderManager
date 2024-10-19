using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using YamlDotNet.Serialization;


namespace ZO.LoadOrderManager
{

    [Flags]
    public enum FileFlags
    {
        None = 0,
        IsMonitored = 1,
        GameAppData = 2,
        GameDocs = 4,
        GameFolder = 8,
        Plugin = 16,
        Config = 32,
        IsJunction = 64,
        IsArchive = 128
    }

    public class FileInfo
    {

        private static readonly Dictionary<string, string> FolderDefinitions = new()
    {
        { "GameAppData", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "starfield") },
        { "GameDocs", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Starfield") },
        { "GameFolder", Config.Instance.GameFolder }
    };



        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(
           string lpFileName,
           uint dwDesiredAccess,
           uint dwShareMode,
           IntPtr lpSecurityAttributes,
           uint dwCreationDisposition,
           uint dwFlagsAndAttributes,
           IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetFinalPathNameByHandle(IntPtr hFile, StringBuilder lpszFilePath, int cchFilePath, int dwFlags);

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);


        public long FileID { get; set; }
        public string Filename { get; set; }
        public string? RelativePath { get; set; }
        public string DTStamp { get; set; }
        public string? HASH { get; set; }
        public FileFlags Flags { get; set; }
        public string AbsolutePath { get; set; } // Absolute path of the file
        [YamlIgnore]
        public byte[]? FileContent { get; set; } // Nullable byte array for raw file content
        [YamlIgnore]
        public byte[]? CompressedContent { get; set; } // Property for compressed content

        public FileInfo()
        {
            Filename = string.Empty; // Initialize with default value
            DTStamp = DateTime.Now.ToString("o"); // Initialize with current timestamp
            AbsolutePath = string.Empty;
            Flags = FileFlags.None;
        }

        public FileInfo(string filename)
        {
            Filename = filename.ToLowerInvariant();
            RelativePath = null;
            DTStamp = DateTime.Now.ToString("o");
            HASH = null;
            Flags = GetFlagsFromFileName(filename);
            AbsolutePath = filename;
        }

        public FileInfo(System.IO.FileInfo fileInfo, string gameFolderPath, bool checkHash = true)
        {
            Filename = fileInfo.Name.ToLowerInvariant();
            RelativePath = Path.GetRelativePath(Path.Combine(gameFolderPath, "data"), fileInfo.FullName);
            DTStamp = fileInfo.LastWriteTime.ToString("o");
            if (checkHash) { HASH = ComputeHash(fileInfo.FullName); }
            Flags = GetFlagsFromFileObject(fileInfo);
            AbsolutePath = Flags.HasFlag(FileFlags.IsJunction) ? GetJunctionTarget(fileInfo.FullName) : fileInfo.FullName;
        }

        public FileInfo(string filename, bool monitor, bool? storeFile = false)
        {
            Filename = filename.ToLowerInvariant();
            RelativePath = null;
            DTStamp = DateTime.Now.ToString("o");
            if (storeFile == true)
            {
                HASH = ComputeHash(filename);
                FileContent = System.IO.File.ReadAllBytes(filename); // Store raw file content
                CompressedContent = CompressFile(FileContent); // Store compressed content
            }

            AbsolutePath = filename;

            GetFlagsFromFileName(filename);

            if (monitor)
            {
                Flags |= FileFlags.IsMonitored;
                storeFile = true;
            }
            else
            {
                Flags = FileFlags.None;
            }


            if (storeFile == true)
            {
                FileContent = System.IO.File.ReadAllBytes(filename); // Store raw file content
                CompressedContent = CompressFile(FileContent); // Store compressed content
            }
        }

        private FileFlags GetFlagsFromFileName(string filename)
        {
            FileFlags flags = FileFlags.None;

            // Use helper methods to set flags more cleanly
            SetFlagState(ref flags, FileFlags.IsArchive, CheckIfArchive(filename));

            // Check for plugins
            bool isPlugin = filename.EndsWith(".esp", StringComparison.OrdinalIgnoreCase) || filename.EndsWith(".esm", StringComparison.OrdinalIgnoreCase);
            SetFlagState(ref flags, FileFlags.Plugin, isPlugin);

            // Check for config files
            bool isConfig = filename.EndsWith(".ini", StringComparison.OrdinalIgnoreCase) ||
                            filename.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                            filename.EndsWith(".ccc", StringComparison.OrdinalIgnoreCase);
            SetFlagState(ref flags, FileFlags.Config, isConfig);

            // Folder-specific flag setting
            foreach (var folder in FolderDefinitions)
            {
                if (filename.StartsWith(folder.Value, StringComparison.OrdinalIgnoreCase))
                {
                    switch (folder.Key)
                    {
                        case "GameAppData":
                            SetFlagState(ref flags, FileFlags.GameAppData, true);
                            break;
                        case "GameDocs":
                            SetFlagState(ref flags, FileFlags.GameDocs, true);
                            break;
                        case "GameFolder":
                            SetFlagState(ref flags, FileFlags.GameFolder, true);
                            break;
                    }
                }
            }

            return flags;
        }

        // Helper method for setting flags
        private void SetFlagState(ref FileFlags flags, FileFlags flag, bool isEnabled)
        {
            if (isEnabled)
            {
                flags |= flag;
            }
            else
            {
                flags &= ~flag;
            }
        }


        private FileFlags GetFlagsFromFileObject(System.IO.FileInfo fileInfo)
        {
            FileFlags flags = FileFlags.None;
            if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                flags |= FileFlags.IsJunction;
            }
             flags |= GetFlagsFromFileName(fileInfo.FullName);
            return Flags;
        }

        private bool CheckIfArchive(string filename)
        {
            string extension = Path.GetExtension(filename).ToLowerInvariant();
            return extension == ".rar" || extension == ".zip" || extension == ".7z" || extension == ".ba2";
        }

        private bool CheckIfJunction(string filePath)
        {
            try
            {
                var fileInfo = new System.IO.FileInfo(filePath);
                return fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private void SetFlagsBasedOnPath()
        {
            foreach (var folder in FolderDefinitions)
            {
                if (AbsolutePath.StartsWith(folder.Value, StringComparison.OrdinalIgnoreCase))
                {
                    switch (folder.Key)
                    {
                        case "GameAppData":
                            Flags |= FileFlags.GameAppData;
                            break;
                        case "GameDocs":
                            Flags |= FileFlags.GameDocs;
                            break;
                        case "GameFolder":
                            Flags |= FileFlags.GameFolder;
                            break;
                    }
                }
            }
        }

        private string GetPathByFlags(FileFlags flags, string filename)
        {
            if (flags.HasFlag(FileFlags.GameAppData))
            {
                return Path.Combine(FolderDefinitions["GameAppData"], filename);
            }
            else if (flags.HasFlag(FileFlags.GameDocs))
            {
                return Path.Combine(FolderDefinitions["GameDocs"], filename);
            }
            else if (flags.HasFlag(FileFlags.GameFolder))
            {
                return Path.Combine(FolderDefinitions["GameFolder"], filename);
            }
            else
            {
                throw new InvalidOperationException("File does not have a valid path flag set.");
            }
        }

        public string GetPathByFlags()
        {
            if (Flags.HasFlag(FileFlags.GameAppData))
            {
                return Path.Combine(FolderDefinitions["GameAppData"], Filename);
            }
            else if (Flags.HasFlag(FileFlags.GameDocs))
            {
                return Path.Combine(FolderDefinitions["GameDocs"], Filename);
            }
            else if (Flags.HasFlag(FileFlags.GameFolder))
            {
                return Path.Combine(FolderDefinitions["GameFolder"], Filename);
            }
            else
            {
                throw new InvalidOperationException("File does not have a valid path flag set.");
            }
        }

        private byte[] CompressFile(byte[] fileContent)
        {
            using var compressedFileStream = new MemoryStream();
            using (var compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
            {
                compressionStream.Write(fileContent, 0, fileContent.Length);
            }
            return compressedFileStream.ToArray();
        }

        private static byte[] DecompressFile(byte[] compressedContent)
        {
            using var compressedStream = new MemoryStream(compressedContent);
            using var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var decompressedStream = new MemoryStream();
            decompressionStream.CopyTo(decompressedStream);
            return decompressedStream.ToArray();
        }

        private FileFlags CheckFileFlags(System.IO.FileInfo fileInfo)
        {
            FileFlags flags = FileFlags.None;

            if (CheckIfArchive(fileInfo.Name))
            {
                flags |= FileFlags.IsArchive;
            }

            if (CheckIfJunction(fileInfo.FullName))
            {
                flags |= FileFlags.IsJunction;
            }

            // Add logic to check if the file is monitored and set the flag accordingly
            // if (IsMonitoredFile(fileInfo.FullName))
            // {
            //     flags |= FileFlags.IsMonitored;
            // }

            return flags;
        }

        private bool CheckIfArchive(string filename)
        {
            string extension = Path.GetExtension(filename).ToLowerInvariant();
            return extension == ".rar" || extension == ".zip" || extension == ".7z" || extension == ".ba2";
        }

        private bool CheckIfJunction(string filePath)
        {
            var fileInfo = new System.IO.FileInfo(filePath);
            return fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }

        private string GetJunctionTarget(string junctionPath)
        {
            const int FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
            const int OPEN_EXISTING = 3;
            const int FILE_SHARE_READ = 1;
            const int FILE_SHARE_WRITE = 2;
            const int FILE_SHARE_DELETE = 4;

            IntPtr handle = CreateFile(junctionPath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
            if (handle == INVALID_HANDLE_VALUE)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            try
            {
                StringBuilder path = new StringBuilder(512);
                int result = GetFinalPathNameByHandle(handle, path, path.Capacity, 0);
                if (result == 0)
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }

                // Remove the "\\?\" prefix
                if (path.Length > 4 && path.ToString().StartsWith(@"\\?\"))
                {
                    return path.ToString().Substring(4);
                }

                return path.ToString();
            }
            finally
            {
                _ = CloseHandle(handle);
            }
        }
        public bool FileCheck()
        {
            var currentHash = ComputeHash(AbsolutePath);
            return currentHash == HASH;
        }

        public static FileInfo FileCheck(string filename, bool checkHash)
        {
            string? currentHash = null;
            if (checkHash)
            {
                currentHash = ComputeHash(filename);
            }

            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);
            command.CommandText = "SELECT * FROM FileInfo WHERE Filename = @Filename";
            _ = command.Parameters.AddWithValue("@Filename", filename.ToLowerInvariant());

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var fileInfo = new FileInfo
                {
                    FileID = reader.GetInt64(reader.GetOrdinal("FileID")),
                    Filename = reader.GetString(reader.GetOrdinal("Filename")),
                    RelativePath = reader.IsDBNull(reader.GetOrdinal("RelativePath")) ? null : reader.GetString(reader.GetOrdinal("RelativePath")),
                    DTStamp = reader.GetString(reader.GetOrdinal("DTStamp")),
                    HASH = reader.IsDBNull(reader.GetOrdinal("HASH")) ? null : reader.GetString(reader.GetOrdinal("HASH")),
                    Flags = (FileFlags)reader.GetInt64(reader.GetOrdinal("Flags")),
                    AbsolutePath = reader.IsDBNull(reader.GetOrdinal("AbsolutePath")) ? null : reader.GetString(reader.GetOrdinal("AbsolutePath")),
                    FileContent = reader.IsDBNull(reader.GetOrdinal("FileContent")) ? null : (byte[])reader["FileContent"]
                };

                if (string.IsNullOrEmpty(fileInfo.AbsolutePath) && string.IsNullOrEmpty(fileInfo.RelativePath))
                {
                    try
                    {
                        // Create an instance of FileInfo to call the non-static method
                        var fileInfoInstance = new FileInfo();
                        fileInfo.AbsolutePath = fileInfoInstance.GetPathByFlags(fileInfo.Flags, filename);
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw new FileNotFoundException("File path could not be determined from flags.", ex);
                    }
                }

                if (fileInfo.Filename == filename.ToLowerInvariant())
                {
                    if (checkHash && fileInfo.HASH != currentHash)
                    {
                        fileInfo.HASH = currentHash;
                    }

                    if (fileInfo.FileContent == null || fileInfo.FileContent.Length == 0 || (checkHash && fileInfo.HASH == currentHash))
                    {
                        fileInfo.DTStamp = new System.IO.FileInfo(fileInfo.AbsolutePath).LastWriteTime.ToString("o");
                        fileInfo.FileContent = System.IO.File.ReadAllBytes(fileInfo.AbsolutePath);
                        fileInfo.CompressedContent = fileInfo.CompressFile(fileInfo.FileContent);
                        fileInfo.SetFlagsBasedOnPath(); // Set flags based on path
                        _ = InsertFileInfo(fileInfo);
                    }
                }

                return fileInfo;
            }
            else
            {
                var newFileInfo = new FileInfo(filename, true, true);
                newFileInfo.SetFlagsBasedOnPath(); // Set flags based on path
                _ = InsertFileInfo(newFileInfo);
                return newFileInfo;
            }
        }


        public static List<FileInfo> GetAllFiles()
        {
            var fileInfos = new List<FileInfo>();

            using var connection = DbManager.Instance.GetConnection();

            using (var pragmaCommand = new SQLiteCommand("PRAGMA read_uncommitted = true;", connection))
            {
                _ = pragmaCommand.ExecuteNonQuery();
            }

            using var command = new SQLiteCommand(
                "SELECT DISTINCT FileID, Filename, RelativePath, DTStamp, HASH, Flags, AbsolutePath " +
                "FROM vwPluginFiles", connection);

            _ = command.Parameters.AddWithValue("@GameFolderFlag", (long)FileFlags.GameFolder);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var fileInfo = new FileInfo
                {
                    FileID = reader.GetInt64(0),
                    Filename = reader.GetString(1),
                    RelativePath = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    DTStamp = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    HASH = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    Flags = reader.IsDBNull(5) ? FileFlags.None : (FileFlags)reader.GetInt64(5),
                    AbsolutePath = reader.IsDBNull(6) ? string.Empty : reader.GetString(6)
                };
                fileInfos.Add(fileInfo);
            }

            return fileInfos;
        }


        public static string ComputeHash(string filePath)
        {
            const int bufferSize = 8 * 1024 * 1024; // 8MB buffer


            System.Threading.Thread.Sleep(10);

            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: bufferSize,
                useAsync: false);

            var hasher = new XxHash64();

            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                hasher.Append(buffer.AsSpan(0, bytesRead));
            }

            byte[] hashBytes = hasher.GetCurrentHash();

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }



        public override string ToString()
        {
            return $"FileID: {FileID}, Filename: {Filename}, RelativePath: {RelativePath}, DTStamp: {DTStamp}, HASH: {HASH}, Flags: {Flags}, AbsolutePath: {AbsolutePath}";
        }

        public List<FileInfo> LoadFilesByPlugin(long pluginId)
        {
            var fileInfos = new List<FileInfo>();

            using var connection = DbManager.Instance.GetConnection();

            using (var pragmaCommand = new SQLiteCommand("PRAGMA read_uncommitted = true;", connection))
            {
                _ = pragmaCommand.ExecuteNonQuery();
            }

            using var command = new SQLiteCommand(
                "SELECT FileID, Filename, RelativePath, DTStamp, HASH, Flags, AbsolutePath " +
                "FROM vwPluginFiles WHERE PluginID = @PluginID", connection);

            _ = command.Parameters.AddWithValue("@PluginID", pluginId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var fileInfo = new FileInfo
                {
                    FileID = reader.GetInt64(0), // Ensure this matches the INTEGER type in the schema
                    Filename = reader.GetString(1), // Ensure this matches the TEXT type in the schema
                    RelativePath = reader.IsDBNull(2) ? string.Empty : reader.GetString(2), // Handle potential null values
                    DTStamp = reader.IsDBNull(3) ? string.Empty : reader.GetString(3), // Ensure this matches the TEXT type in the schema
                    HASH = reader.IsDBNull(4) ? string.Empty : reader.GetString(4), // Handle potential null values
                    Flags = reader.IsDBNull(5) ? FileFlags.None : (FileFlags)reader.GetInt64(5), // Handle potential null values
                    AbsolutePath = reader.IsDBNull(6) ? string.Empty : reader.GetString(6) // Ensure this matches the TEXT type in the schema
                };
                fileInfos.Add(fileInfo);
            }

            return fileInfos;
        }

        public static FileInfo InsertFileInfo(FileInfo fileInfo, long pluginId)
        {
            using var connection = DbManager.Instance.GetConnection();

#if WINDOWS
            App.LogDebug($"Fileinfo Begin Transaction");
#endif

            using var transaction = connection.BeginTransaction();

            try
            {
                using var command = new SQLiteCommand(connection);
                if (fileInfo.FileID == 0)
                {
                    command.CommandText = @"
                INSERT INTO FileInfo (PluginID, Filename, RelativePath, DTStamp, HASH, Flags, AbsolutePath)
                VALUES (@PluginID, @Filename, @RelativePath, @DTStamp, @HASH, @Flags, @AbsolutePath)
                ON CONFLICT(Filename) DO UPDATE 
                SET RelativePath = COALESCE(excluded.RelativePath, FileInfo.RelativePath), 
                    DTStamp = COALESCE(excluded.DTStamp, FileInfo.DTStamp), 
                    HASH = COALESCE(excluded.HASH, FileInfo.HASH), 
                    Flags = FileInfo.Flags | excluded.Flags,
                    AbsolutePath = COALESCE(excluded.AbsolutePath, FileInfo.AbsolutePath)";

                    command.Parameters.AddWithValue("@PluginID", pluginId);
                    command.Parameters.AddWithValue("@Filename", fileInfo.Filename);
                    command.Parameters.AddWithValue("@RelativePath", fileInfo.RelativePath ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@DTStamp", fileInfo.DTStamp);
                    command.Parameters.AddWithValue("@HASH", fileInfo.HASH ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Flags", (long)fileInfo.Flags);
                    command.Parameters.AddWithValue("@AbsolutePath", fileInfo.AbsolutePath);

                    fileInfo.FileID = Convert.ToInt64(command.ExecuteScalar());
                }
                else
                {
                    command.CommandText = @"
                    INSERT INTO FileInfo (PluginID, Filename, RelativePath, DTStamp, HASH, Flags, AbsolutePath)
                    VALUES (@PluginID, @Filename, @RelativePath, @DTStamp, @HASH, @Flags, @AbsolutePath)
                    ON CONFLICT(Filename) DO UPDATE 
                    SET RelativePath = COALESCE(excluded.RelativePath, FileInfo.RelativePath), 
                        DTStamp = COALESCE(excluded.DTStamp, FileInfo.DTStamp), 
                        HASH = COALESCE(excluded.HASH, FileInfo.HASH), 
                        Flags = COALESCE(FileInfo.Flags, 0) | COALESCE(excluded.Flags, 0),
                        AbsolutePath = COALESCE(excluded.AbsolutePath, FileInfo.AbsolutePath)";



                    command.Parameters.AddWithValue("@FileID", fileInfo.FileID);
                    command.Parameters.AddWithValue("@PluginID", pluginId);
                    command.Parameters.AddWithValue("@Filename", fileInfo.Filename);
                    command.Parameters.AddWithValue("@RelativePath", fileInfo.RelativePath ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@DTStamp", fileInfo.DTStamp);
                    command.Parameters.AddWithValue("@HASH", fileInfo.HASH ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Flags", (long)fileInfo.Flags);
                    command.Parameters.AddWithValue("@AbsolutePath", fileInfo.AbsolutePath);

                    command.ExecuteNonQuery();
                }

#if WINDOWS
                App.LogDebug($"Fileinfo Commit Transaction");
#endif

                transaction.Commit();
                return fileInfo;
            }
            catch (Exception ex)
            {
#if WINDOWS
                App.LogDebug($"Error inserting/updating file info: {ex.Message}");
#endif
                transaction.Rollback();
                throw;
            }
        }

        public static FileInfo InsertFileInfo(FileInfo fileInfo)
        {
            using var connection = DbManager.Instance.GetConnection();

#if WINDOWS
            App.LogDebug($"Fileinfo Begin Transaction");
#endif

            using var transaction = connection.BeginTransaction();

            try
            {
                using var command = new SQLiteCommand(connection);
                if (fileInfo.FileID == 0)
                {
                    command.CommandText = @"
                INSERT INTO FileInfo (Filename, RelativePath, DTStamp, HASH, Flags, AbsolutePath, FileContent)
                VALUES (@Filename, @RelativePath, @DTStamp, @HASH, @Flags, @AbsolutePath, @FileContent)
                ON CONFLICT(Filename) DO UPDATE 
                SET RelativePath = COALESCE(excluded.RelativePath, FileInfo.RelativePath), 
                    DTStamp = COALESCE(excluded.DTStamp, FileInfo.DTStamp), 
                    HASH = COALESCE(excluded.HASH, FileInfo.HASH), 
                    Flags = excluded.Flags,
                    AbsolutePath = excluded.AbsolutePath,
                    FileContent = excluded.FileContent";

                    command.Parameters.AddWithValue("@Filename", fileInfo.Filename);
                    command.Parameters.AddWithValue("@RelativePath", fileInfo.RelativePath ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@DTStamp", fileInfo.DTStamp);
                    command.Parameters.AddWithValue("@HASH", fileInfo.HASH ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Flags", (long)fileInfo.Flags);
                    command.Parameters.AddWithValue("@AbsolutePath", fileInfo.AbsolutePath);
                    command.Parameters.AddWithValue("@FileContent", fileInfo.CompressedContent ?? (object)DBNull.Value);

                    fileInfo.FileID = Convert.ToInt64(command.ExecuteScalar());
                }
                else
                {
                    command.CommandText = @"
                UPDATE FileInfo
                SET Filename = @Filename,
                    RelativePath = @RelativePath,
                    DTStamp = COALESCE(@DTStamp, DTStamp),
                    HASH = COALESCE(@HASH, HASH),
                    Flags = @Flags,
                    AbsolutePath = @AbsolutePath,
                    FileContent = @FileContent
                WHERE FileID = @FileID";

                    command.Parameters.AddWithValue("@FileID", fileInfo.FileID);
                    command.Parameters.AddWithValue("@Filename", fileInfo.Filename);
                    command.Parameters.AddWithValue("@RelativePath", fileInfo.RelativePath ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@DTStamp", fileInfo.DTStamp);
                    command.Parameters.AddWithValue("@HASH", fileInfo.HASH ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Flags", (long)fileInfo.Flags);
                    command.Parameters.AddWithValue("@AbsolutePath", fileInfo.AbsolutePath);
                    command.Parameters.AddWithValue("@FileContent", fileInfo.CompressedContent ?? (object)DBNull.Value);

                    command.ExecuteNonQuery();
                }

#if WINDOWS
                App.LogDebug($"Fileinfo Commit Transaction");
#endif

                transaction.Commit();
                return fileInfo;
            }
            catch (Exception ex)
            {
#if WINDOWS
                App.LogDebug($"Error inserting/updating file info: {ex.Message}");
#endif
                transaction.Rollback();
                throw;
            }
        }

        public void ReplaceFlags(FileFlags newFlags)
        {
            this.Flags = newFlags;
            UpdateFlagsInDatabase();
        }

        private void UpdateFlagsInDatabase()
        {
            using var connection = DbManager.Instance.GetConnection();
            using var command = new SQLiteCommand(connection);

            command.CommandText = @"
        UPDATE FileInfo
        SET Flags = @Flags
        WHERE FileID = @FileID";

            _ = command.Parameters.AddWithValue("@FileID", this.FileID);
            _ = command.Parameters.AddWithValue("@Flags", (long)this.Flags);

            _ = command.ExecuteNonQuery();
        }


        public static List<FileInfo> GetMonitoredFiles()
        {
            var monitoredFiles = new List<FileInfo>();

            using var connection = DbManager.Instance.GetConnection();

            using (var pragmaCommand = new SQLiteCommand("PRAGMA read_uncommitted = true;", connection))
            {
                _ = pragmaCommand.ExecuteNonQuery();
            }

            using var command = new SQLiteCommand(
                "SELECT FileID, Filename, RelativePath, DTStamp, HASH, Flags, AbsolutePath, FileContent " +
                "FROM FileInfo WHERE (Flags & @IsMonitoredFlag) = @IsMonitoredFlag", connection);

            _ = command.Parameters.AddWithValue("@IsMonitoredFlag", (long)FileFlags.IsMonitored);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var fileInfo = new FileInfo
                {
                    FileID = reader.GetInt64(0),
                    Filename = reader.GetString(1),
                    RelativePath = reader.IsDBNull(2) ? null : reader.GetString(2),
                    DTStamp = reader.GetString(3),
                    HASH = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Flags = (FileFlags)reader.GetInt32(5),
                    AbsolutePath = reader.IsDBNull(6) ? null : reader.GetString(6),
                    CompressedContent = reader.IsDBNull(7) ? null : (byte[])reader[7]
                };

                if (string.IsNullOrEmpty(fileInfo.AbsolutePath))
                {
                    fileInfo.AbsolutePath = fileInfo.GetPathByFlags(fileInfo.Flags, fileInfo.Filename);
                }

                if (fileInfo.CompressedContent != null)
                {
                    fileInfo.FileContent = DecompressFile(fileInfo.CompressedContent);
                }

                monitoredFiles.Add(fileInfo);
            }

            return monitoredFiles;
        }

        public override bool Equals(object obj)
        {
            if (obj is FileInfo other)
            {
                return this.FileID == other.FileID || this.Filename == other.Filename || this.HASH == other.HASH;
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + FileID.GetHashCode();
                hash = hash * 23 + Filename.GetHashCode();
                hash = hash * 23 + (HASH?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}

