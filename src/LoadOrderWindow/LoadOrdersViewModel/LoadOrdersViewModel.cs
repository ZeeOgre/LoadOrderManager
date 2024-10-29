using System.Collections.ObjectModel;
using System.Windows;
using ZO.LoadOrderManager;

public class LoadOrdersViewModel : ViewModelBase
{
    private ObservableCollection<LoadOrderItemViewModel> items;

    public ObservableCollection<LoadOrderItemViewModel> Items
    {
        get => items;
        set => SetProperty(ref items, value);
    }

    private GroupSet _selectedGroupSet;
    public GroupSet SelectedGroupSet
    {
        get => _selectedGroupSet;
        set
        {
            if (InitializationManager.IsAnyInitializing()) return;

            if (SetProperty(ref _selectedGroupSet, value)) // This will only proceed if the value changes
            {
                OnSelectedGroupSetChanged();
            }
        }
    }



    private void OnSelectedGroupSetChanged()
    {
        RefreshData();
    }

    private LoadOut _selectedLoadOut;
    public LoadOut SelectedLoadOut
    {
        get => _selectedLoadOut;
        set
        {
            if (InitializationManager.IsAnyInitializing()) return;
            if (SetProperty(ref _selectedLoadOut, value))
            {
                OnSelectedLoadOutChanged();
            }
        }
    }

    private void OnSelectedLoadOutChanged()
    {
        RefreshActivePlugins(_selectedLoadOut);
        //RefreshData();
    }

    public bool Suppress997 { get; set; }
    public bool IsCached { get; set; }

    public LoadOrdersViewModel()
    {
        Items = new ObservableCollection<LoadOrderItemViewModel>();
        //AggLoadInfo.Instance.DataRefreshed += OnDataRefreshed;

    }

    public void LoadData(GroupSet groupSet, LoadOut loadOut, bool suppress997 = false, bool isCached = false)
    {
        // Ensure the collection updates happen on the UI thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            Items.Clear();
            AggLoadInfo.Instance.PopulateLoadOrders(this, groupSet, loadOut, suppress997, isCached);
            AggLoadInfo.Instance.SetDirty(false);
        });
    }


    public void RefreshData()
    {
        if (IsCached) return;
        // Use AggLoadInfo to populate the LoadOrdersViewModel
        var activeGroupSet = AggLoadInfo.Instance.ActiveGroupSet;
        var activeLoadOut = AggLoadInfo.Instance.ActiveLoadOut;

        //LoadData(activeGroupSet, activeLoadOut, suppress997: false, isCached: false);
        LoadData(activeGroupSet, activeLoadOut, this.Suppress997, this.IsCached);
        OnPropertyChanged(nameof(Items));


    }


    public void RefreshActivePlugins(LoadOut? loadOut = null)
    {
        if (loadOut == null)
        {
            loadOut = AggLoadInfo.Instance.ActiveLoadOut;
        }
        foreach (LoadOrderItemViewModel item in Items)
        {
            if (item.EntityType == EntityType.Plugin)
            {
                long pluginID = item.PluginData.PluginID;
                item.IsActive = loadOut.IsPluginEnabled(pluginID);
            }
        }
        //OnPropertyChanged(nameof(Items)); // Notify the TreeView to refresh
        RefreshData();
    }
}
