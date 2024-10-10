using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using ZO.LoadOrderManager;
using Timer = System.Timers.Timer;

namespace ZO.LoadOrderManager
{
    public partial class LoadOrderWindowViewModel : INotifyPropertyChanged
    {
        // ICommands
        public ICommand SaveCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand SavePluginsCommand { get; }
        public ICommand SaveLoadOutCommand { get; }

        private void Save(object? parameter)
        {
            if (SelectedLoadOut != null)
            {
                SelectedLoadOut.WriteProfile();
                UpdateStatus("Profile saved successfully.");
            }
            else
            {
                UpdateStatus("No loadout selected.");
            }
        }

        private void SaveCurrentState() => Save(this);

        private bool CanExecuteSave()
        {
            return SelectedLoadOut != null && SelectedGroupSet != null;
        }

        public bool CanMoveUp()
        {
            // Check if the current item is a LoadOrderItemViewModel and if GroupSetID equals 1
            if (!(SelectedItems.FirstOrDefault() is LoadOrderItemViewModel loadOrderItem) || loadOrderItem.GroupSetID == 1)
                return false;

            // Normal check for move up if not from cached and GroupSetID is not 1
            var selectedItems = SelectedItems.OfType<LoadOrderItemViewModel>().ToList();
            var lowestParentID = selectedItems.Min(item => item.ParentID);

            var firstItem = selectedItems.FirstOrDefault(item => item.ParentID == lowestParentID);
            if (firstItem != null)
            {
                if (firstItem.EntityType == EntityType.Group)
                {
                    return AggLoadInfo.Instance.GroupSetGroups.Items
                        .Any(g => g.groupID == firstItem.GroupID && g.Ordinal > 1);
                }
                else if (firstItem.EntityType == EntityType.Plugin)
                {
                    return AggLoadInfo.Instance.GroupSetPlugins.Items
                        .Any(p => p.pluginID == firstItem.PluginData.PluginID && p.Ordinal > 1);
                }
            }

            return false;
        }

        public bool CanMoveDown()
        {
            // Check if the current item is a LoadOrderItemViewModel and if GroupSetID equals 1
            if (!(SelectedItems.FirstOrDefault() is LoadOrderItemViewModel loadOrderItem) || loadOrderItem.GroupSetID == 1)
                return false;

            // Normal check for move down if not from cached and GroupSetID is not 1
            var selectedItems = SelectedItems.OfType<LoadOrderItemViewModel>().ToList();
            var lowestParentID = selectedItems.Min(item => item.ParentID);

            var lastItem = selectedItems.LastOrDefault(item => item.ParentID == lowestParentID);
            if (lastItem != null)
            {
                if (lastItem.EntityType == EntityType.Group)
                {
                    return AggLoadInfo.Instance.GroupSetGroups.Items
                        .Any(g => g.groupID == lastItem.GroupID && g.Ordinal < AggLoadInfo.Instance.GroupSetGroups.Items.Max(gr => gr.Ordinal));
                }
                else if (lastItem.EntityType == EntityType.Plugin)
                {
                    return AggLoadInfo.Instance.GroupSetPlugins.Items
                        .Any(p => p.pluginID == lastItem.PluginData.PluginID && p.Ordinal < AggLoadInfo.Instance.GroupSetPlugins.Items.Max(pl => pl.Ordinal));
                }
            }

            return false;
        }



        //private void MoveUp(LoadOrderItemViewModel loadOrderItem)
        //{
        //    var underlyingObject = EntityTypeHelper.GetUnderlyingObject(loadOrderItem);

        //    if (underlyingObject is ModGroup modGroup)
        //    {
        //        var previousItem = AggLoadInfo.Instance.Groups
        //            .FirstOrDefault(g => g.ParentID == modGroup.ParentID && g.Ordinal == modGroup.Ordinal - 1);

        //        if (previousItem != null)
        //        {
        //            // Swap locations
        //            modGroup.SwapLocations(previousItem);

        //            // Refresh all data to update the ViewModel
        //            AggLoadInfo.Instance.RefreshAllData();
        //        }
        //    }
        //    else if (underlyingObject is Plugin plugin)
        //    {
        //        var previousItem = AggLoadInfo.Instance.Plugins
        //            .FirstOrDefault(p => p.GroupID == plugin.GroupID && p.GroupOrdinal == plugin.GroupOrdinal - 1);

        //        if (previousItem != null)
        //        {
        //            // Swap locations
        //            plugin.SwapLocations(previousItem);

        //            // Refresh all data to update the ViewModel
        //            AggLoadInfo.Instance.RefreshAllData();
        //        }
        //    }
        //}


        //private void MoveDown(LoadOrderItemViewModel loadOrderItem)
        //{
        //    var underlyingObject = EntityTypeHelper.GetUnderlyingObject(loadOrderItem);

        //    if (underlyingObject is ModGroup modGroup)
        //    {
        //        var nextItem = AggLoadInfo.Instance.Groups
        //            .FirstOrDefault(g => g.ParentID == modGroup.ParentID && g.Ordinal == modGroup.Ordinal + 1);

        //        if (nextItem != null)
        //        {
        //            // Swap locations
        //            modGroup.SwapLocations(nextItem);

        //            // Refresh all data to update the ViewModel
        //            AggLoadInfo.Instance.RefreshAllData();
        //        }
        //    }
        //    else if (underlyingObject is Plugin plugin)
        //    {
        //        var nextItem = AggLoadInfo.Instance.Plugins
        //            .FirstOrDefault(p => p.GroupID == plugin.GroupID && p.GroupOrdinal == plugin.GroupOrdinal + 1);

        //        if (nextItem != null)
        //        {
        //            // Swap locations
        //            plugin.SwapLocations(nextItem);

        //            // Refresh all data to update the ViewModel
        //            AggLoadInfo.Instance.RefreshAllData();
        //        }
        //    }
        //}
        private void MoveUp(LoadOrderItemViewModel loadOrderItem)
        {
            var selectedItems = SelectedItems.OfType<LoadOrderItemViewModel>().ToList();

            // Find the lowest ParentID from the selected items
            var lowestParentID = selectedItems.Min(item => item.ParentID);

            // Filter the items that match the lowest ParentID
            var itemsToMove = selectedItems.Where(item => item.ParentID == lowestParentID).ToList();

            var firstItem = itemsToMove.FirstOrDefault();
            if (firstItem != null)
            {
                if (firstItem.EntityType == EntityType.Group)
                {
                    // Find the previous sibling for ModGroup using GroupSetGroups (gsg)
                    var groupTuple = AggLoadInfo.Instance.GroupSetGroups.Items
                        .FirstOrDefault(g => g.groupID == firstItem.GroupID && g.groupSetID == firstItem.GroupSetID);

                    if (groupTuple != default)
                    {
                        var previousTuple = AggLoadInfo.Instance.GroupSetGroups.Items
                            .FirstOrDefault(g => g.parentID == groupTuple.parentID && g.Ordinal == groupTuple.Ordinal - 1);

                        if (previousTuple != default)
                        {
                            var previousModGroup = ModGroup.LoadModGroup(previousTuple.groupID, previousTuple.groupSetID);
                            ((ModGroup)firstItem.UnderlyingObject).SwapLocations(previousModGroup);
                        }
                    }
                }
                else if (firstItem.EntityType == EntityType.Plugin)
                {
                    // Find the previous sibling for Plugin using GroupSetPlugins (gsp)
                    var pluginTuple = AggLoadInfo.Instance.GroupSetPlugins.Items
                        .FirstOrDefault(p => p.pluginID == firstItem.PluginData.PluginID && p.groupSetID == firstItem.GroupSetID);

                    if (pluginTuple != default)
                    {
                        var previousPluginTuple = AggLoadInfo.Instance.GroupSetPlugins.Items
                            .FirstOrDefault(p => p.groupID == pluginTuple.groupID && p.Ordinal == pluginTuple.Ordinal - 1);

                        if (previousPluginTuple != default)
                        {
                            var previousPlugin = Plugin.LoadPlugin(previousPluginTuple.pluginID, null, previousPluginTuple.groupSetID);
                            ((Plugin)firstItem.UnderlyingObject).SwapLocations(previousPlugin);
                        }
                    }
                }
            }

            // Refresh all data to update the ViewModel after the move
            AggLoadInfo.Instance.RefreshAllData();
        }

        private void MoveDown(LoadOrderItemViewModel loadOrderItem)
        {
            var selectedItems = SelectedItems.OfType<LoadOrderItemViewModel>().ToList();

            // Find the lowest ParentID from the selected items
            var lowestParentID = selectedItems.Min(item => item.ParentID);

            // Filter the items that match the lowest ParentID
            var itemsToMove = selectedItems.Where(item => item.ParentID == lowestParentID).ToList();

            var lastItem = itemsToMove.LastOrDefault();
            if (lastItem != null)
            {
                if (lastItem.EntityType == EntityType.Group)
                {
                    // Find the next sibling for ModGroup using GroupSetGroups (gsg)
                    var groupTuple = AggLoadInfo.Instance.GroupSetGroups.Items
                        .FirstOrDefault(g => g.groupID == lastItem.GroupID && g.groupSetID == AggLoadInfo.Instance.ActiveGroupSet.GroupSetID);

                    if (groupTuple != default)
                    {
                        var nextTuple = AggLoadInfo.Instance.GroupSetGroups.Items
                            .FirstOrDefault(g => g.parentID == groupTuple.parentID && g.Ordinal == groupTuple.Ordinal + 1);

                        if (nextTuple != default)
                        {
                            var nextModGroup = ModGroup.LoadModGroup(nextTuple.groupID, nextTuple.groupSetID);
                            ((ModGroup)lastItem.UnderlyingObject).SwapLocations(nextModGroup);
                        }
                    }
                }
                else if (lastItem.EntityType == EntityType.Plugin)
                {
                    // Find the next sibling for Plugin using GroupSetPlugins (gsp)
                    var pluginTuple = AggLoadInfo.Instance.GroupSetPlugins.Items
                        .FirstOrDefault(p => p.pluginID == lastItem.PluginData.PluginID && p.groupSetID == AggLoadInfo.Instance.ActiveGroupSet.GroupSetID);

                    if (pluginTuple != default)
                    {
                        var nextPluginTuple = AggLoadInfo.Instance.GroupSetPlugins.Items
                            .FirstOrDefault(p => p.groupID == pluginTuple.groupID && p.Ordinal == pluginTuple.Ordinal + 1);

                        if (nextPluginTuple != default)
                        {
                            var nextPlugin = Plugin.LoadPlugin(nextPluginTuple.pluginID, null, nextPluginTuple.groupSetID);
                            ((Plugin)lastItem.UnderlyingObject).SwapLocations(nextPlugin);
                        }
                    }
                }
            }

            // Refresh all data to update the ViewModel after the move
            AggLoadInfo.Instance.RefreshAllData();
        }


        public ObservableCollection<LoadOrderItemViewModel> GetFlattenedList(EntityType entityType)
        {
            var flattenedList = new List<LoadOrderItemViewModel>();

            System.Diagnostics.Debug.WriteLine($"Flattening list for EntityType: {entityType}");

            foreach (var item in LoadOrders.Items)
            {
                FlattenItem(item, entityType, flattenedList);
            }

            // Simplified sorting using exposed properties Ordinal and ParentID/GroupID
            var orderedList = flattenedList
                .OrderBy(i => i.ParentID)  // Sort by ParentID first to maintain group hierarchy
                .ThenBy(i => i.Ordinal)    // Then sort by Ordinal within each parent
                .ToList();

            System.Diagnostics.Debug.WriteLine($"Flattened list contains {orderedList.Count} items after sorting.");

            foreach (var item in orderedList)
            {
                System.Diagnostics.Debug.WriteLine($"Flattened Item: {item.DisplayName} | PluginID: {item.PluginData?.PluginID}, GroupID: {item.GroupID}");
            }

            return new ObservableCollection<LoadOrderItemViewModel>(orderedList);
        }


        private void FlattenItem(LoadOrderItemViewModel item, EntityType entityType, List<LoadOrderItemViewModel> flattenedList)
        {
            // Add the item to the flattened list if it matches the entity type
            if (item.EntityType == entityType)
            {
                flattenedList.Add(item);
                System.Diagnostics.Debug.WriteLine($"Added {item.DisplayName} to flattened list.");
            }

            // Recursively process child items only for groups (because plugins don't have children)
            if (item.EntityType == EntityType.Group && item.Children != null && item.Children.Any())
            {
                foreach (var child in item.Children)
                {
                    FlattenItem(child, entityType, flattenedList);
                }
            }
        }



        public ICommand AddNewGroupSetCommand => new RelayCommand(_ => AddNewGroupSet());
        public ICommand AddNewLoadoutCommand => new RelayCommand(_ => AddNewLoadout());
        public ICommand CompareCommand => new RelayCommand(_ => Compare());

        private void Compare()
        {
            // Launch the Diff Viewer with null parameters to trigger file pickers
            var diffViewer = new DiffViewer((string?)null, (string?)null);
            diffViewer.ShowDialog();
        }


        private LoadOut AddNewLoadout(GroupSet? groupSet = null)
        {
            // Step 1: Use the provided groupSet or fall back to the active one
            groupSet ??= AggLoadInfo.Instance.ActiveGroupSet;

            // Create a default name for the new LoadOut
            var newLoadOutName = $"NEW_LO_{groupSet.GroupSetName}";
            var dialog = new InputDialog("Enter the name for the new LoadOut:", newLoadOutName);
            if (dialog.ShowDialog() == true)
            {
                newLoadOutName = dialog.ResponseText;
            }

            // Create the new LoadOut
            LoadOut newLoadOut = new LoadOut(groupSet) { Name = newLoadOutName };

            // Add to the LoadOuts of the GroupSet
            groupSet.LoadOuts.Add(newLoadOut);

            // Set it as the active LoadOut if appropriate
            if (AggLoadInfo.Instance.ActiveLoadOut == null || AggLoadInfo.Instance.ActiveLoadOut.GroupSetID != groupSet.GroupSetID)
            {
                AggLoadInfo.Instance.ActiveLoadOut = newLoadOut;
            }

            // Save the new LoadOut to the database
            newLoadOut.WriteProfile();

            // Optionally refresh the UI or data context
            //OnPropertyChanged(nameof(AggLoadInfo.Instance.LoadOuts));
            
            return newLoadOut;
        }


        private void AddNewGroupSet()
        {
            // Create a new GroupSetEditor window with a new GroupSet (no arguments)
            var groupSetEditor = new GroupSetEditor();

            // Show the dialog for the new GroupSetEditor
            groupSetEditor.ShowDialog();

            // Optionally, refresh the AggLoadInfo instance or the view as needed
            AggLoadInfo.Instance.RefreshAllData();
        }


        private void SwapLocations(LoadOrderItemViewModel item1, LoadOrderItemViewModel item2)
        {
            var underlyingObject1 = EntityTypeHelper.GetUnderlyingObject(item1);
            var underlyingObject2 = EntityTypeHelper.GetUnderlyingObject(item2);

            if (underlyingObject1 is ModGroup modGroup1 && underlyingObject2 is ModGroup modGroup2)
            {
                modGroup1.SwapLocations(modGroup2);
            }
            else if (underlyingObject1 is Plugin plugin1 && underlyingObject2 is Plugin plugin2)
            {
                plugin1.SwapLocations(plugin2);
            }

            OnPropertyChanged(nameof(Items));
        }

        private void SavePlugins()
        {
            var groupSetName = SelectedGroupSet.GroupSetName;
            var profileName = SelectedLoadOut.Name;
            var defaultFilePath = FileManager.PluginsFile;

            var result = MessageBox.Show($"Producing {defaultFilePath}. Do you want to save to a different location?", "Save Plugins", MessageBoxButton.YesNo);

            string? outputFileName = null;
            if (result == MessageBoxResult.Yes)
            {
                var defaultFileName = $"Plugins_{groupSetName}_{profileName}.txt";
                defaultFilePath = Path.Combine(FileManager.GameLocalAppDataFolder, defaultFileName);


                var saveFileDialog = new SaveFileDialog
                {
                    FileName = defaultFileName,
                    DefaultExt = ".txt",
                    Filter = "Text documents (.txt)|*.txt",
                    InitialDirectory = FileManager.AppDataFolder
                };

                bool? dialogResult = saveFileDialog.ShowDialog();
                if (dialogResult == true)
                {
                    outputFileName = saveFileDialog.FileName;
                }
            }
            else if (result == MessageBoxResult.No)
            {
                outputFileName = defaultFilePath;
            }

            FileManager.ProducePluginsTxt(LoadOrders, outputFileName);
            StatusMessage = "Plugins.txt file has been successfully created.";
        }

        private bool CanSavePlugins()
        {
            return true;
        }
    }
}
