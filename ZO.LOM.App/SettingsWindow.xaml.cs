using System.Windows;

namespace ZO.LoadOrderManager
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

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
        }

        // Close the window after saving
        private void OnSaveCompleted()
        {
            // Close the window when saving is completed
            this.Close();
        }
    }
}
