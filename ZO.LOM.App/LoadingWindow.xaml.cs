using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls; // Add this line
using System.Windows.Media.Animation; // Add this line

namespace ZO.LoadOrderManager
{
    public partial class LoadingWindow : MetroWindow
    {
        public LoadingWindow()
        {
            InitializeComponent();
        }

        public void UpdateProgress(long progress, string message)
        {
            ProgressBar.Value = progress;
            MessageLabel.Content = message;
        }


    }
}

