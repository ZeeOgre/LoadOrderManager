using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ZO.LoadOrderManager
{
    public class PluginViewModel : INotifyPropertyChanged
    {
        private Plugin _plugin;
        private bool _isEnabled;
        private ObservableCollection<ModGroup> _groups;
        private ObservableCollection<LoadOut> _loadouts;
        private string _files;
        private Dictionary<string, bool> _loadOutEnabled;
        private GroupSet _selectedGroupSet;
        private ObservableCollection<GroupSet> _availableGroupSets;

        public PluginViewModel()
        {
            _plugin = new Plugin();
            _groups = new ObservableCollection<ModGroup>();
            _loadouts = new ObservableCollection<LoadOut>();
            _files = string.Empty;
            _loadOutEnabled = new Dictionary<string, bool>();
            _selectedGroupSet = AggLoadInfo.Instance.ActiveGroupSet;
            _availableGroupSets = AggLoadInfo.Instance.GroupSets;
            PropertyChanged = delegate { };
        }

        public PluginViewModel(Plugin plugin)
        {
            _plugin = plugin;
            _groups = new ObservableCollection<ModGroup>();
            _loadouts = new ObservableCollection<LoadOut>();
            _files = string.Empty;
            _loadOutEnabled = new Dictionary<string, bool>();
            _selectedGroupSet = AggLoadInfo.Instance.ActiveGroupSet;
            _availableGroupSets = AggLoadInfo.Instance.GroupSets;
            PropertyChanged = delegate { };
        }

        public PluginViewModel(IEnumerable<Plugin> plugins)
        {
            _plugin = plugins.FirstOrDefault() ?? new Plugin();
            _groups = new ObservableCollection<ModGroup>();
            _loadouts = new ObservableCollection<LoadOut>();
            _files = string.Empty;
            _loadOutEnabled = new Dictionary<string, bool>();
            _selectedGroupSet = AggLoadInfo.Instance.ActiveGroupSet;
            _availableGroupSets = AggLoadInfo.Instance.GroupSets;
            PropertyChanged = delegate { };
        }

        public Plugin Plugin
        {
            get { return _plugin; }
            set { _plugin = value; OnPropertyChanged(nameof(Plugin)); }
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
        }

        public ObservableCollection<ModGroup> Groups
        {
            get { return _groups; }
            set { _groups = value; OnPropertyChanged(nameof(Groups)); }
        }

        public ObservableCollection<LoadOut> LoadOuts
        {
            get { return _loadouts; }
            set { _loadouts = value; OnPropertyChanged(nameof(LoadOuts)); }
        }

        public Dictionary<string, bool> LoadOutEnabled
        {
            get { return _loadOutEnabled; }
            set { _loadOutEnabled = value; OnPropertyChanged(nameof(LoadOutEnabled)); }
        }

        public string Files
        {
            get { return _files; }
            set { _files = value; OnPropertyChanged(nameof(Files)); }
        }

        public long PluginID => _plugin.PluginID;

        public GroupSet SelectedGroupSet
        {
            get { return _selectedGroupSet; }
            set
            {
                if (_selectedGroupSet != value)
                {
                    _selectedGroupSet = value;
                    OnPropertyChanged(nameof(SelectedGroupSet));
                    UpdateGroupsAndLoadouts();
                }
            }
        }

        public ObservableCollection<GroupSet> AvailableGroupSets
        {
            get { return _availableGroupSets; }
            set { _availableGroupSets = value; OnPropertyChanged(nameof(AvailableGroupSets)); }
        }

        public bool IsBethesdaChecked
        {
            get => _plugin.State.HasFlag(ModState.Bethesda) || !string.IsNullOrEmpty(_plugin.BethesdaID);
            set
            {
                if (value)
                {
                    _plugin.State |= ModState.Bethesda;
                }
                else
                {
                    _plugin.State &= ~ModState.Bethesda;
                }
                OnPropertyChanged(nameof(IsBethesdaChecked));
            }
        }

        public bool IsNexusChecked
        {
            get => _plugin.State.HasFlag(ModState.Nexus) || !string.IsNullOrEmpty(_plugin.NexusID);
            set
            {
                if (value)
                {
                    _plugin.State |= ModState.Nexus;
                }
                else
                {
                    _plugin.State &= ~ModState.Nexus;
                }
                OnPropertyChanged(nameof(IsNexusChecked));
            }
        }

        public bool IsGameFolderChecked
        {
            get => _plugin.State.HasFlag(ModState.GameFolder);
            set
            {
                if (value)
                {
                    _plugin.State |= ModState.GameFolder;
                }
                else
                {
                    _plugin.State &= ~ModState.GameFolder;
                }
                OnPropertyChanged(nameof(IsGameFolderChecked));
            }
        }

        public bool IsModManagerChecked
        {
            get => _plugin.State.HasFlag(ModState.ModManager);
            set
            {
                if (value)
                {
                    _plugin.State |= ModState.ModManager;
                }
                else
                {
                    _plugin.State &= ~ModState.ModManager;
                }
                OnPropertyChanged(nameof(IsModManagerChecked));
            }
        }

        public void Save()
        {
            Plugin.WriteMod();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UpdateGroupsAndLoadouts()
        {
            if (_selectedGroupSet != null)
            {
                Groups = new ObservableCollection<ModGroup>(_selectedGroupSet.ModGroups);
                LoadOuts = new ObservableCollection<LoadOut>(_selectedGroupSet.LoadOuts);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
