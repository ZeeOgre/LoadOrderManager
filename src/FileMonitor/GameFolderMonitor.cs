using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using ZO.LoadOrderManager;
using zoFileInfo  = ZO.LoadOrderManager.FileInfo;

public class GameFolderMonitor : IDisposable
{
    private static readonly List<GameFolderMonitor> _monitors = new List<GameFolderMonitor>(); // Prevent garbage collection
    private FileSystemWatcher _esmWatcher;
    private FileSystemWatcher _espWatcher;
    private static readonly string gameFolder = Path.Combine(Config.Instance.GameFolder, "Data");

    private bool _disposed = false;

    // Singleton instance
    private static GameFolderMonitor _instance;

    // Static reference to keep the instance alive
    private static GameFolderMonitor _persistentReference;

    // Constructor is private to enforce singleton
    private GameFolderMonitor()
    {
        App.LogDebug($"Initializing GameFolderMonitor for folder: {gameFolder}");

        // Watcher for .esm files
        _esmWatcher = new FileSystemWatcher(gameFolder)
        {
            Filter = "*.esm",
            NotifyFilter = NotifyFilters.FileName // Monitor file existence
        };
        _esmWatcher.Created += OnNewPluginFileDetected;
        _esmWatcher.Deleted += OnPluginFileDeleted;
        _esmWatcher.EnableRaisingEvents = true;

        // Watcher for .esp files
        _espWatcher = new FileSystemWatcher(gameFolder)
        {
            Filter = "*.esp",
            NotifyFilter = NotifyFilters.FileName // Monitor file existence
        };
        _espWatcher.Created += OnNewPluginFileDetected;
        _espWatcher.Deleted += OnPluginFileDeleted;
        _espWatcher.EnableRaisingEvents = true;

        _monitors.Add(this); // Prevent GC

        App.LogDebug("GameFolderMonitor initialized.");
    }

    public static GameFolderMonitor Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameFolderMonitor();
                _persistentReference = _instance; // Keep a static reference to prevent GC
            }
            return _instance;
        }
    }

    private async void OnNewPluginFileDetected(object sender, FileSystemEventArgs e)
    {
        string pluginFile = e.FullPath;
        string pluginName = Path.GetFileName(pluginFile).ToLowerInvariant();
        App.LogDebug($"New plugin file detected: {pluginName}");

        try
        {
            // Check if the plugin already exists
            var existingPlugin = Plugin.LoadPlugin(null, pluginName, AggLoadInfo.Instance.ActiveGroupSet.GroupSetID);
            if (existingPlugin != null)
            {
                // Update flags and timestamps
                existingPlugin.InGameFolder = true;
                existingPlugin.DTStamp = File.GetLastWriteTime(pluginFile).ToString("o");
                existingPlugin.WriteMod();
                AggLoadInfo.Instance.UpdatePlugin(existingPlugin);

                var files = new ObservableCollection<zoFileInfo>(new zoFileInfo().LoadFilesByPlugin(existingPlugin.PluginID));
                foreach (var file in files)
                {
                    file.ReplaceFlags(file.Flags | FileFlags.GameFolder);
                    file.HASH = zoFileInfo.ComputeHash(pluginFile); // Recalculate hash
                }
            }
            else
            {
                // Create a new plugin if it doesn't exist
                var newPlugin = new Plugin
                {
                    PluginName = pluginName,
                    DTStamp = File.GetLastWriteTime(pluginFile).ToString("o"),
                    GroupID = -997,
                    GroupSetID = AggLoadInfo.Instance.ActiveGroupSet.GroupSetID,  // Default GroupSetID
                    GroupOrdinal = AggLoadInfo.GetNextPluginOrdinal(-997, AggLoadInfo.Instance.ActiveGroupSet.GroupSetID),
                    InGameFolder = true
                };
                newPlugin.WriteMod();
                AggLoadInfo.Instance.Plugins.Add(newPlugin);

                // Create and insert FileInfo
                var newFileInfo = new zoFileInfo(pluginFile)
                {
                    Flags = FileFlags.Plugin | FileFlags.GameFolder,
                    DTStamp = newPlugin.DTStamp,
                    HASH = zoFileInfo.ComputeHash(pluginFile),
                    AbsolutePath = pluginFile
                };
                zoFileInfo.InsertFileInfo(newFileInfo, newPlugin.PluginID);

                // Process affiliated files
                App.LogDebug($"Processing affiliated files for {pluginName}");
                await Task.Run(() => FileManager.AddAffiliatedFiles(new System.IO.FileInfo(pluginFile), newFileInfo.FileID, true));
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                LoadOrderWindow.Instance.LOWVM.UpdateStatus($"Found and added {pluginName}");
                LoadOrderWindow.Instance.LOWVM.LoadOrders.RefreshData(); // Clear the warning after scan completion
            });
        }
        catch (Exception ex)
        {
            App.LogDebug($"Error in OnNewPluginFileDetected: {ex.Message}");
        }
    }

    private async void OnPluginFileDeleted(object sender, FileSystemEventArgs e)
    {
        string pluginFile = e.FullPath;
        string pluginName = Path.GetFileName(pluginFile).ToLowerInvariant();
        App.LogDebug($"Plugin file deleted: {pluginName}");

        try
        {
            // Check if the plugin exists
            var existingPlugin = Plugin.LoadPlugin(null, pluginName, AggLoadInfo.Instance.ActiveGroupSet.GroupSetID);
            if (existingPlugin != null)
            {
                // Remove the InGameFolder flag
                existingPlugin.InGameFolder = false;
                //existingPlugin.WriteMod();
                existingPlugin.WriteMod();
                var files = new ObservableCollection<zoFileInfo>(new zoFileInfo().LoadFilesByPlugin(existingPlugin.PluginID));
                foreach (var file in files)
                {
                    file.ReplaceFlags(file.Flags & ~FileFlags.GameFolder);
                }
                AggLoadInfo.Instance.UpdatePlugin(existingPlugin);
            }

            

            Application.Current.Dispatcher.Invoke(() =>
            {
                LoadOrderWindow.Instance.LOWVM.UpdateStatus($"{pluginFile} was removed from the Game Folder");
                LoadOrderWindow.Instance.LOWVM.LoadOrders.RefreshData(); // Clear the warning after scan completion
            });
        }
        catch (Exception ex)
        {
            App.LogDebug($"Error in OnPluginFileDeleted: {ex.Message}");
        }
    }

    public void StartMonitoring()
    {
        _esmWatcher.EnableRaisingEvents = true;
        _espWatcher.EnableRaisingEvents = true;
    }

    public void StopMonitoring()
    {
        _esmWatcher.EnableRaisingEvents = false;
        _espWatcher.EnableRaisingEvents = false;
    }

    public static void InitializeAllMonitors()
    {
        App.LogDebug("Initializing GameFolderMonitor...");
        GameFolderMonitor.Instance.StartMonitoring();
    }

    public static void StopAllMonitors()
    {
        GameFolderMonitor.Instance.StopMonitoring();
    }

    ~GameFolderMonitor()
    {
        App.LogDebug($"GameFolderMonitor for folder {gameFolder} is being garbage collected.");
    }

    public void Dispose()
    {
        App.LogDebug($"Disposing GameFolderMonitor for {gameFolder}.");
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _esmWatcher?.Dispose();
                _espWatcher?.Dispose();
            }
            _disposed = true;
        }
    }
}
