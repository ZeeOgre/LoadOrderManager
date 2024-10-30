using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Formats.Tar;
using System.Data.SQLite;
using System;
using System.Diagnostics;
using MessageBox = System.Windows.MessageBox;
using System.Reflection;

namespace ZO.LoadOrderManager
{
    partial class FileManager
    {
        static readonly string _repoFolder = Config.Instance?.ModManagerRepoFolder ?? throw new InvalidOperationException("ModManagerRepoFolder is not configured.");
        static readonly string _modMetaDataFile = Path.Combine(_repoFolder, "nexus_modlist.json");
        
        public static string ModMetaDataFile => _modMetaDataFile;

        

        public static void UpdatePluginsFromModList(bool quiet = false)
        {
            _quiet = quiet;
            if (string.IsNullOrEmpty(Config.Instance.ModManagerRepoFolder) || !Directory.Exists(Config.Instance.ModManagerRepoFolder) || !File.Exists(FileManager.ModMetaDataFile))
            {
                if (!InitializationManager.IsAnyInitializing()) MessageBox.Show("Mod list file not found. Save the Modlist Backup: this game as nexus_modlist.json to the mod staging folder", "Mod list not found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            MWMessage($"Updating plugins from mod list...{_modMetaDataFile}",true);
            MWMessage($"Updating plugins from mod list...{_modMetaDataFile}", false);
            var modList = NexusModItem.LoadModList(_modMetaDataFile);
            var knownFiles = ZO.LoadOrderManager.FileInfo.GetAllFiles()
.GroupBy(f => f.Filename, StringComparer.OrdinalIgnoreCase)
.Select(g => g.First())
.ToDictionary(f => f.Filename, StringComparer.OrdinalIgnoreCase);
            
var knownFileNames = new HashSet<string>(knownFiles.Select(f => f.Value.Filename));
            
            int totalFiles = modList.Count;
            int currentFileIndex = 0;
            var AggGroupSetID = AggLoadInfo.Instance.ActiveGroupSet.GroupSetID;

            foreach (var mod in modList)
             
            {
                currentFileIndex++;
                long progress = (long)(100 * ((double)currentFileIndex / totalFiles));
                if (InitializationManager.IsAnyInitializing()) InitializationManager.ReportProgress(progress, $"({currentFileIndex}/{totalFiles}) Adding file info for {mod.Name}");
                MWMessage($"Updating {mod.Name} ({currentFileIndex}/{totalFiles})", false);
                var modFolder = Path.Combine(_repoFolder, mod.VortexId);
                if (!Directory.Exists(modFolder))
                {
                    continue;
                }

                var esmFiles = Directory.GetFiles(modFolder, "*.esm", SearchOption.AllDirectories);
                var espFiles = Directory.GetFiles(modFolder, "*.esp", SearchOption.AllDirectories);
                var pluginFiles = esmFiles.Concat(espFiles).ToList();
                
                long? pluginId = null;
                var Ordinal997 = AggLoadInfo.GetNextPluginOrdinal(-997, AggGroupSetID);
                foreach (var pluginFile in pluginFiles)
                {
                    Plugin plugin = null;
                    var fileInfo = new System.IO.FileInfo(pluginFile);
                    var dtStamp = fileInfo.LastWriteTime.ToString("o");
                    string? newHash = null;

                    var pluginName = fileInfo.Name;
                    

                    if (knownFiles.TryGetValue(pluginName, out var existingFileInfo))
                    {

                        var existingPlugin = AggLoadInfo.Instance.Plugins.FirstOrDefault(p => p.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
                        bool coreFile = existingPlugin != null && (existingPlugin.GroupID == -999);
                        if (existingPlugin != null)
                        {

                            existingPlugin.DTStamp = dtStamp;
                            //existingPlugin.InGameFolder = true;
                            existingPlugin.NexusID = mod.ModId.ToString();
                            existingPlugin.Description = mod.Name;
                            existingPlugin.State |= ModState.Nexus | ModState.ModManager;   
                            _ = existingPlugin.WriteMod();

                            if (!coreFile) existingFileInfo.HASH = ZO.LoadOrderManager.FileInfo.ComputeHash(pluginFile);
                            existingFileInfo.Flags |= FileFlags.Plugin;
                            existingFileInfo.AbsolutePath = fileInfo.FullName;
                            _ = ZO.LoadOrderManager.FileInfo.InsertFileInfo(existingFileInfo, existingPlugin.PluginID);


                            // Check for affiliated archives
                            AddAffiliatedFiles(fileInfo, existingPlugin.PluginID, !coreFile);
                            plugin = existingPlugin;
                        }
                    }
                    else
                    {
                        newHash = ZO.LoadOrderManager.FileInfo.ComputeHash(fileInfo.FullName);
                        // Create new plugin using the constructor that takes a System.IO.FileInfo object
                        var newPlugin = new Plugin
                        {
                            PluginName = pluginName,
                            Description = mod.Name,
                            NexusID = mod.ModId.ToString(),
                            DTStamp = dtStamp,
                            GroupID = -997,
                            GroupSetID = AggGroupSetID,
                            GroupOrdinal = Ordinal997++,
                            InGameFolder = false,
                            State = ModState.Nexus | ModState.ModManager, // Set Nexus and ModManager flags
                        };
                        _ = newPlugin.WriteMod();
                        AggLoadInfo.Instance.Plugins.Add(newPlugin);

                        var newFileInfo = new ZO.LoadOrderManager.FileInfo
                        {
                            Filename = pluginName,
                            DTStamp = dtStamp,
                            HASH = newHash,
                            Flags = FileFlags.Plugin,
                            AbsolutePath = fileInfo.FullName,
                            RelativePath = Path.GetRelativePath(GameFolder, fileInfo.FullName)
                        };
                        _ = ZO.LoadOrderManager.FileInfo.InsertFileInfo(newFileInfo, newPlugin.PluginID);

                        plugin = newPlugin;
                    }

                    // Add affiliated files
                    //AddAffiliatedFiles(new System.IO.FileInfo(pluginFile), plugin?.PluginID ?? pluginId.Value, true);
                    AddModFolderFiles(modFolder, plugin?.PluginID ?? pluginId.Value);
                }



            }
            AggLoadInfo.Instance.RefreshAllData();
            Application.Current.Dispatcher.Invoke(() =>
            {
                LoadOrderWindow.Instance.LOWVM.UpdateStatus("Completed Filescan");
                MWClear();
                LoadOrderWindow.Instance.LOWVM.LoadOrders.RefreshData(); // Clear the warning after scan completion
            });
        }

        public static void AddModFolderFiles(string modFolder, long pluginId)
        {
            // Retrieve all known files for the plugin
            var knownFiles = new FileInfo().LoadFilesByPlugin(pluginId);
            var knownFileNames = new HashSet<string>(knownFiles.Select(f => f.Filename));

            // Get all files in the mod folder
            var modFiles = Directory.GetFiles(modFolder, "*", SearchOption.AllDirectories);

            foreach (var modFile in modFiles)
            {
                var fileName = Path.GetFileName(modFile);
                // Change the method call to use an instance of FileInfo instead of calling it statically
                var fileInfoInstance = new ZO.LoadOrderManager.FileInfo();
                
                if (knownFileNames.Contains(fileName.ToLowerInvariant()))
                {
                    // Skip known files
                    continue;
                }

                // Classify the file
                FileFlags fileFlags = FileFlags.None;
                if (fileName.EndsWith(".esp", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".esm", StringComparison.OrdinalIgnoreCase))
                {
                    fileFlags = FileFlags.Plugin;
                }
                else if (fileName.EndsWith(".ba2", StringComparison.OrdinalIgnoreCase))
                {
                    fileFlags = FileFlags.IsArchive;
                }
                else if (fileName.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
                {
                    fileFlags = FileFlags.Config;
                }

                // Create FileInfo object
                var fileInfo = new ZO.LoadOrderManager.FileInfo
                {
                    Filename = fileName,
                    RelativePath = Path.GetRelativePath(modFolder, modFile),
                    DTStamp = File.GetLastWriteTime(modFile).ToString("o"),
                    HASH = ZO.LoadOrderManager.FileInfo.ComputeHash(modFile),
                    Flags = fileFlags,
                    AbsolutePath = modFile
                };

                // Insert the file info into the database
                ZO.LoadOrderManager.FileInfo.InsertFileInfo(fileInfo, pluginId);
            }
        }
    }
}
