using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ZO.LoadOrderManager
{
    public class MultiSelectTreeView : TreeView
    {
        private bool _isSynchronizing = false; // Flag to suppress notifications

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(nameof(SelectedItems), typeof(ObservableCollection<object>), typeof(MultiSelectTreeView), new PropertyMetadata(new ObservableCollection<object>(), OnSelectedItemsChanged));




        private bool _isInitializing = true; // Suppress initialization notifications during setup
        private bool _isUpdatingSelection = false;
        private bool _isDoubleClick = false;
        private TreeViewItem _firstSelectedItem = null; // Store the first selected item for Shift-click
        private System.Windows.Threading.DispatcherTimer _clickTimer;


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
            Loaded += MultiSelectTreeView_Loaded;

            // Initialize the click timer to differentiate between single and double-clicks
            _clickTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300) // Adjust time for double-click interval
            };
            _clickTimer.Tick += ClickTimer_Tick;
        }

        public Style ItemContainerStyle
        {
            get => (Style)GetValue(ItemContainerStyleProperty);
            set => SetValue(ItemContainerStyleProperty, value);
        }

        private void ClickTimer_Tick(object? sender, EventArgs e)
        {
            _clickTimer.Stop();

            if (_isDoubleClick)
            {
                System.Diagnostics.Debug.WriteLine("Double-click detected.");
                _isDoubleClick = false; // Reset the double-click flag
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Single-click detected.");
            }
        }

        private void MultiSelectTreeView_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitializing = false; // Allow notifications after loading
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

        //private void MultiSelectTreeView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (e.OriginalSource is FrameworkElement element)
        //    {
        //        var item = GetTreeViewItemFromElement(element);
        //        if (item != null)
        //        {
        //            _clickTimer.Start(); // Start click timer to track double-clicks
        //            HandleSelection(item, e);
        //        }
        //    }
        //}

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
                    _clickTimer.Start(); // Start click timer to track double-clicks
                    HandleSelection(item, e);
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

                if (e.ClickCount == 2)
                {
                    HandleDoubleClick(item);
                    return;
                }

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
                    if (_firstSelectedItem == null)
                    {
                        _firstSelectedItem = item;
                    }
                    else
                    {
                        SelectRange(_firstSelectedItem, item);
                    }
                }
                else
                {
                    ClearSelection();
                    SelectedItems.Add(dataContext);
                    dataContext.IsSelected = true;
                }
            }
            finally
            {
                _isUpdatingSelection = false; // End suppression, allow updates
            }
        }




        private void HandleDoubleClick(TreeViewItem item)
        {
            var dataContext = item.DataContext as LoadOrderItemViewModel;
            System.Diagnostics.Debug.WriteLine($"Double-click detected on: {dataContext}");

            if (DataContext is LoadOrderWindowViewModel viewModel && SelectedItems.Count > 0)
            {
                var firstSelectedItem = SelectedItems[0] as LoadOrderItemViewModel;
                viewModel?.EditHighlightedItem(firstSelectedItem);
            }
        }

        private void ClearSelection()
        {
            foreach (var selectedItem in new List<object>(SelectedItems))
            {
                if (selectedItem is LoadOrderItemViewModel viewModel)
                {
                    viewModel.IsSelected = false; // Clear IsSelected in ViewModel
                }
            }
            SelectedItems.Clear();
            System.Diagnostics.Debug.WriteLine("Selection cleared.");
        }

        //private void SelectRange(TreeViewItem startItem, TreeViewItem endItem)
        //{
        //    // Get the entity type and group IDs from the selected items
        //    var startItemDataContext = (LoadOrderItemViewModel)startItem.DataContext;
        //    var endItemDataContext = (LoadOrderItemViewModel)endItem.DataContext;

        //    var startGroupID = startItemDataContext.GroupID;
        //    var endGroupID = endItemDataContext.GroupID;

        //    // Find the path to the root for both start and end groups
        //    var startGroupPath = ModGroup.GetModGroupById(startGroupID)?.CalculatePathToRootUsingCache();
        //    var endGroupPath = ModGroup.GetModGroupById(endGroupID)?.CalculatePathToRootUsingCache();

        //    // Find the lowest common parent
        //    var commonParentID = FindLowestCommonParent(startGroupPath, endGroupPath);

        //    // Get all groups in the range under the common parent
        //    var groupsInRange = AggLoadInfo.Instance.GroupSetGroups.Items
        //        .Where(g => g.parentID == commonParentID &&
        //                    g.Ordinal >= Math.Min(startItemDataContext.Ordinal, endItemDataContext.Ordinal) &&
        //                    g.Ordinal <= Math.Max(startItemDataContext.Ordinal, endItemDataContext.Ordinal))
        //        .ToList();

        //    // Clear the current selection
        //    ClearSelection();

        //    // Select all groups found in the range
        //    foreach (var group in groupsInRange)
        //    {
        //        var loadOrderItem = FindLoadOrderItemByGroupID(group.groupID);
        //        if (loadOrderItem != null)
        //        {
        //            loadOrderItem.IsSelected = true;
        //            SelectedItems.Add(loadOrderItem);
        //            System.Diagnostics.Debug.WriteLine($"Shift-click detected. Added group to selection: {loadOrderItem.DisplayName}");
        //        }
        //    }

        //    // Now get all plugins in the range of the selected groups
        //    var pluginsInRange = AggLoadInfo.Instance.GroupSetPlugins.Items
        //        .Where(p =>
        //            groupsInRange.Any(g => g.groupID == p.groupID) &&
        //            p.groupSetID == commonParentID && // Ensure it belongs to the same GroupSet
        //            p.Ordinal >= Math.Min(startItemDataContext.Ordinal, endItemDataContext.Ordinal) &&
        //            p.Ordinal <= Math.Max(startItemDataContext.Ordinal, endItemDataContext.Ordinal))
        //        .ToList();

        //    // Select all plugins found in the range
        //    foreach (var plugin in pluginsInRange)
        //    {
        //        var pluginItem = FindLoadOrderItemByPluginID(plugin.pluginID);
        //        if (pluginItem != null)
        //        {
        //            pluginItem.IsSelected = true;
        //            SelectedItems.Add(pluginItem);
        //            System.Diagnostics.Debug.WriteLine($"Shift-click detected. Added plugin to selection: {pluginItem.DisplayName}");
        //        }
        //    }
        //}



        private void SelectRange(TreeViewItem startItem, TreeViewItem endItem)
        {
            // Collect all items, regardless of visibility or expansion
            var allItems = new List<LoadOrderItemViewModel>();

            // Collect all the items, flattened
            CollectAllItems(Items, allItems);

            int startIndex = allItems.IndexOf((LoadOrderItemViewModel)startItem.DataContext);
            int endIndex = allItems.IndexOf((LoadOrderItemViewModel)endItem.DataContext);

            // Ensure indices are valid
            if (startIndex < 0 || endIndex < 0) return;

            // Make sure startIndex is less than endIndex
            if (startIndex > endIndex)
            {
                (startIndex, endIndex) = (endIndex, startIndex);
            }

            // Clear current selection
            ClearSelection();

            // Select items in the range
            for (int i = startIndex; i <= endIndex; i++)
            {
                var item = allItems[i];
                if (item != null)
                {
                    item.IsSelected = true;
                    SelectedItems.Add(item);
                    System.Diagnostics.Debug.WriteLine($"Shift-click selected: {item.DisplayName}");
                }
            }
        }


        private void CollectAllItems(ItemCollection items, List<LoadOrderItemViewModel> result)
        {
            foreach (var item in items)
            {
                if (item is LoadOrderItemViewModel dataContext)
                {
                    // Add the item to the result list
                    result.Add(dataContext);
                    System.Diagnostics.Debug.WriteLine($"Collected: {dataContext.DisplayName}");

                    // Recursively collect children if any
                    if (dataContext.Children != null)
                    {
                        foreach (var child in dataContext.Children)
                        {
                            result.Add(child);
                            System.Diagnostics.Debug.WriteLine($"Collected child: {child.DisplayName}");
                        }
                    }
                }
            }
        }

    }
}
