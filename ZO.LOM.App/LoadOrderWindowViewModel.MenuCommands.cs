using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using ZO.LoadOrderManager;
using Timer = System.Timers.Timer;

namespace ZO.LoadOrderManager
{
    public partial class LoadOrderWindowViewModel : INotifyPropertyChanged
    {
        public ICommand ImportPluginsCommand { get; }
        public ICommand SaveAsNewLoadoutCommand { get; }
        
        public ICommand OpenGameFolderCommand { get; }
        public ICommand OpenGameSaveFolderCommand { get; }
        public ICommand OpenAppDataFolderCommand { get; }
        public ICommand OpenGameSettingsCommand { get; }
        public ICommand OpenPluginEditorCommand { get; }
        public ICommand OpenGroupEditorCommand { get; }
        public ICommand OpenGameLocalAppDataCommand { get; }
        
        public ICommand SettingsWindowCommand { get; }
        public ICommand ImportFromYamlCommand { get; }
        public ICommand EditPluginsCommand { get; }
        public ICommand EditContentCatalogCommand { get; }
        public ICommand ImportContextCatalogCommand { get; }
        
        public ICommand CopyTextCommand { get; }
        public ICommand DeleteCommand { get; }

        public ICommand ChangeGroupCommand { get; }
        public ICommand ToggleEnableCommand { get; }


        private void ImportPlugins(AggLoadInfo? aggLoadInfo = null, string? pluginsFile = null)
        {
            if (SelectedItem != null)
            {
                StatusMessage = SelectedItem.ToString();
            }
            // If no AggLoadInfo is provided, use the singleton instance
            aggLoadInfo ??= AggLoadInfo.Instance;

            // Ensure the selected loadout is set in the AggLoadInfo object
            if (SelectedLoadOut != null)
            {
                aggLoadInfo.ActiveLoadOut = SelectedLoadOut;
            }
            else
            {
                throw new InvalidOperationException("No loadout selected for importing plugins.");
            }

            // Perform the import
            FileManager.ParsePluginsTxt(AggLoadInfo.Instance, pluginsFile);

            // Update the UI or any other necessary components
            RefreshData();
        }

        private void SaveAsNewLoadout()
        {
            //var inputDialog = new InputDialog("Enter the name for the new LoadOut:", "New LoadOut");
            //if (inputDialog.ShowDialog() == true)
            //{
            //    var newProfileName = inputDialog.ResponseText;

            //    try
            //    {
            //        var newLoadOut = new LoadOut(newProfileName, SelectedLoadOut);
            //        newLoadOut.WriteProfile();
            //        LoadOuts.Add(newLoadOut);

            //        MessageBox.Show("New loadout saved successfully.", "Save As New Loadout", MessageBoxButton.OK, MessageBoxImage.Information);
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //}
        }

        private void OpenGameFolder()
        {
            OpenFolder(FileManager.GameFolder);
        }

        private void OpenGameSaveFolder()
        {
            OpenFolder(FileManager.GameSaveFolder);
        }

        private void OpenGameSettings()
        {
            OpenFolder(FileManager.GameDocsFolder);
        }

        private void OpenPluginEditor()
        {
            if (SelectedItem is Plugin plugin)
            {
                var editorWindow = new PluginEditorWindow(plugin, SelectedLoadOut);
                if (editorWindow.ShowDialog() == true)
                {
                    RefreshData();
                }
            }
        }

        private void OpenGroupEditor()
        {

            if (SelectedItem is ModGroup modGroup)
            {
                var editorWindow = new ModGroupEditorWindow(modGroup);
                if (editorWindow.ShowDialog() == true)
                {
                    RefreshData();
                }
            }

        }

        private void OpenAppDataFolder()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZeeOgre", "LoadOrderManager");
            OpenFolder(appDataPath);
        }

        private void OpenGameLocalAppData()
        {
            string gameAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "starfield");
            OpenFolder(gameAppDataPath);
        }

        private void OpenFolder(string path)
        {
            try
            {
                _ = Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                App.LogDebug($"Exception in OpenFolder: {ex.Message}");
                _ = MessageBox.Show("An error occurred while trying to open the folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenFile(string path)
        {
            try
            {
                _ = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                App.LogDebug($"Exception in OpenFile: {ex.Message}");
                _ = MessageBox.Show("An error occurred while trying to open the file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void EditPlugins()
        {
            string pluginsFilePath = FileManager.PluginsFile ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "starfield", "plugins.txt");
            OpenFile(pluginsFilePath);
        }

        private void EditContentCatalog()
        {
            string contentCatalogPath = FileManager.ContentCatalogFile ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "starfield", "ContentCatalog.txt");
            OpenFile(contentCatalogPath);
        }

        private void ImportContextCatalog()
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "starfield"),
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Select ContentCatalog.txt file"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var selectedFile = openFileDialog.FileName;
                FileManager.ParseContentCatalogTxt(selectedFile);

                _ = MessageBox.Show("Content catalog imported successfully.", "Import Content Catalog", MessageBoxButton.OK, MessageBoxImage.Information);

                RefreshData();
            }
        }
        
        private void SettingsWindow()

        {
            try
            {
                var settingsWindow = new SettingsWindow(SettingsLaunchSource.MainWindow)
                {
                    Tag = "Settings"
                };
                _ = settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                App.LogDebug($"Exception in SettingsWindow_Click: {ex.Message}");
                _ = MessageBox.Show("An error occurred while trying to open the settings window.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CopyText()
        {
            if (SelectedItem is LoadOrderItemViewModel selectedItem)
            {
                var underlyingObject = EntityTypeHelper.GetUnderlyingObject(selectedItem);
                string textToCopy = underlyingObject?.ToString() ?? string.Empty;
                Clipboard.SetText(textToCopy);
            }
        }

        private bool CanExecuteCopyText(object parameter)
        {
            return SelectedItem is LoadOrderItemViewModel selectedItem &&
                   (selectedItem.EntityType == EntityType.Group || selectedItem.EntityType == EntityType.Plugin);
        }

        private void Delete()
        {
            if (SelectedItem is LoadOrderItemViewModel selectedItem)
            {
                var parentGroup = selectedItem.GetParentGroup();
                if (parentGroup != null)
                {
                    if (selectedItem.EntityType == EntityType.Plugin)
                    {
                        // Adjust ordinals of subsequent sibling plugins
                        var siblingPlugins = parentGroup.Plugins?
                            .Where(p => p.GroupOrdinal > selectedItem.PluginData.GroupOrdinal)
                            .ToList();
                        if (siblingPlugins != null)
                        {
                            foreach (var sibling in siblingPlugins)
                            {
                                sibling.GroupOrdinal--;
                            }
                        }

                        // Remove the plugin from the group
                        parentGroup.Plugins?.Remove(selectedItem.PluginData);

                        // Move the plugin to the unassigned group (-997)
                        MoveToUnassignedGroup(selectedItem.PluginData);
                    }
                    else if (selectedItem.EntityType == EntityType.Group)
                    {
                        // Check if the group has child groups
                        if (selectedItem.Children.Any(child => child.EntityType == EntityType.Group))
                        {
                            // Block deleting groups that hold other groups
                            MessageBox.Show("Cannot delete a group that contains other groups.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // Adjust ordinals of subsequent sibling groups
                        var siblingGroups = AggLoadInfo.Instance.Groups?
                            .Where(g => g.ParentID == parentGroup.GroupID && g.Ordinal > selectedItem.GetModGroup().Ordinal)
                            .ToList();
                        if (siblingGroups != null)
                        {
                            foreach (var sibling in siblingGroups)
                            {
                                sibling.Ordinal--;
                            }
                        }

                        // Move all child plugins to the unassigned group (-997)
                        foreach (var child in selectedItem.Children)
                        {
                            if (child.EntityType == EntityType.Plugin)
                            {
                                MoveToUnassignedGroup(child.PluginData);
                            }
                        }

                        // Remove the group from the parent group's children
                        parentGroup.Plugins?.Remove(selectedItem.PluginData);
                    }
                }
            }
        }

        private void ChangeGroup(object parameter)
        {
            if (SelectedItem is ModGroup modGroup)
            {
                modGroup.ChangeGroup((long)parameter); // Cast parameter to long
            }
            else if (SelectedItem is Plugin plugin)
            {
                plugin.ChangeGroup((long)parameter); // Cast parameter to long
            }
        }

        private bool CanExecuteChangeGroup(object parameter) { return true; }

        private void ToggleEnable(object parameter)
        {
            Debug.WriteLine($"Parameter Type: {parameter?.GetType().Name}");
            Debug.WriteLine($"Parameter Value: {parameter}");

            if (SelectedLoadOut != null && parameter is LoadOrderItemViewModel itemViewModel)
            {
                // Record the old state for debugging
                Debug.WriteLine($"OldState: {itemViewModel.IsActive}");

                // Calculate the new state and log it
                bool newState = itemViewModel.IsActive;
                Debug.WriteLine($"NewState: {newState}");

                // Set the new state to the UI-bound property
                Application.Current.Dispatcher.Invoke(() =>
                {
                    itemViewModel.IsActive = newState;
                });

                // Update the backend data
                LoadOut.SetPluginEnabled(SelectedLoadOut.ProfileID, itemViewModel.PluginData.PluginID, newState);
            }
            else
            {
                UpdateStatus("No loadout or valid item selected.");
            }
        }



        private bool CanExecuteToggleEnable(object parameter)
        {

            //Debug.WriteLine($"Parameter Type: {parameter?.GetType().Name}");
            //Debug.WriteLine($"Parameter Value: {parameter}");

            if (SelectedLoadOut != null && parameter is LoadOrderItemViewModel itemViewModel)
            {
                return itemViewModel.GroupID > 0;

            }
            return true;
        }


        private bool CanExecuteDelete(object parameter)
        {
            return SelectedItem is LoadOrderItemViewModel selectedItem &&
                   (selectedItem.EntityType == EntityType.Group || selectedItem.EntityType == EntityType.Plugin);
        }

        private void ImportFromYaml()
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = FileManager.AppDataFolder,
                Filter = "YAML files (*.yaml)|*.yaml|All files (*.*)|*.*",
                Title = "Select YAML file"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var selectedFile = openFileDialog.FileName;
                try
                {
                    _ = Config.LoadFromYaml(selectedFile);
                    _ = MessageBox.Show("Configuration loaded successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    App.LogDebug($"Exception in ImportFromYaml_Click: {ex.Message}");
                    _ = MessageBox.Show("An error occurred while loading the configuration.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

    }
}