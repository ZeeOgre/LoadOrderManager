using System.Windows;

namespace ZO.LoadOrderManager
{
    public partial class SettingsWindow : Window
    {

        public Task<bool?> ShowDialogAsync()
        {
            TaskCompletionSource<bool?> tcs = new TaskCompletionSource<bool?>();
            this.Closed += (s, e) => tcs.SetResult(this.DialogResult);
            this.Show();
            return tcs.Task;
        }

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
