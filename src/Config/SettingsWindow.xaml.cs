using MahApps.Metro.Controls;
using MaterialDesignThemes.Wpf;
using System.Windows;

namespace ZO.LoadOrderManager
{
    public partial class SettingsWindow : MetroWindow
    {
        private readonly SettingsViewModel _viewModel;
        private bool _isLoaded = false;

        public SettingsWindow(SettingsLaunchSource launchSource)
        {
            InitializeComponent();
            _viewModel = new SettingsViewModel();
            DataContext = _viewModel;

            if (launchSource == SettingsLaunchSource.CommandLine)
            {
                _viewModel.UseEmptyConfig(); // Use an empty config instance for command line launches
            }

            _viewModel.SaveCompleted += OnSaveCompleted;

            // Set the _isLoaded flag to true after the window has finished initializing
            Loaded += (s, e) => _isLoaded = true;
        }

        private void ApplyTheme(bool isDarkMode)
        {
            Config.Instance.DarkMode = isDarkMode;

            // Apply the ModernWpf theme and custom TreeView themes globally via App class
            //((App)Application.Current).ApplyModernTheme();
            ((App)Application.Current).ApplyCustomTheme(isDarkMode);

            // Prompt the user to restart the application
            MessageBoxResult result = MessageBox.Show("Restart now to apply changes?", "Restart Application", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                // Restart the application
                App.RestartApplication();
            }
        }

        private void DarkModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            // Set the DarkMode in the Config and apply the theme
            Config.Instance.DarkMode = true;

            // Apply dark theme immediately
            ApplyTheme(true);
        }

        private void DarkModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            // Set the DarkMode in the Config and apply the theme
            Config.Instance.DarkMode = false;

            // Apply light theme immediately
            ApplyTheme(false);
        }

        // Close the window after saving
        private void OnSaveCompleted()
        {
            // Close the window when saving is completed
            this.Close();
        }

        private readonly PaletteHelper _paletteHelper = new PaletteHelper();
    }
}

