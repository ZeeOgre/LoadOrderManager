using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Windows;
using System.Windows.Input;

namespace ZO.LoadOrderManager
{
    public partial class LoadOrderWindowViewModel : INotifyPropertyChanged
    {
        // Fields
        private bool isSaved;
        private Timer cooldownTimer;
        private string _statusMessage;
        private string _searchText;
        private bool _isInitialDataLoaded = false;
        private bool _isSynchronizing = false;
        private List<LoadOrderItemViewModel>? _cachedFlatList;
        private bool _isRefreshing = false;

        public ObservableCollection<GroupSet> GroupSets { get; set; }
        public ObservableCollection<LoadOut> LoadOuts { get; set; }

        public LoadOrdersViewModel LoadOrders { get; set; }
        public LoadOrdersViewModel CachedGroupSetLoadOrders { get; set; }
        
        
        //public ObservableCollection<LoadOrderItemViewModel> Items { get; }
        //// Backing field for SelectedItems
        
        
        private ObservableCollection<object> selectedItems;

        // Property for SelectedItems with change notification
        public ObservableCollection<object> SelectedItems
        {
            get => selectedItems;
            set
            {
                selectedItems = value;
                if (!_isSynchronizing)
                {
                    OnPropertyChanged(nameof(SelectedItems)); // Notify only when not synchronizing
                }
            }
        }

        // Direct public property for SelectedCachedItems
        public ObservableCollection<object> SelectedCachedItems { get; set; }

        // PropertyChanged Event
        public event PropertyChangedEventHandler? PropertyChanged;

        private void RebuildFlatList()
        {
            _isRefreshing = true;
            _cachedFlatList = Flatten(LoadOrders.Items).ToList();
            _isRefreshing = false;
            //OnPropertyChanged(nameof(LoadOrders.Items)); // Notify UI about the change
        }

        public void StartSync()
        {
            _isSynchronizing = true;
        }

        public void EndSync()
        {
            _isSynchronizing = false;
        }

        private bool _isHandlingAggLoadInfoPropertyChanged = false;
        private void OnAggLoadInfoPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isSynchronizing || _isHandlingAggLoadInfoPropertyChanged)
            {
                return;
            }
            _isHandlingAggLoadInfoPropertyChanged = true;
            StartSync();

            try
            {
                if (e.PropertyName == nameof(AggLoadInfo.ActiveGroupSet))
                {
                    if (!ReferenceEquals(_selectedGroupSet, AggLoadInfo.Instance.ActiveGroupSet))
                    {
                        _selectedGroupSet = AggLoadInfo.Instance.ActiveGroupSet;
                        _selectedLoadOut = AggLoadInfo.Instance.ActiveLoadOut; // Set SelectedLoadOut without notification
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
            }
            finally
            {
                EndSync();
                _isHandlingAggLoadInfoPropertyChanged = false;
            }
        }

        // Properties
        private GroupSet _selectedGroupSet;
        public GroupSet SelectedGroupSet
        {
            get => _selectedGroupSet;
            set
            {
                if (_isSynchronizing || InitializationManager.IsAnyInitializing())
                {
                    return;
                }
                StartSync();
                if (_selectedGroupSet != value)
                {
                    // Check if the new value is different from the current value
                    if (AggLoadInfo.Instance.ActiveGroupSet != value)
                    {
                        // Update ActiveGroupSet in AggLoadInfo
                        AggLoadInfo.Instance.ActiveGroupSet = value;
                    }
                    _selectedGroupSet = value ?? throw new ArgumentNullException(nameof(value));
                }
                EndSync();
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
                    if (_isSynchronizing || InitializationManager.IsAnyInitializing()) return; // Prevent re-entrance

                    StartSync();
                    try
                    {
                        

                        AggLoadInfo.Instance.ActiveLoadOut = _selectedLoadOut;
                        OnPropertyChanged(nameof(SelectedLoadOut));
                        
                    }
                    finally
                    {
                        EndSync();
                    }
                }
            }
        }

        private object _selectedItem;
        public object SelectedItem
        {
            get
            {
                if (SelectedItems != null && SelectedItems.Count > 0)
                    return SelectedItems[0]; // Always return the first item in SelectedItems

                return _selectedItem;
            }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;

                    if (_isSynchronizing)
                        return; // Prevent re-entrance during synchronization

                    StartSync();
                    try
                    {
                        if (SelectedItems != null)
                        {
                            // Ensure that the new selected item is the first in SelectedItems
                            if (_selectedItem != null)
                            {
                                // Remove it if it's already present, then add it to the first position
                                SelectedItems.Remove(_selectedItem);
                                SelectedItems.Insert(0, _selectedItem); // Insert at the first position
                            }
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



        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
            }
        }

        // OnPropertyChanged Method
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Named Methods used in Properties
        //private void ReloadViews()
        //{
         
            
        //    // Notify that the data has changed
        //    //OnPropertyChanged(nameof(AggLoadInfo.Instance.Groups));
        //    //OnPropertyChanged(nameof(AggLoadInfo.Instance.Plugins));
        //    //OnPropertyChanged(nameof(AggLoadInfo.Instance.LoadOuts));

        //    // Refresh checkboxes based on the new data
        //    //RefreshCheckboxes();
        //}

        ////public void RefreshCheckboxes()
        ////{
        ////    // Notify that the SelectedLoadOut has changed
        ////    OnPropertyChanged(nameof(SelectedLoadOut));
        ////}

        public void UpdateStatus(string message)
        {
            StatusMessage = message;
        }

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
    }
}
