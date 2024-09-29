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

        private bool CanMoveUp()
        {
            if (SelectedItem is LoadOrderItemViewModel loadOrderItem)
            {
                if (loadOrderItem.EntityType == EntityType.Plugin)
                {
                    var selectedPlugin = loadOrderItem.PluginData;
                    if (selectedPlugin != null)
                    {
                        if (selectedPlugin.GroupID < 1 || selectedPlugin.GroupOrdinal < 1)
                        {
                            return false;
                        }
                        else { return true; }
                    }
                }
                else if (loadOrderItem.EntityType == EntityType.Group)
                {
                    var selectedGroup = loadOrderItem.GroupID;
                    if (selectedGroup != null)
                    {
                        if (selectedGroup <= 1)
                        {
                            return false;
                        }
                        else { return true; }
                    }
                }
            }
            return false;
        }

        private bool CanMoveDown()
        {
            if (SelectedItem is LoadOrderItemViewModel loadOrderItem)
            {
                if (loadOrderItem.EntityType == EntityType.Plugin)
                {
                    var selectedPlugin = loadOrderItem.PluginData;
                    if (selectedPlugin != null)
                    {
                        if (selectedPlugin.GroupID < 1)
                        {
                            return false;
                        }
                        else { return true; }
                    }
                }
                else if (loadOrderItem.EntityType == EntityType.Group)
                {
                    var selectedGroup = loadOrderItem.GroupID;
                    if (selectedGroup != null)
                    {
                        if (selectedGroup <= 1)
                        {
                            return false;
                        }
                        else { return true; }
                    }
                }
            }
            return false;
        }

        private void MoveUp()
        {
            if (SelectedItem is Plugin selectedPlugin)
            {
                var group = AggLoadInfo.Instance.Groups.FirstOrDefault(g => g.Plugins != null && g.Plugins.Contains(selectedPlugin));
                if (group != null && group.Plugins != null)
                {
                    int index = group.Plugins.IndexOf(selectedPlugin);
                    var previousPlugin = group.Plugins[index - 1];

                    // Swap ordinals
                    long tempOrdinal = selectedPlugin.GroupOrdinal ?? 0;
                    selectedPlugin.GroupOrdinal = previousPlugin.GroupOrdinal;
                    previousPlugin.GroupOrdinal = tempOrdinal;

                    // Move the plugin
                    group.Plugins.Move(index, index - 1);
                }
            }
            else if (SelectedItem is ModGroup selectedGroup)
            {
                int index = AggLoadInfo.Instance.Groups.IndexOf(selectedGroup);
                var previousGroup = AggLoadInfo.Instance.Groups[index - 1];

                // Swap ordinals
                long tempOrdinal = selectedGroup.Ordinal ?? 0;
                selectedGroup.Ordinal = previousGroup.Ordinal;
                previousGroup.Ordinal = tempOrdinal;

                // Move the group
                AggLoadInfo.Instance.Groups.Move(index, index - 1);
            }
            OnPropertyChanged(nameof(AggLoadInfo.Instance.Groups));
        }

        private void MoveDown()
        {
            if (SelectedItem is Plugin selectedPlugin)
            {
                var group = AggLoadInfo.Instance.Groups.FirstOrDefault(g => g.Plugins != null && g.Plugins.Contains(selectedPlugin));
                if (group != null && group.Plugins != null)
                {
                    int index = group.Plugins.IndexOf(selectedPlugin);
                    var nextPlugin = group.Plugins[index + 1];

                    // Swap ordinals
                    long tempOrdinal = selectedPlugin.GroupOrdinal ?? 0;
                    selectedPlugin.GroupOrdinal = nextPlugin.GroupOrdinal;
                    nextPlugin.GroupOrdinal = tempOrdinal;

                    // Move the plugin
                    group.Plugins.Move(index, index + 1);
                }
            }
            else if (SelectedItem is ModGroup selectedGroup)
            {
                int index = AggLoadInfo.Instance.Groups.IndexOf(selectedGroup);
                var nextGroup = AggLoadInfo.Instance.Groups[index + 1];

                // Swap ordinals
                long tempOrdinal = selectedGroup.Ordinal ?? 0;
                selectedGroup.Ordinal = nextGroup.Ordinal;
                nextGroup.Ordinal = tempOrdinal;

                // Move the group
                AggLoadInfo.Instance.Groups.Move(index, index + 1);
            }
            OnPropertyChanged(nameof(AggLoadInfo.Instance.Groups));
        }

        private void SavePlugins()
        {
            var groupSetName = SelectedGroupSet.GroupSetName;
            var profileName = SelectedLoadOut.Name;
            var defaultFileName = $"Plugins_{groupSetName}_{profileName}.txt";
            var defaultFilePath = Path.Combine(FileManager.GameLocalAppDataFolder, defaultFileName);

            var result = MessageBox.Show($"Producing {defaultFilePath}. Do you want to save to a different location?", "Save Plugins", MessageBoxButton.YesNo);

            string? outputFileName = null;
            if (result == MessageBoxResult.Yes)
            {
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

            FileManager.ProducePluginsTxt(LoadOrders, outputFileName);
            StatusMessage = "Plugins.txt file has been successfully created.";
        }

        private bool CanSavePlugins()
        {
            return true;
        }
    }
}