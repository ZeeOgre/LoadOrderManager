using MahApps.Metro.Controls;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ZO.LoadOrderManager
{
    public partial class GroupSetEditor : MetroWindow
    {
        private AggLoadInfo _aggLoadInfo;

        // Existing constructor for GroupSetID
        public GroupSetEditor(long? groupSetID = null)
        {
            InitializeComponent();
            // Use the provided GroupSetID to initialize the AggLoadInfo
            if (groupSetID.HasValue)
            {
                _aggLoadInfo = new AggLoadInfo(groupSetID.Value); // Targeted group set
            }
            else
            {
                // Create a new GroupSet since no ID was provided
                var newGroupSet = GroupSet.CreateEmptyGroupSet();
                newGroupSet.GroupSetName = $"NewGroupSet_{GenerateRandomString(6)}";
                _aggLoadInfo = new AggLoadInfo(newGroupSet.GroupSetID); // Initialize with the new GroupSet
            }

            // Set the DataContext for binding
            DataContext = _aggLoadInfo;
        }

        // New constructor that takes a GroupSet object
        public GroupSetEditor(GroupSet groupSet)
        {
            InitializeComponent();
            _aggLoadInfo = new AggLoadInfo(groupSet.GroupSetID); // Use the provided GroupSet
            DataContext = _aggLoadInfo;

            // Automatically create a LoadOut if the group set has none
            InitializeLoadOut();
        }

        private void InitializeLoadOut()
        {
            // Check if the LoadOuts collection is empty
            if (_aggLoadInfo.ActiveGroupSet.LoadOuts.Count == 0)
            {
                // Create a default LoadOut
                var defaultLoadOut = new LoadOut(_aggLoadInfo.ActiveGroupSet)
                {
                    Name = $"(Default LoadOut)"
                };
                _aggLoadInfo.ActiveGroupSet.LoadOuts.Add(defaultLoadOut);
                _aggLoadInfo.ActiveLoadOut = defaultLoadOut; // Set as active
            }
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
            
            _aggLoadInfo.ActiveGroupSet.GroupSetName = TBGroupSetName.Text; 
            _aggLoadInfo.ActiveGroupSet.GroupSetFlags = GroupFlags.ReadyToLoad;
            LoadOut favoriteLoadOut = _aggLoadInfo.ActiveGroupSet.LoadOuts.FirstOrDefault(l => l.IsFavorite);
            LoadOut selectedLoadOut = favoriteLoadOut ?? _aggLoadInfo.ActiveGroupSet.LoadOuts.FirstOrDefault();
            _aggLoadInfo.ActiveLoadOut = selectedLoadOut;
            _aggLoadInfo.ActiveGroupSet.SaveGroupSet();
            _aggLoadInfo.Save();
            
            // Optionally, refresh the singleton if necessary
            AggLoadInfo.Instance.RefreshAllData();
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Discard changes
        }

        private void AddModGroupCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var addGroupWindow = new GroupSetAddGroupWindow(_aggLoadInfo); // Pass the existing AggLoadInfo object
            addGroupWindow.ShowDialog();
        }

        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"; // Define allowed characters
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private LoadOut AddNewLoadout(GroupSet? groupSet = null)
        {
            // Step 1: Use the provided groupSet or fall back to the active one
            groupSet ??= AggLoadInfo.Instance.ActiveGroupSet;

            // Create a default name for the new LoadOut
            var newLoadOutName = $"NEW_LO_{groupSet.GroupSetName}";
            var dialog = new InputDialog("Enter the name for the new LoadOut:", newLoadOutName);
            if (dialog.ShowDialog() == true)
            {
                newLoadOutName = dialog.ResponseText;
            }

            // Create the new LoadOut
            LoadOut newLoadOut = new LoadOut(groupSet) { Name = newLoadOutName };

            // Add to the LoadOuts of the GroupSet
            groupSet.LoadOuts.Add(newLoadOut);

            // Set it as the active LoadOut if appropriate
            if (AggLoadInfo.Instance.ActiveLoadOut == null || AggLoadInfo.Instance.ActiveLoadOut.GroupSetID != groupSet.GroupSetID)
            {
                AggLoadInfo.Instance.ActiveLoadOut = newLoadOut;
            }

            // Save the new LoadOut to the database
            newLoadOut.WriteProfile();

            // Optionally refresh the UI or data context
            OnPropertyChanged(nameof(AggLoadInfo.Instance.LoadOuts));

            return newLoadOut;
        }

        private void ModGroupsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ModGroup selectedGroup)
            {
                // Open the editor for the selected ModGroup
                OpenModGroupEditor(selectedGroup);
            }
        }

        private void PluginsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is Plugin selectedPlugin)
            {
                // Open the editor for the selected Plugin
                OpenPluginEditor(selectedPlugin);
            }
        }

        private void ModGroupsListBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                listBox.SelectedItem = null; // Deselect any items before opening the context menu
            }
        }

        private void PluginsListBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                listBox.SelectedItem = null; // Deselect any items before opening the context menu
            }
        }

        // Your methods to open the respective editors
        private void OpenModGroupEditor(ModGroup modGroup)
        {
            // Implement the logic to open the ModGroup editor
        }

        private void OpenPluginEditor(Plugin plugin)
        {
            // Implement the logic to open the Plugin editor
        }

        private void ModGroups_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem != null)
            {
                // Show context menu
                var contextMenu = listBox.ContextMenu;
                contextMenu.PlacementTarget = listBox;
                contextMenu.IsOpen = true;
            }
        }

        private void Plugins_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem != null)
            {
                var contextMenu = listBox.ContextMenu;
                contextMenu.PlacementTarget = listBox;
                contextMenu.IsOpen = true;
            }
        }

        private void LoadOuts_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem != null)
            {
                var contextMenu = listBox.ContextMenu;
                contextMenu.PlacementTarget = listBox;
                contextMenu.IsOpen = true;
            }
        }

        private void EditModGroup_Click(object sender, RoutedEventArgs e)
        {
            if (ModGroupsListBox.SelectedItem is ModGroup selectedGroup)
            {
                // Implement the logic to edit the selected ModGroup
                var editor = new ModGroupEditor(selectedGroup);
                editor.ShowDialog();
            }
        }

        private void RemoveModGroup_Click(object sender, RoutedEventArgs e)
        {
            if (ModGroupsListBox.SelectedItem is ModGroup selectedGroup)
            {
                // Remove the selected ModGroup from the GroupSet
                _aggLoadInfo.ActiveGroupSet.ModGroups.Remove(selectedGroup);
            }
        }

        private void EditPlugin_Click(object sender, RoutedEventArgs e)
        {
            if (PluginsListBox.SelectedItem is Plugin selectedPlugin)
            {
                // Implement the logic to edit the selected Plugin
                var editor = new PluginEditor(selectedPlugin);
                editor.ShowDialog();
            }
        }

        private void RemovePlugin_Click(object sender, RoutedEventArgs e)
        {
            if (PluginsListBox.SelectedItem is Plugin selectedPlugin)
            {
                // Remove the selected Plugin from the GroupSet
                _aggLoadInfo.ActiveGroupSet.Plugins.Remove(selectedPlugin);
            }
        }

        private void EditLoadOut_Click(object sender, RoutedEventArgs e)
        {
            if (LoadOutsListBox.SelectedItem is LoadOut selectedLoadOut)
            {
                // Implement the logic to edit the selected LoadOut
                var editor = new LoadOutEditor(selectedLoadOut);
                editor.ShowDialog();
            }
        }

        private void RemoveLoadOut_Click(object sender, RoutedEventArgs e)
        {
            if (LoadOutsListBox.SelectedItem is LoadOut selectedLoadOut)
            {
                // Remove the selected LoadOut from the GroupSet
                _aggLoadInfo.ActiveGroupSet.LoadOuts.Remove(selectedLoadOut);
            }
        }


        private void ModGroupsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ModGroupsListBox.SelectedItem is ModGroup selectedGroup)
            {
                EditModGroup_Click(sender, e);
            }
        }

        private void PluginsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PluginsListBox.SelectedItem is Plugin selectedPlugin)
            {
                EditPlugin_Click(sender, e);
            }
        }

        private void LoadOutsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LoadOutsListBox.SelectedItem is LoadOut selectedLoadOut)
            {
                EditLoadOut_Click(sender, e);
            }
        }


    }
}
