using MahApps.Metro.Controls;
using System.Windows.Input;
using System.Windows;
using System.Collections.ObjectModel;

namespace ZO.LoadOrderManager
{
    public partial class LoadOutEditor : MetroWindow
    {
        private LoadOut _loadOut;
        private readonly AggLoadInfo _aggLoadInfo;

        public LoadOutEditor(LoadOut loadOut)
        {
            InitializeComponent();
            _loadOut = loadOut;
            _aggLoadInfo = AggLoadInfo.Instance; // Assuming AggLoadInfo is a singleton

            // Set DataContext to this editor for command binding
            this.DataContext = this;

            // Bind the LoadOut object separately to the relevant parts of the UI
            BindDisplayToRecord();

            // Initialize navigator commands
            FirstRecordCommand = new RelayCommand(_ => NavigateFirstRecord());
            PreviousRecordCommand = new RelayCommand(_ => NavigatePreviousRecord(), _ => CanNavigatePrevious());
            NextRecordCommand = new RelayCommand(_ => NavigateNextRecord(), _ => CanNavigateNext());
            LastRecordCommand = new RelayCommand(_ => NavigateLastRecord());
            JumpToRecordCommand = new RelayCommand<object>(JumpToRecord);

            // Initialize add and delete commands
            AddNewCommand = new RelayCommand(_ => AddNewLoadOut());
            DeleteCommand = new RelayCommand(_ => DeleteCurrentLoadOut(), _ => CanDeleteLoadOut());

            // Initialize the record navigation state
            UpdateCurrentRecordInfo();
        }

        private void BindDisplayToRecord()
        {
            LoadOutNameTextBox.DataContext = _loadOut;
            GroupSetIDTextBox.DataContext = _loadOut;
            IsFavoriteCheckbox.DataContext = _loadOut;
            PluginIDGrid.DataContext = _loadOut;
            ProfileIDTextBox.DataContext = _loadOut;
        }

        #region Navigator Commands
        public ICommand FirstRecordCommand { get; }
        public ICommand PreviousRecordCommand { get; }
        public ICommand NextRecordCommand { get; }
        public ICommand LastRecordCommand { get; }
        public ICommand JumpToRecordCommand { get; }


        public string CurrentRecordInfo { get; set; }
        public int JumpToProfileID { get; set; } // For the ProfileID input

        private void NavigateFirstRecord()
        {
            _loadOut = _aggLoadInfo.LoadOuts.FirstOrDefault();
            BindDisplayToRecord();
            UpdateCurrentRecordInfo();
        }

        private void NavigatePreviousRecord()
        {
            int currentIndex = _aggLoadInfo.LoadOuts.IndexOf(_loadOut);
            if (currentIndex > 0)
            {
                _loadOut = _aggLoadInfo.LoadOuts[currentIndex - 1];
                BindDisplayToRecord();
                UpdateCurrentRecordInfo();
            }
        }

        private bool CanNavigatePrevious()
        {
            return _aggLoadInfo.LoadOuts.IndexOf(_loadOut) > 0;
        }

        private void NavigateNextRecord()
        {
            int currentIndex = _aggLoadInfo.LoadOuts.IndexOf(_loadOut);
            if (currentIndex < _aggLoadInfo.LoadOuts.Count - 1)
            {
                _loadOut = _aggLoadInfo.LoadOuts[currentIndex + 1];
                BindDisplayToRecord();
                UpdateCurrentRecordInfo();
            }
        }

        private bool CanNavigateNext()
        {
            return _aggLoadInfo.LoadOuts.IndexOf(_loadOut) < _aggLoadInfo.LoadOuts.Count - 1;
        }

        private void NavigateLastRecord()
        {
            _loadOut = _aggLoadInfo.LoadOuts.LastOrDefault();
            BindDisplayToRecord();
            UpdateCurrentRecordInfo();
        }

        private void JumpToRecord(object parameter)
        {
            if (parameter is string targetProfileIDString && int.TryParse(targetProfileIDString, out int targetProfileID))
            {
                var targetLoadOut = _aggLoadInfo.LoadOuts.FirstOrDefault(lo => lo.ProfileID == targetProfileID);
                if (targetLoadOut != null)
                {
                    _loadOut = targetLoadOut;
                    BindDisplayToRecord();
                    UpdateCurrentRecordInfo();
                }
                else
                {
                    MessageBox.Show("ProfileID not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
        #endregion

        #region Add and Delete Commands
        public ICommand AddNewCommand { get; }
        public ICommand DeleteCommand { get; }

        private void AddNewLoadOut()
        {
            var newLoadOut = new LoadOut(_aggLoadInfo.ActiveGroupSet) { Name = $"{_aggLoadInfo.ActiveGroupSet.GroupSetName}_NEW_LOADOUT" };
            _aggLoadInfo.LoadOuts.Add(newLoadOut);
            _loadOut = newLoadOut;
            BindDisplayToRecord();
            UpdateCurrentRecordInfo();
        }

        private void DeleteCurrentLoadOut()
        {
            if (_loadOut != null)
            {
                if (_loadOut.IsFavorite)
                {
                    MessageBox.Show("Please select another LoadOut as favorite before deleting this one.", "Cannot Delete Favorite", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_aggLoadInfo.ActiveLoadOut == _loadOut)
                {
                    MessageBox.Show("Please select another LoadOut as active before deleting this one.", "Cannot Delete Active LoadOut", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _aggLoadInfo.LoadOuts.Remove(_loadOut);
                _loadOut.DeleteProfile();
                NavigateFirstRecord(); // Navigate to the first record after deletion
            }
        }

        private bool CanDeleteLoadOut()
        {
            App.LogDebug($"CanDeleteLoadOut: _loadOut {_loadOut.Name} != null: {_loadOut != null}");
            App.LogDebug($"CanDeleteLoadOut: _aggLoadInfo.LoadOuts.Contains(_loadOut): {_aggLoadInfo.LoadOuts.Contains(_loadOut)}");
            App.LogDebug($"CanDeleteLoadOut: _aggLoadInfo.ActiveLoadOut != _loadOut: {_aggLoadInfo.ActiveLoadOut != _loadOut}");
            App.LogDebug($"CanDeleteLoadOut: !_loadOut.IsFavorite: {!_loadOut.IsFavorite}");

            return _loadOut != null &&
                   _aggLoadInfo.LoadOuts.Contains(_loadOut) &&
                   _aggLoadInfo.ActiveLoadOut != _loadOut &&
                   !_loadOut.IsFavorite;
        }
        #endregion

        private void UpdateCurrentRecordInfo()
        {
            int currentIndex = _aggLoadInfo.LoadOuts.IndexOf(_loadOut) + 1;
            CurrentRecordInfo = $"Record {currentIndex} of {_aggLoadInfo.LoadOuts.Count}";
            OnPropertyChanged(nameof(CurrentRecordInfo));
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _loadOut = _loadOut.WriteProfile(); // Save to the database
            _aggLoadInfo.LoadOuts.Add(_loadOut); // Add to the ObservableCollection
            var loadOutsList = GroupSet.GetAllLoadOuts(_aggLoadInfo.ActiveGroupSet.GroupSetID);
            this.DialogResult = true; // Indicate that we are closing the dialog with a successful result
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Indicate that we are closing the dialog with a canceled result
            this.Close();
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public LoadOut GetLoadOut()
        {
            return _loadOut;
        }

    }
}