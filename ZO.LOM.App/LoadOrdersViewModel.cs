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
                Children = new ObservableCollection<LoadOrderItemViewModel>()
            };

            // Add plugins to the group view model
            foreach (var plugin in group.Plugins)
            {
                var pluginViewModel = new LoadOrderItemViewModel
                {
                    DisplayName = plugin.PluginName,
                    PluginData = plugin,
                    IsEnabled = plugin.Achievements, // Example property
                    EntityType = EntityType.Plugin,
                    GroupID = plugin.GroupID // Set GroupID for plugin
                };
                groupViewModel.Children.Add(pluginViewModel);
            }

            Items.Add(groupViewModel);
        }

        // Populate Items with data from aggLoadInfo for LoadOuts
        foreach (var loadOut in aggLoadInfoInstance.LoadOuts)
        {
            var loadOutViewModel = new LoadOrderItemViewModel
            {
                DisplayName = loadOut.Name,
                EntityType = EntityType.LoadOut,
                Children = new ObservableCollection<LoadOrderItemViewModel>()
            };

            // Fetch plugins based on enabled plugin IDs in LoadOut
            var enabledPlugins = loadOut.enabledPlugins; // Directly use enabledPlugins from LoadOut

            foreach (var pluginId in enabledPlugins)
            {
                // Find the plugin by ID in the global plugin list
                var plugin = aggLoadInfoInstance.Plugins.FirstOrDefault(p => p.PluginID == pluginId);

                if (plugin != null)
                {
                    var pluginViewModel = new LoadOrderItemViewModel
                    {
                        DisplayName = plugin.PluginName,
                        PluginData = plugin,
                        IsEnabled = true, // Plugin is enabled in this LoadOut
                        EntityType = EntityType.Plugin,
                        GroupID = plugin.GroupID // Set GroupID for plugin
                    };
                    loadOutViewModel.Children.Add(pluginViewModel);
                }
            }

            Items.Add(loadOutViewModel);
        }

        // Call SortItems after loading the data
        SortItems();
    }

    public void SortItems()
    {
        // Ensure sorting happens only after data is loaded
        SortingHelper.SortGroupsAndPlugins(AggLoadInfo.Instance.Groups, AggLoadInfo.Instance.Plugins, Items);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

