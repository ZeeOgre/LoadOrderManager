using Microsoft.Win32;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Windows;


namespace ZO.LoadOrderManager
{

    public class FileMonitor
    {
        private static readonly List<FileMonitor> _monitors = new List<FileMonitor>();
        private static DiffViewer? _currentDiffViewer;

        private FileSystemWatcher _watcher;
        private string _filePath;
        private string _lastHash;
        private byte[] _lastContent;

        public FileMonitor(string filePath, byte[] initialContent)
        {
            _filePath = filePath;
            _lastHash = ComputeFileHash(filePath);
            _lastContent = initialContent;

            _watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath))
            {
                Filter = Path.GetFileName(filePath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Renamed += OnFileChanged;
            _watcher.Deleted += OnFileChanged;
            _watcher.EnableRaisingEvents = true;

            // Add this instance to the static list to prevent garbage collection
            _monitors.Add(this);
        }

        ~FileMonitor()
        {
            Stop();
            App.LogDebug($"FileMonitor for {_filePath} is being garbage collected.");
            Console.WriteLine($"FileMonitor for {_filePath} is being garbage collected.");
        }

        public void Stop()
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _ = _monitors.Remove(this);
            App.LogDebug($"FileMonitor for {_filePath} has been stopped.");
            Console.WriteLine($"FileMonitor for {_filePath} has been stopped.");
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Renamed)
            {
                // Compute the new file hash and compare
                string newHash = ComputeFileHash(_filePath);
                if (newHash != _lastHash)
                {
                    _lastHash = newHash;
                    byte[] newContent = File.ReadAllBytes(_filePath);

                    // Launch the DiffViewer
                    LaunchDiffViewer(_lastContent, newContent);

                    // Update the last content
                    _lastContent = newContent;
                }
            }
        }

        private string ComputeFileHash(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        private void LaunchDiffViewer(byte[] oldContent, byte[] newContent)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_currentDiffViewer == null || !_currentDiffViewer.IsVisible)
                {
                    _currentDiffViewer = new DiffViewer(oldContent, newContent);
                    _currentDiffViewer.Closed += (s, e) => _currentDiffViewer = null;
                    _currentDiffViewer.Show();
                }
                else
                {
                    // Optionally, you can bring the existing DiffViewer to the front
                    _ = _currentDiffViewer.Activate();
                }
            });
        }

        public static void InitializeAllMonitors()
        {
            try
            {
                List<FileInfo> monitoredFiles = FileInfo.GetMonitoredFiles();
                foreach (var file in monitoredFiles)
                {
                    try
                    {
                        string resolvedPath = ResolveFilePath(file.AbsolutePath);

                        if (file.FileContent != null)
                        {
                            // Check if the file and its hash match the database
                            string currentHash = FileInfo.ComputeHash(resolvedPath);
                            if (file.HASH != currentHash)
                            {
                                file.HASH = currentHash;
                                file.FileContent = File.ReadAllBytes(resolvedPath);
                                file.CompressedContent = CompressFile(file.FileContent);
                                file.DTStamp = File.GetLastWriteTime(resolvedPath).ToString("yyyy-MM-dd HH:mm:ss");
                                _ = FileInfo.InsertFileInfo(file);
                            }

                            _ = new FileMonitor(resolvedPath, file.FileContent);
                        }
                        else
                        {
                            // Log or handle the case where FileContent is null
                            Console.WriteLine($"FileContent is null for file: {file}");

                            // Create a new FileInfo object for the target file
                            var newFileInfo = new FileInfo
                            {
                                AbsolutePath = resolvedPath,
                                Filename = file.Filename,
                                DTStamp = File.GetLastWriteTime(resolvedPath).ToString("yyyy-MM-dd HH:mm:ss"),
                                FileContent = File.ReadAllBytes(resolvedPath),
                                HASH = FileInfo.ComputeHash(resolvedPath),
                                CompressedContent = CompressFile(File.ReadAllBytes(resolvedPath)),
                                Flags = file.Flags | FileFlags.IsMonitored
                            };

                            // Update the database with the new FileInfo
                            _ = FileInfo.InsertFileInfo(newFileInfo);

                            // Initialize the FileMonitor with the new FileInfo
                            _ = new FileMonitor(newFileInfo.AbsolutePath, newFileInfo.FileContent);
                            App.LogDebug($"New FileMonitor for {newFileInfo.AbsolutePath} established");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log or handle the exception for this specific file
                        Console.WriteLine($"An error occurred while initializing file monitor for {file.Filename}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle the general exception
                Console.WriteLine($"An error occurred while initializing file monitors: {ex.Message}");
            }
        }

        private static byte[] CompressFile(byte[] fileContent)
        {
            using var outputStream = new MemoryStream();
            using (var compressionStream = new GZipStream(outputStream, CompressionMode.Compress))
            {
                compressionStream.Write(fileContent, 0, fileContent.Length);
            }
            return outputStream.ToArray();
        }

        private static string ResolveFilePath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
            {
                // Replace environment variables if present
                fullPath = Environment.ExpandEnvironmentVariables(fullPath);

                if (!File.Exists(fullPath))
                {
                    // Prompt the user with a WPF file picker dialog
                    var openFileDialog = new OpenFileDialog
                    {
                        FileName = Path.GetFileName(fullPath),
                        Title = $"Select the {fullPath} file for monitoring"
                    };

                    if (openFileDialog.ShowDialog() == true)
                    {
                        fullPath = openFileDialog.FileName;
                    }
                    else
                    {
                        throw new FileNotFoundException($"{fullPath} File not found and user did not select a file.");
                    }
                }
            }

            return fullPath;
        }
    }
}
