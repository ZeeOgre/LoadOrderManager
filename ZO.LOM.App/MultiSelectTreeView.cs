using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ZO.LoadOrderManager
{
    public class MultiSelectTreeView : TreeView
    {
        private bool _isUpdatingSelection = false; // Flag to suppress notifications
        private TreeViewItem _firstSelectedItem = null; // Store the first selected item for Shift-click

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(nameof(SelectedItems), typeof(ObservableCollection<object>), typeof(MultiSelectTreeView), new PropertyMetadata(new ObservableCollection<object>(), OnSelectedItemsChanged));

        public ObservableCollection<object> SelectedItems
        {
            get => (ObservableCollection<object>)GetValue(SelectedItemsProperty);
            set
            {
                if (_isUpdatingSelection) return; // Suppress updates during selection
                SetValue(SelectedItemsProperty, value);
            }
        }

        public MultiSelectTreeView()
        {
            SelectedItems = new ObservableCollection<object>();
            PreviewMouseDown += MultiSelectTreeView_PreviewMouseDown;
            MouseDoubleClick += MultiSelectTreeView_MouseDoubleClick; // Add this line
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var treeView = d as MultiSelectTreeView;
            if (treeView != null && !treeView._isUpdatingSelection && !Equals(e.OldValue, e.NewValue))
            {
                treeView._isUpdatingSelection = true;
                // Logic for handling selection change can be added here if necessary
                treeView._isUpdatingSelection = false;
            }
        }

        private void MultiSelectTreeView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Allow right-click event for context menu handling
            if (e.RightButton == MouseButtonState.Pressed)
            {
                return; // Let the right-click event pass through for context menu handling
            }

            if (e.OriginalSource is FrameworkElement element)
            {
                var item = GetTreeViewItemFromElement(element);
                if (item != null)
                {
                    HandleSelection(item, e);
                }
            }
        }

        private void MultiSelectTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement element)
            {
                var item = GetTreeViewItemFromElement(element);
                if (item != null)
                {
                    var dataContext = item.DataContext as LoadOrderItemViewModel;
                    if (dataContext != null)
                    {
                        LaunchEditor(dataContext);
                    }
                }
            }
        }

        private void LaunchEditor(LoadOrderItemViewModel item)
        {
            if (item.EntityType == EntityType.Group)
            {
                // Launch the editor for ModGroup
                var modGroupEditor = new ModGroupEditorWindow(item.GetModGroup());
                modGroupEditor.ShowDialog();
            }
            else if (item.EntityType == EntityType.Plugin)
            {
                // Launch the editor for Plugin
                var pluginEditor = new PluginEditorWindow(item.PluginData);
                pluginEditor.ShowDialog();
            }
        }

        private TreeViewItem GetTreeViewItemFromElement(FrameworkElement element)
        {
            while (element != null && !(element is TreeViewItem))
            {
                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }
            return element as TreeViewItem;
        }

        private void HandleSelection(TreeViewItem item, MouseButtonEventArgs e)
        {
            try
            {
                _isUpdatingSelection = true; // Suppress updates during selection
                var dataContext = item.DataContext as LoadOrderItemViewModel;

                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (!SelectedItems.Contains(dataContext))
                    {
                        SelectedItems.Add(dataContext);
                        dataContext.IsSelected = true;
                    }
                    else
                    {
                        SelectedItems.Remove(dataContext);
                        dataContext.IsSelected = false;
                    }
                }
                else if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    System.Diagnostics.Debug.WriteLine("Shift-click detected.");
                    if (_firstSelectedItem == null)
                    {
                        _firstSelectedItem = item;
                    }
                    else
                    {
                        var firstDataContext = _firstSelectedItem.DataContext as LoadOrderItemViewModel;
                        var lastDataContext = item.DataContext as LoadOrderItemViewModel;

                        if (firstDataContext != null && lastDataContext != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"First item: {firstDataContext.DisplayName} Grp-{firstDataContext.ParentID} Ord-{firstDataContext.Ordinal}" +
                                $", Last item: {lastDataContext.DisplayName} Grp-{lastDataContext.ParentID} Ord-{lastDataContext.Ordinal}" +
                                $"");

                            // Get the selected range from the SelectRange method
                            var selectedRange = SelectRange(firstDataContext, lastDataContext);
                            foreach (var rangeItem in selectedRange)
                            {
                                if (!SelectedItems.Contains(rangeItem))
                                {
                                    SelectedItems.Add(rangeItem);
                                    rangeItem.IsSelected = true;
                                }
                            }
                        }

                        _firstSelectedItem = null; // Reset after range selection
                    }
                }
                else
                {
                    ClearSelection();
                    SelectedItems.Add(dataContext);
                    dataContext.IsSelected = true;
                    _firstSelectedItem = item; // Set the first selected item for potential Shift-click
                }
            }
            finally
            {
                _isUpdatingSelection = false; // End suppression, allow updates
            }
        }

        private List<LoadOrderItemViewModel> SelectRange(LoadOrderItemViewModel firstItem, LoadOrderItemViewModel lastItem)
        {
            var selectedRange = new List<LoadOrderItemViewModel>();

            // Determine if we are working with groups or plugins
            if (firstItem.EntityType == EntityType.Group && lastItem.EntityType == EntityType.Group)
            {
                // Handle range selection for groups using GroupSetGroups (gsg)
                var groupRange = GetGroupRange(firstItem.GroupID, lastItem.GroupID);
                selectedRange = SelectViewModelsByGroupID(groupRange);
            }
            else if (firstItem.EntityType == EntityType.Plugin && lastItem.EntityType == EntityType.Plugin)
            {
                // Handle range selection for plugins using GroupSetPlugins (gsp)
                var pluginRange = GetPluginRange(firstItem.PluginData.PluginID, lastItem.PluginData.PluginID);
                DebugItemsSource();
                selectedRange = SelectViewModelsByPluginID(pluginRange);
            }

            return selectedRange;
        }

        private void DebugItemsSource()
        {
            var allItems = ItemsSource.OfType<LoadOrderItemViewModel>().ToList();
            System.Diagnostics.Debug.WriteLine($"Total view models in ItemsSource: {allItems.Count}");

            foreach (var item in allItems)
            {
                System.Diagnostics.Debug.WriteLine($"ViewModel in ItemsSource: {item.DisplayName} | PluginID: {item.PluginData.PluginID}, GroupID: {item.GroupID} Ordinal: {item.Ordinal}");
            }
        }

        private List<long> GetGroupRange(long startGroupID, long endGroupID)
        {
            // First, gather all groups that belong to the active GroupSet
            var filteredGroups = AggLoadInfo.Instance.GroupSetGroups
                .Where(g => g.groupSetID == AggLoadInfo.Instance.ActiveGroupSet.GroupSetID)
                .OrderBy(g => g.parentID)    // Ensure we maintain the hierarchical structure
                .ThenBy(g => g.Ordinal)      // Further sort by the Ordinal within the hierarchy
                .ToList();

            // Find the indices of start and end GroupIDs
            int startIndex = filteredGroups.FindIndex(g => g.groupID == startGroupID);
            int endIndex = filteredGroups.FindIndex(g => g.groupID == endGroupID);

            // Ensure that startIndex is less than or equal to endIndex by swapping if necessary
            if (startIndex > endIndex)
            {
                (startIndex, endIndex) = (endIndex, startIndex); // Swap the indices
            }

            // Now traverse the filtered groups and collect the range between startIndex and endIndex
            var selectedGroups = filteredGroups
                .Skip(startIndex)
                .Take(endIndex - startIndex + 1)
                .Select(g => g.groupID)
                .ToList();

            return selectedGroups;
        }


        private List<long> GetPluginRange(long startPluginID, long endPluginID)
        {
            // First, gather all plugins that belong to the active GroupSet
            var filteredPlugins = AggLoadInfo.Instance.GroupSetPlugins
                .Where(p => p.groupSetID == AggLoadInfo.Instance.ActiveGroupSet.GroupSetID)
                .OrderBy(p => p.groupID)
                .ThenBy(p => p.Ordinal)
                .ToList();

            // Find the indices of start and end PluginIDs
            int startIndex = filteredPlugins.FindIndex(p => p.pluginID == startPluginID);
            int endIndex = filteredPlugins.FindIndex(p => p.pluginID == endPluginID);

            // Ensure that startIndex is less than or equal to endIndex by swapping if necessary
            if (startIndex > endIndex)
            {
                (startIndex, endIndex) = (endIndex, startIndex); // Swap the indices
            }

            // Collect all pluginIDs between startIndex and endIndex (inclusive)
            var selectedPlugins = filteredPlugins
                .Skip(startIndex)
                .Take(endIndex - startIndex + 1)
                .Select(p => p.pluginID)
                .ToList();

            // Return the selected pluginIDs
            return selectedPlugins;
        }


        private List<LoadOrderItemViewModel> SelectViewModelsByPluginID(List<long> pluginIDs)
        {
            var loadOrderWindowViewModel = DataContext as LoadOrderWindowViewModel;
            if (loadOrderWindowViewModel == null)
            {
                System.Diagnostics.Debug.WriteLine("LoadOrderWindowViewModel not found.");
                return new List<LoadOrderItemViewModel>();
            }

            // Get the flattened list of plugins
            var flattenedPlugins = loadOrderWindowViewModel.GetFlattenedList(EntityType.Plugin);

            System.Diagnostics.Debug.WriteLine($"Looking for PluginID matches in the flattened plugin list: {string.Join(", ", pluginIDs)}");

            var selectedItems = flattenedPlugins.Where(vm => pluginIDs.Contains(vm.PluginData.PluginID)).ToList();

            System.Diagnostics.Debug.WriteLine($"Found {selectedItems.Count} matching plugin view models.");

            foreach (var item in selectedItems)
            {
                System.Diagnostics.Debug.WriteLine($"Matched Plugin: {item.DisplayName} | PluginID: {item.PluginData.PluginID} | Ordinal {item.Ordinal}");
            }

            return selectedItems;
        }

        private List<LoadOrderItemViewModel> SelectViewModelsByGroupID(List<long> groupIDs)
        {
            var loadOrderWindowViewModel = DataContext as LoadOrderWindowViewModel;
            if (loadOrderWindowViewModel == null)
            {
                System.Diagnostics.Debug.WriteLine("LoadOrderWindowViewModel not found.");
                return new List<LoadOrderItemViewModel>();
            }

            // Get the flattened list of groups
            var flattenedGroups = loadOrderWindowViewModel.GetFlattenedList(EntityType.Group);

            System.Diagnostics.Debug.WriteLine($"Looking for GroupID matches in the flattened group list: {string.Join(", ", groupIDs)}");

            var selectedItems = flattenedGroups.Where(vm => groupIDs.Contains(vm.GroupID)).ToList();

            System.Diagnostics.Debug.WriteLine($"Found {selectedItems.Count} matching group view models.");

            foreach (var item in selectedItems)
            {
                System.Diagnostics.Debug.WriteLine($"Matched Group: {item.DisplayName} | GroupID: {item.GroupID}");
            }

            return selectedItems;
        }



        private void ClearSelection()
        {
            foreach (var item in SelectedItems)
            {
                if (item is LoadOrderItemViewModel viewModel)
                {
                    viewModel.IsSelected = false;
                }
            }

            SelectedItems.Clear();
        }
    }
}
