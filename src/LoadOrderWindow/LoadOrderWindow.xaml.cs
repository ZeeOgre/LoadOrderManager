using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace ZO.LoadOrderManager
{
    public partial class LoadOrderWindow : MetroWindow
    {
        private bool isSaved;
        private long SelectedLoadOutID;
       
        private bool _isLoadOrderTreeViewInitialized = false;
        private bool _isCachedGroupSetTreeViewInitialized = false;

        private static LoadOrderWindow _instance;

        public LoadOrderWindowViewModel LOWVM
        {
            get => this.DataContext as LoadOrderWindowViewModel;
        }


        private LoadOrderWindow()
        {
            InitializationManager.StartInitialization(nameof(LoadOrderWindow));
            InitializeComponent();
            

            try
            {
                if (AggLoadInfo.Instance != null)
                {
                    if (App.Current.MainWindow is LoadingWindow loadingWindow)
                    {
                        App.Current.Dispatcher.Invoke(() =>
                            loadingWindow.UpdateProgress(10, "Initializing LoadOrderWindow..."));

                        var viewModel = new LoadOrderWindowViewModel();
                        this.DataContext = viewModel;

                        App.LogDebug("LoadOrderWindow: DataContext set to LoadOrderWindowViewModel");

                        App.Current.Dispatcher.Invoke(() =>
                            loadingWindow.UpdateProgress(50, "LoadOrderWindow initialized successfully."));
                    }
                    else
                    {
                        _ = MessageBox.Show("MainWindow is not of type LoadingWindow.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                    }
                }
                else
                {
                    _ = MessageBox.Show("Singleton is not initialized. Please initialize the singleton before opening the window.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadOrderWindow: Exception occurred - {ex.Message}");
                _ = MessageBox.Show($"An error occurred while initializing the window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        public static LoadOrderWindow Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LoadOrderWindow();
                }
                return _instance;
            }
        }

        public static void DisposeInstance()
        {
            if (_instance != null)
            {
                _instance.Close();
                _instance = null;
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

            // Ensure final initialization steps and visibility adjustments
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

            if (App.Current.MainWindow is LoadingWindow loadingWindow)
            {
                App.Current.Dispatcher.Invoke(() =>
                    loadingWindow.UpdateProgress(99, "LoadOrderWindow components loaded."));
            }
            InitializationManager.EndInitialization(nameof(LoadOrderWindow));

            cmbGroupSet.SelectedItem = viewModel.SelectedGroupSet;
            cmbLoadOut.SelectedItem = viewModel.SelectedLoadOut;

            // Ensure window visibility after initialization
            this.Visibility = Visibility.Visible;
            _ = this.Activate();
            InitializationManager.ReportProgress(100, "LoadOrderWindow fully visible");
        }

        private void LoadOrderWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            App.LogDebug("MainWindow is closing.");
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Uri uri = e.Uri;
            _ = uri.IsAbsoluteUri ? uri.AbsoluteUri : string.Empty;

            if (!uri.IsAbsoluteUri || string.IsNullOrEmpty(uri.OriginalString))
            {
                string newUri = "https://google.com";
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

        private void TreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is LoadOrderWindowViewModel viewModel)
            {
                if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (viewModel.SelectedItems.Count > 0)
                    {
                        viewModel.CopyTextCommand.Execute(viewModel.SelectedItems[0]);
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
                    viewModel.DeleteCommand.Execute(viewModel.SelectedItems[0]);
                }
                else if (e.Key == Key.Insert)
                {
                    if (viewModel.SelectedItems.Count > 0)
                    {
                        var firstSelectedItem = viewModel.SelectedItems[0] as LoadOrderItemViewModel;
                        if (firstSelectedItem != null)
                        {
                            viewModel.EditHighlightedItem(firstSelectedItem);
                        }
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
                    if (viewModel.SelectedItems.Count > 0)
                    {
                        viewModel.MoveUpCommand.Execute(viewModel.SelectedItems[0]);
                    }
                }
                else if (e.Key == Key.Down && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (viewModel.SelectedItems.Count > 0)
                    {
                        viewModel.MoveDownCommand.Execute(viewModel.SelectedItems[0]);
                    }
                }
                else if (e.Key == Key.Space)
                {
                    // Toggle IsActive state when the spacebar is pressed
                    if (viewModel.SelectedItems.Count > 0)
                    {
                        var selectedItem = viewModel.SelectedItems[0] as LoadOrderItemViewModel;
                        if (selectedItem != null)
                        {
                            selectedItem.IsActive = !selectedItem.IsActive;
                            viewModel.OnPropertyChanged(nameof(selectedItem.IsActive));
                        }
                    }
                }
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is LoadOrderWindowViewModel viewModel)
                {
                    viewModel.SearchCommand.Execute(null);
                }
            }
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
                if (item is LoadOrderItemViewModel)
                {
                    var treeViewItem = LoadOrderTreeView.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;

                    if (treeViewItem != null)
                    {
                        if (treeViewItem.IsExpanded != expand)
                        {
                            treeViewItem.IsExpanded = expand;
                        }

                        if (treeViewItem.HasItems && treeViewItem.IsExpanded == expand)
                        {
                            ExpandOrCollapseGroups(treeViewItem.Items, expand);
                        }
                    }
                }
            }
        }

        private void TreeView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
            if (treeViewItem != null)
            {
                _ = treeViewItem.Focus();
                e.Handled = true;
            }
        }

        private static TreeViewItem? VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }
            return source as TreeViewItem;
        }

    }
}
