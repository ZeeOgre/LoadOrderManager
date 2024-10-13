using Microsoft.Win32; // For OpenFileDialog
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ZO.LoadOrderManager
{

    public enum SettingsLaunchSource
    {
        MainWindow,
        CommandLine,
        DatabaseInitialization,
        MissingConfigDialog
    }


    public class SettingsViewModel : ViewModelBase
    {
        private Config _config;
        private readonly DbManager _dbManager;

        public event Action SaveCompleted;

        // Bindable properties for UI
        public ObservableCollection<FileInfo> MonitoredFiles { get; set; }

        public FileInfo SelectedMonitoredFile { get; set; }

        public bool AutoCheckAtStartup
        {
            get => _config.AutoCheckForUpdates;
            set
            {
                if (_config.AutoCheckForUpdates != value)
                {
                    _config.AutoCheckForUpdates = value;
                    OnPropertyChanged();
                }
            }
        }

        public string GameFolder
        {
            get => _config.GameFolder;
            set
            {
                if (_config.GameFolder != value)
                {
                    _config.GameFolder = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DarkMode
        {
            get => _config.DarkMode;
            set
            {
                if (_config.DarkMode != value)
                {
                    _config.DarkMode = value;
                    OnPropertyChanged();

                    // Call the updated methods from the App class
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Apply the modern theme and custom TreeView theme
                        //((App)Application.Current).ApplyModernTheme();
                        //((App)Application.Current).ApplyCustomTheme(_config.DarkMode);
                    });
                    Save();

                }
            }
        }

        public string Version => App.Version;

        // Read-only command properties initialized in the constructor
        public ICommand AddNewMonitoredFileCommand { get; private set; }
        public ICommand RestartMonitorCommand { get; private set; }
        public ICommand VacuumReindexCommand { get; private set; }
        public ICommand CleanOrdinalsCommand { get; private set; }
        public ICommand EditFileCommand { get; private set; }
        public ICommand CompareFileCommand { get; private set; }
        public ICommand BrowseGameFolderCommand { get; private set; }
        public ICommand CheckForUpdatesCommand { get; private set; }
        public ICommand LoadFromYamlCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }

        public SettingsViewModel()
        {
            _config = Config.Instance;
            _dbManager = DbManager.Instance;
            InitializeViewModel();
        }

        public void UseEmptyConfig()
        {
            _config = new Config(); // Create a new empty config object
            InitializeViewModel();  // Re-initialize commands and properties with the new config
        }

        private void InitializeViewModel()
        {
            // Initialize monitored files from Config
            MonitoredFiles = new ObservableCollection<FileInfo>(_config.MonitoredFiles);

            // Initialize commands in the constructor
            AddNewMonitoredFileCommand = new RelayCommand(_ => AddNewFile());
            RestartMonitorCommand = new RelayCommand(_ => RestartMonitor());
            VacuumReindexCommand = new RelayCommand(_ => VacuumDatabase());
            CleanOrdinalsCommand = new RelayCommand(_ => CleanOrdinals());
            EditFileCommand = new RelayCommand<FileInfo>(file => EditFile(file));
            CompareFileCommand = new RelayCommand<FileInfo>(file => CompareFile(file));
            BrowseGameFolderCommand = new RelayCommand(_ => BrowseGameFolder());
            CheckForUpdatesCommand = new RelayCommand(_ => CheckForUpdates());
            LoadFromYamlCommand = new RelayCommand(_ => LoadFromYaml());
            SaveCommand = new RelayCommand(_ => Save());
        }

        private void AddNewFile()
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                var filePath = openFileDialog.FileName;
                var newFileInfo = new FileInfo(filePath, true, true);

                MonitoredFiles.Add(newFileInfo);
                _config.MonitoredFiles.Add(newFileInfo);

                OnPropertyChanged(nameof(MonitoredFiles));
            }
        }

        private void RestartMonitor()
        {
            FileMonitor.InitializeAllMonitors();
        }

        private void VacuumDatabase()
        {
            try
            {
                DbManager.FlushDB();
                _dbManager.Initialize();
                MessageBox.Show("Database vacuum and reindex completed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during vacuum and reindex: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CleanOrdinals()
        {
            try
            {
                // Suppress all ObservableCollection change notifications using UniversalCollectionDisabler

                using (var connection = DbManager.Instance.GetConnection())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        const string cleanOrdinalsQuery = @"
                    WITH OrderedGSP AS (
                        SELECT GroupSetID, GroupID, PluginID,
                               ROW_NUMBER() OVER (PARTITION BY GroupSetID, GroupID ORDER BY Ordinal) AS NewOrdinal
                        FROM GroupSetPlugins
                    )
                    UPDATE GroupSetPlugins
                    SET Ordinal = OrderedGSP.NewOrdinal
                    FROM OrderedGSP
                    WHERE GroupSetPlugins.GroupSetID = OrderedGSP.GroupSetID
                      AND GroupSetPlugins.GroupID = OrderedGSP.GroupID
                      AND GroupSetPlugins.PluginID = OrderedGSP.PluginID;

                    WITH OrderedGSG AS (
                        SELECT GroupSetID, ParentID, GroupID,
                               CASE WHEN GroupID < 0 THEN -GroupID + 9000
                                    ELSE ROW_NUMBER() OVER (PARTITION BY GroupSetID, ParentID ORDER BY Ordinal)
                               END AS NewOrdinal
                        FROM GroupSetGroups
                    )
                    UPDATE GroupSetGroups
                    SET Ordinal = OrderedGSG.NewOrdinal
                    FROM OrderedGSG
                    WHERE GroupSetGroups.GroupSetID = OrderedGSG.GroupSetID
                      AND GroupSetGroups.ParentID = OrderedGSG.ParentID
                      AND GroupSetGroups.GroupID = OrderedGSG.GroupID;
                ";

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = cleanOrdinalsQuery;
                            command.Transaction = transaction;
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                }

                // Refresh metadata from the database after the transaction
                AggLoadInfo.Instance.RefreshMetadataFromDB();
            
                // Inform the user of success
                MessageBox.Show("Ordinals cleaned successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
            catch (Exception ex)
            {
                // Handle and inform the user of any errors
                MessageBox.Show($"Error during cleaning ordinals: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
}


        private void CompareFile(FileInfo file)
        {
            if (file.FileContent != null && System.IO.File.Exists(file.AbsolutePath))
            {
                var currentFileContent = System.IO.File.ReadAllBytes(file.AbsolutePath);
                var diffViewer = new DiffViewer(file);
                diffViewer.Show();
            }
            else
            {
                var openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() == true)
                {
                    file.AbsolutePath = openFileDialog.FileName;
                    var currentFileContent = System.IO.File.ReadAllBytes(file.AbsolutePath);
                    var diffViewer = new DiffViewer(file);
                    diffViewer.Show();
                }
            }
        }

        private void EditFile(FileInfo file)
        {
            if (!string.IsNullOrEmpty(file.AbsolutePath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = file.AbsolutePath,
                    UseShellExecute = true
                });
            }
        }

        private void BrowseGameFolder()
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder",
                Filter = "Folders|*.",
                Title = "Select the game folder"
            };

            if (dialog.ShowDialog() == true)
            {
                string folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                _config.GameFolder = folderPath;
                OnPropertyChanged(nameof(GameFolder));
            }
        }

        private void CheckForUpdates()
        {
            App.CheckForUpdates(Application.Current.MainWindow);
        }

        private void LoadFromYaml()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZeeOgre", "LoadOrderManager"),
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

        public void Save()
        {
            try
            {
                // Update the singleton instance with current values
                Config.Instance.UpdateFrom(_config);

                // Save to YAML
                Config.SaveToYaml();

                // Save to Database
                Config.SaveToDatabase();

                SaveCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving configuration: " + ex.Message);
            }
        }

    }
}
