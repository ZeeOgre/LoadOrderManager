using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ZO.LoadOrderManager
{
    public partial class LoadOrderWindowViewModel
    {
        // Constructor to initialize data and load essential properties
        public LoadOrderWindowViewModel()
        {
            // Set ActiveGroupSet and ActiveLoadOut to selected properties
            _selectedGroupSet = AggLoadInfo.Instance.ActiveGroupSet;
            _selectedLoadOut = AggLoadInfo.Instance.ActiveLoadOut;

            // Initialize ObservableCollections for GroupSets, LoadOuts, and selection
            GroupSets = new ObservableCollection<GroupSet>();
            LoadOuts = new ObservableCollection<LoadOut>();
            SelectedItems = new ObservableCollection<object>();
            SelectedCachedItems = new ObservableCollection<object>();

            // Initialize the LoadOrders ViewModels
            LoadOrders = new LoadOrdersViewModel();
            CachedGroupSetLoadOrders = new LoadOrdersViewModel();


            SearchCommand = new RelayCommand(_ => Search(SearchText)); // LoadOrderWindowViewModel.TreeCommands.cs

            MoveUpCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(MoveUp), param => CanMoveUp()); // LoadOrderWindowViewModel.ButtonCommands.cs
            MoveDownCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(MoveDown), param => CanMoveDown()); // LoadOrderWindowViewModel.ButtonCommands.cs
            EditHighlightedItemCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(EditHighlightedItem), param => CanExecuteCheckAllItems()); // LoadOrderWindowViewModel.TreeCommands.cs
            CopyTextCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(CopyText), param => CanExecuteCheckAllItems()); // LoadOrderWindowViewModel.MenuCommands.cs
            DeleteCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(Delete), param => CanExecuteCheckAllItems()); // LoadOrderWindowViewModel.MenuCommands.cs
            EditCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(EditHighlightedItem), param => CanExecuteCheckAllItems()); // LoadOrderWindowViewModel.TreeCommands.cs
            ToggleEnableCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(item => ToggleActive(item, param)), param => CanExecuteCheckAllItems()); // LoadOrderWindowViewModel.MenuCommands.cs
            ToggleEnableCheckboxCommand = new RelayCommand<object>(param => HandleMultipleSelectedItems(item => ToggleEnableCheckbox(item, param)), param => CanExecuteCheckAllItems());

            ChangeGroupCommand = new RelayCommandWithParameter(
                param => HandleMultipleSelectedItems(item => ChangeGroup(item, param)),
                param => SelectedItems.All(item => CanChangeGroup((LoadOrderItemViewModel)item))
            );

            SaveAsNewLoadoutCommand = new RelayCommand<object?>(param => SaveAsNewLoadout()); // LoadOrderWindowViewModel.MenuCommands.cs
            OpenGameFolderCommand = new RelayCommand<object?>(param => OpenGameFolder(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            OpenGameSaveFolderCommand = new RelayCommand<object?>(param => OpenGameSaveFolder(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            EditPluginsCommand = new RelayCommand<object?>(param => EditPlugins(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            EditContentCatalogCommand = new RelayCommand<object?>(param => EditContentCatalog(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            ImportPluginsCommand = new RelayCommand<object?>(param => ImportPlugins()); // LoadOrderWindowViewModel.MenuCommands.cs
            ImportContextCatalogCommand = new RelayCommand<object?>(param => ImportContentCatalog()); // LoadOrderWindowViewModel.MenuCommands.cs
            ScanGameFolderCommand = new RelayCommand<object?>(param => ScanGameFolder(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            SavePluginsCommand = new RelayCommand(param => SavePlugins(), param => CanSavePlugins()); // LoadOrderWindowViewModel.ButtonCommands.cs
            OpenAppDataFolderCommand = new RelayCommand<object?>(param => OpenAppDataFolder(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            OpenGameLocalAppDataCommand = new RelayCommand<object?>(param => OpenGameLocalAppData(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            SettingsWindowCommand = new RelayCommand<object?>(param => SettingsWindow(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            ImportFromYamlCommand = new RelayCommand<object?>(param => ImportFromYaml()); // LoadOrderWindowViewModel.MenuCommands.cs
            OpenGameSettingsCommand = new RelayCommand<object?>(param => OpenGameSettings(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs


            EditGroupSetCommand = new RelayCommand(ExecuteEditGroupSetCommand, CanExecuteEditGroupSetCommand); // LoadOrderWindowViewModel.ContextMenuCommands.cs
            EditLoadOutCommand = new RelayCommand(ExecuteEditLoadOutCommand, CanExecuteEditLoadOutCommand); // LoadOrderWindowViewModel.ContextMenuCommands.cs

            RefreshCommand = new RelayCommand(_ => RefreshData()); // LoadOrderWindowViewModel.MenuCommands.cs

            // Load initial data
            LoadInitialData();

        }

        // Load initial data for the ViewModel
        private void LoadInitialData()
        {
            if (_isInitialDataLoaded)
            {
                return; // Prevent double loading
            }

            InitializationManager.StartInitialization(nameof(LoadOrderWindowViewModel));

            try
            {
                if (AggLoadInfo.Instance != null)
                {
                    StartSync();
                    // Clear existing GroupSets and LoadOuts
                    GroupSets.Clear();
                    LoadOuts.Clear();

                    // Populate GroupSets and LoadOuts
                    foreach (var groupSet in AggLoadInfo.GroupSets)
                    {
                        GroupSets.Add(groupSet);
                    }

                    foreach (var loadOut in AggLoadInfo.Instance.LoadOuts)
                    {
                        LoadOuts.Add(loadOut);
                    }

                    // Load LoadOrders and CachedGroupSetLoadOrders
                    LoadOrders.LoadData(_selectedGroupSet, _selectedLoadOut, false, false);
                    CachedGroupSetLoadOrders.LoadData(AggLoadInfo.GetCachedGroupSet1(), LoadOut.Load(1), true, true);
                    EndSync();
                    // Rebuild FlatList for the current view
                    RebuildFlatList();
                    UpdateStatusLight();
                    _isInitialDataLoaded = true;
                }
            }
            finally
            {
                InitializationManager.EndInitialization(nameof(LoadOrderWindowViewModel));
                RefreshData(); // Trigger UI update
            }
        }

        // Refresh data for the LoadOrders and CachedGroupSetLoadOrders
        private async void RefreshData()
        {
            if (InitializationManager.IsAnyInitializing()) return;

            // Disable UI
            IsUiEnabled = false;

            // Update status message for UI
            UpdateStatus("Refreshing data...");

            if (SelectedLoadOut != null && !_isSynchronizing)
            {
                _isSynchronizing = true;
                try
                {
                    // Asynchronous task for data fetching
                    await Task.Run(() =>
                    {
                        // Perform data refresh (on a background thread)
                        LoadOrders.RefreshData();
                        //CachedGroupSetLoadOrders.RefreshData();
                    });

                    // Update the status message after successful load
                    StatusMessage = $"Loaded plugins for profile: {SelectedLoadOut.Name}";
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that occurred during the data loading
                    StatusMessage = $"Error refreshing data: {ex.Message}";
                }
                finally
                {
                    // Ensure synchronization flag is reset
                    _isSynchronizing = false;

                    // Ensure the flat list is rebuilt (on the UI thread)
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        RebuildFlatList(); // Rebuild the flat list to reflect the latest state
                        UpdateStatus(StatusMessage); // Update the status message
                        UpdateStatusLight();
                    });
                }
            }
            else
            {
                StatusMessage = "No LoadOut selected.";
            }

            // Re-enable UI
            IsUiEnabled = true;
        }

    }
}
