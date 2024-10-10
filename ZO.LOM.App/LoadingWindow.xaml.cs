using MahApps.Metro.Controls;
using System.Windows;

namespace ZO.LoadOrderManager
{
    public partial class LoadingWindow : MetroWindow
    {
        public LoadingWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        public void UpdateProgress(long progress, string message)
        {
            ProgressBar.Value = progress;
            MessageLabel.Content = message;
        }

        public void ShowInForeground()
        {
            this.Show();
        }
    }
}
