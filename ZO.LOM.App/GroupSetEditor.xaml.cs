using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ZO.LoadOrderManager
{
    /// <summary>
    /// Interaction logic for GroupSetEditor.xaml
    /// </summary>
    public partial class GroupSetEditor : Window
    {
        // Constructor for existing GroupSet
        public GroupSetEditor(GroupSet groupSet)
        {
            InitializeComponent();
            DataContext = new GroupSetViewModel(groupSet.GroupSetID);
        }

        // Constructor for new GroupSet
        public GroupSetEditor()
        {
            InitializeComponent();
            DataContext = new GroupSetViewModel(0);
        }

        public void AddModGroup(ModGroup modGroup)
        {
            if (DataContext is GroupSetViewModel viewModel)
            {
                viewModel.AddModGroup(modGroup);
            }
        }

    }
}
