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
            // Initialize collections and commands
            Groups = new ObservableCollection<ModGroup>();
            Plugins = new ObservableCollection<PluginViewModel>();
            LoadOuts = new ObservableCollection<LoadOut>();
            LoadOrders = new LoadOrdersViewModel();
            CachedGroupSetLoadOrders = new LoadOrdersViewModel().GroupSet1LoadOrdersViewModel();
            Items = new ObservableCollection<LoadOrderItemViewModel>();
            GroupSets = new ObservableCollection<GroupSet>();

            // Initialize commands
            SearchCommand = new RelayCommand<string?>(Search);
            MoveUpCommand = new RelayCommand<object?>(param => MoveUp(), param => CanMoveUp());
            MoveDownCommand = new RelayCommand<object?>(param => MoveDown(), param => CanMoveDown());
            SaveAsNewLoadoutCommand = new RelayCommand<object?>(param => SaveAsNewLoadout());
            OpenGameFolderCommand = new RelayCommand<object?>(param => OpenGameFolder(), _ => true);
            OpenGameSaveFolderCommand = new RelayCommand<object?>(param => OpenGameSaveFolder(), _ => true);
            EditPluginsCommand = new RelayCommand<object?>(param => EditPlugins(), _ => true);
            EditContentCatalogCommand = new RelayCommand<object?>(param => EditContentCatalog(), _ => true);
            ImportContextCatalogCommand = new RelayCommand<object?>(param => ImportContextCatalog());
            SavePluginsCommand = new RelayCommand(param => SavePlugins(), param => CanSavePlugins());
            EditHighlightedItemCommand = new RelayCommand<object?>(param => EditHighlightedItem());
            OpenAppDataFolderCommand = new RelayCommand<object?>(param => OpenAppDataFolder(), _ => true);
            OpenGameLocalAppDataCommand = new RelayCommand<object?>(param => OpenGameLocalAppData(), _ => true);
            SettingsWindowCommand = new RelayCommand<object?>(param => SettingsWindow(), _ => true);
            ImportFromYamlCommand = new RelayCommand<object?>(param => ImportFromYaml());
            OpenGameSettingsCommand = new RelayCommand<object?>(param => OpenGameSettings(), _ => true);
            OpenPluginEditorCommand = new RelayCommand<object?>(param => OpenPluginEditor());
            OpenGroupEditorCommand = new RelayCommand<object?>(param => OpenGroupEditor());
            RefreshDataCommand = new RelayCommand<object?>(param => RefreshData(), _ => true);
            CopyTextCommand = new RelayCommand<object?>(param => CopyText(), param => CanExecuteCopyText(null));
            DeleteCommand = new RelayCommand<object?>(param => Delete(), param => CanExecuteDelete(null));
            EditCommand = new RelayCommand<object?>(param => EditHighlightedItem(), param => CanExecuteEdit(null));
            ToggleEnableCommand = new RelayCommand<object?>(param => ToggleEnable(null), param => CanExecuteToggleEnable(null));
            ChangeGroupCommand = new RelayCommandWithParameter(ChangeGroup, CanExecuteChangeGroup);

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
                    // Load initial collections from AggLoadInfo
                    GroupSets = new ObservableCollection<GroupSet>(AggLoadInfo.Instance.GetGroupSets());
                    LoadOuts = new ObservableCollection<LoadOut>(AggLoadInfo.Instance.LoadOuts);
                    Groups = new ObservableCollection<ModGroup>(AggLoadInfo.Instance.Groups);
                    Plugins = new ObservableCollection<PluginViewModel>(
                                AggLoadInfo.Instance.Plugins.Select(plugin => new PluginViewModel(plugin))
                            );



                    // Clear existing items
                    Items.Clear();

                    SelectedGroupSet = AggLoadInfo.Instance.ActiveGroupSet;
                    // Check if there is a LoadOut available
                    SelectedLoadOut = AggLoadInfo.Instance.ActiveLoadOut;

                    if (SelectedLoadOut == null)
                    {
                        //// Prompt the user to create a new LoadOut
                        //var result = MessageBox.Show(
                        //    "No LoadOut found. Would you like to create a new LoadOut?",
                        //    "Create New LoadOut",
                        //    MessageBoxButton.YesNo,
                        //    MessageBoxImage.Question);

                        //if (result == MessageBoxResult.Yes)
                        //{
                        //    // Create a new LoadOut if the user agrees
                        //    var newLoadOutName = "New LoadOut";
                        //    var newLoadOut = new LoadOut
                        //    {
                        //        Name = newLoadOutName,
                        //        ProfileID = GenerateNewProfileID(), // Generate a new profile ID
                        //        enabledPlugins = new ObservableHashSet<long>()
                        //    };

                        //    LoadOuts.Add(newLoadOut);
                        //    AggLoadInfo.Instance.LoadOuts.Add(newLoadOut); // Add to AggLoadInfo

                        //    SelectedLoadOut = newLoadOut;
                        //    StatusMessage = $"Created new LoadOut: {newLoadOut.Name}";
                        //}
                        //else
                        //{
                        StatusMessage = "No LoadOut selected. Please create a new LoadOut.";
                        //    return;
                        //}
                    }
                    else 
                    { 
                        
                    }

                    var enabledPluginIds = SelectedLoadOut.enabledPlugins;

                    // Use the enabledPlugins hashset directly from SelectedLoadOut


                    // Iterate over each group and create the group view models
                    foreach (var group in Groups)
                    {
                        var groupViewModel = CreateGroupViewModel(group);

                        // Iterate over each plugin in the group and determine if it is enabled
                        foreach (var plugin in group.Plugins ?? Enumerable.Empty<Plugin>())
                        {
                            var isEnabled = enabledPluginIds.Contains(plugin.PluginID); // Simplified check
                            groupViewModel.Children.Add(new LoadOrderItemViewModel
                            {
                                PluginData = plugin,
                                IsActive = isEnabled,
                                EntityType = EntityType.Plugin
                            });
                        }

                        // Add the group view model to the Items collection
                        Items.Add(groupViewModel);
                    }

                    // Update the status message
                    StatusMessage = $"Loaded plugins for profile: {SelectedLoadOut.Name}";
                    UpdateStatus(StatusMessage);

                    // Mark initial data as loaded
                    _isInitialDataLoaded = true;
                }
            }
            finally
            {
                // Ensure initialization is marked as complete
                InitializationManager.EndInitialization(nameof(LoadOrderWindowViewModel));
            }
        }

        private LoadOrderItemViewModel CreateGroupViewModel(ModGroup group)
        {
            return new LoadOrderItemViewModel(group);
        }

        private void RefreshData()
        {
            if (InitializationManager.IsAnyInitializing()) return;
            UpdateStatus("Refreshing data...");

            if (SelectedLoadOut != null)
            {
                // Directly using the enabledPlugins hashset from SelectedLoadOut
                var enabledPluginIds = SelectedLoadOut.enabledPlugins;

                Items.Clear();
                foreach (var group in Groups)
                {
                    var groupViewModel = CreateGroupViewModel(group);

                    foreach (var plugin in group.Plugins ?? Enumerable.Empty<Plugin>())
                    {
                        var isEnabled = enabledPluginIds.Contains(plugin.PluginID); // Simplified check
                        groupViewModel.Children.Add(new LoadOrderItemViewModel
                        {
                            PluginData = plugin,
                            IsActive = isEnabled,
                            EntityType = EntityType.Plugin
                        });
                    }
                    Items.Add(groupViewModel);
                }

                StatusMessage = $"Loaded plugins for profile: {SelectedLoadOut.Name}";
                UpdateStatus(StatusMessage);
            }
            else
            {
                UpdateStatus("No LoadOut selected.");
            }
        }

    }
}