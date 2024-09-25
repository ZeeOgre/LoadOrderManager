namespace ZO.LOM.App
{
    public partial class LoadOrderWindowViewModel
    {
        // Properties
        private bool isSaved;
        private Timer cooldownTimer;
        private object? _selectedItem;
        private long? _selectedProfileId;
        private string _statusMessage;
        private string _searchText;
        private GroupSet _selectedGroupSet;
        private LoadOut _selectedLoadOut;
        private bool _isInitialDataLoaded;

        public ObservableCollection<GroupSet> GroupSets { get; set; }
        public GroupSet SelectedGroupSet { get; set; }
        public object? SelectedItem { get; set; }
        public long? SelectedProfileId { get; set; }
        public LoadOut SelectedLoadOut { get; set; }
        public string StatusMessage { get; set; }
        public string SearchText { get; set; }
        public ObservableCollection<ModGroup> Groups { get; set; }
        public ObservableCollection<PluginViewModel> Plugins { get; set; }
        public ObservableCollection<LoadOut> LoadOuts { get; set; }
        public ObservableCollection<LoadOrderItemViewModel> Items { get; }
    }
}
