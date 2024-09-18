using System.IO;
using System.Runtime.Versioning; // Add this using directive
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ZO.LoadOrderManager
{
    public enum SettingsLaunchSource
    {
        MainWindow,
        CommandLine,
        DatabaseInitialization,
        MissingConfigDialog
    }

    public partial class SettingsWindow : Window
    {
        private static SettingsWindow? _instance;
        private readonly SettingsLaunchSource _launchSource;
        private bool _isSaveButtonClicked;

        public SettingsWindow(SettingsLaunchSource launchSource)
        {
            InitializeComponent();
            _launchSource = launchSource;

            if (launchSource == SettingsLaunchSource.CommandLine)
            {
                DataContext = new Config(); // Use an empty config instance
            }
            else
            {
                DataContext = Config.Instance; // Use the singleton instance
            }
        }

        public static SettingsWindow GetInstance(SettingsLaunchSource launchSource)
        {
            if (_instance == null)
            {
                _instance = new SettingsWindow(launchSource);
            }
            return _instance;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            HandleWindowClosure();
            _instance = null;
        }

        private void HandleWindowClosure()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            // If the Save button was clicked, we can assume settings are saved
            if (_isSaveButtonClicked)
            {
                App.LogDebug($"{timestamp} - Settings have been saved. Handling closure based on launch source.");
            }
            else
            {
                CloseWithoutSaving();
            }

            if (_launchSource == SettingsLaunchSource.CommandLine)
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                App.LogDebug($"{timestamp} - Restarting application due to CommandLine launch source.");
                this.Close();
            }
            else if (_launchSource == SettingsLaunchSource.MainWindow)
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                App.LogDebug($"{timestamp} - Bringing LoadOrderWindow to front from MainWindow launch source.");
                BringLoadOrderWindowToFront();
            }
        }




        private void SaveSettings()
        {
            if (_isSaveButtonClicked) return; // Prevent multiple save attempts

            _isSaveButtonClicked = true; // Set flag to prevent repeated saves

            try
            {
                var config = (Config)DataContext;
                if (config != null)
                {
                    Config.Instance.UpdateFrom(config); // Update the singleton instance with current values
                    Config.SaveToYaml();
                    Config.SaveToDatabase();
                }
                Close(); // Close the window after saving
            }
            catch (IndexOutOfRangeException ex)
            {
                App.LogDebug($"IndexOutOfRangeException in SaveSettings: {ex.Message}");
                _ = MessageBox.Show($"An index error occurred while saving the configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _isSaveButtonClicked = false; // Reset the flag if an error occurs
            }
            catch (Exception ex)
            {
                App.LogDebug($"Exception in SaveSettings: {ex.Message}");
                _ = MessageBox.Show($"An error occurred while saving the configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _isSaveButtonClicked = false; // Reset the flag if an error occurs
            }
        }


        private void CloseWithoutSaving()
        {
            _isSaveButtonClicked = true;
            Close();
        }

        private void BringLoadOrderWindowToFront()
        {
            var loadOrderWindow = Application.Current.Windows.OfType<LoadOrderWindow>().FirstOrDefault();
            if (loadOrderWindow != null)
            {
                _ = loadOrderWindow.Activate();
            }
        }

        private void ImportFromYaml_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZeeOgre", "LoadOrderManager"),
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

        [SupportedOSPlatform("windows6.1")]
        private void GameFolderButton_Click(object sender, RoutedEventArgs e)
        {
#if WINDOWS
            using var folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Select the game folder";
            folderBrowserDialog.ShowNewFolderButton = false;

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string selectedPath = folderBrowserDialog.SelectedPath;
                GameFolderTextBox.Text = selectedPath;
            }
#endif
        }

        [SupportedOSPlatform("windows")]
        private void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
#if WINDOWS
            App.CheckForUpdates(this);
#endif
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }
    }
}
