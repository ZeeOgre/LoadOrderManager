using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ZO.LoadOrderManager
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private Config _config;
        private string _gameFolder;
        private bool _autoCheckForUpdates;

        public SettingsViewModel()
        {
            _config = Config.Instance;
            SaveCommand = new RelayCommand(_ => SaveSettings());
            LoadCommand = new RelayCommand(_ => LoadSettings());
            LaunchGameFolderCommand = new RelayCommand(_ => LaunchGameFolder());
            CheckForUpdatesCommand = new RelayCommand(_ => CheckForUpdates());
            UpdateFromConfig();
        }

        public Window ParentWindow { get; set; }
        public ICommand SaveCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand LaunchGameFolderCommand { get; }
        public ICommand CheckForUpdatesCommand { get; }

        public bool AutoCheckForUpdates
        {
            get => _autoCheckForUpdates;
            set
            {
                _autoCheckForUpdates = value;
                OnPropertyChanged();
            }
        }

        public string GameFolder
        {
            get => _gameFolder;
            set
            {
                _gameFolder = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> AvailableArchiveFormats { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void UpdateFromConfig()
        {
            GameFolder = _config.GameFolder;
            AutoCheckForUpdates = _config.AutoCheckForUpdates;
        }

        private void SaveSettings()
        {
            _config.GameFolder = GameFolder;
            _config.AutoCheckForUpdates = AutoCheckForUpdates;
            Config.SaveToYaml();
            Config.SaveToDatabase();
            _ = MessageBox.Show("Settings saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadSettings()
        {
            UpdateFromConfig();
        }

        private void LaunchGameFolder()
        {
            if (!string.IsNullOrEmpty(GameFolder) && Directory.Exists(GameFolder))
            {
                _ = Process.Start(new ProcessStartInfo
                {
                    FileName = GameFolder,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            else
            {
                _ = MessageBox.Show("Game folder path is invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckForUpdates()
        {
            // Implement the logic to check for updates
            _ = MessageBox.Show("Check for updates clicked.");
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

   
}
