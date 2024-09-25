namespace ZO.LOM.App
{
    public partial class LoadOrderWindowViewModel
    {
        // Non-command methods
        private void UpdateLoadOrders() { }
        private void LoadInitialData() { }
        private long GenerateNewProfileID() { }
        public void RefreshCheckboxes() { }
        private void UpdateStatusMessage() { }
        private LoadOrderItemViewModel CreateGroupViewModel(ModGroup group) { }
        protected virtual void OnPropertyChanged(string propertyName) { }
        public void UpdateStatus(string message) { }
        private void MoveToUnassignedGroup(Plugin plugin) { }
        private IEnumerable<LoadOrderItemViewModel> Flatten(ObservableCollection<LoadOrderItemViewModel> items) { }
        private void ReloadViews() { }
    }
}
