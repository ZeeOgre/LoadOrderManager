using System.Collections.ObjectModel;
using System.ComponentModel;
using ZO.LoadOrderManager;

public class LoadOrdersViewModel : INotifyPropertyChanged
{
    private ObservableCollection<LoadOrderItemViewModel> items;

    public ObservableCollection<LoadOrderItemViewModel> Items
    {
        get => items;
        set
        {
            if (items != value)
            {
                items = value;
                OnPropertyChanged(nameof(Items));
            }
        }
    }

    public LoadOrdersViewModel()
    {
        // Initialize Items using data from aggLoadInfo singleton
        var aggLoadInfoInstance = AggLoadInfo.Instance;
        Items = new ObservableCollection<LoadOrderItemViewModel>();

        // Populate Items with data from aggLoadInfo for Groups
        foreach (var group in aggLoadInfoInstance.Groups)
        {
            var groupViewModel = new LoadOrderItemViewModel
            {
                DisplayName = $"{group.GroupName} | {group.Description}",
                EntityType = EntityType.Group,
                GroupID = group.GroupID, // Set GroupID for group
                
                //GroupSet = GroupSet.LoadGroupSet(group.GroupSetID ?? 1), // Set GroupSetID for group
                Children = new ObservableCollection<LoadOrderItemViewModel>()
            };

            // Add plugins to the group view model
            foreach (var plugin in group.Plugins)
            {
                var pluginViewModel = new LoadOrderItemViewModel
                {
                    DisplayName = plugin.PluginName,
                    PluginData = plugin,
                    IsActive = plugin.Achievements, // Example property
                    EntityType = EntityType.Plugin,
                    //GroupSet = GroupSet.LoadGroupSet(group.GroupSetID ?? 1),
                    GroupID = plugin.GroupID // Set GroupID for plugin
                };
                groupViewModel.Children.Add(pluginViewModel);
            }

            Items.Add(groupViewModel);
        }

        // Update the IsActive property of plugins based on LoadOuts
        foreach (var loadOut in aggLoadInfoInstance.LoadOuts)
        {
            // Fetch plugins based on enabled plugin IDs in LoadOut
            var enabledPlugins = loadOut.enabledPlugins; // Directly use enabledPlugins from LoadOut

            foreach (var pluginId in enabledPlugins)
            {
                // Find the plugin by ID in the global plugin list
                var plugin = aggLoadInfoInstance.Plugins.FirstOrDefault(p => p.PluginID == pluginId);

                if (plugin != null)
                {
                    // Find the corresponding plugin view model and update IsActive
                    var pluginViewModel = FindPluginViewModel(plugin.PluginID);
                    if (pluginViewModel != null)
                    {
                        pluginViewModel.IsActive = true; // Plugin is enabled in this LoadOut
                    }
                }
            }
        }

        // Call SortItems after loading the data
        SortItems();
    }

    // Specialized constructor for loading GroupSetID=1
    public LoadOrdersViewModel GroupSet1LoadOrdersViewModel()
    {
        // Initialize Items using data from cached GroupSet1
        var cachedGroupSet1 = AggLoadInfo.Instance.GetCachedGroupSet1();
        Items = new ObservableCollection<LoadOrderItemViewModel>();

        // Populate Items with data from cached GroupSet1 for Groups
        foreach (var group in cachedGroupSet1.ModGroups)
        {
            var groupViewModel = new LoadOrderItemViewModel
            {
                DisplayName = $"{group.GroupName} | {group.Description}",
                EntityType = EntityType.Group,
                GroupID = group.GroupID, // Set GroupID for group
                GroupSet = GroupSet.LoadGroupSet(1), // Set GroupSetID for group
                Children = new ObservableCollection<LoadOrderItemViewModel>()
            };

            // Add plugins to the group view model
            foreach (var plugin in group.Plugins)
            {
                var pluginViewModel = new LoadOrderItemViewModel
                {
                    DisplayName = plugin.PluginName,
                    PluginData = plugin,
                    IsActive = plugin.Achievements, // Example property
                    EntityType = EntityType.Plugin,
                    GroupSet = GroupSet.LoadGroupSet(1), // Set GroupSetID for group
                    GroupID = plugin.GroupID // Set GroupID for plugin
                };
                groupViewModel.Children.Add(pluginViewModel);
            }

            Items.Add(groupViewModel);
        }

        // Update the IsActive property of plugins based on LoadOuts
        foreach (var loadOut in cachedGroupSet1.LoadOuts)
        {
            // Fetch plugins based on enabled plugin IDs in LoadOut
            var enabledPlugins = loadOut.enabledPlugins; // Directly use enabledPlugins from LoadOut

            foreach (var pluginId in enabledPlugins)
            {
                // Find the plugin by ID in the global plugin list
                var plugin = AggLoadInfo.Instance.Plugins.FirstOrDefault(p => p.PluginID == pluginId);

                if (plugin != null)
                {
                    // Find the corresponding plugin view model and update IsActive
                    var pluginViewModel = FindPluginViewModel(plugin.PluginID);
                    if (pluginViewModel != null)
                    {
                        pluginViewModel.IsActive = true; // Plugin is enabled in this LoadOut
                    }
                }
            }
        }

        // Call SortItems after loading the data
        //SortItems();

        return this;
    }

    private LoadOrderItemViewModel? FindPluginViewModel(long pluginId)
    {
        foreach (var group in Items)
        {
            if (group.EntityType == EntityType.Group)
            {
                foreach (var plugin in group.Children)
                {
                    if (plugin.EntityType == EntityType.Plugin && plugin.PluginData.PluginID == pluginId)
                    {
                        return plugin;
                    }
                }
            }
        }
        return null;
    }

    public void SortItems()
    {
        // Ensure sorting happens only after data is loaded
        SortingHelper.SortGroupsAndPlugins(AggLoadInfo.Instance.Groups, AggLoadInfo.Instance.Plugins, Items, AggLoadInfo.Instance.ActiveGroupSet);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

