using MahApps.Metro.Controls;
using System;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using DiffPlex.Wpf.Controls;
using System.Windows.Media;

namespace ZO.LoadOrderManager
{
    public partial class DiffViewer : MetroWindow
    {
        private string? filePath1;
        private string? filePath2;

        // Constructor 1: FileInfo handling
        public DiffViewer(FileInfo fileInfo)
        {
            InitializeComponent();

            // Try to get file from AbsolutePath or load from byte[] if needed
            filePath1 = fileInfo.AbsolutePath ?? OpenFileDialog("Select the first file to compare");

            // Prompt for the second file path
            filePath2 = GetSecondFilePath();

            // Load content from FileInfo and the second file path
            string oldText = Encoding.UTF8.GetString(fileInfo.FileContent);
            string newText = File.ReadAllText(filePath2);

            // Set texts in DiffView
            DiffView.OldText = oldText;
            DiffView.NewText = newText;

            LoadData();
        }

        // Constructor 2: Two nullable file paths, prompts if missing
        public DiffViewer(string? filePath1, string? filePath2)
        {
            InitializeComponent();

            // Prompt for missing file paths
            if (string.IsNullOrEmpty(filePath1))
                filePath1 = OpenFileDialog("Select the first file to compare");

            if (string.IsNullOrEmpty(filePath2))
                filePath2 = OpenFileDialog("Select the second file to compare");

            // Load text from both files
            string oldText = File.ReadAllText(filePath1);
            string newText = File.ReadAllText(filePath2);

            // Set texts in DiffView
            DiffView.OldText = oldText;
            DiffView.NewText = newText;

            LoadData();
        }

        // Constructor 3: FileInfo and a single file path, prompts for missing file
        public DiffViewer(FileInfo fileInfo, string? filePath2)
        {
            InitializeComponent();

            filePath1 = fileInfo.AbsolutePath ?? OpenFileDialog("Select the first file to compare");

            if (string.IsNullOrEmpty(filePath2))
                filePath2 = OpenFileDialog("Select the second file to compare");

            // Load content from FileInfo and the second file path
            string oldText = Encoding.UTF8.GetString(fileInfo.FileContent);
            string newText = File.ReadAllText(filePath2);

            // Set texts in DiffView
            DiffView.OldText = oldText;
            DiffView.NewText = newText;

            LoadData();
        }
        //constructor 4 byte arrays 
        public DiffViewer(byte[] oldContent, byte[] newContent)
        {
            InitializeComponent();

            // Convert byte arrays to strings
            string oldText = Encoding.UTF8.GetString(oldContent);
            string newText = Encoding.UTF8.GetString(newContent);

            // Set texts in DiffView
            DiffView.OldText = oldText;
            DiffView.NewText = newText;

            LoadData();
        }

        // Helper method for opening file dialog
        private string? OpenFileDialog(string title)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = title,
                Filter = "All files (*.*)|*.*"
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }

        // Placeholder for logic to retrieve second file path if necessary
        private string? GetSecondFilePath()
        {
            return OpenFileDialog("Select the second file to compare");
        }

        // LoadData method to adjust view settings, dark/light modes, etc.
        private void LoadData()
        {
            // Adjust appearance based on the time of day (dark/light mode)
            var now = DateTime.Now;
            var isDark = false; // now.Hour < 6 || now.Hour >= 18;
            DiffView.Foreground = new SolidColorBrush(isDark ? Color.FromRgb(240, 240, 240) : Color.FromRgb(32, 32, 32));

            // Optionally set headers or adjust appearance
            DiffView.SetHeaderAsOldToNew();

            // Set background color
            Background = new SolidColorBrush(isDark ? Color.FromRgb(32, 32, 32) : Color.FromRgb(251, 251, 251));
        }

        // Switch between inline and side-by-side views
        private void DiffButton_Click(object sender, RoutedEventArgs e)
        {
            if (DiffView.IsInlineViewMode)
            {
                DiffView.ShowSideBySide();
            }
            else
            {
                DiffView.ShowInline();
            }
        }

        // Open additional view options (collapse unchanged, etc.)
        private void FutherActionsButton_Click(object sender, RoutedEventArgs e)
        {
            DiffView.OpenViewModeContextMenu();
        }
    }
}
