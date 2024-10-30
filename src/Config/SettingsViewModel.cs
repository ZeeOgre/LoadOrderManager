using Microsoft.Win32; // For OpenFileDialog
using System;
using System.Collections.ObjectModel;
using System.Data.SQLite;
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

    class SettingsViewModel : ViewModelBase
    {
        private Config _config;
        private readonly DbManager _dbManager;

        public event Action SaveCompleted;
        public ObservableCollection<FileInfo> MonitoredFiles { get; set; }
        public FileInfo SelectedMonitoredFile { get; set; }


        public bool AutoScanModRepoFolder
        {
            get => _config.AutoScanModRepoFolder;
            set
            {
                if (_config.AutoScanModRepoFolder != value)
                {
                    _config.AutoScanModRepoFolder = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AutoScanGameFolder
        {
            get => _config.AutoScanGameFolder;
            set
            {
                if (_config.AutoScanGameFolder != value)
                {
                    _config.AutoScanGameFolder = value;
                    OnPropertyChanged();
                }
            }
        }


        public bool AutoCheckForUpdates
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




        // New properties for the additional settings
        public string LootExePath
        {
            get => _config.LootExePath;
            set
            {
                if (_config.LootExePath != value)
                {
                    _config.LootExePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string NexusExportFile
        {
            get => _config.NexusExportFile;
            set
            {
                if (_config.NexusExportFile != value)
                {
                    _config.NexusExportFile = value;
                    OnPropertyChanged();
                }
            }
        }

        public string MO2ExportFile
        {
            get => _config.MO2ExportFile;
            set
            {
                if (_config.MO2ExportFile != value)
                {
                    _config.MO2ExportFile = value;
                    OnPropertyChanged();
                }
            }
        }

        public int WebServicePort
        {
            get => _config.WebServicePort;
            set
            {
                if (_config.WebServicePort != value)
                {
                    _config.WebServicePort = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool PluginWarning
        {
            get => _config.PluginWarning;
            set
            {
                if (_config.PluginWarning != value)
                {
                    _config.PluginWarning = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowDiff
        {
            get => _config.ShowDiff;
            set
            {
                if (_config.ShowDiff != value)
                {
                    _config.ShowDiff = value;
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
                    Save();
                }
            }
        }

        public string ModManagerExecutable
        {
            get => _config.ModManagerExecutable;
            set
            {
                if (_config.ModManagerExecutable != value)
                {
                    _config.ModManagerExecutable = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ModManagerArguments
        {
            get => _config.ModManagerArguments;
            set
            {
                if (_config.ModManagerArguments != value)
                {
                    _config.ModManagerArguments = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ModManagerRepoFolder
        {
            get => _config.ModManagerRepoFolder;
            set
            {
                if (_config.ModManagerRepoFolder != value)
                {
                    _config.ModManagerRepoFolder = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Version => App.Version;

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
        public ICommand BrowseModManagerExecutableCommand { get; private set; }
        public ICommand BrowseModManagerRepoFolderCommand { get; private set; }

        // New commands for additional settings
        public ICommand BrowseLootExecutableCommand { get; private set; }

        public SettingsViewModel()
        {
            _config = Config.Instance;
            _dbManager = DbManager.Instance;
            InitializeViewModel();
        }

        public void UseEmptyConfig()
        {
            _config = new Config();
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            MonitoredFiles = new ObservableCollection<FileInfo>(_config.MonitoredFiles);

            AddNewMonitoredFileCommand = new RelayCommand(_ => AddNewFile());
            RestartMonitorCommand = new RelayCommand(_ => RestartMonitor());
            VacuumReindexCommand = new RelayCommand(_ => VacuumDatabase());
            CleanOrdinalsCommand = new RelayCommand(_ => CleanOrdinals(true, false));
            EditFileCommand = new RelayCommand<FileInfo>(file => EditFile(file));
            CompareFileCommand = new RelayCommand<FileInfo>(file => CompareFile(file));
            BrowseGameFolderCommand = new RelayCommand(_ => BrowseGameFolder());
            CheckForUpdatesCommand = new RelayCommand(_ => CheckForUpdates());
            LoadFromYamlCommand = new RelayCommand(_ => LoadFromYaml());
            SaveCommand = new RelayCommand(_ => Save());
            BrowseModManagerExecutableCommand = new RelayCommand(_ => BrowseModManagerExecutable());
            BrowseModManagerRepoFolderCommand = new RelayCommand(_ => BrowseModManagerRepoFolder());
            BrowseLootExecutableCommand = new RelayCommand(_ => BrowseLootExecutable());
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
                _ = MessageBox.Show("Database vacuum and reindex completed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error during vacuum and reindex: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        public static void CleanOrdinals(bool refreshMetadata = true, bool quiet = false)
        {
            try
            {
                using (var connection = DbManager.Instance.GetConnection())
                {
                    using var transaction = connection.BeginTransaction();
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
                        _ = command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }

                if (refreshMetadata)
                {
                    AggLoadInfo.Instance.RefreshMetadataFromDB();
                    if (!quiet) _ = MessageBox.Show("Ordinals cleaned successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error during cleaning ordinals: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CompareFile(FileInfo file)
        {
            if (file.FileContent != null && System.IO.File.Exists(file.AbsolutePath))
            {
                _ = System.IO.File.ReadAllBytes(file.AbsolutePath);
                var diffViewer = new DiffViewer(file);
                diffViewer.Show();
            }
            else
            {
                var openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() == true)
                {
                    file.AbsolutePath = openFileDialog.FileName;
                    _ = System.IO.File.ReadAllBytes(file.AbsolutePath);
                    var diffViewer = new DiffViewer(file);
                    diffViewer.Show();
                }
            }
        }

        private void EditFile(FileInfo file)
        {
            if (!string.IsNullOrEmpty(file.AbsolutePath))
            {
                _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
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

        private void BrowseModManagerExecutable()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select Mod Manager Executable"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ModManagerExecutable = openFileDialog.FileName;
            }
        }

        private void BrowseModManagerRepoFolder()
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder",
                Filter = "Folders|*.",
                Title = "Select your Mod Staging Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                string folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                ModManagerRepoFolder = folderPath;
            }
        }

        private void BrowseLootExecutable()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select Loot Executable"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LootExePath = openFileDialog.FileName;
            }
        }

        private void CheckForUpdates()
        {
            App.CheckForUpdates(Application.Current.MainWindow);
        }

        private void LoadFromYaml()
        {
            var openFileDialog = new OpenFileDialog
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
                    _config = Config.LoadFromYaml(selectedFile);
                    InitializeViewModel();
                    OnPropertyChanged(nameof(MonitoredFiles));
                    OnPropertyChanged(nameof(AutoCheckForUpdates));
                    OnPropertyChanged(nameof(AutoScanModRepoFolder));
                    OnPropertyChanged(nameof(AutoScanGameFolder));  
                    OnPropertyChanged(nameof(GameFolder));
                    OnPropertyChanged(nameof(DarkMode));
                    OnPropertyChanged(nameof(ModManagerExecutable));
                    OnPropertyChanged(nameof(ModManagerArguments));
                    OnPropertyChanged(nameof(ModManagerRepoFolder));
                    OnPropertyChanged(nameof(LootExePath));
                    OnPropertyChanged(nameof(NexusExportFile));
                    OnPropertyChanged(nameof(MO2ExportFile));
                    OnPropertyChanged(nameof(WebServicePort));
                    OnPropertyChanged(nameof(PluginWarning));
                    OnPropertyChanged(nameof(ShowDiff));
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
                Config.Instance.UpdateFrom(_config);
                Config.SaveToYaml();
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
