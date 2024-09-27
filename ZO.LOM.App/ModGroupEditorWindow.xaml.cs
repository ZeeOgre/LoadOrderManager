using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;

namespace ZO.LoadOrderManager
{
    public partial class ModGroupEditorWindow : Window
    {
        private ModGroup _originalModGroup;
        private ModGroup _tempModGroup;
        private ObservableCollection<ModGroup> _allModGroups;
        private ObservableCollection<GroupSet> _filteredGroupSets;

        public ModGroupEditorWindow(ModGroup modGroup)
        {
            InitializeComponent();
            _originalModGroup = modGroup;
            _tempModGroup = modGroup.Clone(); // Assuming Clone method creates a deep copy
            _allModGroups = new ObservableCollection<ModGroup>(
                AggLoadInfo.Instance.Groups
                .Where(g => g.GroupID != _originalModGroup.GroupID && g.GroupID >= 0)
            );

            DataContext = _tempModGroup;

            ParentGroupComboBox.ItemsSource = _allModGroups;
            ParentGroupComboBox.DisplayMemberPath = "DisplayName";
            ParentGroupComboBox.SelectedValuePath = "GroupID";
            ParentGroupComboBox.SelectedValue = _tempModGroup.ParentID;

            LoadPlugins();
            LoadGroupSets();
        }

        private void LoadPlugins()
        {
            PluginsGrid.Children.Clear();
            var pluginIDs = new List<long>();
            var pluginNames = new List<string>();

            using (var connection = DbManager.Instance.GetConnection())
            {
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT PluginID, PluginName FROM vwPlugins WHERE GroupID = @GroupID and GroupSetID = @GroupSetID ORDER BY GroupOrdinal";
                    command.Parameters.AddWithValue("@GroupID", _tempModGroup.GroupID);
                    command.Parameters.AddWithValue("@GroupSetID", _tempModGroup.GroupSetID);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            pluginIDs.Add(reader.GetInt64(0));
                            pluginNames.Add(reader.GetString(1));
                        }
                    }
                }
            }

            // Display plugin names in the UniformGrid
            foreach (var pluginName in pluginNames)
            {
                var textBlock = new TextBlock
                {
                    Text = pluginName,
                    Margin = new Thickness(5)
                };
                PluginsGrid.Children.Add(textBlock);
            }

            // Display plugin IDs in a non-editable TextBox
            PluginIDsTextBox.Text = string.Join(", ", pluginIDs);
            PluginIDsTextBox.IsReadOnly = true;
            PluginIDsTextBox.Background = System.Windows.Media.Brushes.LightGray;
        }

        private void LoadGroupSets()
        {
            _filteredGroupSets = new ObservableCollection<GroupSet>();

            using (var connection = DbManager.Instance.GetConnection())
            {
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT GroupSetID, GroupSetName FROM vwGroupSetGroups WHERE GroupID = @GroupID";
                    command.Parameters.AddWithValue("@GroupID", _tempModGroup.GroupID);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var groupSet = new GroupSet
                            {
                                GroupSetID = reader.GetInt64(0),
                                GroupSetName = reader.GetString(1)
                            };
                            _filteredGroupSets.Add(groupSet);
                        }
                    }
                }
            }

            GroupSetComboBox.ItemsSource = _filteredGroupSets;
            GroupSetComboBox.DisplayMemberPath = "GroupSetName";
            GroupSetComboBox.SelectedValuePath = "GroupSetID";
            GroupSetComboBox.SelectedValue = _tempModGroup.GroupSetID;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Copy changes from _tempModGroup to _originalModGroup
            _originalModGroup.GroupName = _tempModGroup.GroupName;
            _originalModGroup.Description = _tempModGroup.Description;
            _originalModGroup.ParentID = _tempModGroup.ParentID;

            // Persist changes
            _originalModGroup.WriteGroup();

            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void AddNewButton_Click(object sender, RoutedEventArgs e)
        {
            // Implementation for adding a new group set
        }

        private void AddToNewGroupSet_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
