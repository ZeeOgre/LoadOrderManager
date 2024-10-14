using MahApps.Metro.Controls;
using System.Windows;

namespace ZO.LoadOrderManager
{
    public partial class GroupSetSelector : MetroWindow
    {
        public GroupSet SelectedGroupSet { get; private set; }

        public GroupSetSelector()
        {
            InitializeComponent();
            cmbGroupSets.ItemsSource = AggLoadInfo.GroupSets;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            SelectedGroupSet = cmbGroupSets.SelectedItem as GroupSet;
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
