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
            if (SelectedItem is LoadOrderItemViewModel loadOrderItem)
            {
                var underlyingObject = EntityTypeHelper.GetUnderlyingObject(loadOrderItem);
                if (underlyingObject is ModGroup modGroup)
                {
                    return modGroup.Ordinal > 1 && AggLoadInfo.Instance.Groups
                        .Any(g => g.ParentID == modGroup.ParentID && g.Ordinal == modGroup.Ordinal - 1);
                }
                else if (underlyingObject is Plugin plugin)
                {
                    return plugin.GroupOrdinal > 1 && AggLoadInfo.Instance.Plugins
                        .Any(p => p.GroupID == plugin.GroupID && p.GroupOrdinal == plugin.GroupOrdinal - 1);
                }
            }
            return false;
        }

        public bool CanMoveDown()
        {
            if (SelectedItem is LoadOrderItemViewModel loadOrderItem)
            {
                var underlyingObject = EntityTypeHelper.GetUnderlyingObject(loadOrderItem);
                if (underlyingObject is ModGroup modGroup)
                {
                    return AggLoadInfo.Instance.Groups
                        .Any(g => g.ParentID == modGroup.ParentID && g.Ordinal == modGroup.Ordinal + 1);
                }
                else if (underlyingObject is Plugin plugin)
                {
                    return AggLoadInfo.Instance.Plugins
                        .Any(p => p.GroupID == plugin.GroupID && p.GroupOrdinal == plugin.GroupOrdinal + 1);
                }
            }
            return false;
        }

        private void MoveUp()
        {
            if (CanMoveUp())
            {
                if (SelectedItem is LoadOrderItemViewModel loadOrderItem)
                {
                    var underlyingObject = EntityTypeHelper.GetUnderlyingObject(loadOrderItem);
                    if (underlyingObject is ModGroup modGroup)
                    {
                        var previousItem = AggLoadInfo.Instance.Groups
                            .FirstOrDefault(g => g.ParentID == modGroup.ParentID && g.Ordinal == modGroup.Ordinal - 1);

                        if (previousItem != null)
                        {
                            // Swap locations
                            modGroup.SwapLocations(previousItem);

                            // Refresh all data to update the ViewModel
                            AggLoadInfo.Instance.RefreshAllData();
                        }
                    }
                    else if (underlyingObject is Plugin plugin)
                    {
                        var previousItem = AggLoadInfo.Instance.Plugins
                            .FirstOrDefault(p => p.GroupID == plugin.GroupID && p.GroupOrdinal == plugin.GroupOrdinal - 1);

                        if (previousItem != null)
                        {
                            // Swap locations
                            plugin.SwapLocations(previousItem);

                            // Refresh all data to update the ViewModel
                            AggLoadInfo.Instance.RefreshAllData();
                        }
                    }
                }
            }
        }

        private void MoveDown()
        {
            if (CanMoveDown())
            {
                if (SelectedItem is LoadOrderItemViewModel loadOrderItem)
                {
                    var underlyingObject = EntityTypeHelper.GetUnderlyingObject(loadOrderItem);
                    if (underlyingObject is ModGroup modGroup)
                    {
                        var nextItem = AggLoadInfo.Instance.Groups
                            .FirstOrDefault(g => g.ParentID == modGroup.ParentID && g.Ordinal == modGroup.Ordinal + 1);

                        if (nextItem != null)
                        {
                            // Swap locations
                            modGroup.SwapLocations(nextItem);

                            // Refresh all data to update the ViewModel
                            AggLoadInfo.Instance.RefreshAllData();
                        }
                    }
                    else if (underlyingObject is Plugin plugin)
                    {
                        var nextItem = AggLoadInfo.Instance.Plugins
                            .FirstOrDefault(p => p.GroupID == plugin.GroupID && p.GroupOrdinal == plugin.GroupOrdinal + 1);

                        if (nextItem != null)
                        {
                            // Swap locations
                            plugin.SwapLocations(nextItem);

                            // Refresh all data to update the ViewModel
                            AggLoadInfo.Instance.RefreshAllData();
                        }
                    }
                }
            }
        }

        private ObservableCollection<LoadOrderItemViewModel> GetFlattenedList(EntityType entityType)
        {
            var flattenedList = new List<LoadOrderItemViewModel>();

            foreach (var item in LoadOrders.Items)
            {
                if (entityType == EntityType.Group && item.EntityType == EntityType.Group)
                {
                    flattenedList.Add(item);
                }
                else if (entityType == EntityType.Plugin && item.EntityType == EntityType.Plugin)
                {
                    flattenedList.Add(item);
                }
            }

            // Sort the list based on Ordinal for ModGroup and GroupOrdinal for Plugin
            var orderedList = flattenedList
                .OrderBy(i => i.EntityType == EntityType.Group
                    ? ((ModGroup?)i.GetModGroup())?.Ordinal ?? 0
                    : ((Plugin?)i.PluginData)?.GroupOrdinal ?? 0)
                .ToList();

            return new ObservableCollection<LoadOrderItemViewModel>(orderedList);
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

        private void AddNewGroupSet()
        {
            // Logic for adding a new GroupSet skeleton
        }

        private void AddNewLoadout()
        {
            // Logic for adding a new Loadout skeleton
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