using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace ZO.LoadOrderManager
{
    public partial class LoadOrderWindowViewModel : INotifyPropertyChanged
    {
        // Fields
        private bool isSaved;
        private Timer cooldownTimer;
        private object? _selectedItem;
        private long? _selectedProfileId;
        private string _statusMessage;
        private string _searchText;
        private GroupSet _selectedGroupSet;
        private LoadOut _selectedLoadOut;
        private bool _isInitialDataLoaded = false;

        // PropertyChanged Event
        public event PropertyChangedEventHandler? PropertyChanged;

        // Properties
        public ObservableCollection<GroupSet> GroupSets { get; set; }
        public GroupSet SelectedGroupSet
        {
            get => _selectedGroupSet;
            set
            {
                if (_selectedGroupSet != value)
                {
                    _selectedGroupSet = value ?? throw new ArgumentNullException(nameof(value));
                    OnPropertyChanged(nameof(SelectedGroupSet));
                    AggLoadInfo.Instance.ActiveGroupSet = _selectedGroupSet;
                    ReloadViews();
                    if (LoadOuts.Any())
                    {
                        SelectedLoadOut = LoadOuts.First();
                    }
                    RefreshCheckboxes();
                }
            }
        }

        public object? SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
                ((RelayCommand<object>)MoveUpCommand).RaiseCanExecuteChanged();
                ((RelayCommand<object>)MoveDownCommand).RaiseCanExecuteChanged();
                UpdateStatusMessage();
            }
        }

        public long? SelectedProfileId
        {
            get => _selectedProfileId;
            set
            {
                if (_selectedProfileId != value)
                {
                    _selectedProfileId = value;
                    OnPropertyChanged(nameof(SelectedProfileId));
                }
            }
        }

        public LoadOut SelectedLoadOut
        {
            get => _selectedLoadOut;
            set
            {
                _selectedLoadOut = value;
                AggLoadInfo.Instance.ActiveLoadOut = value;
                OnPropertyChanged(nameof(SelectedLoadOut));
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

        public ObservableCollection<ModGroup> Groups { get; set; }
        public ObservableCollection<PluginViewModel> Plugins { get; set; }
        public ObservableCollection<LoadOut> LoadOuts { get; set; }
        public LoadOrdersViewModel LoadOrders { get; set; }
        public LoadOrdersViewModel CachedGroupSetLoadOrders { get; set; }
        public ObservableCollection<LoadOrderItemViewModel> Items { get; }

       

        // OnPropertyChanged Method
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Named Methods used in Properties
        private void ReloadViews()
        {
            // Clear and reload the collections based on the updated AggLoadInfo
            Groups.Clear();
            Plugins.Clear();
            LoadOuts.Clear();

            foreach (var group in AggLoadInfo.Instance.Groups)
            {
                Groups.Add(group);
            }

            foreach (var plugin in AggLoadInfo.Instance.Plugins)
            {
                Plugins.Add(new PluginViewModel(plugin));
            }

            foreach (var loadOut in AggLoadInfo.Instance.LoadOuts)
            {
                LoadOuts.Add(loadOut);
            }

            // Update other properties or views as needed
            UpdateLoadOrders();
        }

        public void RefreshCheckboxes()
        {
            if (SelectedLoadOut == null)
            {
                return;
            }

            foreach (var plugin in Plugins)
            {
                plugin.IsEnabled = SelectedLoadOut.enabledPlugins.Contains(plugin.PluginID);
            }
        }
        
        public void UpdateStatus(string message)
        {
            StatusMessage = message;
        }

        private void UpdateStatusMessage()
        {
            if (SelectedItem != null)
            {
                StatusMessage = SelectedItem.ToString();
            }
            else
            {
                StatusMessage = "No item selected";
            }
        }


        private void UpdateLoadOrders()
        {
            // Load data based on selected GroupSet
            var selectedGroupSet = GroupSets.FirstOrDefault(gs => gs.GroupSetID == SelectedGroupSet.GroupSetID);
            if (selectedGroupSet != null)
            {
                // Update other properties (e.g., Items) based on selected GroupSet
                // Items.Clear(); // Clear and repopulate as needed
            }
        }

    }
}
