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
        // Backing field for SelectedItems
        private ObservableCollection<object> selectedItems;

        // Property for SelectedItems with change notification
        public ObservableCollection<object> SelectedItems
        {
            get => selectedItems;
            set
            {
                selectedItems = value;
                OnPropertyChanged(nameof(SelectedItems)); // Notify when SelectedItems changes
            }
        }

        // Direct public property for SelectedCachedItems
        public ObservableCollection<object> SelectedCachedItems { get; set; }

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
                        //ReloadViews();
                    }
                    SelectedLoadOut = GetLoadOutForGroupSet(SelectedGroupSet);
                    _selectedGroupSet = value ?? throw new ArgumentNullException(nameof(value));
                    OnPropertyChanged(nameof(SelectedGroupSet));

                   

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
                        

                        // Usage:
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
            }
        }





        private LoadOut GetLoadOutForGroupSet(GroupSet groupSet)
        {
            // Try to find the favorite loadout
            var favoriteLoadOut = groupSet.LoadOuts.FirstOrDefault(l => l.IsFavorite);
            if (favoriteLoadOut != null)
            {
                return favoriteLoadOut;
            }

            // Try to find the default loadout
            var defaultLoadOut = groupSet.LoadOuts.FirstOrDefault(l => l.Name == "Default");
            if (defaultLoadOut != null)
            {
                return defaultLoadOut;
            }

            // Try to find the first loadout
            var firstLoadOut = groupSet.LoadOuts.FirstOrDefault();
            if (firstLoadOut != null)
            {
                return firstLoadOut;
            }

            // Prompt the user to create a new loadout
            var result = MessageBox.Show("No LoadOuts found. Do you want to create a new LoadOut?", "Create LoadOut", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                // Get the name for the new loadout
                var inputDialog = new InputDialog("Enter the name for the new LoadOut:");
                if (inputDialog.ShowDialog() == true)
                {
                    var newLoadoutName = inputDialog.ResponseText;

                    // Create and add the new loadout to the groupset
                    var newLoadOut = new LoadOut(groupSet)
                    {
                        Name = newLoadoutName
                    };
                    groupSet.LoadOuts.Add(newLoadOut);
                    return newLoadOut;
                }
            }

            // Return null if no loadout is found and the user chooses not to create a new one
            return null;
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
