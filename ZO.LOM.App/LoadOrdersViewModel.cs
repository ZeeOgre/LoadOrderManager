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

    public GroupSet SelectedGroupSet { get; set; }
    public LoadOut SelectedLoadOut { get; set; }
    public bool Suppress997 { get; set; }
    public bool IsCached { get; set; }

    public LoadOrdersViewModel()
    {
        Items = new ObservableCollection<LoadOrderItemViewModel>();
        AggLoadInfo.Instance.DataRefreshed += OnDataRefreshed;
    }

    public void LoadData(GroupSet groupSet, LoadOut loadOut, bool suppress997 = false, bool isCached = false)
    {
        Items.Clear();
        AggLoadInfo.Instance.PopulateLoadOrders(this, groupSet, loadOut, suppress997, isCached);
    }

    public void RefreshData()
    {
        // Use AggLoadInfo to populate the LoadOrdersViewModel
        var activeGroupSet = AggLoadInfo.Instance.ActiveGroupSet;
        var activeLoadOut = AggLoadInfo.Instance.ActiveLoadOut;

        AggLoadInfo.Instance.PopulateLoadOrders(this, activeGroupSet, activeLoadOut, suppress997: true);
        OnPropertyChanged(nameof(Items));


        // If there is a cached group, also populate it
        var cachedGroupSet = AggLoadInfo.GetCachedGroupSet1();
        if (cachedGroupSet != null)
        {
            var cachedViewModel = new LoadOrdersViewModel();
            AggLoadInfo.Instance.PopulateLoadOrders(cachedViewModel, cachedGroupSet, activeLoadOut, suppress997: true, isCached: true);
        }
    }

    public void OnDataRefreshed(object? sender, EventArgs e)
    {
        AggLoadInfo.Instance.PerformLockedAction(() =>
        {
            // Reload the underlying data for the main LoadOrders
            RefreshData();

            // Unset the dirty flag on the sender after processing
            if (sender is AggLoadInfo aggLoadInfo)
            {
                aggLoadInfo.SetDirty(false);
            }
        });
    }
}
