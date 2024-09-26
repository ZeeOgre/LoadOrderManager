using System.Collections.ObjectModel;
using System.Windows;

namespace ZO.LoadOrderManager
{
    public partial class PluginEditorWindow : Window
    {
        private PluginViewModel _viewModel;

        public PluginEditorWindow(Plugin plugin, LoadOut loadOut)
        {
            InitializeComponent();
            _viewModel = new PluginViewModel(plugin); // Assuming the plugin is enabled by default
            DataContext = _viewModel;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Save();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

 
    }
}
