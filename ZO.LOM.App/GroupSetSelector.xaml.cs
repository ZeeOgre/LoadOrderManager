using System.Windows;

namespace ZO.LoadOrderManager
{
    public partial class SelectGroupSetWindow : Window
    {
        public GroupSet SelectedGroupSet { get; private set; }

        public SelectGroupSetWindow()
        {
            InitializeComponent();
            cmbGroupSets.ItemsSource = AggLoadInfo.Instance.GroupSets;
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
