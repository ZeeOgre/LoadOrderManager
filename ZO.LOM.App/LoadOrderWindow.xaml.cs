using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace ZO.LoadOrderManager
{
    public partial class LoadOrderWindow : Window
    {
        private bool isSaved;
        private long SelectedLoadOutID;
        private System.Timers.Timer cooldownTimer;
        private bool _isLoadOrderTreeViewInitialized = false;
        private bool _isCachedGroupSetTreeViewInitialized = false;

        //public LoadOrdersViewModel GroupSet1ViewModel { get; set; }

        //public LoadOrderWindow()
        //{
        //    InitializeComponent();
        //    cooldownTimer = new System.Timers.Timer();

        //    try
        //    {
        //        if (AggLoadInfo.Instance != null)
        //        {
        //            App.Current.Dispatcher.Invoke(() =>
        //                ((LoadingWindow)App.Current.MainWindow).UpdateProgress(10, "Initializing LoadOrderWindow..."));

        //            this.DataContext = new LoadOrderWindowViewModel();
        //            App.LogDebug("LoadOrderWindow: DataContext set to LoadOrderWindowViewModel");

        //            // Initialize GroupSet1ViewModel
        //            //GroupSet1ViewModel = LoadOrdersViewModel.GroupSet1LoadOrdersViewModel();

        //            //var viewModel = (LoadOrderWindowViewModel)DataContext;
        //            //viewModel.SelectedGroupSet = AggLoadInfo.Instance.ActiveGroupSet;
        //            //viewModel.SelectedLoadOut = AggLoadInfo.Instance.ActiveLoadOut;

        //            App.Current.Dispatcher.Invoke(() =>
        //                ((LoadingWindow)App.Current.MainWindow).UpdateProgress(50, "LoadOrderWindow initialized successfully."));
        //        }
        //        else
        //        {
        //            MessageBox.Show("Singleton is not initialized. Please initialize the singleton before opening the window.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            Close();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"LoadOrderWindow: Exception occurred - {ex.Message}");
        //        MessageBox.Show($"An error occurred while initializing the window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        Close();
        //    }
        //}
        public LoadOrderWindow()
        {
            InitializationManager.StartInitialization(nameof(LoadOrderWindow));
            InitializeComponent();
            cooldownTimer = new System.Timers.Timer();

            try
            {
                if (AggLoadInfo.Instance != null)
                {
                    App.Current.Dispatcher.Invoke(() =>
                        ((LoadingWindow)App.Current.MainWindow).UpdateProgress(10, "Initializing LoadOrderWindow..."));

                    var viewModel = new LoadOrderWindowViewModel();
                    
                    this.DataContext = viewModel;
                    App.LogDebug("LoadOrderWindow: DataContext set to LoadOrderWindowViewModel");

                    //viewModel.SelectedGroupSet = AggLoadInfo.Instance.ActiveGroupSet;
                    //viewModel.SelectedLoadOut = AggLoadInfo.Instance.ActiveLoadOut;

                    //Dispatcher.BeginInvoke((Action)(() =>
                    //{
                    //    Debug.WriteLine("Beginning delayed initialization...");

                    //    if (cmbGroupSet.ItemsSource != null)
                    //    {
                    //        cmbGroupSet.SelectedItem = viewModel.SelectedGroupSet;
                    //        cmbGroupSet.GetBindingExpression(ComboBox.SelectedItemProperty)?.UpdateTarget();
                    //        Debug.WriteLine($"cmbGroupSet delayed initialization: SelectedItem set to {viewModel.SelectedGroupSet.GroupSetName}");
                    //        Debug.WriteLine($"cmbGroupSet delayed initialization: SelectedItem is {cmbGroupSet.SelectedItem}");
                    //    }
                    //    else
                    //    {
                    //        Debug.WriteLine("cmbGroupSet.ItemsSource is null during delayed initialization.");
                    //    }

                    //    if (cmbProfile.ItemsSource != null)
                    //    {
                    //        cmbProfile.SelectedItem = viewModel.SelectedLoadOut;
                    //        cmbProfile.GetBindingExpression(ComboBox.SelectedItemProperty)?.UpdateTarget();
                    //        Debug.WriteLine($"cmbProfile delayed initialization: SelectedItem set to {viewModel.SelectedLoadOut.Name}");
                    //        Debug.WriteLine($"cmbProfile delayed initialization: SelectedItem is {cmbProfile.SelectedItem}");
                    //    }
                    //    else
                    //    {
                    //        Debug.WriteLine("cmbProfile.ItemsSource is null during delayed initialization.");
                    //    }

                        
                    //}));

                    App.Current.Dispatcher.Invoke(() =>
                        ((LoadingWindow)App.Current.MainWindow).UpdateProgress(50, "LoadOrderWindow initialized successfully."));
                }
                else
                {
                    MessageBox.Show("Singleton is not initialized. Please initialize the singleton before opening the window.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadOrderWindow: Exception occurred - {ex.Message}");
                MessageBox.Show($"An error occurred while initializing the window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void LoadOrderWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as LoadOrderWindowViewModel;

            if (viewModel == null)
            {
                Debug.WriteLine("LoadOrderWindow: ViewModel is null.");
                return;
            }

            App.LogDebug("Loading groups and plugins...");

            // Final progress updates and window visibility adjustments
            if (!_isLoadOrderTreeViewInitialized)
            {
                LoadOrderTreeView_Loaded(sender, e);
                _isLoadOrderTreeViewInitialized = true;
                InitializationManager.ReportProgress(90, "LoadOrder tree view initialized");
            }

            if (!_isCachedGroupSetTreeViewInitialized)
            {
                CachedGroupSetTreeView_Loaded(sender, e);
                _isCachedGroupSetTreeViewInitialized = true;
                InitializationManager.ReportProgress(95, "Cached group set tree view initialized");
            }

            App.Current.Dispatcher.Invoke(() =>
                ((LoadingWindow)App.Current.MainWindow).UpdateProgress(99, "LoadOrderWindow components loaded."));
            InitializationManager.EndInitialization(nameof(LoadOrderWindow));

            //// Select items by unique identifier
            ////SelectComboBoxItemById(cmbGroupSet, viewModel.SelectedGroupSet?.GroupSetID, "GroupSetID");
            ////SelectComboBoxItemById(cmbProfile, viewModel.SelectedLoadOut?.ProfileID, "ProfileID");
            
            cmbGroupSet.SelectedItem = viewModel.SelectedGroupSet;
            cmbProfile.SelectedItem = viewModel.SelectedLoadOut;


            // Ensure window visibility after initialization
            this.Visibility = Visibility.Visible;
            this.Activate();

            InitializationManager.ReportProgress(100, "LoadOrderWindow fully visible");
        }

        //private void SelectComboBoxItemById(ComboBox comboBox, object? id, string idPropertyName)
        //{
        //    if (comboBox.ItemsSource != null && id != null)
        //    {
        //        var item = comboBox.ItemsSource.Cast<object>().FirstOrDefault(x => GetId(x, idPropertyName)?.Equals(id) == true);
        //        if (item != null)
        //        {
        //            comboBox.SelectedItem = item;
        //        }
        //        else
        //        {
        //            Debug.WriteLine($"Item with {idPropertyName} {id} not found in ComboBox.");
        //        }
        //    }
        //    else
        //    {
        //        Debug.WriteLine("ComboBox.ItemsSource is null or id is null.");
        //    }
        //}

        //private object? GetId(object item, string idPropertyName)
        //{
        //    var propertyInfo = item.GetType().GetProperty(idPropertyName);
        //    return propertyInfo?.GetValue(item, null);
        //}





        //private void LoadOrderWindow_Loaded(object sender, RoutedEventArgs e)
        //{
        //    App.LogDebug("Loading groups and plugins...");


        //    // Set initial selected item manually if binding did not update it
        //    if (cmbGroupSet.SelectedItem == null)
        //    {
        //        cmbGroupSet.SelectedItem = viewModel.SelectedGroupSet;
        //    }

        //    if (cmbProfile.SelectedItem == null)
        //    {
        //        cmbProfile.SelectedItem = viewModel.SelectedLoadOut;
        //    }

        //    // Final progress updates and window visibility adjustments
        //    if (!_isLoadOrderTreeViewInitialized)
        //    {
        //        LoadOrderTreeView_Loaded(sender, e);
        //        _isLoadOrderTreeViewInitialized = true;
        //        InitializationManager.ReportProgress(90, "LoadOrder tree view initialized");
        //    }

        //    if (!_isCachedGroupSetTreeViewInitialized)
        //    {
        //        CachedGroupSetTreeView_Loaded(sender, e);
        //        _isCachedGroupSetTreeViewInitialized = true;
        //        InitializationManager.ReportProgress(95, "Cached group set tree view initialized");
        //    }

        //    //cmbGroupSet.SelectedItem = AggLoadInfo.Instance.ActiveGroupSet;
        //    //cmbProfile.SelectedItem = AggLoadInfo.Instance.ActiveLoadOut;

        //    App.Current.Dispatcher.Invoke(() =>
        //        ((LoadingWindow)App.Current.MainWindow).UpdateProgress(99, "LoadOrderWindow components loaded."));

        //    // Ensure window visibility after initialization
        //    this.Visibility = Visibility.Visible;
        //    this.Activate();

        //    InitializationManager.ReportProgress(100, "LoadOrderWindow fully visible");
        //}

        private void LoadOrderWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            App.LogDebug("MainWindow is closing.");
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Uri uri = e.Uri;
            string newUri = uri.AbsoluteUri;

            if (!uri.IsAbsoluteUri || string.IsNullOrEmpty(uri.OriginalString))
            {
                newUri = "https://google.com";
                if (uri.OriginalString.Contains("Nexus") || string.IsNullOrEmpty(uri.OriginalString))
                {
                    newUri = "https://www.nexusmods.com/starfield";
                }
                else if (uri.OriginalString.Contains("Bethesda") || string.IsNullOrEmpty(uri.OriginalString))
                {
                    newUri = "https://creations.bethesda.net/en/starfield/";
                }
                uri = new Uri(newUri);
            }

            _ = Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
            isSaved = false;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Debug.WriteLine("TreeView_SelectedItemChanged event triggered.");
            if (DataContext is LoadOrderWindowViewModel viewModel)
            {
                viewModel.SelectedItem = e.NewValue;
                Debug.WriteLine($"SelectedItem set to: {e.NewValue}");
            }
        }

        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is LoadOrderWindowViewModel viewModel && viewModel.SelectedItem != null)
            {
                viewModel.EditHighlightedItem();
            }
        }

        private void TreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is LoadOrderWindowViewModel viewModel)
            {
                if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (viewModel.SelectedItem != null)
                    {
                        viewModel.CopyTextCommand.Execute(viewModel.SelectedItem);
                    }
                }
                else if (e.Key == Key.Up)
                {
                    viewModel.SelectPreviousItem();
                }
                else if (e.Key == Key.Down)
                {
                    viewModel.SelectNextItem();
                }
                else if (e.Key == Key.Delete)
                {
                    if (viewModel.SelectedItem != null)
                    {
                        viewModel.DeleteCommand.Execute(viewModel.SelectedItem);
                    }
                }
                else if (e.Key == Key.Insert)
                {
                    if (viewModel.SelectedItem != null)
                    {
                        viewModel.EditHighlightedItem();
                    }
                }
                else if (e.Key == Key.Home)
                {
                    viewModel.SelectFirstItem();
                }
                else if (e.Key == Key.End)
                {
                    viewModel.SelectLastItem();
                }
                else if (e.Key == Key.Up && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (viewModel.SelectedItem != null)
                    {
                        viewModel.MoveUpCommand.Execute(viewModel.SelectedItem);
                    }
                }
                else if (e.Key == Key.Down && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (viewModel.SelectedItem != null)
                    {
                        viewModel.MoveDownCommand.Execute(viewModel.SelectedItem);
                    }
                }
            }
        }

        private void TreeView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }
        }

        private static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        private void LoadOrderTreeView_Loaded(object sender, RoutedEventArgs e)
        {
            ExpandOrCollapseGroups(LoadOrderTreeView.Items, true);
        }

        private void CachedGroupSetTreeView_Loaded(object sender, RoutedEventArgs e)
        {
            ExpandOrCollapseGroups(CachedGroupSetTreeView.Items, true);
        }

        private void ExpandOrCollapseGroups(ItemCollection items, bool expand)
        {
            foreach (var item in items)
            {
                if (item is LoadOrderItemViewModel viewModel)
                {
                    var treeViewItem = (TreeViewItem)LoadOrderTreeView.ItemContainerGenerator.ContainerFromItem(item);
                    if (treeViewItem != null)
                    {
                        if (viewModel.GroupID > 0)
                        {
                            treeViewItem.IsExpanded = expand;
                        }
                        else
                        {
                            treeViewItem.IsExpanded = expand;
                        }

                        ExpandOrCollapseGroups(treeViewItem.Items, expand);
                    }
                }
            }
        }
        //private void CheckBox_Loaded(object sender, RoutedEventArgs e)
        //{
        //    var checkBox = sender as CheckBox;
        //    Debug.WriteLine($"CheckBox DataContext: {checkBox?.DataContext?.GetType().Name}");
        //}

        //private void cmbGroupSet_Loaded(object sender, RoutedEventArgs e)
        //{
        //    // Ensure the ComboBox displays the selected item
        //    var comboBox = sender as ComboBox;
        //    if (comboBox != null)
        //    {
        //        comboBox.SelectedItem = ((LoadOrderWindowViewModel)DataContext).SelectedGroupSet;
        //    }
        //}

        //private void cmbProfile_Loaded(object sender, RoutedEventArgs e)
        //{
        //    // Ensure the ComboBox displays the selected item
        //    var comboBox = sender as ComboBox;
        //    if (comboBox != null)
        //    {
        //        comboBox.SelectedItem = ((LoadOrderWindowViewModel)DataContext).SelectedLoadOut;
        //    }
        //}
    }
}
