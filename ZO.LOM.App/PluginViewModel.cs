using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using ZO.LoadOrderManager;

public class PluginViewModel : INotifyPropertyChanged
{
    private Plugin _plugin;
    private ObservableCollection<LoadOut> _loadouts;
    private ObservableCollection<ModGroup> _groups;
    private ObservableCollection<FileInfo> _files;
    private ObservableCollection<GroupSet> _availableGroupSets;
    private GroupSet _selectedGroupSet;
    private ModGroup _selectedGroup;
    private AggLoadInfo _aggLoadInfo;

    public PluginViewModel(Plugin plugin, AggLoadInfo? aggLoadInfo = null)
    {
        Plugin = plugin;
        _aggLoadInfo = aggLoadInfo ?? AggLoadInfo.Instance; // Use passed AggLoadInfo or fallback to singleton
        Initialize();
        TogglePluginEnabledInLoadOutCommand = new RelayCommand<LoadOut>(TogglePluginEnabledInLoadOut);
    }

    private void Initialize()
    {
        // Load groups, loadouts, and group sets
        _groups = new ObservableCollection<ModGroup>(_aggLoadInfo.Groups);
        _loadouts = new ObservableCollection<LoadOut>(_aggLoadInfo.LoadOuts);
        _availableGroupSets = new ObservableCollection<GroupSet>(_aggLoadInfo.GetGroupSets());

        // Assign the GroupSet for the plugin
        _selectedGroupSet = _availableGroupSets.FirstOrDefault(gs => gs.GroupSetID == Plugin.GroupSetID);
        OnPropertyChanged(nameof(SelectedGroupSet));

        // Set the selected group based on Plugin.GroupID
        _selectedGroup = _groups.FirstOrDefault(g => g.GroupID == Plugin.GroupID);
        OnPropertyChanged(nameof(SelectedGroup));

        // Load files for the plugin
        _files = new ObservableCollection<FileInfo>(new FileInfo().LoadFilesByPlugin(Plugin.PluginID));
        OnPropertyChanged(nameof(Files));

        // Notify property changes for data binding
        OnPropertyChanged(nameof(Groups));
        OnPropertyChanged(nameof(LoadOuts));
        OnPropertyChanged(nameof(AvailableGroupSets));
        OnPropertyChanged(nameof(IsGameFolderChecked));
        OnPropertyChanged(nameof(IsBethesdaChecked));
        OnPropertyChanged(nameof(IsNexusChecked));
        OnPropertyChanged(nameof(IsModManagerChecked));
    }

    public Plugin Plugin
    {
        get => _plugin;
        set
        {
            _plugin = value;
            OnPropertyChanged(nameof(Plugin));
        }
    }

    public GroupSet SelectedGroupSet
    {
        get => _selectedGroupSet;
        set
        {
            if (_selectedGroupSet != value)
            {
                _selectedGroupSet = value;
                UpdateGroupsAndLoadouts();
                OnPropertyChanged(nameof(SelectedGroupSet));
            }
        }
    }

    public ModGroup SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (_selectedGroup != value)
            {
                // Only update the Plugin's GroupID without saving immediately
                _selectedGroup = value;
                Plugin.GroupID = value.GroupID;  // Update the Plugin's group ID

                // Notify UI about the change
                OnPropertyChanged(nameof(SelectedGroup));
            }
        }
    }

    public ObservableCollection<ModGroup> Groups
    {
        get => _groups;
        set
        {
            _groups = value;
            OnPropertyChanged(nameof(Groups));
        }
    }

    public ObservableCollection<LoadOut> LoadOuts
    {
        get => _loadouts;
        set
        {
            _loadouts = value;
            OnPropertyChanged(nameof(LoadOuts));
        }
    }

    public ObservableCollection<GroupSet> AvailableGroupSets
    {
        get => _availableGroupSets;
        set
        {
            _availableGroupSets = value;
            OnPropertyChanged(nameof(AvailableGroupSets));
        }
    }

    public ObservableCollection<FileInfo> Files
    {
        get => _files;
        set
        {
            _files = value;
            OnPropertyChanged(nameof(Files));
        }
    }

    public bool IsPluginEnabledInSelectedLoadOut
    {
        get => SelectedLoadOut != null && IsPluginEnabledInLoadOut(SelectedLoadOut);
        set
        {
            if (SelectedLoadOut != null)
            {
                LoadOut.SetPluginEnabled(SelectedLoadOut.ProfileID, Plugin.PluginID, value);
                OnPropertyChanged(nameof(IsPluginEnabledInSelectedLoadOut));
            }
        }
    }

    private LoadOut _selectedLoadOut;
    public LoadOut SelectedLoadOut
    {
        get => _selectedLoadOut;
        set
        {
            _selectedLoadOut = value;
            OnPropertyChanged(nameof(IsPluginEnabledInSelectedLoadOut)); // Notify when the selected loadout changes
        }
    }

    public bool IsPluginEnabledInLoadOut(LoadOut loadOut)
    {
        return loadOut.IsPluginEnabled(Plugin.PluginID);
    }

    public bool IsGameFolderChecked
    {
        get => (Plugin.State & ModState.GameFolder) == ModState.GameFolder;
        set
        {
            if (value)
                Plugin.State |= ModState.GameFolder; // Add the flag
            else
                Plugin.State &= ~ModState.GameFolder; // Remove the flag
            OnPropertyChanged(nameof(IsGameFolderChecked));
        }
    }

    public bool IsBethesdaChecked
    {
        get => !string.IsNullOrEmpty(Plugin.BethesdaID) || (Plugin.State & ModState.Bethesda) == ModState.Bethesda;
        set
        {
            if (value)
            {
                // If checked, set the Bethesda flag
                Plugin.State |= ModState.Bethesda;

                // Optionally, you can enforce an empty BethesdaID here if needed
                if (string.IsNullOrEmpty(Plugin.BethesdaID))
                    Plugin.BethesdaID = string.Empty; // or assign a default value if necessary
            }
            else
            {
                // If unchecked, remove the Bethesda flag
                Plugin.State &= ~ModState.Bethesda;
            }
            OnPropertyChanged(nameof(IsBethesdaChecked));
        }
    }

    public bool IsNexusChecked
    {
        get => !string.IsNullOrEmpty(Plugin.NexusID) || (Plugin.State & ModState.Nexus) == ModState.Nexus;
        set
        {
            if (value)
            {
                // If checked, set the Nexus flag
                Plugin.State |= ModState.Nexus;

                // Optionally, you can enforce an empty NexusID here if needed
                if (string.IsNullOrEmpty(Plugin.NexusID))
                    Plugin.NexusID = string.Empty; // or assign a default value if necessary
            }
            else
            {
                // If unchecked, remove the Nexus flag
                Plugin.State &= ~ModState.Nexus;
            }
            OnPropertyChanged(nameof(IsNexusChecked));
        }
    }

    public bool IsModManagerChecked
    {
        get => (Plugin.State & ModState.ModManager) == ModState.ModManager;
        set
        {
            if (value)
                Plugin.State |= ModState.ModManager;
            else
                Plugin.State &= ~ModState.ModManager;
            OnPropertyChanged(nameof(IsModManagerChecked));
        }
    }

    public void UpdateGroupsAndLoadouts()
    {
        _groups = new ObservableCollection<ModGroup>(_aggLoadInfo.Groups);
        _loadouts = new ObservableCollection<LoadOut>(_aggLoadInfo.LoadOuts);
        OnPropertyChanged(nameof(Groups));
        OnPropertyChanged(nameof(LoadOuts));
    }



    public void SavePluginChanges()
    {

        _plugin.GroupID = SelectedGroup?.GroupID ?? 0; // Ensure GroupID is updated

        // No longer checking for empty or null, to allow fields to be cleared
        _plugin.Description = Plugin.Description ?? string.Empty;  // Allow clearing
        _plugin.NexusID = Plugin.NexusID ?? string.Empty;          // Allow clearing
        _plugin.BethesdaID = Plugin.BethesdaID ?? string.Empty;     // Allow clearing
        _plugin.Version = Plugin.Version ?? string.Empty;           // Allow clearing
        _plugin.DTStamp = Plugin.DTStamp ?? string.Empty;

        // Update mod state based on flags
        _plugin.State = Plugin.State;

        _aggLoadInfo.UpdatePlugin(_plugin);
        // Write the plugin to the database, ensuring all changes are persisted
        _plugin.WriteMod();
    }




    public ICommand TogglePluginEnabledInLoadOutCommand { get; }

    private void TogglePluginEnabledInLoadOut(LoadOut loadOut)
    {
        bool isActive = loadOut.IsPluginEnabled(Plugin.PluginID);
        LoadOut.SetPluginEnabled(loadOut.ProfileID, Plugin.PluginID, !isActive);
        OnPropertyChanged(nameof(LoadOuts));
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
