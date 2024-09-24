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
    public class LoadOrderWindowViewModel : INotifyPropertyChanged
    {
        private bool isSaved;
        private Timer cooldownTimer;
        private object? _selectedItem;
        private long? _selectedProfileId;
        private string _statusMessage;
        private string _searchText;

        public event PropertyChangedEventHandler? PropertyChanged;




        public object? SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
                ((RelayCommand<object>)MoveUpCommand).RaiseCanExecuteChanged();
                ((RelayCommand<object>)MoveDownCommand).RaiseCanExecuteChanged();
                UpdateStatusMessage();
            }
        }

        public long? SelectedProfileId
        {
            get => _selectedProfileId;
            set
            {
                if (_selectedProfileId != value)
                {
                    _selectedProfileId = value;
                    OnPropertyChanged(nameof(SelectedProfileId));
                }
            }
        }
        
        private LoadOut _selectedLoadOut;
        public LoadOut SelectedLoadOut
        {
            get => _selectedLoadOut;
            set
            {
                _selectedLoadOut = value;
                AggLoadInfo.Instance.ActiveLoadOut = value;
                OnPropertyChanged(nameof(SelectedLoadOut));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
            }
        }

        public ObservableCollection<ModGroup> Groups { get; set; }
        public ObservableCollection<Plugin> Plugins { get; set; }
        public ObservableCollection<LoadOut> LoadOuts { get; set; }
        public LoadOrdersViewModel LoadOrders { get; set; }
        public ObservableCollection<LoadOrderItemViewModel> Items { get; }

        public ICommand SaveCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        //public ICommand ImportPluginsCommand { get; }
        public ICommand SaveAsNewLoadoutCommand { get; }
        public ICommand OpenGameFolderCommand { get; }
        public ICommand OpenGameSaveFolderCommand { get; }
        public ICommand EditPluginsCommand { get; }
        public ICommand EditContentCatalogCommand { get; }
        public ICommand ImportContextCatalogCommand { get; }
        public ICommand SavePluginsCommand { get; }
        public ICommand EditHighlightedItemCommand { get; }
        public ICommand OpenAppDataFolderCommand { get; }
        public ICommand OpenGameLocalAppDataCommand { get; }
        public ICommand SettingsWindowCommand { get; }
        public ICommand ImportFromYamlCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand SaveLoadOutCommand { get; }
        public ICommand OpenGameSettingsCommand { get; }
        public ICommand OpenPluginEditorCommand { get; }
        public ICommand OpenGroupEditorCommand { get; }
        public ICommand RefreshDataCommand { get; }
        public ICommand CopyTextCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand ChangeGroupCommand { get; }
        public ICommand ToggleEnableCommand { get; }



        public LoadOrderWindowViewModel()
        {
            // Initialize collections and commands
            Groups = new ObservableCollection<ModGroup>();
            Plugins = new ObservableCollection<Plugin>();
            LoadOuts = new ObservableCollection<LoadOut>();
            LoadOrders = new LoadOrdersViewModel();
            Items = new ObservableCollection<LoadOrderItemViewModel>();

            // Initialize commands
            // Initialize commands
            SearchCommand = new RelayCommand<string?>(Search);
            MoveUpCommand = new RelayCommand<object?>(param => MoveUp(), param => CanMoveUp());
            MoveDownCommand = new RelayCommand<object?>(param => MoveDown(), param => CanMoveDown());
            SaveAsNewLoadoutCommand = new RelayCommand<object?>(param => SaveAsNewLoadout());
            OpenGameFolderCommand = new RelayCommand<object?>(param => OpenGameFolder());
            OpenGameSaveFolderCommand = new RelayCommand<object?>(param => OpenGameSaveFolder());
            EditPluginsCommand = new RelayCommand<object?>(param => EditPlugins());
            EditContentCatalogCommand = new RelayCommand<object?>(param => EditContentCatalog());
            ImportContextCatalogCommand = new RelayCommand<object?>(param => ImportContextCatalog());
            SavePluginsCommand = new RelayCommand<object?>(param => SavePlugins());
            EditHighlightedItemCommand = new RelayCommand<object?>(param => EditHighlightedItem());
            OpenAppDataFolderCommand = new RelayCommand<object?>(param => OpenAppDataFolder());
            OpenGameLocalAppDataCommand = new RelayCommand<object?>(param => OpenGameLocalAppData());
            SettingsWindowCommand = new RelayCommand<object?>(param => SettingsWindow());
            ImportFromYamlCommand = new RelayCommand<object?>(param => ImportFromYaml());
            OpenGameSettingsCommand = new RelayCommand<object?>(param => OpenGameSettings());
            OpenPluginEditorCommand = new RelayCommand<object?>(param => OpenPluginEditor());
            OpenGroupEditorCommand = new RelayCommand<object?>(param => OpenGroupEditor());
            RefreshDataCommand = new RelayCommand<object?>(param => RefreshData());
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
            InitializationManager.StartInitialization(nameof(LoadOrderWindowViewModel));
            try
            {
                if (AggLoadInfo.Instance != null)
                {
                    // Load initial collections from AggLoadInfo
                    Groups = new ObservableCollection<ModGroup>(AggLoadInfo.Instance.Groups);
                    Plugins = new ObservableCollection<Plugin>(AggLoadInfo.Instance.Plugins);
                    LoadOuts = new ObservableCollection<LoadOut>(AggLoadInfo.Instance.LoadOuts);

                    // Clear existing items
                    Items.Clear();

                    // Check if there is a LoadOut available
                    SelectedLoadOut = LoadOuts.FirstOrDefault();

                    if (SelectedLoadOut == null)
                    {
                        // Prompt the user to create a new LoadOut
                        var result = MessageBox.Show(
                            "No LoadOut found. Would you like to create a new LoadOut?",
                            "Create New LoadOut",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            // Create a new LoadOut if the user agrees
                            var newLoadOutName = "New LoadOut";
                            var newLoadOut = new LoadOut
                            {
                                Name = newLoadOutName,
                                ProfileID = GenerateNewProfileID(), // Generate a new profile ID
                                enabledPlugins = new HashSet<long>()
                            };

                            LoadOuts.Add(newLoadOut);
                            AggLoadInfo.Instance.LoadOuts.Add(newLoadOut); // Add to AggLoadInfo

                            SelectedLoadOut = newLoadOut;
                            StatusMessage = $"Created new LoadOut: {newLoadOut.Name}";
                        }
                        else
                        {
                            StatusMessage = "No LoadOut selected. Please create a new LoadOut.";
                            return;
                        }
                    }

                    // Use the enabledPlugins hashset directly from SelectedLoadOut
                    var enabledPluginIds = SelectedLoadOut.enabledPlugins;

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
                                IsEnabled = isEnabled,
                                EntityType = EntityType.Plugin
                            });
                        }

                        // Add the group view model to the Items collection
                        Items.Add(groupViewModel);
                    }

                    // Update the status message
                    StatusMessage = $"Loaded plugins for profile: {SelectedLoadOut.Name}";
                    UpdateStatus(StatusMessage);
                }
            }
            finally
            {
                // Ensure initialization is marked as complete
                InitializationManager.EndInitialization(nameof(LoadOrderWindowViewModel));
            }
        }

        // Helper method to generate a new ProfileID
        private long GenerateNewProfileID()
        {
            // Logic to generate a unique ProfileID, for example, incrementing the maximum existing ID
            return LoadOuts.Any() ? LoadOuts.Max(lo => lo.ProfileID) + 1 : 1;
        }





        private void UpdateStatusMessage()
        {
            if (SelectedItem != null)
            {
                StatusMessage = SelectedItem.ToString();
            }
            else
            {
                StatusMessage = "No item selected";
            }
        }

        private LoadOrderItemViewModel CreateGroupViewModel(ModGroup group)
        {
            return new LoadOrderItemViewModel(group);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (InitializationManager.IsAnyInitializing()) return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public void UpdateStatus(string message)
        {
            StatusMessage = message;
        }

        private void OpenPluginEditor()
        {
            if (SelectedItem is Plugin plugin)
            {
                var editorWindow = new PluginEditorWindow(plugin,SelectedLoadOut);
                if (editorWindow.ShowDialog() == true)
                {
                    RefreshData();
                }
            }
        }

        private void OpenGroupEditor()
        {

            if (SelectedItem is ModGroup modGroup)
            {
                var editorWindow = new ModGroupEditorWindow(modGroup);
                if (editorWindow.ShowDialog() == true)
                {
                    RefreshData();
                }
            }

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
                            IsEnabled = isEnabled,
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



        private bool CanExecuteSave()
        {
            return SelectedLoadOut != null;
        }

        private void Save(object? parameter)
        {
            if (SelectedLoadOut != null)
            {
                SelectedLoadOut.WriteProfile();
                UpdateStatus("Profile saved successfully.");
            }
            else
            {
                UpdateStatus("No loadout selected.");
            }
        }

        private void SaveCurrentState() => Save(this);

        private bool CanMoveUp()
        {
            if (SelectedItem is Plugin selectedPlugin)
            {
                if (selectedPlugin.GroupID == -999 || selectedPlugin.GroupID == -997)
                {
                    return false;
                }

                var group = Groups.FirstOrDefault(g => g.Plugins != null && g.Plugins.Contains(selectedPlugin));
                if (group != null)
                {
                    long index = group.Plugins.IndexOf(selectedPlugin);
                    return index > 0;
                }
            }
            else if (SelectedItem is ModGroup selectedGroup)
            {
                long index = Groups.IndexOf(selectedGroup);
                return index > 0;
            }
            return false;
        }

        private bool CanMoveDown()
        {
            if (SelectedItem is Plugin selectedPlugin)
            {
                if (selectedPlugin.GroupID == -999 || selectedPlugin.GroupID == -997)
                {
                    return false;
                }

                var group = Groups.FirstOrDefault(g => g.Plugins != null && g.Plugins.Contains(selectedPlugin));
                if (group != null)
                {
                    long index = group.Plugins.IndexOf(selectedPlugin);
                    return index < group.Plugins.Count - 1;
                }
            }
            else if (SelectedItem is ModGroup selectedGroup)
            {
                long index = Groups.IndexOf(selectedGroup);
                return index < Groups.Count - 1;
            }
            return false;
        }

        private void MoveUp()
        {
            if (SelectedItem is Plugin selectedPlugin)
            {
                var group = Groups.FirstOrDefault(g => g.Plugins != null && g.Plugins.Contains(selectedPlugin));
                if (group != null && group.Plugins != null)
                {
                    int index = group.Plugins.IndexOf(selectedPlugin);
                    var previousPlugin = group.Plugins[index - 1];

                    // Swap ordinals
                    long tempOrdinal = selectedPlugin.GroupOrdinal ?? 0;
                    selectedPlugin.GroupOrdinal = previousPlugin.GroupOrdinal;
                    previousPlugin.GroupOrdinal = tempOrdinal;

                    // Move the plugin
                    group.Plugins.Move(index, index - 1);
                }
            }
            else if (SelectedItem is ModGroup selectedGroup)
            {
                int index = Groups.IndexOf(selectedGroup);
                var previousGroup = Groups[index - 1];

                // Swap ordinals
                long tempOrdinal = selectedGroup.Ordinal ?? 0;
                selectedGroup.Ordinal = previousGroup.Ordinal;
                previousGroup.Ordinal = tempOrdinal;

                // Move the group
                Groups.Move(index, index - 1);
            }
            OnPropertyChanged(nameof(Groups));
        }

        private void MoveDown()
        {
            if (SelectedItem is Plugin selectedPlugin)
            {
                var group = Groups.FirstOrDefault(g => g.Plugins != null && g.Plugins.Contains(selectedPlugin));
                if (group != null && group.Plugins != null)
                {
                    int index = group.Plugins.IndexOf(selectedPlugin);
                    var nextPlugin = group.Plugins[index + 1];

                    // Swap ordinals
                    long tempOrdinal = selectedPlugin.GroupOrdinal ?? 0;
                    selectedPlugin.GroupOrdinal = nextPlugin.GroupOrdinal;
                    nextPlugin.GroupOrdinal = tempOrdinal;

                    // Move the plugin
                    group.Plugins.Move(index, index + 1);

                }
            }
            else if (SelectedItem is ModGroup selectedGroup)
            {
                int index = Groups.IndexOf(selectedGroup);
                var nextGroup = Groups[index + 1];

                // Swap ordinals
                long tempOrdinal = selectedGroup.Ordinal ?? 0;
                selectedGroup.Ordinal = nextGroup.Ordinal;
                nextGroup.Ordinal = tempOrdinal;

                // Move the group
                Groups.Move(index, index + 1);
            }
            OnPropertyChanged(nameof(Groups));
        }

        private void ImportPlugins(AggLoadInfo aggLoadInfo = null, string pluginsFile = null)
        {
            // If no AggLoadInfo is provided, use the singleton instance
            aggLoadInfo ??= AggLoadInfo.Instance;

            // Ensure the selected loadout is set in the AggLoadInfo object
            if (_selectedLoadOut != null)
            {
                aggLoadInfo.ActiveLoadOut = _selectedLoadOut;
            }
            else
            {
                throw new InvalidOperationException("No loadout selected for importing plugins.");
            }

            // Perform the import
            FileManager.ParsePluginsTxt(AggLoadInfo.Instance, pluginsFile);

            // Update the UI or any other necessary components
            RefreshData();
        }


        //private void ImportPlugins(string pluginsFile, string mode)
        //{
        //    try
        //    {
        //        // Parse the plugins file
        //        var aggLoadInfo = ParsePluginsFile(pluginsFile, mode);

        //        // Update the LoadOuts and Plugins collections
        //        LoadOuts.Clear();
        //        foreach (var loadOut in aggLoadInfo.LoadOuts)
        //        {
        //            LoadOuts.Add(loadOut);
        //        }

        //        Plugins.Clear();
        //        foreach (var plugin in aggLoadInfo.Plugins)
        //        {
        //            Plugins.Add(plugin);
        //        }

        //        // Update the UI based on the mode
        //        if (mode == "new")
        //        {
        //            SelectedLoadOut = LoadOuts.FirstOrDefault();
        //        }
        //        else
        //        {
        //            SelectedLoadOut = aggLoadInfo.LoadOuts.FirstOrDefault();
        //        }

        //        RefreshData();
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle any exceptions that occur during parsing
        //        StatusMessage = $"Error importing plugins: {ex.Message}";
        //    }
        //}


        //private AggLoadInfo ParsePluginsFile(string pluginsFile, string mode)
        //{
        //    // Call the ParsePluginsTxt method
        //    //return AggLoadInfo.ParsePluginsTxt(pluginsFile, null, mode);
        //}

        private void SaveAsNewLoadout()
        {
            //var inputDialog = new InputDialog("Enter the name for the new LoadOut:", "New LoadOut");
            //if (inputDialog.ShowDialog() == true)
            //{
            //    var newProfileName = inputDialog.ResponseText;

            //    try
            //    {
            //        var newLoadOut = new LoadOut(newProfileName, SelectedLoadOut);
            //        newLoadOut.WriteProfile();
            //        LoadOuts.Add(newLoadOut);

            //        MessageBox.Show("New loadout saved successfully.", "Save As New Loadout", MessageBoxButton.OK, MessageBoxImage.Information);
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //}
        }

        private void OpenGameFolder()
        {
            OpenFolder(FileManager.GameFolder);
        }

        private void OpenGameSaveFolder()
        {
            OpenFolder(FileManager.GameSaveFolder);
        }

        private void EditPlugins()
        {
            string pluginsFilePath = FileManager.PluginsFile ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "starfield", "plugins.txt");
            OpenFile(pluginsFilePath);
        }

        private void EditContentCatalog()
        {
            string contentCatalogPath = FileManager.ContentCatalogFile ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "starfield", "ContentCatalog.txt");
            OpenFile(contentCatalogPath);
        }

        private void ImportContextCatalog()
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "starfield"),
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Select ContentCatalog.txt file"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var selectedFile = openFileDialog.FileName;
                FileManager.ParseContentCatalogTxt(selectedFile);

                _ = MessageBox.Show("Content catalog imported successfully.", "Import Content Catalog", MessageBoxButton.OK, MessageBoxImage.Information);

                RefreshData();
            }
        }

        private void SavePlugins()
        {
            Save(this);
            if (SelectedProfileId.HasValue)
            {
                //var currentLoadOut = SelectedLoadOut;
                AggLoadInfo.Instance.ActiveLoadOut = SelectedLoadOut;
                if (AggLoadInfo.Instance.ActiveLoadOut == null)
                {
                    StatusMessage = "Selected profile not found.";
                    return;
                }

                var profileName = AggLoadInfo.Instance.ActiveLoadOut.Name;
                var defaultFileName = $"Plugins_{profileName}.txt";
                var defaultFilePath = Path.Combine(FileManager.AppDataFolder, defaultFileName);

                var result = MessageBox.Show($"Producing {defaultFilePath}. Do you want to save to a different location?", "Save Plugins", MessageBoxButton.YesNo);

                string? outputFileName = null;
                if (result == MessageBoxResult.Yes)
                {
                    var saveFileDialog = new SaveFileDialog
                    {
                        FileName = defaultFileName,
                        DefaultExt = ".txt",
                        Filter = "Text documents (.txt)|*.txt",
                        InitialDirectory = FileManager.AppDataFolder
                    };

                    bool? dialogResult = saveFileDialog.ShowDialog();
                    if (dialogResult == true)
                    {
                        outputFileName = saveFileDialog.FileName;
                    }
                }

                FileManager.ProducePluginsTxt(AggLoadInfo.Instance.ActiveLoadOut, outputFileName);
                StatusMessage = "Plugins.txt file has been successfully created.";
            }
            else
            {
                StatusMessage = "Please select a profile to save the plugins.txt file.";
            }
        }
        
        private bool CanExecuteEdit(object parameter)
        {
            return true; // Add your logic here
        }
        
        public void EditHighlightedItem()
        {
            if (SelectedItem is LoadOrderItemViewModel selectedItem)
            {
                var underlyingObject = EntityTypeHelper.GetUnderlyingObject(selectedItem);

                switch (selectedItem.EntityType)
                {
                    case EntityType.Group:
                        var modGroup = underlyingObject as ModGroup;
                        if (modGroup != null)
                        {
                            var editorWindow = new ModGroupEditorWindow(modGroup);
                            if (editorWindow.ShowDialog() == true)
                            {
                                // Handle successful edit
                            }
                        }
                        else
                        {
                            MessageBox.Show("ModGroup not found. Please create a new group using the group editor.", "Group Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        break;

                    case EntityType.Plugin:
                        var plugin = underlyingObject as Plugin;
                        if (plugin != null)
                        {
                            var pluginEditorWindow = new PluginEditorWindow(plugin, SelectedLoadOut);
                            if (pluginEditorWindow.ShowDialog() == true)
                            {
                                // Handle successful edit
                            }
                        }
                        break;

                    default:
                        MessageBox.Show("Please select a valid item to edit.", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                }
            }
            else
            {
                MessageBox.Show("Please select a valid item to edit.", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            RefreshData();
        }

        private void OpenAppDataFolder()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZeeOgre", "LoadOrderManager");
            OpenFolder(appDataPath);
        }

        private void OpenGameLocalAppData()
        {
            string gameAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "starfield");
            OpenFolder(gameAppDataPath);
        }

        private void SettingsWindow()
        {
            try
            {
                var settingsWindow = new SettingsWindow(SettingsLaunchSource.MainWindow)
                {
                    Tag = "Settings"
                };
                _ = settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                App.LogDebug($"Exception in SettingsWindow_Click: {ex.Message}");
                _ = MessageBox.Show("An error occurred while trying to open the settings window.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CopyText()
        {
            if (SelectedItem is LoadOrderItemViewModel selectedItem)
            {
                var underlyingObject = EntityTypeHelper.GetUnderlyingObject(selectedItem);
                string textToCopy = underlyingObject?.ToString() ?? string.Empty;
                Clipboard.SetText(textToCopy);
            }
        }

        private bool CanExecuteCopyText(object parameter)
        {
            return SelectedItem is LoadOrderItemViewModel selectedItem &&
                   (selectedItem.EntityType == EntityType.Group || selectedItem.EntityType == EntityType.Plugin);
        }

        public void Delete()
        {
            if (SelectedItem is LoadOrderItemViewModel selectedItem)
            {
                var parentGroup = selectedItem.FindModGroup(selectedItem.DisplayName);
                if (parentGroup != null)
                {
                    // Adjust ordinals of subsequent siblings
                    var siblings = parentGroup.Plugins?.Where(p => p.GroupOrdinal > selectedItem.PluginData.GroupOrdinal).ToList();
                    if (siblings != null)
                    {
                        foreach (var sibling in siblings)
                        {
                            sibling.GroupOrdinal--;
                        }
                    }

                    // Remove the mod from the group
                    parentGroup.Plugins?.Remove(selectedItem.PluginData);

                    // Move to unassigned group (-996)
                    MoveToUnassignedGroup(selectedItem.PluginData);
                }
            }
        }

        private void ChangeGroup(object parameter)
        {
            if (SelectedItem is ModGroup modGroup)
            {
                modGroup.ChangeGroup((long)parameter); // Cast parameter to long
            }
            else if (SelectedItem is Plugin plugin)
            {
                plugin.ChangeGroup((long)parameter); // Cast parameter to long
            }
        }

        private bool CanExecuteChangeGroup(object parameter) { return true; }

        private void ToggleEnable(object parameter)
        {
            if (SelectedLoadOut != null && parameter is PluginViewModel pluginViewModel)
            {
                if (pluginViewModel.IsEnabled)
                {
                    LoadOut.SetPluginEnabled(SelectedLoadOut.ProfileID, pluginViewModel.pluginID, false);
                }
                else
                {
                    LoadOut.SetPluginEnabled(SelectedLoadOut.ProfileID, pluginViewModel.pluginID, true);
                }

                pluginViewModel.IsEnabled = !pluginViewModel.IsEnabled;
            }
            else
            {
                UpdateStatus("No loadout or plugin selected.");
            }
        }


        private bool CanExecuteToggleEnable(object parameter)
        {
            return SelectedLoadOut != null && parameter is PluginViewModel;
        }

        private bool CanExecuteDelete(object parameter)
        {
            return SelectedItem is LoadOrderItemViewModel selectedItem &&
                   (selectedItem.EntityType == EntityType.Group || selectedItem.EntityType == EntityType.Plugin);
        }

        private void ImportFromYaml()
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = FileManager.AppDataFolder,
                Filter = "YAML files (*.yaml)|*.yaml|All files (*.*)|*.*",
                Title = "Select YAML file"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var selectedFile = openFileDialog.FileName;
                try
                {
                    _ = Config.LoadFromYaml(selectedFile);
                    _ = MessageBox.Show("Configuration loaded successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    App.LogDebug($"Exception in ImportFromYaml_Click: {ex.Message}");
                    _ = MessageBox.Show("An error occurred while loading the configuration.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenGameSettings()
        {
            OpenFolder(FileManager.GameDocsFolder);
        }

        private void OpenFolder(string path)
        {
            try
            {
                _ = Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                App.LogDebug($"Exception in OpenFolder: {ex.Message}");
                _ = MessageBox.Show("An error occurred while trying to open the folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenFile(string path)
        {
            try
            {
                _ = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                App.LogDebug($"Exception in OpenFile: {ex.Message}");
                _ = MessageBox.Show("An error occurred while trying to open the file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void Search(string? searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                // If search text is empty, show all items
                RefreshData();
            }
            else
            {
                // Filter Groups and Plugins based on the search text
                var filteredGroups = new ObservableCollection<ModGroup>(
                    Groups.Where(g => g.GroupName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                      (g.Plugins != null && g.Plugins.Any(p => p.PluginName.Contains(searchText, StringComparison.OrdinalIgnoreCase))))
                );

                var filteredPlugins = new ObservableCollection<Plugin>(
                    Plugins.Where(p => p.PluginName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                );

                // Update the collections
                Groups = filteredGroups;
                Plugins = filteredPlugins;

                // Notify the UI about the changes
                OnPropertyChanged(nameof(Groups));
                OnPropertyChanged(nameof(Plugins));
            }
        }

        public void SelectPreviousItem()
        {
            if (SelectedItem is not LoadOrderItemViewModel selectedItem || Items == null || Items.Count == 0)
                return;

            var flatList = Flatten(Items).ToList();
            var currentIndex = flatList.IndexOf(selectedItem);

            if (currentIndex > 0)
            {
                SelectedItem = flatList[currentIndex - 1];
            }
        }

        public void SelectNextItem()
        {
            if (SelectedItem is not LoadOrderItemViewModel selectedItem || Items == null || Items.Count == 0)
                return;

            var flatList = Flatten(Items).ToList();
            var currentIndex = flatList.IndexOf(selectedItem);

            if (currentIndex < flatList.Count - 1)
            {
                SelectedItem = flatList[currentIndex + 1];
            }
        }

        public void SelectFirstItem()
        {
            if (Items == null || Items.Count == 0)
                return;

            SelectedItem = Flatten(Items).FirstOrDefault();
        }

        public void SelectLastItem()
        {
            if (Items == null || Items.Count == 0)
                return;

            SelectedItem = Flatten(Items).LastOrDefault();
        }
        
        private void MoveToUnassignedGroup(Plugin plugin)
        {
            var unassignedGroup = Groups.FirstOrDefault(g => g.GroupID == -996);
            if (unassignedGroup == null)
            {
                unassignedGroup = new ModGroup { GroupID = -996, GroupName = "Unassigned" };
                Groups.Add(unassignedGroup);
            }

            plugin.GroupID = unassignedGroup.GroupID;
            plugin.GroupOrdinal = unassignedGroup.Plugins?.Count ?? 0;
            unassignedGroup.Plugins?.Add(plugin);
        }


        private IEnumerable<LoadOrderItemViewModel> Flatten(ObservableCollection<LoadOrderItemViewModel> items)
        {
            foreach (var item in items)
            {
                yield return item;

                foreach (var child in Flatten(item.Children))
                {
                    yield return child;
                }
            }
        }

    }
}