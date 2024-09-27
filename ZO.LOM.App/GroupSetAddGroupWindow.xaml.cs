using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZO.LoadOrderManager
{
    public partial class GroupSetAddGroupWindow : Window
    {
        private ModGroup _tempModGroup;
        private ObservableCollection<GroupSet> _availableGroupSets;

        public GroupSetAddGroupWindow(ModGroup modGroup)
        {
            InitializeComponent();
            _tempModGroup = modGroup.Clone(); // Assuming Clone method creates a deep copy

            DataContext = _tempModGroup;

            LoadAvailableGroupSets();
        }

        private void LoadAvailableGroupSets()
        {
            _availableGroupSets = new ObservableCollection<GroupSet>();

            using (var connection = DbManager.Instance.GetConnection())
            {
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT GroupSetID, GroupSetName FROM vwGroupSetGroups WHERE GroupID != @GroupID";
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
                            _availableGroupSets.Add(groupSet);
                        }
                    }
                }
            }

            GroupSetComboBox.ItemsSource = _availableGroupSets;
            GroupSetComboBox.DisplayMemberPath = "GroupSetName";
            GroupSetComboBox.SelectedValuePath = "GroupSetID";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void AddToNewGroupSet_Click(object sender, RoutedEventArgs e)
        {
            GroupSetEditor groupSetEditor = new GroupSetEditor();
            groupSetEditor.AddModGroup(_tempModGroup);
            groupSetEditor.Show();
        }
    }
}
