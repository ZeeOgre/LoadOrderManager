using MahApps.Metro.Controls;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace TextDiffViewer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ShowDiff_Click(object sender, RoutedEventArgs e)
        {
            // Load text from byte array
            byte[] byteArray = File.ReadAllBytes("path_to_your_byte_array_file");
            string leftText = Encoding.UTF8.GetString(byteArray);

            // Load text from file system
            string rightText = File.ReadAllText("path_to_your_filesystem_file");

            // Display texts
            LeftTextBox.Text = leftText;
            RightTextBox.Text = rightText;

            // Compute and display diff
            ShowDiff(leftText, rightText);
        }

        private void ShowDiff(string leftText, string rightText)
        {
            var diffBuilder = new InlineDiffBuilder(new Differ());
            var diff = diffBuilder.BuildDiffModel(leftText, rightText);

            LeftTextBox.Document.Blocks.Clear();
            RightTextBox.Document.Blocks.Clear();

            foreach (var line in diff.Lines)
            {
                var leftRun = new Run(line.Text) { Background = GetBackgroundBrush(line.Type) };
                var rightRun = new Run(line.Text) { Background = GetBackgroundBrush(line.Type) };

                LeftTextBox.Document.Blocks.Add(new Paragraph(leftRun));
                RightTextBox.Document.Blocks.Add(new Paragraph(rightRun));
            }
        }

        private Brush GetBackgroundBrush(ChangeType changeType)
        {
            switch (changeType)
            {
                case ChangeType.Inserted:
                    return Brushes.LightGreen;
                case ChangeType.Deleted:
                    return Brushes.LightCoral;
                case ChangeType.Modified:
                    return Brushes.LightYellow;
                default:
                    return Brushes.Transparent;
            }
        }
    }
}
