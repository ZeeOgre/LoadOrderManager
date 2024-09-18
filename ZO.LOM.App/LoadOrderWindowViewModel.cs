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
        private int? _selectedProfileId;
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
                UpdateStatusMessage();
            }
        }

        public int? SelectedProfileId
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
        public ICommand ImportPluginsCommand { get; }
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
            SearchCommand = new RelayCommand<string?>(Search);
            SaveCommand = new RelayCommand(Save, CanExecuteSave);
            MoveUpCommand = new RelayCommand(MoveUp, CanMoveUp);
            MoveDownCommand = new RelayCommand(MoveDown, CanMoveDown);
            ImportPluginsCommand = new RelayCommand(ImportPlugins);
            SaveAsNewLoadoutCommand = new RelayCommand(SaveAsNewLoadout);
            OpenGameFolderCommand = new RelayCommand(OpenGameFolder);
            OpenGameSaveFolderCommand = new RelayCommand(OpenGameSaveFolder);
            EditPluginsCommand = new RelayCommand(EditPlugins);
            EditContentCatalogCommand = new RelayCommand(EditContentCatalog);
            ImportContextCatalogCommand = new RelayCommand(ImportContextCatalog);
            SavePluginsCommand = new RelayCommand(SavePlugins);
            EditHighlightedItemCommand = new RelayCommand(EditHighlightedItem);
            OpenAppDataFolderCommand = new RelayCommand(OpenAppDataFolder);
            OpenGameLocalAppDataCommand = new RelayCommand(OpenGameLocalAppData);
            SettingsWindowCommand = new RelayCommand(SettingsWindow);
            ImportFromYamlCommand = new RelayCommand(ImportFromYaml);
            SaveLoadOutCommand = new RelayCommand(Save);
            OpenGameSettingsCommand = new RelayCommand(OpenGameSettings);
            OpenPluginEditorCommand = new RelayCommand(OpenPluginEditor);
            OpenGroupEditorCommand = new RelayCommand(OpenGroupEditor);
            RefreshDataCommand = new RelayCommand(RefreshData);
            CopyTextCommand = new RelayCommand(CopyText, () => CanExecuteCopyText(null));
            DeleteCommand = new RelayCommand(Delete, () => CanExecuteDelete(null));
            EditCommand = new RelayCommand(EditHighlightedItem, () => CanExecuteEdit(null));
            ToggleEnableCommand = new RelayCommand(() => ToggleEnable(null), () => CanExecuteToggleEnable(null));
            ChangeGroupCommand = new RelayCommandWithParameter(ChangeGroup, CanExecuteChangeGroup);
            CopyTextCommand = new RelayCommand(CopyText, () => CanExecuteCopyText(null));
            DeleteCommand = new RelayCommand(Delete, () => CanExecuteDelete(null));

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
                    // Load groups, plugins, and loadouts from the singleton instance
                    Groups = new ObservableCollection<ModGroup>(AggLoadInfo.Instance.Groups);
                    Plugins = new ObservableCollection<Plugin>(AggLoadInfo.Instance.Plugins);
                    LoadOuts = new ObservableCollection<LoadOut>(AggLoadInfo.Instance.LoadOuts);

                    // Initialize Items collection
                    Items.Clear();
                    foreach (var group in Groups)
                    {
                        Items.Add(CreateGroupViewModel(group));
                    }

                    SelectedProfileId = LoadOuts.FirstOrDefault()?.ProfileID;
                }
                else
                {
                    // Handle the case where the singleton instance is not initialized
                    MessageBox.Show("Singleton is not initialized. Please initialize the singleton before opening the window.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                InitializationManager.EndInitialization(nameof(LoadOrderWindowViewModel));
            }
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
            return new LoadOrderItemViewModel
            {
                DisplayName = group.GroupName ?? string.Empty,
                EntityType = EntityType.Group,
                PluginData = null,
                IsEnabled = true,
                Children = new ObservableCollection<LoadOrderItemViewModel>(
                    group.Plugins?.OrderBy(p => p.GroupOrdinal).Select(p => new LoadOrderItemViewModel
                    {
                        DisplayName = p.PluginName,
                        EntityType = EntityType.Plugin,
                        PluginData = p,
                        IsEnabled = p.Achievements
                    }) ?? Enumerable.Empty<LoadOrderItemViewModel>()
                )
            };
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
                var editorWindow = new PluginEditorWindow(plugin);
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
            if (SelectedProfileId.HasValue)
            {
                // Retrieve active plugins for the profile ID
                var activePlugins = LoadOut.GetActivePlugins(SelectedProfileId.Value);

                // Update the Plugins collection with the active plugins
                Plugins = new ObservableCollection<Plugin>(
                    activePlugins.Select(p => p.Plugin)
                );

                // Refresh the Items collection
                Items.Clear();
                foreach (var group in Groups)
                {
                    Items.Add(CreateGroupViewModel(group));
                }

                StatusMessage = $"Loaded plugins for profile ID: {SelectedProfileId}";
                UpdateStatus(StatusMessage);
            }
        }
        private bool CanExecuteSave()
        {
            return true; // Add your logic here
        }

        private void Save()
        {
            // Save the current state of groups and plugins
            foreach (var group in Groups)
            {
                if (group.GroupID >= 0)
                {
                    group.WriteGroup();
                    foreach (var plugin in group.Plugins)
                    {
                        plugin.WriteMod();
                    }
                }
            }

            // Save the current loadout
            var currentLoadOut = LoadOuts.FirstOrDefault(lo => lo.ProfileID == _selectedProfileId);
            if (currentLoadOut != null)
            {
                currentLoadOut.WriteProfile();
            }

            // Create and populate AggLoadInfo
            var aggLoadInfo = AggLoadInfo.Instance;

            // Save to database
            aggLoadInfo.SaveToDatabase();

            isSaved = true;
        }
        private void SaveCurrentState() => Save();

        private bool CanMoveUp()
        {
            if (SelectedItem is Plugin selectedPlugin)
            {
                if (selectedPlugin.GroupID == -999 || selectedPlugin.GroupID == -997)
                {
                    return false;
                }

                var group = Groups.FirstOrDefault(g => g.Plugins != null && g.Plugins.Contains(selectedPlugin));
                if (group != null && group.Plugins != null)
                {
                    int index = group.Plugins.IndexOf(selectedPlugin);
                    return index > 0;
                }
            }
            else if (SelectedItem is ModGroup selectedGroup)
            {
                int index = Groups.IndexOf(selectedGroup);
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
                if (group != null && group.Plugins != null)
                {
                    int index = group.Plugins.IndexOf(selectedPlugin);
                    return index < group.Plugins.Count - 1;
                }
            }
            else if (SelectedItem is ModGroup selectedGroup)
            {
                int index = Groups.IndexOf(selectedGroup);
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
                    int tempOrdinal = selectedPlugin.GroupOrdinal ?? 0;
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
                int tempOrdinal = selectedGroup.Ordinal ?? 0;
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
                    int tempOrdinal = selectedPlugin.GroupOrdinal ?? 0;
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
                int tempOrdinal = selectedGroup.Ordinal ?? 0;
                selectedGroup.Ordinal = nextGroup.Ordinal;
                nextGroup.Ordinal = tempOrdinal;

                // Move the group
                Groups.Move(index, index + 1);
            }
            OnPropertyChanged(nameof(Groups));
        }

        private void ImportPlugins()
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Path.GetDirectoryName(FileManager.PluginsFile),
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Select Plugins.txt file"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var selectedFile = openFileDialog.FileName;
                FileManager.ParsePluginsTxt(selectedFile);

                var result = MessageBox.Show("Do you want to create a new loadout with these plugins?", "New Loadout", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    SaveAsNewLoadout();
                }
                else if (result == MessageBoxResult.No)
                {
                    SavePlugins();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            RefreshData();
        }

        private void SaveAsNewLoadout()
        {
            var inputDialog = new InputDialog("Enter the name for the new LoadOut:", "New LoadOut");
            if (inputDialog.ShowDialog() == true)
            {
                var newProfileName = inputDialog.ResponseText;

                try
                {
                    // Create a new LoadOut instance
                    var newLoadOut = new LoadOut
                    {
                        Name = newProfileName,
                        Plugins = new ObservableCollection<PluginViewModel>(Plugins.Select(p => new PluginViewModel
                        {
                            Plugin = p,
                            IsEnabled = true
                        }))
                    };

                    // Write the new profile to the database
                    newLoadOut.WriteProfile();

                    // Add the new LoadOut to the collection
                    LoadOuts.Add(newLoadOut);

                    MessageBox.Show("New loadout saved successfully.", "Save As New Loadout", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
            Save();
            if (SelectedProfileId.HasValue)
            {
                var currentLoadOut = LoadOuts.FirstOrDefault(lo => lo.ProfileID == SelectedProfileId.Value);
                if (currentLoadOut == null)
                {
                    StatusMessage = "Selected profile not found.";
                    return;
                }

                var profileName = currentLoadOut.Name;
                var defaultFileName = $"profile_{profileName}.txt";
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

                FileManager.ProducePluginsTxt(currentLoadOut, outputFileName);
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
                            var pluginEditorWindow = new PluginEditorWindow(plugin);
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
                modGroup.ChangeGroup((int)parameter); // Cast parameter to int
            }
            else if (SelectedItem is Plugin plugin)
            {
                plugin.ChangeGroup((int)parameter); // Cast parameter to int
            }
        }

        private bool CanExecuteChangeGroup(object parameter) { return true; }

        private void ToggleEnable(object parameter)
        {
            if (SelectedItem is LoadOrderItemViewModel selectedItem && selectedItem.EntityType == EntityType.Plugin)
            {
                var plugin = selectedItem.PluginData;
                var loadOut = LoadOuts.FirstOrDefault(lo => lo.ProfileID == SelectedProfileId);
                if (loadOut != null)
                {
                    var pluginViewModel = loadOut.Plugins.FirstOrDefault(pvm => pvm.Plugin.PluginID == plugin.PluginID);
                    if (pluginViewModel != null)
                    {
                        pluginViewModel.IsEnabled = !pluginViewModel.IsEnabled;
                    }
                    else
                    {
                        loadOut.Plugins.Add(new PluginViewModel(plugin, true));
                    }
                }
            }
        }

        private bool CanExecuteToggleEnable(object parameter)
        {
            return SelectedItem is LoadOrderItemViewModel selectedItem && selectedItem.EntityType == EntityType.Plugin;
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