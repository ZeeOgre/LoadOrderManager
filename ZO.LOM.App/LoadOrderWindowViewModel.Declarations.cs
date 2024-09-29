using System;
using System.Collections.ObjectModel;
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

        public ObservableCollection<GroupSet> GroupSets { get; set; }
        public ObservableCollection<LoadOut> LoadOuts { get; set; }
        public LoadOrdersViewModel LoadOrders { get; set; }
        public LoadOrdersViewModel CachedGroupSetLoadOrders { get; set; }
        public ObservableCollection<LoadOrderItemViewModel> Items { get; }

        // PropertyChanged Event
        public event PropertyChangedEventHandler? PropertyChanged;

        // Properties
        private GroupSet _selectedGroupSet;
        public GroupSet SelectedGroupSet
        {
            get => _selectedGroupSet;
            set
            {
                if (InitializationManager.IsAnyInitializing()) return;
                if (_selectedGroupSet != value)
                {
                    // Check if the new value is different from the current value
                    if (AggLoadInfo.Instance.ActiveGroupSet != value)
                    {
                        // Update ActiveGroupSet in AggLoadInfo and refresh views
                        AggLoadInfo.Instance.ActiveGroupSet = value;
                        ReloadViews();
                    }

                    _selectedGroupSet = value ?? throw new ArgumentNullException(nameof(value));
                    OnPropertyChanged(nameof(SelectedGroupSet));

                    // Sync SelectedItem based on new SelectedGroupSet
                    if (AggLoadInfo.Instance.LoadOuts.Any())
                    {
                        LoadOuts.Clear();
                        foreach (var loadOut in AggLoadInfo.Instance.LoadOuts)
                        {
                            LoadOuts.Add(loadOut);
                        }
                        if (!AggLoadInfo.Instance.LoadOuts.Contains(SelectedLoadOut))
                        {
                            SelectedLoadOut = AggLoadInfo.Instance.LoadOuts.First();
                            AggLoadInfo.Instance.ActiveLoadOut = SelectedLoadOut;
                        }
                        else
                        {
                            SelectedLoadOut = AggLoadInfo.Instance.ActiveLoadOut;
                        }
                    }
                    else
                    {
                        CreateNewLoadOut();
                    }
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
                    if (_isSynchronizing) return; // Prevent re-entrance

                    _isSynchronizing = true;
                    try
                    {
                        _selectedLoadOut = value ?? throw new ArgumentNullException(nameof(value));
                        AggLoadInfo.Instance.ActiveLoadOut = _selectedLoadOut;
                        OnPropertyChanged(nameof(SelectedLoadOut));
                        RefreshCheckboxes();
                        
                    }
                    finally
                    {
                        _isSynchronizing = false;
                    }
                }
            }
        }

        private object? _selectedItem;
        public object? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    if (_isSynchronizing || InitializationManager.IsAnyInitializing()) return; // Prevent re-entrance

                    _isSynchronizing = true;
                    try
                    {
                        _selectedItem = value;
                        OnPropertyChanged(nameof(SelectedItem));

                        // Update Commands' CanExecute status
                        ((RelayCommand<object>)MoveUpCommand).RaiseCanExecuteChanged();
                        ((RelayCommand<object>)MoveDownCommand).RaiseCanExecuteChanged();

                        UpdateStatusMessage();
                    }
                    finally
                    {
                        _isSynchronizing = false;
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
        private void ReloadViews()
        {
            // Notify that the data has changed
            OnPropertyChanged(nameof(AggLoadInfo.Instance.Groups));
            OnPropertyChanged(nameof(AggLoadInfo.Instance.Plugins));
            OnPropertyChanged(nameof(AggLoadInfo.Instance.LoadOuts));

            // Refresh checkboxes based on the new data
            RefreshCheckboxes();
        }

        public void RefreshCheckboxes()
        {
            // Notify that the SelectedLoadOut has changed
            OnPropertyChanged(nameof(SelectedLoadOut));
        }

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

        public void CreateNewLoadOut()
        {
            var result = MessageBox.Show("No LoadOuts found. Do you want to create a new LoadOut?", "Create LoadOut", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                // Generate default name for the new loadout
                var defaultLoadoutName = $"{SelectedGroupSet.GroupSetName}_Loadout_{AggLoadInfo.Instance.LoadOuts.Count + 1}";

                // Show InputDialog to get the name of the new loadout
                var inputDialog = new InputDialog("Enter the name for the new LoadOut:", defaultLoadoutName);
                if (inputDialog.ShowDialog() == true)
                {
                    var newLoadoutName = inputDialog.ResponseText;

                    // Create and add the new loadout to the selected GroupSet
                    var newLoadOut = new LoadOut(SelectedGroupSet)
                    {
                        Name = newLoadoutName
                    };
                    AggLoadInfo.Instance.LoadOuts.Add(newLoadOut);
                    SelectedLoadOut = newLoadOut;
                    AggLoadInfo.Instance.ActiveLoadOut = newLoadOut;
                }
            }
        }
    }
}
