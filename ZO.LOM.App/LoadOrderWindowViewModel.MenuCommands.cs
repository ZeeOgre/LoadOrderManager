using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        //public ICommand OpenPluginEditorCommand { get; }
        //public ICommand OpenGroupEditorCommand { get; }
        public ICommand OpenGameLocalAppDataCommand { get; }
        
        public ICommand SettingsWindowCommand { get; }
        public ICommand ImportFromYamlCommand { get; }
        public ICommand EditPluginsCommand { get; }
        public ICommand EditContentCatalogCommand { get; }
        public ICommand ImportContextCatalogCommand { get; }
        public ICommand ScanGameFolderCommand { get; }

        public ICommand CopyTextCommand { get; }
        public ICommand DeleteCommand { get; }

        public ICommand ChangeGroupCommand { get; }
        public ICommand ToggleEnableCommand { get; }

        public IEnumerable<ModGroup> ValidGroups
        {
            get
            {
                // Ensure the selected item exists and necessary data is available
                if (SelectedItem == null || AggLoadInfo.Instance == null || SelectedGroupSet == null)
                    return Enumerable.Empty<ModGroup>();

                // Cast SelectedItem to LoadOrderItemViewModel to access necessary properties
                if (!(SelectedItem is LoadOrderItemViewModel loadOrderItem))
                    return Enumerable.Empty<ModGroup>();

                // Get all groups within the SelectedGroupSet
                var allGroups = AggLoadInfo.Instance.Groups
                                  .Where(g => g.GroupSetID == SelectedGroupSet.GroupSetID);

                // Exclude groups where the selected item (group/plugin) is already assigned
                var currentParentOrGroupID = loadOrderItem.ParentID; // ParentID for groups or GroupID for plugins

                // Return groups excluding the one where the item is already contained
                return allGroups.Where(g => g.GroupID != currentParentOrGroupID).Distinct();
            }
        }




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
        

        private void ScanGameFolder()
        {
            FileManager.ScanGameDirectoryForStrays();
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


        private void HandleMultipleSelectedItems(Action<LoadOrderItemViewModel> action)
        {
            if (SelectedItems == null || SelectedItems.Count == 0)
                return;

            foreach (var item in SelectedItems)
            {
                if (item is LoadOrderItemViewModel viewModelItem)
                {
                    action(viewModelItem); // Apply the action to each item
                }
            }
        }



        private bool CanExecuteCheckAllItems()
        {
            // If no items are selected, return false
            if (SelectedItems == null || SelectedItems.Count == 0)
                return false;

            // Ensure all selected items meet the condition
            foreach (var item in SelectedItems)
            {
                if (!(item is LoadOrderItemViewModel))
                {
                    return false; // If any item is not a LoadOrderItemViewModel, return false
                }
            }

            return true; // All items are valid, return true
        }



        private void CopyText(LoadOrderItemViewModel item)
        {
            // Your logic for copying a single item's text goes here
            Clipboard.SetText(item.ToString()); // Example action
        }



        private bool CanExecuteCopyText()
        {
            // If no items are selected, the command cannot execute
            if (SelectedItems == null || SelectedItems.Count == 0)
                return false;

            // Ensure all selected items meet the condition
            foreach (var item in SelectedItems)
            {
                if (item is LoadOrderItemViewModel viewModelItem)
                {
                    if (viewModelItem.EntityType != EntityType.Group && viewModelItem.EntityType != EntityType.Plugin)
                    {
                        return false; // If any item doesn't meet the condition, disable the command
                    }
                }
            }

            // All items meet the condition, enable the command
            return true;
        }






        private void Delete(LoadOrderItemViewModel selectedItem)
        {
            var parentGroup = selectedItem.GetParentGroup();
            if (parentGroup != null)
            {
                if (selectedItem.EntityType == EntityType.Plugin)
                {
                    Plugin plugin = selectedItem.PluginData;
                    MoveToUnassignedGroup(plugin);
                }
                else if (selectedItem.EntityType == EntityType.Group)
                {
                    // Block deleting groups that hold other groups
                    if (selectedItem.Children.Any(child => child.EntityType == EntityType.Group))
                    {
                        MessageBox.Show("Cannot delete a group that contains other groups.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Adjust ordinals of sibling groups and move child plugins to unassigned group
                    AdjustSiblingGroupsAndMoveChildPlugins(selectedItem, parentGroup);
                }
            }

            AggLoadInfo.Instance.RefreshAllData();
        }


        private void AdjustSiblingGroupsAndMoveChildPlugins(LoadOrderItemViewModel selectedItem, ModGroup parentGroup)
        {
            var siblingGroups = AggLoadInfo.Instance.Groups?
                .Where(g => g.ParentID == parentGroup.GroupID && g.Ordinal > selectedItem.GetModGroup().Ordinal)
                .ToList();
            if (siblingGroups != null)
            {
                foreach (var sibling in siblingGroups)
                {
                    sibling.Ordinal--;
                    sibling.WriteGroup();
                }
            }

            foreach (var child in selectedItem.Children)
            {
                if (child.EntityType == EntityType.Plugin)
                {
                    MoveToUnassignedGroup(child.PluginData);
                }
            }

            parentGroup.Plugins?.Remove(selectedItem.PluginData);
        }


        private void ChangeGroup(LoadOrderItemViewModel item, object parameter)
        {
            if (parameter is long newGroupId)
            {
                var underlyingObject = EntityTypeHelper.GetUnderlyingObject(item);

                if (underlyingObject is ModGroup modGroup)
                {
                    modGroup.ChangeGroup(newGroupId);
                }
                else if (underlyingObject is Plugin plugin)
                {
                    plugin.ChangeGroup(newGroupId);
                }

                AggLoadInfo.Instance.RefreshAllData();
            }
            else
            {
                throw new ArgumentException("Parameter must be a long representing the new group ID.", nameof(parameter));
            }
        }



        private bool CanExecuteChangeGroup(object parameter) { return true; }

        private void ToggleEnable(LoadOrderItemViewModel itemViewModel, object sender)
        {
            if (SelectedLoadOut == null)
            {
                UpdateStatus("No loadout selected.");
                return;
            }

            // Retrieve the Tag property to determine the source (checkbox or right-click menu)
            if (sender is FrameworkElement element && element.Tag is string tag)
            {
                bool isCheckbox = tag == "checkbox";

                // Record the old state for debugging
                Debug.WriteLine($"OldState: {itemViewModel.IsActive}");

                // Determine the new state based on whether this is a checkbox toggle or right-click menu
                bool newState = isCheckbox ? itemViewModel.IsActive : !itemViewModel.IsActive;

                Debug.WriteLine($"NewState: {newState}");

                // Set the new state to the UI-bound property
                itemViewModel.IsActive = newState;

                // Update the backend data (the database and in-memory LoadOut)
                LoadOut.SetPluginEnabled(SelectedLoadOut.ProfileID, itemViewModel.PluginData.PluginID, newState);

                // Notify the UI to refresh the view
                OnPropertyChanged(nameof(LoadOuts));
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