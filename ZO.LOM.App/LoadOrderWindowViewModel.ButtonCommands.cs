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
using System.Windows.Controls;
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
        public ICommand EditGroupSetCommand { get; }
        public ICommand EditLoadOutCommand { get; }



        private void ExecuteEditGroupSetCommand(object parameter)
        {
            // Attempt to cast the parameter to a ComboBox
            var comboBox = parameter as ComboBox;

            // If the parameter is a ComboBox, use its SelectedItem, otherwise fallback to SelectedGroupSet
            GroupSet targetGroupSet = comboBox?.SelectedItem as GroupSet ?? SelectedGroupSet;

            // Ensure we have a target group set to edit
            if (targetGroupSet == null)
            {
                MessageBox.Show("Please select a GroupSet to edit.", "Edit GroupSet", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create and open the GroupSetEditor window
            var groupSetEditor = new GroupSetEditor(targetGroupSet);
            bool? dialogResult = groupSetEditor.ShowDialog();

            // If editing was successful, refresh the data
            if (dialogResult == true)
            {
                AggLoadInfo.Instance.RefreshAllData();
                OnPropertyChanged(nameof(GroupSets));
            }
        }

        private bool CanExecuteEditGroupSetCommand(object parameter)
        {
            // Ensure a GroupSet is selected for editing
            return SelectedGroupSet != null;
        }

        private void ExecuteEditLoadOutCommand(object parameter)
        {
            // Attempt to cast the parameter to a ComboBox
            var comboBox = parameter as ComboBox;

            // If the parameter is a ComboBox, use its SelectedItem, otherwise fallback to SelectedLoadOut
            LoadOut targetLoadOut = comboBox?.SelectedItem as LoadOut ?? SelectedLoadOut;

            // Ensure we have a target loadout to edit
            if (targetLoadOut == null)
            {
                MessageBox.Show("Please select a LoadOut to edit.", "Edit LoadOut", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create and open the LoadOutEditor window

            var loadOutEditor = new LoadOutEditor(targetLoadOut);
            bool? dialogResult = loadOutEditor.ShowDialog();

            // If editing was successful, refresh the data
            if (dialogResult == true)
            {
                SelectedLoadOut = loadOutEditor.GetLoadOut();

                AggLoadInfo.Instance.RefreshMetadataFromDB();
                OnPropertyChanged(nameof(LoadOuts));
            }
        }

        private bool CanExecuteEditLoadOutCommand(object parameter)
        {
            // Logic to determine if EditLoadOutCommand can execute
            return true;
        }
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
            
            if (SelectedItems is null || SelectedItems.Count == 0 || !IsUiEnabled) return false;
            if (!(SelectedItem is LoadOrderItemViewModel loadOrderItem) || loadOrderItem.GroupSetID == 1 || loadOrderItem.GroupID < 1 )
                return false;
            Debug.WriteLine($"SelectedItem: {loadOrderItem.DisplayName} | ID {loadOrderItem.GroupID} | Parent: {loadOrderItem.ParentID} | Ordinal {loadOrderItem.Ordinal}");
            if (AggLoadInfo.GetNeighbor(loadOrderItem, true) is LoadOrderItemViewModel neighbor)
            {
                return true;
            }
            return false;
        }

        public bool CanMoveDown()
        {
            
            if (SelectedItems is null || SelectedItems.Count == 0 || !IsUiEnabled) return false;
            var firstItem = SelectedItems[0] as LoadOrderItemViewModel;
            var lastItem = SelectedItems[SelectedItems.Count - 1] as LoadOrderItemViewModel;
            var selectedItems = SelectedItems.OfType<LoadOrderItemViewModel>().ToList();


            if ( firstItem.GroupSetID == 1 || lastItem.GroupSetID == 1 || firstItem.GroupID < 1 || lastItem.GroupID < 1 || selectedItems.Any(item => item.ParentID != firstItem.ParentID))
                return false;


            if (AggLoadInfo.GetNeighbor(lastItem, false) is LoadOrderItemViewModel neighbor)
            {
                return true;
            }
            return false;
        }

        private void MoveUp(object? parameter)
        {
            if (SelectedItems.Count > 1)
            {
                MoveUpMulti(SelectedItems.OfType<LoadOrderItemViewModel>().ToList());
            }
            else
            {
                if (SelectedItem is LoadOrderItemViewModel loadOrderItem)
                {
                    var otherItem = AggLoadInfo.GetNeighbor(loadOrderItem, true);
                    if (otherItem != null)
                    {
                        loadOrderItem.SwapLocations(otherItem);
                        RefreshData();
                    }
                }
            }
        }

        private void MoveDown(object? parameter)
        {
            if (SelectedItems.Count > 1)
            {
                MoveDownMulti(SelectedItems.OfType<LoadOrderItemViewModel>().ToList());
            }
            else
            {
                if (SelectedItem is LoadOrderItemViewModel loadOrderItem)
                {
                    var otherItem = AggLoadInfo.GetNeighbor(loadOrderItem, false);
                    if (otherItem != null)
                    {
                        loadOrderItem.SwapLocations(otherItem);
                        RefreshData();
                    }
                }
                
            }
        }


        private void MoveUpMulti(List<LoadOrderItemViewModel> selectedItems)
        {
            var firstItem = SelectedItems[0] as LoadOrderItemViewModel;
            var lastItem = SelectedItems[SelectedItems.Count - 1] as LoadOrderItemViewModel;
            var otherItem = AggLoadInfo.GetNeighbor(firstItem, true);
            if (otherItem != null)
            {
                lastItem.SwapLocations(otherItem);
                RefreshData();
            }
        }

        private void MoveDownMulti(List<LoadOrderItemViewModel> selectedItems)
        {
            var firstItem = SelectedItems[0] as LoadOrderItemViewModel;
            var lastItem = SelectedItems[SelectedItems.Count - 1] as LoadOrderItemViewModel;
            var otherItem = AggLoadInfo.GetNeighbor(lastItem, false);
            if (otherItem != null)
            {
                firstItem.SwapLocations(otherItem);
                RefreshData();
            }
        }


        public ObservableCollection<LoadOrderItemViewModel> GetFlattenedList(EntityType entityType)
        {
            System.Diagnostics.Debug.WriteLine($"Flattening list for EntityType: {entityType}");

            // Use the Flatten method to get the cached flat list
            var flattenedList = Flatten(LoadOrders.Items).ToList();

            // Simplified sorting using exposed properties Ordinal and ParentID/GroupID
            var orderedList = flattenedList
                .Where(i => i.EntityType == entityType) // Filter by entity type
                .OrderBy(i => i.ParentID)  // Sort by ParentID first to maintain group hierarchy
                .ThenBy(i => i.Ordinal)    // Then sort by Ordinal within each parent
                .ToList();

            System.Diagnostics.Debug.WriteLine($"Flattened list contains {orderedList.Count} items after sorting.");

            foreach (var item in orderedList)
            {
                System.Diagnostics.Debug.WriteLine($"Flattened Item: {item.DisplayName} | PluginID: {item.PluginData?.PluginID}, GroupID: {item.GroupID}, Ordinal:{item.Ordinal}");
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



        //public ICommand AddNewGroupSetCommand => new RelayCommand(_ => AddNewGroupSet());
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
            AggLoadInfo.Instance.LoadOuts.Add(newLoadOut);
            // AggLoadInfo.Instance.LoadOuts.Add(newLoadOut);

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


        //private void AddNewGroupSet()
        //{
        //    // Create a new GroupSetEditor window with a new GroupSet (no arguments)
        //    var groupSetEditor = new GroupSetEditor();

        //    // Show the dialog for the new GroupSetEditor
        //    groupSetEditor.ShowDialog();

        //    // Optionally, refresh the AggLoadInfo instance or the view as needed
        //    AggLoadInfo.Instance.RefreshAllData();
        //}


       

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
