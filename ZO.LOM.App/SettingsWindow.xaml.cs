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


        private void ApplyTheme(bool DarkMode)
        {
            ResourceDictionary theme = new ResourceDictionary
            {
                Source = new Uri(DarkMode ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml", UriKind.Relative)
            };

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(theme);
        }

        private void DarkModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Set the DarkMode in the Config and apply the theme
            Config.Instance.DarkMode = true;
            
            // Apply dark theme immediately
            ApplyTheme(true);
        }

        private void DarkModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
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
    }
}
