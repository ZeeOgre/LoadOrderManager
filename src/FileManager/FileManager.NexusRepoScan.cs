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
            int totalFiles = modList.Count;
            int currentFileIndex = 0;
            foreach (var mod in modList)
             
            {
                currentFileIndex++;
                long progress = 95 + (4 * currentFileIndex / totalFiles);
                if (InitializationManager.IsAnyInitializing()) InitializationManager.ReportProgress(progress, $"({currentFileIndex}/{totalFiles}) Adding file info for {mod.Name}");
                MWMessage($"Updating {mod.Name}", false);
                var modFolder = Path.Combine(_repoFolder, mod.VortexId);
                if (!Directory.Exists(modFolder))
                {
                    continue;
                }

                var esmFiles = Directory.GetFiles(modFolder, "*.esm", SearchOption.AllDirectories);
                var espFiles = Directory.GetFiles(modFolder, "*.esp", SearchOption.AllDirectories);
                var pluginFiles = esmFiles.Concat(espFiles).ToList();
                long? pluginId = null;
                foreach (var pluginFile in pluginFiles)
                {
                    
                    var plugin = Plugin.LoadPlugin(null, Path.GetFileName(pluginFile).ToLowerInvariant());
                    if (plugin != null)
                    {
                        // Update existing plugin
                        plugin.NexusID = mod.ModId.ToString();
                        plugin.Description = mod.Name;
                        plugin.State |= ModState.Nexus | ModState.ModManager; // Add Nexus and ModManager flags
                        plugin.WriteMod();
                        AggLoadInfo.Instance.UpdatePlugin(plugin);
                    }
                    else
                    {
                        // Create new plugin using the constructor that takes a System.IO.FileInfo object
                        var newPlugin = new Plugin(new System.IO.FileInfo(pluginFile))
                        {
                            Description = mod.Name,
                            NexusID = mod.ModId.ToString(),
                            GroupID = -997,
                            GroupSetID = 1,
                            GroupOrdinal = AggLoadInfo.GetNextPluginOrdinal(-997, 1),
                            InGameFolder = false,
                            State = ModState.Nexus | ModState.ModManager, // Set Nexus and ModManager flags
                        };

                        // Edit the created FileInfo object
                        if (newPlugin.Files != null && newPlugin.Files.Count > 0)
                        {
                            var fileInfo = newPlugin.Files[0];
                            fileInfo.RelativePath = null;
                            fileInfo.AbsolutePath = pluginFile;
                        }

                        pluginId = newPlugin.WriteMod().PluginID;
                        newPlugin.PluginID = pluginId ?? throw new InvalidOperationException("PluginID is null.");
                        AggLoadInfo.Instance.Plugins.Add(newPlugin);
                    }

                    // Add affiliated files
                    AddAffiliatedFiles(new System.IO.FileInfo(pluginFile), plugin?.PluginID ?? pluginId.Value, true);
                    AddModFolderFiles(modFolder, plugin?.PluginID ?? pluginId.Value);
                }



            }
            MWClear();
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
                    fileFlags = FileFlags.IsArchive | FileFlags.GameFolder;
                }
                else if (fileName.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
                {
                    fileFlags = FileFlags.GameFolder;
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
