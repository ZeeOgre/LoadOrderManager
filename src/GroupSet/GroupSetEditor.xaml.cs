using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Input;

namespace ZO.LoadOrderManager
{
    public partial class GroupSetEditor : MetroWindow
    {
        private AggLoadInfo _aggLoadInfo;

        public GroupSetEditor(GroupSet groupSet)
        {
            InitializeComponent();
            _aggLoadInfo = new AggLoadInfo(groupSet.GroupSetID);

            // Set DataContext to _aggLoadInfo
            this.DataContext = _aggLoadInfo;

            // Initialize navigation commands (if needed)
            UpdateCurrentRecordInfo();
        }

        private void UpdateCurrentRecordInfo()
        {
            // Set DataContext to refresh the UI
            this.DataContext = _aggLoadInfo;

            // Additional logic for updating the current record’s info
            _ = AggLoadInfo.GroupSets.IndexOf(_aggLoadInfo.ActiveGroupSet) + 1;
            // For example: update any label or UI element showing the current record
            // RecordInfoLabel.Content = $"Record {currentIndex} of {AggLoadInfo.GroupSets.Count}";

            _aggLoadInfo.ActiveLoadOut = _aggLoadInfo.GetLoadOutForGroupSet(_aggLoadInfo.ActiveGroupSet);
        }



        #region Mod Groups Handlers

        private void AddModGroup_Click(object sender, RoutedEventArgs e)
        {
            var newModGroup = new ModGroup { GroupName = "New Group" };
            _aggLoadInfo.Groups.Add(newModGroup);
        }

        private void EditModGroup_Click(object sender, RoutedEventArgs e)
        {
            if (ModGroupsListBox.SelectedItem is ModGroup selectedGroup)
            {
                var modGroupEditor = new ModGroupEditorWindow(selectedGroup);
                _ = modGroupEditor.ShowDialog();
            }
        }

        private void RemoveModGroup_Click(object sender, RoutedEventArgs e)
        {
            if (ModGroupsListBox.SelectedItem is ModGroup selectedGroup)
            {
                _ = _aggLoadInfo.Groups.Remove(selectedGroup);
            }
        }

        private void ModGroupsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ModGroupsListBox.SelectedItem is ModGroup selectedGroup)
            {
                var modGroupEditor = new ModGroupEditorWindow(selectedGroup);
                _ = modGroupEditor.ShowDialog();
            }
        }

        #endregion

        #region Plugins Handlers

        private void AddPlugin_Click(object sender, RoutedEventArgs e)
        {
            var newPlugin = new Plugin { PluginName = "New Plugin" };
            _aggLoadInfo.Plugins.Add(newPlugin);
        }

        private void EditPlugin_Click(object sender, RoutedEventArgs e)
        {
            if (PluginsListBox.SelectedItem is Plugin selectedPlugin)
            {
                var pluginEditor = new PluginEditorWindow(selectedPlugin);
                _ = pluginEditor.ShowDialog();
            }
        }

        private void RemovePlugin_Click(object sender, RoutedEventArgs e)
        {
            if (PluginsListBox.SelectedItem is Plugin selectedPlugin)
            {
                _ = _aggLoadInfo.Plugins.Remove(selectedPlugin);
            }
        }

        private void PluginsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PluginsListBox.SelectedItem is Plugin selectedPlugin)
            {
                var pluginEditor = new PluginEditorWindow(selectedPlugin);
                _ = pluginEditor.ShowDialog();
            }
        }

        #endregion

        #region LoadOuts Handlers

        private void AddLoadOut_Click(object sender, RoutedEventArgs e)
        {
            var newLoadOut = new LoadOut { Name = "New LoadOut" };
            _aggLoadInfo.LoadOuts.Add(newLoadOut);
        }

        private void EditLoadOut_Click(object sender, RoutedEventArgs e)
        {
            if (LoadOutsListBox.SelectedItem is LoadOut selectedLoadOut)
            {
                var loadOutEditor = new LoadOutEditor(selectedLoadOut);
                _ = loadOutEditor.ShowDialog();
            }
        }

        private void RemoveLoadOut_Click(object sender, RoutedEventArgs e)
        {
            if (LoadOutsListBox.SelectedItem is LoadOut selectedLoadOut)
            {
                _ = _aggLoadInfo.LoadOuts.Remove(selectedLoadOut);
            }
        }

        private void LoadOutsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LoadOutsListBox.SelectedItem is LoadOut selectedLoadOut)
            {
                var loadOutEditor = new LoadOutEditor(selectedLoadOut);
                _ = loadOutEditor.ShowDialog();
            }
        }

        #endregion

        #region Record Navigation Handlers

        private void FirstRecord_Click(object? sender, RoutedEventArgs? e)
        {
            _aggLoadInfo = new AggLoadInfo(AggLoadInfo.GroupSets.FirstOrDefault().GroupSetID);
            UpdateCurrentRecordInfo();
        }

        private void PreviousRecord_Click(object sender, RoutedEventArgs e)
        {
            int currentIndex = AggLoadInfo.GroupSets.IndexOf(_aggLoadInfo.ActiveGroupSet);
            if (currentIndex > 0)
            {
                _aggLoadInfo = new AggLoadInfo(AggLoadInfo.GroupSets[currentIndex - 1].GroupSetID);
                UpdateCurrentRecordInfo();
            }
        }

        private void NextRecord_Click(object sender, RoutedEventArgs e)
        {
            int currentIndex = AggLoadInfo.GroupSets.IndexOf(_aggLoadInfo.ActiveGroupSet);
            if (currentIndex < AggLoadInfo.GroupSets.Count - 1)
            {
                _aggLoadInfo = new AggLoadInfo(AggLoadInfo.GroupSets[currentIndex + 1].GroupSetID);
                UpdateCurrentRecordInfo();
            }
        }

        private void LastRecord_Click(object sender, RoutedEventArgs e)
        {
            _aggLoadInfo = new AggLoadInfo(AggLoadInfo.GroupSets.LastOrDefault().GroupSetID);
            UpdateCurrentRecordInfo();
        }

        private void NewRecord_Click(object sender, RoutedEventArgs e)
        {
            var groupSet = GroupSet.CreateEmptyGroupSet();
            _aggLoadInfo = new AggLoadInfo(groupSet.GroupSetID);
            UpdateCurrentRecordInfo();
        }

        private void DeleteRecord_Click(object sender, RoutedEventArgs e)
        {
            if (_aggLoadInfo != null)
            {
                if (_aggLoadInfo.ActiveGroupSet.IsFavorite)
                {
                    _ = MessageBox.Show("Please select another GroupSet as favorite before deleting this one.", "Cannot Delete Favorite", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (_aggLoadInfo.ActiveGroupSet.IsDefaultGroup || _aggLoadInfo.ActiveGroupSet.IsReadOnly)
                {
                    _ = MessageBox.Show("Cannot delete the Default or ReadOnly Groupsets.", "Cannot Delete Default or ReadOnly", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (AggLoadInfo.Instance.ActiveGroupSet == _aggLoadInfo.ActiveGroupSet)
                {
                    _ = MessageBox.Show("This is the primary ActiveGroupSet, please select a different one on the main window before deleting.", "Cannot Delete Active LoadOut", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _ = AggLoadInfo.GroupSets.Remove(_aggLoadInfo.ActiveGroupSet);
                GroupSet.DeleteRecord(_aggLoadInfo.ActiveGroupSet.GroupSetID);
                FirstRecord_Click(null, null); // Navigate to the first record after deletion
            }
        }

        private void SetActiveLoadOut_Click(object sender, RoutedEventArgs e)
        {
            if (LoadOutsListBox.SelectedItem is LoadOut selectedLoadOut)
            {
                _aggLoadInfo.ActiveLoadOut = selectedLoadOut;
                // Update UI or perform additional actions as needed
            }
        }

        private void SetFavoriteLoadOut_Click(object sender, RoutedEventArgs e)
        {
            if (LoadOutsListBox.SelectedItem is LoadOut selectedLoadOut)
            {
                selectedLoadOut.IsFavorite = true;
                // Update UI or perform additional actions as needed
            }
        }

        private void ToggleEnable_Click(object sender, RoutedEventArgs e)
        {
            if (PluginsListBox.SelectedItem is Plugin selectedPlugin)
            {
                bool pluginEnabled = _aggLoadInfo.ActiveLoadOut.IsPluginEnabled(selectedPlugin.PluginID);
                LoadOut.SetPluginEnabled(_aggLoadInfo.ActiveLoadOut.ProfileID, selectedPlugin.PluginID, !pluginEnabled, _aggLoadInfo);
                // Update UI or perform additional actions as needed
            }
        }


        #endregion
        private void ImportFiles_Click(object sender, RoutedEventArgs e)
        {

            _ = FileManager.ParsePluginsTxt(_aggLoadInfo);
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _aggLoadInfo.Save();
            this.Close();
        }
    }
}
