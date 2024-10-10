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


        public void Search(string? searchText)
        {
            if (LoadOrders.Items == null || LoadOrders.Items.Count == 0)
                return;

            var flatList = Flatten(LoadOrders.Items).ToList();

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
    (item.PluginData != null &&
     (item.PluginData.PluginName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true ||
      item.PluginData.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true))
);

                foreach (var item in matchingItems)
                {
                    item.IsHighlighted = true;
                }

                // Notify the UI about the changes
                OnPropertyChanged(nameof(LoadOrders.Items));
            }
        }


        public void SelectPreviousItem()
        {
            if (SelectedItem is not LoadOrderItemViewModel selectedItem || Items == null || Items.Count == 0)
                return;

            var flatList = Flatten(LoadOrders.Items).ToList();
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

            var flatList = Flatten(LoadOrders.Items).ToList();
            var currentIndex = flatList.IndexOf(selectedItem);

            if (currentIndex < flatList.Count - 1)
            {
                SelectedItem = flatList[currentIndex + 1];
            }
        }

        public void SelectFirstItem()
        {
            if (LoadOrders.Items == null || LoadOrders.Items.Count == 0)
                return;

            SelectedItem = Flatten(LoadOrders.Items).FirstOrDefault();
        }

        public void SelectLastItem()
        {
            if (LoadOrders.Items == null || LoadOrders.Items.Count == 0)
                return;

            SelectedItem = Flatten(LoadOrders.Items).LastOrDefault();
        }

        private void MoveToUnassignedGroup(Plugin plugin)
        {
            var unassignedGroup = AggLoadInfo.Instance.Groups.FirstOrDefault(g => g.GroupID == -997);
            plugin.GroupID = unassignedGroup.GroupID;
            plugin.GroupOrdinal = unassignedGroup.Plugins?.Count ?? 0;
            unassignedGroup.Plugins?.Add(plugin);
            plugin.WriteMod();
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

        public async void EditHighlightedItem(LoadOrderItemViewModel selectedItem)
        {
            var underlyingObject = EntityTypeHelper.GetUnderlyingObject(selectedItem);

            switch (selectedItem.EntityType)
            {
                case EntityType.Group:
                    var modGroup = underlyingObject as ModGroup;
                    if (modGroup != null)
                    {
                        var editorWindow = new ModGroupEditorWindow(modGroup);
                        if (await editorWindow.ShowDialogAsync() == true)
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
                        var pluginEditorWindow = new PluginEditorWindow(plugin);
                        if (await pluginEditorWindow.ShowDialogAsync() == true)
                        {
                            // Handle successful edit
                        }
                    }
                    break;

                default:
                    MessageBox.Show("Please select a valid item to edit.", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
            }

            RefreshData(); // Refresh after editing each item
        }



    }
}
