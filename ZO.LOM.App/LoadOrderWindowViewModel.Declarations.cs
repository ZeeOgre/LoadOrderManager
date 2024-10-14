using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Timers;

namespace ZO.LoadOrderManager
{
    public partial class LoadOrderWindowViewModel : INotifyPropertyChanged
    {
        // Fields
        private bool isSaved;
        private string _statusMessage;
        private string _searchText;
        private bool _isInitialDataLoaded = false;
        private bool _isSynchronizing = false;
        private List<LoadOrderItemViewModel>? _cachedFlatList;
        private bool _isRefreshing = false;
        private bool _isUiEnabled = true;
        public bool IsUiEnabled
        {
            get => _isUiEnabled;
            set
            {
                _isUiEnabled = value;
                OnPropertyChanged(nameof(IsUiEnabled));
            }
        }

        // Observable collections for GroupSets, LoadOuts, and SelectedItems
        public ObservableCollection<GroupSet> GroupSets { get; set; }
        public ObservableCollection<LoadOut> LoadOuts { get; set; }

        private LoadOrdersViewModel _loadOrders;
        public LoadOrdersViewModel LoadOrders
        {
            get => _loadOrders;
            set
            {
                _loadOrders = value;
                RebuildFlatList();
                OnPropertyChanged(nameof(LoadOrders));
            }
        }

        private LoadOrdersViewModel _cachedGroupSetLoadOrders;
        public LoadOrdersViewModel CachedGroupSetLoadOrders
        {
            get => _cachedGroupSetLoadOrders;
            set
            {
                _cachedGroupSetLoadOrders = value;
                OnPropertyChanged(nameof(CachedGroupSetLoadOrders));
            }
        }

        // Property for SelectedItems with change notification
        private ObservableCollection<object> selectedItems;
        public ObservableCollection<object> SelectedItems
        {
            get => selectedItems;
            set
            {
                selectedItems = value;
                if (!_isSynchronizing)
                {
                    OnPropertyChanged(nameof(SelectedItems));
                }
            }
        }

        // Direct public property for SelectedCachedItems
        public ObservableCollection<object> SelectedCachedItems { get; set; }

        // PropertyChanged Event
        public event PropertyChangedEventHandler? PropertyChanged;

        // Status message
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        // Search text
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
            }
        }

        // Core synchronization logic
        public void StartSync() => _isSynchronizing = true;
        public void EndSync() => _isSynchronizing = false;

        // OnPropertyChanged helper
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Update status method
        public void UpdateStatus(string message)
        {
            StatusMessage = message;
        }

        // Update the status message
        private void UpdateStatusMessage()
        {
            if (SelectedItem != null)
            {
                StatusMessage = SelectedItem?.ToString() ?? "No item selected";
            }
            else
            {
                StatusMessage = "No item selected";
            }
        }

        // Rebuild flat list based on LoadOrders
        private void RebuildFlatList()
        {
            if (_isRefreshing || _isSynchronizing || LoadOrders.Items.Count == 0) return;
            _isRefreshing = true;

            // Rebuild the flat list from LoadOrders.Items
            _cachedFlatList = Flatten(LoadOrders.Items,true).ToList();

            _isRefreshing = false;
        }

        // SelectedItem: shortcut to first SelectedItems
        private object _selectedItem;
        public object SelectedItem
        {
            get
            {
                if (SelectedItems != null && SelectedItems.Count > 0)
                    return SelectedItems[0];
                return _selectedItem;
            }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    if (_isSynchronizing) return;

                    StartSync();
                    try
                    {
                        if (SelectedItems != null)
                        {
                            SelectedItems.Remove(_selectedItem);
                            SelectedItems.Insert(0, _selectedItem);
                        }
                        OnPropertyChanged(nameof(SelectedItem));
                    }
                    finally
                    {
                        EndSync();
                    }
                }
            }
        }

        // Handling property changes for AggLoadInfo
        private bool _isHandlingAggLoadInfoPropertyChanged = false;
        private void OnAggLoadInfoPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isSynchronizing || _isHandlingAggLoadInfoPropertyChanged) return;

            _isHandlingAggLoadInfoPropertyChanged = true;
            StartSync();
            try
            {
                if (e.PropertyName == nameof(AggLoadInfo.ActiveGroupSet))
                {
                    if (!ReferenceEquals(_selectedGroupSet, AggLoadInfo.Instance.ActiveGroupSet))
                    {
                        _selectedGroupSet = AggLoadInfo.Instance.ActiveGroupSet;
                        _selectedLoadOut = AggLoadInfo.Instance.ActiveLoadOut;
                        OnPropertyChanged(nameof(SelectedGroupSet));
                        OnPropertyChanged(nameof(SelectedLoadOut));
                    }
                }
                else if (e.PropertyName == nameof(AggLoadInfo.ActiveLoadOut))
                {
                    if (!ReferenceEquals(_selectedLoadOut, AggLoadInfo.Instance.ActiveLoadOut))
                    {
                        _selectedLoadOut = AggLoadInfo.Instance.ActiveLoadOut;
                        OnPropertyChanged(nameof(SelectedLoadOut));
                    }
                }
                else if (e.PropertyName == nameof(LoadOuts)) {
                    UpdateLoadOutsForSelectedGroupSet();
                }
                else if (e.PropertyName == nameof(GroupSets))
                {
                    OnPropertyChanged(nameof(GroupSets));
                }
            }
            finally
            {
                EndSync();
                _isHandlingAggLoadInfoPropertyChanged = false;
            }
        }
        private void UpdateLoadOutsForSelectedGroupSet()
        {
            LoadOuts.Clear();

            // Populate LoadOuts for the selected GroupSet
            foreach (var loadOut in AggLoadInfo.Instance.LoadOuts.Where(lo => lo.GroupSetID == SelectedGroupSet.GroupSetID))
            {
                LoadOuts.Add(loadOut);
            }

            SelectedLoadOut = AggLoadInfo.Instance.GetLoadOutForGroupSet(AggLoadInfo.Instance.ActiveGroupSet);

            OnPropertyChanged(nameof(LoadOuts)); // Notify that LoadOuts collection has changed
        }
        // Properties
        private GroupSet _selectedGroupSet;
        public GroupSet SelectedGroupSet
        {
            get => _selectedGroupSet;
            set
            {
                if (_isSynchronizing || InitializationManager.IsAnyInitializing()) return;

                StartSync();
                try
                {
                    if (_selectedGroupSet != value)
                    {
                        // Only update AggLoadInfo if necessary
                        if (AggLoadInfo.Instance.ActiveGroupSet != value)
                        {
                            AggLoadInfo.Instance.ActiveGroupSet = value;
                        }

                        // Now reflect the change in the ViewModel
                        _selectedGroupSet = value ?? throw new ArgumentNullException(nameof(value));

                        // Trigger updates related to LoadOuts
                        UpdateLoadOutsForSelectedGroupSet();

                        // Ensure SelectedLoadOut is set to the appropriate value
                        //SelectedLoadOut = LoadOuts.FirstOrDefault() ?? throw new Exception("No LoadOuts available for the selected GroupSet.");

                        OnPropertyChanged(nameof(SelectedGroupSet)); // Notify only when changed
                    }
                }
                finally
                {
                    EndSync();
                }
            }
        }

        private LoadOut _selectedLoadOut;
        public LoadOut SelectedLoadOut
        {
            get => _selectedLoadOut;
            set
            {
                if (_selectedLoadOut != value)
                {
                    if (_isSynchronizing || InitializationManager.IsAnyInitializing()) return;

                    StartSync();
                    try
                    {
                        // Update the ActiveLoadOut in AggLoadInfo only if different
                        if (AggLoadInfo.Instance.ActiveLoadOut != value)
                        {
                            AggLoadInfo.Instance.ActiveLoadOut = value;
                        }

                        // Now reflect the change in the ViewModel
                        _selectedLoadOut = value ?? throw new ArgumentNullException(nameof(value));
                        OnPropertyChanged(nameof(SelectedLoadOut));
                    }
                    finally
                    {
                        EndSync();
                    }
                }
            }
        }
    }
}
