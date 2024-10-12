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

        public GroupSetEditor(long? groupSetID = null)
        {
            InitializeComponent();
            if (groupSetID.HasValue)
            {
                _aggLoadInfo = new AggLoadInfo(groupSetID.Value);
            }
            else
            {
                var newGroupSet = GroupSet.CreateEmptyGroupSet();
                newGroupSet.GroupSetName = $"NewGroupSet_{GenerateRandomString(6)}";
                _aggLoadInfo = new AggLoadInfo(newGroupSet.GroupSetID);
            }

            DataContext = _aggLoadInfo;
        }

        public GroupSetEditor(GroupSet groupSet)
        {
            InitializeComponent();
            _aggLoadInfo = new AggLoadInfo(groupSet.GroupSetID);
            DataContext = _aggLoadInfo;
            InitializeLoadOut();
        }

        private void InitializeLoadOut()
        {
            if (_aggLoadInfo.LoadOuts.Count == 0)
            {
                var defaultLoadOut = new LoadOut(_aggLoadInfo.ActiveGroupSet)
                {
                    Name = "(Default LoadOut)"
                };
                _aggLoadInfo.LoadOuts.Add(defaultLoadOut);
                _aggLoadInfo.ActiveLoadOut = defaultLoadOut;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _aggLoadInfo.ActiveGroupSet.GroupSetName = TBGroupSetName.Text;
            _aggLoadInfo.ActiveGroupSet.GroupSetFlags = GroupFlags.ReadyToLoad;
            LoadOut? favoriteLoadOut = _aggLoadInfo.LoadOuts.FirstOrDefault(l => l.IsFavorite);
            LoadOut? selectedLoadOut = favoriteLoadOut ?? _aggLoadInfo.LoadOuts.FirstOrDefault();
            _aggLoadInfo.ActiveLoadOut = selectedLoadOut ?? throw new InvalidOperationException("No LoadOuts available.");
            _aggLoadInfo.ActiveGroupSet.SaveGroupSet();
            _aggLoadInfo.Save();
            AggLoadInfo.Instance.RefreshAllData();
            this.Close();
        }

        private void AddModGroup_Click(object sender, RoutedEventArgs e)
        {
            var addGroupWindow = new GroupSetAddObjectWindow(_aggLoadInfo, sender);
            addGroupWindow.ShowDialog();
            DataContext = null;
            DataContext = _aggLoadInfo;
        }

        private void AddPlugin_Click(object sender, RoutedEventArgs e)
        {
            var addPluginWindow = new GroupSetAddObjectWindow(_aggLoadInfo, sender);
            addPluginWindow.ShowDialog();
            DataContext = null;
            DataContext = _aggLoadInfo;
        }

        private void AddLoadOut_Click(object sender, RoutedEventArgs e)
        {
            var addLoadOutWindow = new GroupSetAddObjectWindow(_aggLoadInfo, sender);
            addLoadOutWindow.ShowDialog();
            DataContext = null;
            DataContext = _aggLoadInfo;
        }

        private void ModGroups_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.ContextMenu != null)
            {
                listBox.ContextMenu.IsOpen = true;
            }
        }

        private void Plugins_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.ContextMenu != null)
            {
                listBox.ContextMenu.IsOpen = true;
            }
        }

        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void ModGroupsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ModGroup selectedGroup)
            {
                EditModGroup_Click(sender, e);
            }
        }

        private void PluginsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is Plugin selectedPlugin)
            {
                EditPlugin_Click(sender, e);
            }
        }

        private void LoadOutsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is LoadOut selectedLoadOut)
            {
                EditLoadOut_Click(sender, e);
            }
        }

        private void EditModGroup_Click(object sender, RoutedEventArgs e)
        {
            if (ModGroupsListBox.SelectedItem is ModGroup selectedGroup)
            {
                var editor = new ModGroupEditorWindow(selectedGroup);
                editor.ShowDialog();
            }
        }

        private void RemoveModGroup_Click(object sender, RoutedEventArgs e)
        {
            if (ModGroupsListBox.SelectedItem is ModGroup selectedGroup)
            {
                _aggLoadInfo.Groups.Remove(selectedGroup);
            }
        }

        private void EditPlugin_Click(object sender, RoutedEventArgs e)
        {
            if (PluginsListBox.SelectedItem is Plugin selectedPlugin)
            {
                var editor = new PluginEditorWindow(selectedPlugin);
                editor.ShowDialog();
            }
        }

        private void RemovePlugin_Click(object sender, RoutedEventArgs e)
        {
            if (PluginsListBox.SelectedItem is Plugin selectedPlugin)
            {
                _aggLoadInfo.Plugins.Remove(selectedPlugin);
            }
        }

        private void EditLoadOut_Click(object sender, RoutedEventArgs e)
        {
            if (LoadOutsListBox.SelectedItem is LoadOut selectedLoadOut)
            {
                var editor = new LoadOutEditor(selectedLoadOut);
                editor.ShowDialog();
            }
        }

        private void RemoveLoadOut_Click(object sender, RoutedEventArgs e)
        {
            if (LoadOutsListBox.SelectedItem is LoadOut selectedLoadOut)
            {
                _aggLoadInfo.LoadOuts.Remove(selectedLoadOut);
            }
        }

        // Navigation Commands for Record Navigation Control
        private void FirstRecordCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (AggLoadInfo.Instance.GroupSets.Any())
            {
                var firstGroupSet = AggLoadInfo.Instance.GroupSets.First();
                _aggLoadInfo = new AggLoadInfo(firstGroupSet.GroupSetID);
                DataContext = _aggLoadInfo;
            }
        }

        private void PreviousRecordCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var currentIndex = AggLoadInfo.Instance.GroupSets.IndexOf(_aggLoadInfo.ActiveGroupSet);
            if (currentIndex > 0)
            {
                var previousGroupSet = AggLoadInfo.Instance.GroupSets[currentIndex - 1];
                _aggLoadInfo = new AggLoadInfo(previousGroupSet.GroupSetID);
                DataContext = _aggLoadInfo;
            }
        }

        private void NextRecordCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var currentIndex = AggLoadInfo.Instance.GroupSets.IndexOf(_aggLoadInfo.ActiveGroupSet);
            if (currentIndex < AggLoadInfo.Instance.GroupSets.Count - 1)
            {
                var nextGroupSet = AggLoadInfo.Instance.GroupSets[currentIndex + 1];
                _aggLoadInfo = new AggLoadInfo(nextGroupSet.GroupSetID);
                DataContext = _aggLoadInfo;
            }
        }

        private void LastRecordCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (AggLoadInfo.Instance.GroupSets.Any())
            {
                var lastGroupSet = AggLoadInfo.Instance.GroupSets.Last();
                _aggLoadInfo = new AggLoadInfo(lastGroupSet.GroupSetID);
                DataContext = _aggLoadInfo;
            }
        }
    }
}