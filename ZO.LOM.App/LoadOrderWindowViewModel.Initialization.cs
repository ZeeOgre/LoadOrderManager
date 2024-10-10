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

            AggLoadInfo.Instance.PropertyChanged += OnAggLoadInfoPropertyChanged;


            // Initialize collections and commands
            Items = new ObservableCollection<LoadOrderItemViewModel>();
            GroupSets = new ObservableCollection<GroupSet>();
            LoadOuts = new ObservableCollection<LoadOut>();
            LoadOrders = new LoadOrdersViewModel();
            CachedGroupSetLoadOrders = new LoadOrdersViewModel();

            // Initialize commands
            SearchCommand = new RelayCommand(_ => Search(SearchText));

            //MultiSelectCommand enabled items;
            MoveUpCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(MoveUp), param => CanMoveUp());
            MoveDownCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(MoveDown), param => CanMoveDown());
            EditHighlightedItemCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(EditHighlightedItem), param => CanExecuteCheckAllItems());
            CopyTextCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(CopyText), param => CanExecuteCheckAllItems());
            DeleteCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(Delete), param => CanExecuteCheckAllItems());
            EditCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(EditHighlightedItem), param => CanExecuteCheckAllItems());
            ToggleEnableCommand = new RelayCommand<object?>(param => HandleMultipleSelectedItems(item => ToggleEnable(item, param)), param => CanExecuteCheckAllItems());
            ChangeGroupCommand = new RelayCommandWithParameter(param => HandleMultipleSelectedItems(item => ChangeGroup(item, param)), param => CanExecuteCheckAllItems());


            SaveAsNewLoadoutCommand = new RelayCommand<object?>(param => SaveAsNewLoadout());
            OpenGameFolderCommand = new RelayCommand<object?>(param => OpenGameFolder(), _ => true);
            OpenGameSaveFolderCommand = new RelayCommand<object?>(param => OpenGameSaveFolder(), _ => true);
            EditPluginsCommand = new RelayCommand<object?>(param => EditPlugins(), _ => true);
            EditContentCatalogCommand = new RelayCommand<object?>(param => EditContentCatalog(), _ => true);
            ImportContextCatalogCommand = new RelayCommand<object?>(param => ImportContextCatalog());
            ScanGameFolderCommand = new RelayCommand<object?>(param => ScanGameFolder(), _ => true);
            SavePluginsCommand = new RelayCommand(param => SavePlugins(), param => CanSavePlugins());
            OpenAppDataFolderCommand = new RelayCommand<object?>(param => OpenAppDataFolder(), _ => true);
            OpenGameLocalAppDataCommand = new RelayCommand<object?>(param => OpenGameLocalAppData(), _ => true);
            SettingsWindowCommand = new RelayCommand<object?>(param => SettingsWindow(), _ => true);
            ImportFromYamlCommand = new RelayCommand<object?>(param => ImportFromYaml());
            OpenGameSettingsCommand = new RelayCommand<object?>(param => OpenGameSettings(), _ => true);
            //OpenPluginEditorCommand = new RelayCommand<object?>(param => OpenPluginEditor());
            //OpenGroupEditorCommand = new RelayCommand<object?>(param => OpenGroupEditor());
            RefreshDataCommand = new RelayCommand<object?>(param => RefreshData(), _ => true);
           
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
                    Items.Clear();
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
                    LoadOrders.LoadData(_selectedGroupSet, _selectedLoadOut,false,false);
                    CachedGroupSetLoadOrders.LoadData(AggLoadInfo.Instance.GetCachedGroupSet1(), LoadOut.Load(1),true,true);

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
