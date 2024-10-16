using MahApps.Metro.Controls;
using System.Windows;

namespace ZO.LoadOrderManager
{
    public partial class MissingConfigDialog : MetroWindow
    {
        public MissingConfigDialog()
        {
            InitializeComponent();
        }

        private void CopySample_Click(object sender, RoutedEventArgs e)
        {
            // Logic to copy the sample configuration file
            _ = MessageBox.Show("Sample configuration file copied.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SettingsWindow_Click(object sender, RoutedEventArgs e)
        {
            // Logic to open the settings window
            var settingsWindow = new SettingsWindow(SettingsLaunchSource.MissingConfigDialog);
            settingsWindow.Show();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // Logic to exit the application
            Application.Current.Shutdown();
        }
    }
}
