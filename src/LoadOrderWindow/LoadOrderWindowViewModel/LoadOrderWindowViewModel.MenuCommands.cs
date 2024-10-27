using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

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
                // Ensure selected items exist and necessary data is available
                if (SelectedItems == null || AggLoadInfo.Instance == null || SelectedGroupSet == null)
                    return Enumerable.Empty<ModGroup>();

                // Cast SelectedItems to a collection of LoadOrderItemViewModel
                var selectedItems = SelectedItems.OfType<LoadOrderItemViewModel>().ToList();

                if (!selectedItems.Any())
                    return Enumerable.Empty<ModGroup>();

                // Get all groups within the SelectedGroupSet
                var allGroups = AggLoadInfo.Instance.Groups
                                  .Where(g => g.GroupSetID == SelectedGroupSet.GroupSetID);

                // Get all ParentIDs from the selected items
                var parentIDs = selectedItems.Select(item => item.ParentID).Distinct();

                // Exclude groups where any selected item is already assigned
                return allGroups.Where(g => !parentIDs.Contains(g.GroupID))
                    .Distinct(); //Only get one
                                 // Put them in order by group
            }
        }



        private void SaveAsNewLoadout()
        {
            if (SelectedLoadOut == null)
            {
                _ = MessageBox.Show("No loadout selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Prompt the user for a new loadout name, pre-filling with the existing name appended with "_new"
            var dialog = new InputDialog("Enter a new name for the loadout:", SelectedLoadOut.Name + "_new");
            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.ResponseText))
            {
                _ = MessageBox.Show("Loadout name cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var newLoadoutName = dialog.ResponseText;

            // Create a new blank loadout
            var newLoadout = new LoadOut
            {
                Name = newLoadoutName,
                GroupSetID = SelectedLoadOut.GroupSetID,
                enabledPlugins = new ObservableHashSet<long>()
            };

            // Copy the enabled plugins from the selected loadout
            foreach (var plugin in SelectedLoadOut.enabledPlugins)
            {
                _ = newLoadout.enabledPlugins.Add(plugin);
            }

            // Save the new loadout to the database
            _ = newLoadout.WriteProfile();

            // Add the new loadout to AggLoadInfo.Instance
            AggLoadInfo.Instance.LoadOuts.Add(newLoadout);

            // Set the new loadout as the active loadout
            AggLoadInfo.Instance.ActiveLoadOut = newLoadout;

            // Refresh the UI   
            OnPropertyChanged(nameof(LoadOuts));
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

        private bool AskUserForConfirmation(string message)
        {
            var result = MessageBox.Show(message, "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        private void ImportPlugins(AggLoadInfo? aggLoadInfo = null, string? pluginsFile = null)
        {

            // If no AggLoadInfo is provided, ask the user if they want to set up a new GroupSet
            if (aggLoadInfo == null)
            {
                bool isNewGroupSet = AskUserForConfirmation("Do you want to import into a new GroupSet?");
                if (isNewGroupSet)
                {
                    // Create a new GroupSet
                    GroupSet newGroupSet = GroupSet.CreateEmptyGroupSet();
                    aggLoadInfo = new AggLoadInfo(newGroupSet.GroupSetID);
                }
                else
                {
                    // Use the singleton instance if no new GroupSet is created
                    aggLoadInfo = AggLoadInfo.Instance;
                }
            }

            // Ask the user if they want to import into a new LoadOut
            bool isNewLoadOut = AskUserForConfirmation("Do you want to import into a new LoadOut?");
            if (isNewLoadOut)
            {
                // Prompt the user for a new loadout name
                var dialog = new InputDialog("Enter the name for your new LoadOut", "NewLoadOut");
                if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.ResponseText))
                {
                    _ = MessageBox.Show("Loadout name cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var newLoadoutName = dialog.ResponseText;

                // Create a new LoadOut with the provided name
                LoadOut newLoadOut = new LoadOut(aggLoadInfo.ActiveGroupSet) { Name = newLoadoutName };
                aggLoadInfo.ActiveLoadOut = newLoadOut;
            }

            // Perform the import
            _ = FileManager.ParsePluginsTxt(aggLoadInfo, pluginsFile);

            // Update the UI or any other necessary components
            RefreshData();
        }


        private void ImportContentCatalog()
        {

            FileManager.ParseContentCatalogTxt(FileManager.ContentCatalogFile, AggLoadInfo.Instance.ActiveGroupSet.GroupSetID);
            //var openFileDialog = new OpenFileDialog
            //{
            //    InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "starfield"),
            //    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            //    Title = "Select ContentCatalog.txt file"
            //};

            //if (openFileDialog.ShowDialog() == true)
            //{
            //    var selectedFile = openFileDialog.FileName;
            //    FileManager.ParseContentCatalogTxt(selectedFile);

            //    _ = MessageBox.Show("Content catalog imported successfully.", "Import Content Catalog", MessageBoxButton.OK, MessageBoxImage.Information);

            //    RefreshData();
            //}
            RefreshData();
        }


        void ScanGameFolder()
        {
            ImportContentCatalog();
            // Prompt the user to choose between a full scan and a quick scan
            var result = MessageBox.Show("Do you want to perform a full scan?", "Scan Type", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            // Check the user's choice
            if (result == MessageBoxResult.Yes)
            {
                // Perform a full scan
                FileManager.ScanGameDirectoryForStrays(fullScan: true,AggLoadInfo.Instance.ActiveGroupSet.GroupSetID);
            }
            else if (result == MessageBoxResult.No)
            {
                // Perform a quick scan
                FileManager.ScanGameDirectoryForStrays(fullScan: false, AggLoadInfo.Instance.ActiveGroupSet.GroupSetID);
            }
            else
            {
                // User canceled the operation
                _ = MessageBox.Show("Scan operation canceled.", "Canceled", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            RefreshData();
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
                        _ = MessageBox.Show("Cannot delete a group that contains other groups.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    _ = sibling.WriteGroup();
                }
            }

            foreach (var child in selectedItem.Children)
            {
                if (child.EntityType == EntityType.Plugin)
                {
                    MoveToUnassignedGroup(child.PluginData);
                }
            }

            _ = (parentGroup.Plugins?.Remove(selectedItem.PluginData));
        }

        private bool CanChangeGroup(LoadOrderItemViewModel item)
        {
            var underlyingObject = EntityTypeHelper.GetUnderlyingObject(item);

            if (underlyingObject is ModGroup && item.GroupID <= 1)
            {
                return false; // Cannot change group if it's a group and GroupID <= 1
            }
            return true; // Can change group otherwise
        }


        private void ChangeGroup(LoadOrderItemViewModel item, object parameter)
        {
            if (!CanChangeGroup(item))
            {
                // Optionally, you can show a message to the user indicating why the action cannot be performed
                MessageBox.Show($"Cannot change group for this {item}.");
                return;
            }

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
                item.ParentID = newGroupId;
            }
            else
            {
                throw new ArgumentException("Parameter must be a long representing the new group ID.", nameof(parameter));
            }
            RefreshData();
        }




        //private void ToggleActive(LoadOrderItemViewModel itemViewModel, object sender)
        //{
        //    if (SelectedLoadOut == null)
        //    {
        //        UpdateStatus("No loadout selected.");
        //        return;
        //    }

        //    // Retrieve the Tag property to determine the source (checkbox or right-click menu)
        //    //if (sender is FrameworkElement element && element.Tag is string tag)
        //    //{
        //    //    bool isCheckbox = tag == "checkbox";

        //        // Record the old state for debugging
        //        Debug.WriteLine($"OldState: {itemViewModel.IsActive}");

        //        // Determine the new state based on whether this is a checkbox toggle or right-click menu
        //        //bool newState = isCheckbox ? itemViewModel.IsActive : !itemViewModel.IsActive;
        //        bool newState = !itemViewModel.IsActive;
        //        Debug.WriteLine($"NewState: {newState}");

        //        // Set the new state to the UI-bound property
        //        itemViewModel.IsActive = newState;

        //        // Update the backend data (the database and in-memory LoadOut)
        //        LoadOut.SetPluginEnabled(SelectedLoadOut.ProfileID, itemViewModel.PluginData.PluginID, newState);

        //        // Notify the UI to refresh the view
        //        OnPropertyChanged(nameof(LoadOuts));
        //    //}
        //}


        private void ToggleActive(LoadOrderItemViewModel itemViewModel, object sender)
        {
            if (SelectedLoadOut == null)
            {
                UpdateStatus("No loadout selected.");
                return;
            }

            if (!CanExecuteToggleActive(itemViewModel))
            {
                UpdateStatus("Cannot toggle this group.");
                return;
            }

            // Toggle the new state
            bool newState = !itemViewModel.IsActive;

            // Set the new state
            itemViewModel.IsActive = newState;

            // If the item is a group, recursively toggle all its child items
            if (itemViewModel.EntityType == EntityType.Group)
            {
                ToggleChildrenRecursively(itemViewModel, newState);
            }
            else if (itemViewModel.EntityType == EntityType.Plugin)
            {
                // Update the backend for the plugin
                LoadOut.SetPluginEnabled(SelectedLoadOut.ProfileID, itemViewModel.PluginData.PluginID, newState);
            }

            // Notify the UI to refresh the view
            OnPropertyChanged(nameof(LoadOuts));
        }

        /// <summary>
        /// Recursively toggles the child items of a group to match the group's new state.
        /// </summary>
        /// <param name="parentItem">The parent group whose children need to be toggled.</param>
        /// <param name="newState">The new state to apply to the group's children.</param>
        private void ToggleChildrenRecursively(LoadOrderItemViewModel parentItem, bool newState)
        {
            if (parentItem.Children == null || !parentItem.Children.Any())
                return;

            foreach (var childItem in parentItem.Children)
            {
                // For plugins, toggle IsActive and update the backend
                if (childItem.EntityType == EntityType.Plugin)
                {
                    childItem.IsActive = newState;
                    LoadOut.SetPluginEnabled(SelectedLoadOut.ProfileID, childItem.PluginData.PluginID, newState);
                }
                // For nested groups, toggle the group and recursively toggle its children
                else if (childItem.EntityType == EntityType.Group)
                {
                    childItem.IsActive = newState;
                    ToggleChildrenRecursively(childItem, newState);
                }
            }
        }


        private bool CanExecuteToggleActive(object parameter)
        {
            if (SelectedLoadOut != null && parameter is LoadOrderItemViewModel itemViewModel)
            {
                return itemViewModel.GroupID > 1 || itemViewModel.GroupID == -997;  // Validate based on GroupID
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
