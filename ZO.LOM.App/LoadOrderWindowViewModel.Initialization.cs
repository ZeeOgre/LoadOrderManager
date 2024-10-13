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
        public ICommand RefreshDataCommand { get; }

        public LoadOrderWindowViewModel()
        {
            _selectedGroupSet = AggLoadInfo.Instance.ActiveGroupSet;
            _selectedLoadOut = AggLoadInfo.Instance.ActiveLoadOut;

            SelectedItems = new ObservableCollection<object>();
            SelectedCachedItems = new ObservableCollection<object>();

            GroupSets = new ObservableCollection<GroupSet>();
            LoadOuts = new ObservableCollection<LoadOut>();

            LoadOrders = new LoadOrdersViewModel();
            CachedGroupSetLoadOrders = new LoadOrdersViewModel();

            if (LoadOrders != null && LoadOrders.Items != null)
            {
                LoadOrders.Items.CollectionChanged += (s, e) =>
                {
                    if (!InitializationManager.IsAnyInitializing() && !_isSynchronizing)
                    {
                        RebuildFlatList();
                    }
                };
            }
            

            SearchCommand = new RelayCommand(_ => Search(SearchText)); // LoadOrderWindowViewModel.TreeCommands.cs

            MoveUpCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(MoveUp), param => CanMoveUp()); // LoadOrderWindowViewModel.ButtonCommands.cs
            MoveDownCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(MoveDown), param => CanMoveDown()); // LoadOrderWindowViewModel.ButtonCommands.cs
            EditHighlightedItemCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(EditHighlightedItem), param => CanExecuteCheckAllItems()); // LoadOrderWindowViewModel.TreeCommands.cs
            CopyTextCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(CopyText), param => CanExecuteCheckAllItems()); // LoadOrderWindowViewModel.MenuCommands.cs
            DeleteCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(Delete), param => CanExecuteCheckAllItems()); // LoadOrderWindowViewModel.MenuCommands.cs
            EditCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(EditHighlightedItem), param => CanExecuteCheckAllItems()); // LoadOrderWindowViewModel.TreeCommands.cs
            ToggleEnableCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(item => ToggleEnable(item, param)), param => CanExecuteCheckAllItems()); // LoadOrderWindowViewModel.MenuCommands.cs
            ChangeGroupCommand = new RelayCommandWithParameter(param => HandleMultipleSelectedItems(item => ChangeGroup(item, param)), param => CanExecuteCheckAllItems()); // LoadOrderWindowViewModel.MenuCommands.cs

            SaveAsNewLoadoutCommand = new RelayCommand<object?>(param => SaveAsNewLoadout()); // LoadOrderWindowViewModel.MenuCommands.cs
            OpenGameFolderCommand = new RelayCommand<object?>(param => OpenGameFolder(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            OpenGameSaveFolderCommand = new RelayCommand<object?>(param => OpenGameSaveFolder(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            EditPluginsCommand = new RelayCommand<object?>(param => EditPlugins(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            EditContentCatalogCommand = new RelayCommand<object?>(param => EditContentCatalog(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            ImportContextCatalogCommand = new RelayCommand<object?>(param => ImportContextCatalog()); // LoadOrderWindowViewModel.MenuCommands.cs
            ScanGameFolderCommand = new RelayCommand<object?>(param => ScanGameFolder(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            SavePluginsCommand = new RelayCommand(param => SavePlugins(), param => CanSavePlugins()); // LoadOrderWindowViewModel.ButtonCommands.cs
            OpenAppDataFolderCommand = new RelayCommand<object?>(param => OpenAppDataFolder(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            OpenGameLocalAppDataCommand = new RelayCommand<object?>(param => OpenGameLocalAppData(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            SettingsWindowCommand = new RelayCommand<object?>(param => SettingsWindow(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            ImportFromYamlCommand = new RelayCommand<object?>(param => ImportFromYaml()); // LoadOrderWindowViewModel.MenuCommands.cs
            OpenGameSettingsCommand = new RelayCommand<object?>(param => OpenGameSettings(), _ => true); // LoadOrderWindowViewModel.MenuCommands.cs
            RefreshDataCommand = new RelayCommand<object?>(param => RefreshData(), _ => true); // LoadOrderWindowViewModel.Initialization.cs

            EditGroupSetCommand = new RelayCommand(ExecuteEditGroupSetCommand, CanExecuteEditGroupSetCommand); // LoadOrderWindowViewModel.ContextMenuCommands.cs
            RemoveGroupSetCommand = new RelayCommand(ExecuteRemoveGroupSetCommand, CanExecuteRemoveGroupSetCommand); // LoadOrderWindowViewModel.ContextMenuCommands.cs
            EditLoadOutCommand = new RelayCommand(ExecuteEditLoadOutCommand, CanExecuteEditLoadOutCommand); // LoadOrderWindowViewModel.ContextMenuCommands.cs
            RemoveLoadOutCommand = new RelayCommand(ExecuteRemoveLoadOutCommand, CanExecuteRemoveLoadOutCommand); // LoadOrderWindowViewModel.ContextMenuCommands.cs
            AddNewLoadOutCommand = new RelayCommand(ExecuteAddNewLoadOutCommand, CanExecuteAddNewLoadOutCommand); // LoadOrderWindowViewModel.ContextMenuCommands.cs


            // Load initial data
            LoadInitialData();
        }

        private void LoadInitialData()
        {
            if (_isInitialDataLoaded)
            {
                return;
            }

            InitializationManager.StartInitialization(nameof(LoadOrderWindowViewModel));
            try
            {
                if (AggLoadInfo.Instance != null)
                {
                    // Clear existing items and select active GroupSet and LoadOut
                    //Items.Clear();
                    GroupSets.Clear();
                    LoadOuts.Clear();

                    foreach (var groupSet in AggLoadInfo.Instance.GroupSets)
                    {
                        GroupSets.Add(groupSet);
                    }

                    foreach (var loadOut in AggLoadInfo.Instance.LoadOuts)
                    {
                        LoadOuts.Add(loadOut);
                    }

                    // Initialize LoadOrders and CachedGroupSetLoadOrders with the selected GroupSet and LoadOut
                    LoadOrders.LoadData(_selectedGroupSet, _selectedLoadOut, false, false);
                    CachedGroupSetLoadOrders.LoadData(AggLoadInfo.GetCachedGroupSet1(), LoadOut.Load(1), true, true);
                    RebuildFlatList();

                    InitializationManager.ReportProgress(95, "Initial data loaded into view");

                    StatusMessage = $"Loaded plugins for profile: {SelectedLoadOut.Name}";
                    UpdateStatus(StatusMessage);

                    _isInitialDataLoaded = true;
                }
            }
            finally
            {
                InitializationManager.EndInitialization(nameof(LoadOrderWindowViewModel));
                // Refresh data in the view
                RefreshData();
            }
        }

        private LoadOrderItemViewModel CreateGroupViewModel(ModGroup group)
        {
            return new LoadOrderItemViewModel(group);
        }

        private async void RefreshData()
        {
            if (InitializationManager.IsAnyInitializing()) return;

            UpdateStatus("Refreshing data...");

            if (SelectedLoadOut != null && !_isSynchronizing)
            {
                _isSynchronizing = true;
                // Using async to improve performance and avoid blocking the UI
                await Task.Run(() =>
                {
                    LoadOrders.RefreshData();
                    CachedGroupSetLoadOrders.RefreshData();
                    RebuildFlatList();
                });

                StatusMessage = $"Loaded plugins for profile: {SelectedLoadOut.Name}";
            }
            else
            {
                StatusMessage = "No LoadOut selected.";
            }
            _isSynchronizing = false;

            UpdateStatus(StatusMessage);
        }
    }
}
