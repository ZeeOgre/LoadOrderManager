using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;

namespace ZO.LoadOrderManager
{
    public partial class DiffViewer : Window
    {
        private byte[] byteArray;
        private string filePath;
        private FileFlags fileFlags;

        public DiffViewer(byte[] byteArray, string filePath, FileFlags fileFlags)
        {
            InitializeComponent();
            this.byteArray = byteArray;
            this.filePath = filePath;
            this.fileFlags = fileFlags;
        }

        private void ShowDiff_Click(object sender, RoutedEventArgs e)
        {
            // Load text from byte array
            string leftText = Encoding.UTF8.GetString(byteArray);

            // Load text from file system
            string rightText = File.ReadAllText(filePath);

            // Display texts
            LeftRichTextBox.Document.Blocks.Clear();
            RightRichTextBox.Document.Blocks.Clear();
            LeftRichTextBox.Document.Blocks.Add(new Paragraph(new Run(leftText)));
            RightRichTextBox.Document.Blocks.Add(new Paragraph(new Run(rightText)));

            // Compute and display diff
            ShowDiff(leftText, rightText);
        }

        private void ShowDiff(string leftText, string rightText)
        {
            var diffBuilder = new InlineDiffBuilder(new Differ());
            var diff = diffBuilder.BuildDiffModel(leftText, rightText);

            LeftRichTextBox.Document.Blocks.Clear();
            RightRichTextBox.Document.Blocks.Clear();

            foreach (var line in diff.Lines)
            {
                var leftRun = new Run(line.Text) { Background = GetBackgroundBrush(line.Type) };
                var rightRun = new Run(line.Text) { Background = GetBackgroundBrush(line.Type) };

                LeftRichTextBox.Document.Blocks.Add(new Paragraph(leftRun));
                RightRichTextBox.Document.Blocks.Add(new Paragraph(rightRun));
            }
        }

        private Brush GetBackgroundBrush(ChangeType changeType)
        {
            return changeType switch
            {
                ChangeType.Inserted => Brushes.LightGreen,
                ChangeType.Deleted => Brushes.LightCoral,
                ChangeType.Modified => Brushes.LightYellow,
                _ => Brushes.Transparent,
            };
        }

        private void ApplyFromSaved_Click(object sender, RoutedEventArgs e)
        {
            // Get text from the left RichTextBox
            string leftText = new TextRange(LeftRichTextBox.Document.ContentStart, LeftRichTextBox.Document.ContentEnd).Text;

            // Save text to the filePath
            File.WriteAllText(filePath, leftText);

            // Create a new FileInfo object
            var fileInfo = new FileInfo(filePath, true)
            {
                Flags = fileFlags,
                FileContent = Encoding.UTF8.GetBytes(leftText)
            };

            // Insert or update the FileInfo in the database
            FileInfo.InsertFileInfo(fileInfo);

            MessageBox.Show("Changes applied from saved file.");
        }

        private void AcceptNew_Click(object sender, RoutedEventArgs e)
        {
            // Get text from the right RichTextBox
            string rightText = new TextRange(RightRichTextBox.Document.ContentStart, RightRichTextBox.Document.ContentEnd).Text;

            // Save text to the filePath
            File.WriteAllText(filePath, rightText);

            // Create a new FileInfo object
            var fileInfo = new FileInfo(filePath, true)
            {
                Flags = fileFlags,
                FileContent = Encoding.UTF8.GetBytes(rightText)
            };

            // Insert or update the FileInfo in the database
            FileInfo.InsertFileInfo(fileInfo);

            MessageBox.Show("New changes accepted.");
        }
    }
}
