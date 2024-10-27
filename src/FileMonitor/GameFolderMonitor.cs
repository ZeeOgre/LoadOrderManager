using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZO.LoadOrderManager.src.FileMonitor
{
    public class GameFolderMonitor
    {
        private FileSystemWatcher _watcher;
        private static readonly string gameFolder = Config.Instance.GameFolder;

        public GameFolderMonitor()
        {
            _watcher = new FileSystemWatcher(gameFolder)
            {
                Filter = "*.esm|*.esp", // Watch for .esp and .esm files
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            _watcher.Created += OnNewPluginFileDetected;
            _watcher.EnableRaisingEvents = true;
        }

        private async void OnNewPluginFileDetected(object sender, FileSystemEventArgs e)
        {
            string pluginFile = e.FullPath;
            string pluginName = Path.GetFileName(pluginFile);
            LoadOrderWindow.Instance.LOWVM.UpdateStatus($"Adding Files for {pluginName}");
            App.LogDebug($"New plugin file detected: {pluginName}");

            // Create a new FileInfo object
            var newFileInfo = new FileInfo(pluginFile)
            {
                Flags = FileFlags.Plugin | FileFlags.GameFolder,
                DTStamp = File.GetLastWriteTime(pluginFile).ToString("o")
            };
            newFileInfo.HASH = FileInfo.ComputeHash(pluginFile);

            // Insert the FileInfo object
            FileInfo.InsertFileInfo(newFileInfo);

            // Process affiliated files
            App.LogDebug($"Processing affiliated files for {pluginName}");

            
            await Task.Run(() => FileManager.AddAffiliatedFiles(new System.IO.FileInfo(pluginFile), newFileInfo.FileID, true));
            LoadOrderWindow.Instance.LOWVM.ClearWarning();
        }

        public void StopMonitoring()
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
        }
    }

}
