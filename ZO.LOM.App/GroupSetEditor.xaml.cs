using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Input;

namespace ZO.LoadOrderManager
{
    public partial class GroupSetEditor : MetroWindow
    {
        private AggLoadInfo _aggLoadInfo;

        public GroupSetEditor(long? groupSetID = null)
        {
            InitializeComponent();

            // Check if GroupSetID is provided, if not, use the Singleton and clone it
            if (groupSetID.HasValue)
            {
                _aggLoadInfo = new AggLoadInfo(groupSetID.Value);  // Targeted group set
            }
            else
            {
                _aggLoadInfo = AggLoadInfo.Instance.Clone();  // Clone from singleton
            }

            // Set the DataContext for binding
            DataContext = _aggLoadInfo;
        }

        private void AddModGroup_Click(object sender, RoutedEventArgs e)
        {
            var addGroupWindow = new GroupSetAddGroupWindow(_aggLoadInfo);
            addGroupWindow.ShowDialog();
            // Refresh the DataContext after adding the group
            DataContext = null;
            DataContext = _aggLoadInfo;
        }

        private void AddPlugin_Click(object sender, RoutedEventArgs e)
        {
            // Implement adding plugin functionality
            // You may want to open a plugin selection window here
        }

        private void AddLoadOut_Click(object sender, RoutedEventArgs e)
        {
            // Implement adding loadout functionality
            // You may want to open a loadout selection window here
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Save all changes
            _aggLoadInfo.Save();
            // Optionally, refresh the singleton if necessary
            AggLoadInfo.Instance.RefreshAllData();
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();  // Discard changes
        }

        private void AddModGroupCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var addGroupWindow = new GroupSetAddGroupWindow(_aggLoadInfo); // Pass the existing AggLoadInfo object
            addGroupWindow.ShowDialog();
        }

    }
}
