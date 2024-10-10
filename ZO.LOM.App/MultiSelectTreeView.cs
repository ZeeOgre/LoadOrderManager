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
        private bool _isSynchronizing = false; // Flag to suppress notifications
        private bool _isInitializing = true; // Suppress initialization notifications during setup
        private bool _isUpdatingSelection = false;
        private bool _isDoubleClick = false;
        private TreeViewItem _firstSelectedItem = null; // Store the first selected item for Shift-click
        private System.Windows.Threading.DispatcherTimer _clickTimer;

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

        // Style property to visually distinguish groups, plugins, and selection states
        public Style ItemContainerStyle
        {
            get => base.ItemContainerStyle;
            set => base.ItemContainerStyle = value; // Apply styles based on selection state or type (group/plugin)
        }

        public MultiSelectTreeView()
        {
            SelectedItems = new ObservableCollection<object>();
            PreviewMouseDown += MultiSelectTreeView_PreviewMouseDown;
            Loaded += MultiSelectTreeView_Loaded;

            // Initialize the click timer to differentiate between single and double-clicks
            _clickTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300) // Adjust time for double-click interval
            };
            _clickTimer.Tick += ClickTimer_Tick;
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var treeView = d as MultiSelectTreeView;

            if (treeView != null && !treeView._isInitializing && !treeView._isUpdatingSelection && !Equals(e.OldValue, e.NewValue))
            {
                treeView._isUpdatingSelection = true;
                // Logic for handling selection change can be added here if necessary
                treeView._isUpdatingSelection = false;
            }
        }

        private void MultiSelectTreeView_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitializing = false;
        }

        private void MultiSelectTreeView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Ignore right-clicks to allow context menu to work
            if (e.RightButton == MouseButtonState.Pressed)
            {
                return; // Let the right-click event pass through for context menu handling
            }

            // Continue with normal selection handling for other mouse buttons
            if (e.OriginalSource is FrameworkElement element)
            {
                var item = GetTreeViewItemFromElement(element);
                if (item != null)
                {
                    // Start the click timer to track double-clicks
                    _clickTimer.Start();
                    HandleSelection(item, e); // Handle single or multiple selection
                }
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

                if (dataContext == null) return;

                // Handle double-click
                if (e.ClickCount == 2)
                {
                    HandleDoubleClick(item);
                    return;
                }

                // Handle Ctrl-click (for toggling selection)
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (!SelectedItems.Contains(dataContext))
                    {
                        SelectedItems.Add(dataContext);
                        dataContext.IsSelected = true;
                        System.Diagnostics.Debug.WriteLine($"Ctrl-click selected: {dataContext.DisplayName} | PluginID: {dataContext.PluginData.PluginID}, GroupID: {dataContext.GroupID}");
                    }
                    else
                    {
                        SelectedItems.Remove(dataContext);
                        dataContext.IsSelected = false;
                        System.Diagnostics.Debug.WriteLine($"Ctrl-click deselected: {dataContext.DisplayName} | PluginID: {dataContext.PluginData.PluginID}, GroupID: {dataContext.GroupID}");
                    }
                }
                // Handle Shift-click (for range selection)
                else if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    System.Diagnostics.Debug.WriteLine($"Shift-click detected | Last item selected: {dataContext.DisplayName} | PluginID: {dataContext.PluginData.PluginID}, GroupID: {dataContext.GroupID}");

                    if (_firstSelectedItem == null)
                    {
                        _firstSelectedItem = item;
                        System.Diagnostics.Debug.WriteLine($"First item selected: {_firstSelectedItem.DataContext} | PluginID: {dataContext.PluginData.PluginID}, GroupID: {dataContext.GroupID}");
                    }
                    else
                    {
                        var firstDataContext = _firstSelectedItem.DataContext as LoadOrderItemViewModel;
                        var lastDataContext = item.DataContext as LoadOrderItemViewModel;

                        if (firstDataContext != null && lastDataContext != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"First item: {firstDataContext.DisplayName}, Last item: {lastDataContext.DisplayName}");
                            SelectRange(firstDataContext, lastDataContext); // Perform range selection
                            _firstSelectedItem = null; // Reset after range selection
                        }
                    }
                }
                // Normal single-click (clear other selections and select only this item)
                else
                {
                    ClearSelections();
                    SelectedItems.Add(dataContext);
                    dataContext.IsSelected = true;
                    _firstSelectedItem = item; // Set the first selected item for potential Shift-click
                    System.Diagnostics.Debug.WriteLine($"Single-click selected: {dataContext.DisplayName} | PluginID: {dataContext.PluginData.PluginID}, GroupID: {dataContext.GroupID}");
                }
            }
            finally
            {
                _isUpdatingSelection = false; // End suppression, allow updates
            }

            // Debugging output for the count of selected items
            System.Diagnostics.Debug.WriteLine($"SelectedItems count: {SelectedItems.Count}");
        }



        private void ClearSelections()
        {
            foreach (var item in SelectedItems.OfType<LoadOrderItemViewModel>())
            {
                item.IsSelected = false;
            }
            SelectedItems.Clear();

            // Debugging output after clearing selections
            System.Diagnostics.Debug.WriteLine("Cleared all selections.");
        }


        private void HandleDoubleClick(TreeViewItem item)
        {
            var dataContext = item.DataContext as LoadOrderItemViewModel;
            System.Diagnostics.Debug.WriteLine($"Double-click detected on: {dataContext?.DisplayName}");

            if (DataContext is LoadOrderWindowViewModel viewModel && SelectedItems.Count > 0)
            {
                var firstSelectedItem = SelectedItems[0] as LoadOrderItemViewModel;
                viewModel?.EditHighlightedItem(firstSelectedItem);
            }
        }

        private void ClickTimer_Tick(object? sender, EventArgs e)
        {
            _clickTimer.Stop();
        }

        // Handles the entity selection logic (Shift-click or Ctrl-click)
        private void HandleEntitySelection(LoadOrderItemViewModel firstItem, LoadOrderItemViewModel lastItem, bool isShiftClick)
        {
            // Ensure correct order of start and end items
            var startIndex = Items.IndexOf(firstItem);
            var endIndex = Items.IndexOf(lastItem);
            if (startIndex > endIndex)
            {
                (startIndex, endIndex) = (endIndex, startIndex); // Swap if necessary
            }

            if (firstItem.EntityType == lastItem.EntityType)
            {
                // Both are the same entity type (either group or plugin), proceed with range selection
                var selectedRange = SelectRange(firstItem, lastItem);
                MarkItemsAsSelected(selectedRange);
            }
            else
            {
                // Handle the outlier case where first is a plugin and second is a group
                HandleMismatchedSelection(firstItem, lastItem);
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
                selectedRange = SelectViewModelsByPluginID(pluginRange);
            }

            // Return the selected range
            return selectedRange;
        }


        private List<long> GetGroupRange(long startGroupID, long endGroupID)
        {
            var groupRange = AggLoadInfo.Instance.GroupSetGroups
                .Where(g => g.groupID >= Math.Min(startGroupID, endGroupID) && g.groupID <= Math.Max(startGroupID, endGroupID))
                .OrderBy(g => g.Ordinal)
                .Select(g => g.groupID)
                .ToList();

            return groupRange;
        }

        private List<long> GetPluginRange(long startPluginID, long endPluginID)
        {
            var pluginRange = AggLoadInfo.Instance.GroupSetPlugins
                .Where(p => p.pluginID >= Math.Min(startPluginID, endPluginID) && p.pluginID <= Math.Max(startPluginID, endPluginID))
                .OrderBy(p => p.Ordinal)
                .Select(p => p.pluginID)
                .ToList();

            return pluginRange;
        }



        private List<LoadOrderItemViewModel> SelectViewModelsByGroupID(List<long> groupIDs)
        {
            return ItemsSource.OfType<LoadOrderItemViewModel>()
                .Where(vm => groupIDs.Contains(vm.GroupID))
                .ToList();
        }

        private List<LoadOrderItemViewModel> SelectViewModelsByPluginID(List<long> pluginIDs)
        {
            return ItemsSource.OfType<LoadOrderItemViewModel>()
                .Where(vm => pluginIDs.Contains(vm.PluginData.PluginID))
                .ToList();
        }


        private void HandleMismatchedSelection(LoadOrderItemViewModel firstItem, LoadOrderItemViewModel lastItem)
        {
            if (firstItem.EntityType == EntityType.Plugin && lastItem.EntityType == EntityType.Group)
            {
                // Plugin first, group second – select the containing group
                var containingGroup = firstItem.GroupID;
                var groupItem = GetByGroupId(containingGroup);

                if (groupItem != null)
                {
                    HandleEntitySelection(firstItem, groupItem, true); // Recursive call
                }
            }
            else
            {
                // Normal case, group first, plugin second (handled directly)
                HandleEntitySelection(firstItem, lastItem, true);
            }
        }

        private LoadOrderItemViewModel GetByGroupId(long groupId)
        {
            return Items.OfType<LoadOrderItemViewModel>()
                .FirstOrDefault(item => item.EntityType == EntityType.Group && item.GroupID == groupId);
        }

        private void MarkItemsAsSelected(List<LoadOrderItemViewModel> items)
        {
            foreach (var item in items)
            {
                item.IsSelected = true;

                // Debugging output for selection
                System.Diagnostics.Debug.WriteLine($"Selected: {item.DisplayName}");

                if (!SelectedItems.Contains(item))
                {
                    SelectedItems.Add(item);
                }
            }

            // Debugging output for the count of selected items
            System.Diagnostics.Debug.WriteLine($"SelectedItems count: {SelectedItems.Count}");
        }

    }
}
