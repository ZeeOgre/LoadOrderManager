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

        public ICommand SearchCommand { get; }
        public ICommand EditHighlightedItemCommand { get; }
        public ICommand EditCommand { get; }

        //private void Search(string? searchText)
        //{
        //    if (string.IsNullOrEmpty(searchText))
        //    {
        //        // If search text is empty, show all items
        //        RefreshData();
        //    }
        //    else
        //    {
        //        // Filter Groups and Plugins based on the search text
        //        var filteredGroups = new ObservableCollection<ModGroup>(
        //            Groups.Where(g => g.GroupName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
        //                              (g.Plugins != null && g.Plugins.Any(p => p.PluginName.Contains(searchText, StringComparison.OrdinalIgnoreCase))))
        //        );

        //        var filteredPlugins = new ObservableCollection<PluginViewModel>(
        //            Plugins.Where(p => p.Plugin.PluginName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
        //        );

        //        // Update the collections
        //        Groups = filteredGroups;
        //        Plugins = filteredPlugins;

        //        // Notify the UI about the changes
        //        OnPropertyChanged(nameof(Groups));
        //        OnPropertyChanged(nameof(Plugins));
        //    }
        //}

        public void Search(string? searchText)
        {
            if (Items == null || Items.Count == 0)
                return;

            var flatList = Flatten(Items).ToList();

            foreach (var item in flatList)
            {
                item.IsHighlighted = false; // Reset highlighting
            }

            if (string.IsNullOrEmpty(searchText))
            {
                // If search text is empty, show all items
                RefreshData();
            }
            else
            {
                // Filter and highlight Groups and Plugins based on the search text
                var matchingItems = flatList.Where(item =>
                    (item.DisplayName != null && item.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (item.PluginData != null && (item.PluginData.PluginName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                                 item.PluginData.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
                );

                foreach (var item in matchingItems)
                {
                    item.IsHighlighted = true;
                }

                // Notify the UI about the changes
                OnPropertyChanged(nameof(Items));
            }
        }


        public void SelectPreviousItem()
        {
            if (SelectedItem is not LoadOrderItemViewModel selectedItem || Items == null || Items.Count == 0)
                return;

            var flatList = Flatten(Items).ToList();
            var currentIndex = flatList.IndexOf(selectedItem);

            if (currentIndex > 0)
            {
                SelectedItem = flatList[currentIndex - 1];
            }
        }

        public void SelectNextItem()
        {
            if (SelectedItem is not LoadOrderItemViewModel selectedItem || Items == null || Items.Count == 0)
                return;

            var flatList = Flatten(Items).ToList();
            var currentIndex = flatList.IndexOf(selectedItem);

            if (currentIndex < flatList.Count - 1)
            {
                SelectedItem = flatList[currentIndex + 1];
            }
        }

        public void SelectFirstItem()
        {
            if (Items == null || Items.Count == 0)
                return;

            SelectedItem = Flatten(Items).FirstOrDefault();
        }

        public void SelectLastItem()
        {
            if (Items == null || Items.Count == 0)
                return;

            SelectedItem = Flatten(Items).LastOrDefault();
        }

        private void MoveToUnassignedGroup(Plugin plugin)
        {
            var unassignedGroup = AggLoadInfo.Instance.Groups.FirstOrDefault(g => g.GroupID == -997);
            plugin.GroupID = unassignedGroup.GroupID;
            plugin.GroupOrdinal = unassignedGroup.Plugins?.Count ?? 0;
            unassignedGroup.Plugins?.Add(plugin);
        }

        private IEnumerable<LoadOrderItemViewModel> Flatten(ObservableCollection<LoadOrderItemViewModel> items)
        {
            foreach (var item in items)
            {
                yield return item;

                foreach (var child in Flatten(item.Children))
                {
                    yield return child;
                }
            }
        }

        private bool CanExecuteEdit(object parameter)
        {
            return true; // Add your logic here
        }

        public void EditHighlightedItem()
        {
            if (SelectedItem is LoadOrderItemViewModel selectedItem)
            {
                var underlyingObject = EntityTypeHelper.GetUnderlyingObject(selectedItem);

                switch (selectedItem.EntityType)
                {
                    case EntityType.Group:
                        var modGroup = underlyingObject as ModGroup;
                        if (modGroup != null)
                        {
                            var editorWindow = new ModGroupEditorWindow(modGroup);
                            if (editorWindow.ShowDialog() == true)
                            {
                                // Handle successful edit
                            }
                        }
                        else
                        {
                            MessageBox.Show("ModGroup not found. Please create a new group using the group editor.", "Group Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        break;

                    case EntityType.Plugin:
                        var plugin = underlyingObject as Plugin;
                        if (plugin != null)
                        {
                            var pluginEditorWindow = new PluginEditorWindow(plugin, SelectedLoadOut);
                            if (pluginEditorWindow.ShowDialog() == true)
                            {
                                // Handle successful edit
                            }
                        }
                        break;

                    default:
                        MessageBox.Show("Please select a valid item to edit.", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                }
            }
            else
            {
                MessageBox.Show("Please select a valid item to edit.", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            RefreshData();
        }


    }
}