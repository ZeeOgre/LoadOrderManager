using MahApps.Metro.Controls;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows;

namespace ZO.LoadOrderManager
{
    public partial class GroupSetAddGroupWindow : MetroWindow
    {
        private ModGroup _newModGroup;
        private ObservableCollection<GroupSet> _availableGroupSets;
        private ObservableCollection<ModGroup> _availableParentGroups;
        private AggLoadInfo _aggLoadInfo;

        public GroupSetAddGroupWindow(AggLoadInfo aggLoadInfo)
        {
            InitializeComponent();
            _aggLoadInfo = aggLoadInfo;
            _newModGroup = new ModGroup(); // Create a new ModGroup

            DataContext = _newModGroup;

            LoadAvailableGroupSets();
        }

        private void LoadAvailableGroupSets()
        {
            _availableGroupSets = new ObservableCollection<GroupSet>(_aggLoadInfo.GroupSets);

            GroupSetComboBox.ItemsSource = _availableGroupSets;
            GroupSetComboBox.DisplayMemberPath = "GroupSetName";
            GroupSetComboBox.SelectedValuePath = "GroupSetID";
        }

        private void GroupSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // When the GroupSet is changed, update the ParentGroupComboBox
            var selectedGroupSet = (GroupSet)GroupSetComboBox.SelectedItem;
            if (selectedGroupSet != null)
            {
                _availableParentGroups = new ObservableCollection<ModGroup>(selectedGroupSet.ModGroups);
                ParentGroupComboBox.ItemsSource = _availableParentGroups;
                ParentGroupComboBox.DisplayMemberPath = "GroupName";
                ParentGroupComboBox.SelectedValuePath = "GroupID";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Add new ModGroup to the selected GroupSet and ParentGroup
            var selectedGroupSet = (GroupSet)GroupSetComboBox.SelectedItem;
            var selectedParentGroup = (ModGroup)ParentGroupComboBox.SelectedItem;

            if (selectedGroupSet != null)
            {
                _newModGroup.GroupSetID = selectedGroupSet.GroupSetID;
                _newModGroup.ParentID = selectedParentGroup?.GroupID;
                selectedGroupSet.ModGroups.Add(_newModGroup);

                _aggLoadInfo.Save();
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please select a GroupSet and Parent Group.");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void AddToNewGroupSet_Click(object sender, RoutedEventArgs e)
        {
            GroupSetEditor groupSetEditor = new GroupSetEditor(_aggLoadInfo.ActiveGroupSet.GroupSetID); // Pass the AggLoadInfo
            groupSetEditor.ShowDialog();

            // Refresh the available GroupSets after adding a new one
            LoadAvailableGroupSets();
        }
    }
}
