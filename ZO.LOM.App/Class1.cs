using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows;

public class FileMonitor
{
    private FileSystemWatcher _watcher;
    private string _filePath;
    private string _lastHash;

    public FileMonitor(string filePath)
    {
        _filePath = filePath;
        _lastHash = ComputeFileHash(filePath);

        _watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath))
        {
            Filter = Path.GetFileName(filePath),
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Renamed += OnFileChanged;
        _watcher.Deleted += OnFileChanged;
        _watcher.EnableRaisingEvents = true;
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

                // Prompt the user
                MessageBoxResult result = MessageBox.Show("The file has been modified. Would you like to take action?", "File Modified", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    // Take the desired action here
                    HandleFileChange();
                }
            }
        }
    }

    private string ComputeFileHash(string filePath)
    {
        using (var sha256 = SHA256.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                var hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    private void HandleFileChange()
    {
        // Custom logic to handle the file change
        MessageBox.Show("File has been changed. Handling...");
    }
}
