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
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(nameof(SelectedItems), typeof(ObservableCollection<object>), typeof(MultiSelectTreeView), new PropertyMetadata(new ObservableCollection<object>(), OnSelectedItemsChanged));

        public ObservableCollection<object> SelectedItems
        {
            get => (ObservableCollection<object>)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        private bool _isInitializing = true; // Suppress initialization notifications during setup
        private bool _isUpdatingSelection = false;
        private bool _isDoubleClick = false;
        private TreeViewItem _firstSelectedItem = null; // Store the first selected item for Shift-click
        private System.Windows.Threading.DispatcherTimer _clickTimer;

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
            var dataContext = item.DataContext as LoadOrderItemViewModel;

            if (e.ClickCount == 2)
            {
                _isDoubleClick = true;
                HandleDoubleClick(item);
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                // Ctrl-click logic
                if (!SelectedItems.Contains(dataContext))
                {
                    SelectedItems.Add(dataContext);
                    dataContext.IsSelected = true; // Set IsSelected for the group/item
                    System.Diagnostics.Debug.WriteLine($"Ctrl-click detected. Added to selection: {dataContext}");
                }
                else
                {
                    SelectedItems.Remove(dataContext);
                    dataContext.IsSelected = false; // Set IsSelected for the group/item
                    System.Diagnostics.Debug.WriteLine($"Ctrl-click detected. Removed from selection: {dataContext}");
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                // Shift-click logic to select a range
                if (_firstSelectedItem == null)
                {
                    _firstSelectedItem = item; // Set the first item if shift is the first click
                }
                else
                {
                    var startItem = _firstSelectedItem;
                    var endItem = item;

                    if (startItem != null && endItem != null)
                    {
                        SelectRange(startItem, endItem);
                    }
                }
            }
            else
            {
                // Single-click logic
                ClearSelection();
                SelectedItems.Add(dataContext);
                dataContext.IsSelected = true; // Set IsSelected for the group/item
                System.Diagnostics.Debug.WriteLine($"Single item selected: {dataContext}");
            }

            System.Diagnostics.Debug.WriteLine($"SelectedItems count after update: {SelectedItems.Count}");
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

        private void SelectRange(TreeViewItem startItem, TreeViewItem endItem)
        {
            // Logic to select items in range, ensuring IsSelected is set in ViewModel
            var allItems = new List<LoadOrderItemViewModel>();

            // Get the EntityType of the start item
            var itemType = ((LoadOrderItemViewModel)startItem.DataContext).EntityType;

            CollectAllItems(Items, allItems, itemType); // Pass the expected EntityType

            int startIndex = allItems.IndexOf((LoadOrderItemViewModel)startItem.DataContext);
            int endIndex = allItems.IndexOf((LoadOrderItemViewModel)endItem.DataContext);

            // Validate indices
            if (startIndex < 0 || endIndex < 0) return; // Early exit if indices are invalid

            // Ensure startIndex is less than endIndex for iteration
            if (startIndex > endIndex)
            {
                (startIndex, endIndex) = (endIndex, startIndex);
            }

            ClearSelection();
            for (int i = startIndex; i <= endIndex; i++)
            {
                var item = allItems[i];
                if (item != null)
                {
                    item.IsSelected = true; // Ensure LoadOrderItemViewModel has IsSelected property
                    SelectedItems.Add(item); // Assuming SelectedItems is a collection for your view model
                    System.Diagnostics.Debug.WriteLine($"Shift-click detected. Added to selection: {item.DisplayName}");
                }
            }
        }

        private void CollectAllItems(ItemCollection items, List<LoadOrderItemViewModel> result, EntityType entityType)
        {
            foreach (var item in items)
            {
                // Try to cast the item directly to LoadOrderItemViewModel
                var dataContext = item as LoadOrderItemViewModel;

                if (dataContext != null)
                {
                    // Check if the item matches the expected entity type
                    if (dataContext.EntityType == entityType)
                    {
                        result.Add(dataContext);
                        System.Diagnostics.Debug.WriteLine($"Collected: {dataContext.DisplayName} of type {dataContext.EntityType}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Skipped: {dataContext.DisplayName} (Expected: {entityType}, Found: {dataContext.EntityType})");
                    }
                }
                else
                {
                    // If the item is not a LoadOrderItemViewModel, log the failure
                    System.Diagnostics.Debug.WriteLine($"Failed to collect for item: {item}");
                }

                // If this item is a group, collect its children directly
                if (dataContext != null && dataContext.EntityType == EntityType.Group)
                {
                    foreach (var child in dataContext.Children)
                    {
                        // Add child directly to the result if it matches the expected entity type
                        if (child.EntityType == entityType)
                        {
                            result.Add(child);
                            System.Diagnostics.Debug.WriteLine($"Collected child: {child.DisplayName} of type {child.EntityType}");
                        }
                    }
                }
            }
        }
    }
}
