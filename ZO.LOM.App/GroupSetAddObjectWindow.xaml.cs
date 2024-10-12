using MahApps.Metro.Controls;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ZO.LoadOrderManager
{
    public partial class GroupSetAddObjectWindow : MetroWindow
    {
        private AggLoadInfo _aggLoadInfo;
        private object _objectToEdit;

        public GroupSetAddObjectWindow(AggLoadInfo aggLoadInfo, object objectToEdit)
        {
            InitializeComponent();
            _aggLoadInfo = aggLoadInfo;
            _objectToEdit = objectToEdit;

            DataContext = this;
            InitializeFields();
        }

        private void InitializeFields()
        {
            if (_objectToEdit is LoadOut loadOut)
            {
                NameTextBox.Text = loadOut.Name;
                NameTextBox.IsReadOnly = false;
                DescriptionTextBox.Visibility = Visibility.Collapsed;
                ParentGroupComboBox.Visibility = Visibility.Collapsed;
            }
            else if (_objectToEdit is ModGroup modGroup)
            {
                NameTextBox.Text = modGroup.GroupName;
                NameTextBox.IsReadOnly = true;
                DescriptionTextBox.Text = modGroup.Description;
                DescriptionTextBox.Visibility = Visibility.Visible;
                ParentGroupComboBox.ItemsSource = _aggLoadInfo.Groups;
                ParentGroupComboBox.SelectedValue = modGroup.ParentID;
                ParentGroupComboBox.Visibility = Visibility.Visible;
            }
            else if (_objectToEdit is Plugin plugin)
            {
                NameTextBox.Text = plugin.PluginName;
                NameTextBox.IsReadOnly = true;
                DescriptionTextBox.Text = plugin.Description;
                DescriptionTextBox.Visibility = Visibility.Visible;
                ParentGroupComboBox.ItemsSource = _aggLoadInfo.Groups;
                ParentGroupComboBox.SelectedValue = plugin.GroupID;
                ParentGroupComboBox.Visibility = Visibility.Visible;
            }

            GroupSetTextBlock.Text = _aggLoadInfo.ActiveGroupSet.GroupSetName;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_objectToEdit is LoadOut loadOut)
            {
                loadOut.Name = NameTextBox.Text;
                _aggLoadInfo.Save();
            }
            else if (_objectToEdit is ModGroup modGroup)
            {
                modGroup.ParentID = (long?)ParentGroupComboBox.SelectedValue;
                _aggLoadInfo.Save();
            }
            else if (_objectToEdit is Plugin plugin)
            {
                plugin.GroupID = (long?)ParentGroupComboBox.SelectedValue;
                _aggLoadInfo.Save();
            }
            
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
