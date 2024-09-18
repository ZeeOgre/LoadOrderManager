using System.Collections.ObjectModel;
using System.Windows;

namespace ZO.LoadOrderManager
{
    public partial class PluginEditorWindow : Window
    {
        private Plugin _originalPlugin;
        private Plugin _tempPlugin;
        public ObservableCollection<ModGroup> Groups { get; set; }
        public ObservableCollection<LoadOut> Loadouts { get; set; }
        public string Files { get; set; }

        public PluginEditorWindow(Plugin plugin)
        {
            InitializeComponent();
            _originalPlugin = plugin;
            _tempPlugin = plugin.Clone(); // Use Clone method to create a deep copy
            Groups = AggLoadInfo.Instance.Groups;
            Loadouts = AggLoadInfo.Instance.LoadOuts;
            Files = string.Join(", ", _tempPlugin.Files.Select(f => f.Filename));
            DataContext = _tempPlugin;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Update _tempPlugin object
            _tempPlugin.Files = Files.Split(',').Select(f => new FileInfo(f.Trim())).ToList();

            // Check if the GroupID has changed
            var originalGroupID = AggLoadInfo.Instance.Plugins
                .FirstOrDefault(p => p.PluginID == _originalPlugin.PluginID)?.GroupID;
            var newGroupID = _tempPlugin.GroupID;

            if (originalGroupID.HasValue && originalGroupID.Value != newGroupID)
            {
                // Update the GroupID and GroupOrdinal
                _tempPlugin.GroupID = newGroupID;
                _tempPlugin.GroupOrdinal = AggLoadInfo.Instance.Plugins
                    .Where(p => p.GroupID == newGroupID)
                    .Select(p => p.GroupOrdinal)
                    .DefaultIfEmpty(1)
                    .Max() + 1;
            }

            // Update the database and the Plugins singleton
            foreach (var file in _tempPlugin.Files)
            {
                FileInfo.InsertFileInfo(file, _tempPlugin.PluginID);
            }

            _tempPlugin.WriteMod();

            // Remove the old plugin and add the updated one
            var existingPlugin = AggLoadInfo.Instance.Plugins.FirstOrDefault(p => p.PluginID == _originalPlugin.PluginID);
            if (existingPlugin != null)
            {
                _ = AggLoadInfo.Instance.Plugins.Remove(existingPlugin);
            }
            AggLoadInfo.Instance.Plugins.Add(_tempPlugin);

            // Copy changes from _tempPlugin to _originalPlugin
            _originalPlugin.PluginName = _tempPlugin.PluginName;
            _originalPlugin.Description = _tempPlugin.Description;
            _originalPlugin.Achievements = _tempPlugin.Achievements;
            _originalPlugin.DTStamp = _tempPlugin.DTStamp;
            _originalPlugin.Version = _tempPlugin.Version;
            _originalPlugin.BethesdaID = _tempPlugin.BethesdaID;
            _originalPlugin.NexusID = _tempPlugin.NexusID;
            _originalPlugin.GroupID = _tempPlugin.GroupID;
            _originalPlugin.GroupOrdinal = _tempPlugin.GroupOrdinal;
            _originalPlugin.Files = _tempPlugin.Files;

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
