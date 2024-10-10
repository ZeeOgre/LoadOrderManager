using MahApps.Metro.Controls;
using System.Windows;

namespace ZO.LoadOrderManager
{
    public partial class LoadOutEditor : MetroWindow
    {
        private LoadOut _loadOut;

        public LoadOutEditor(LoadOut loadOut)
        {
            InitializeComponent();
            _loadOut = loadOut;

            // Set DataContext to the LoadOut object
            DataContext = _loadOut;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Assuming LoadOut already binds properties, we just need to save.
            _loadOut.WriteProfile(); // Save to the database
            this.DialogResult = true; // Indicate that we are closing the dialog with a successful result
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Indicate that we are closing the dialog with a canceled result
            this.Close();
        }
    }
}
