using MahApps.Metro.Controls;
using System;
using System.Windows;

namespace ZO.LoadOrderManager
{
    public partial class SettingsWindow : MetroWindow
    {
        // Expose the launch source so external callers (e.g. App.xaml.cs) can set it.
        public SettingsLaunchSource LaunchSource { get; set; }

        private readonly SettingsViewModel _viewModel;

        public SettingsWindow() : this(null) { }

        public SettingsWindow(SettingsViewModel? viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel ?? new SettingsViewModel();
            DataContext = _viewModel;

            // Subscribe to save completion so UI can react (non-invasive).
            _viewModel.SaveCompleted += ViewModel_SaveCompleted;
        }

        private void ViewModel_SaveCompleted()
        {
            // Minimal UI feedback; keep non-blocking for automated tests.
            Application.Current.Dispatcher.Invoke(() =>
            {
                _ = MessageBox.Show(this, "Settings saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // These handlers exist so XAML can reference them. Data binding already updates the VM,
        // but we also mirror the state to the ViewModel to ensure immediate effect.
        private void DarkModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                try
                {
                    vm.DarkMode = true;
                }
                catch { /* swallow - binding will remain authoritative */ }
            }
        }

        private void DarkModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                try
                {
                    vm.DarkMode = false;
                }
                catch { /* swallow */ }
            }
        }
    }
}

