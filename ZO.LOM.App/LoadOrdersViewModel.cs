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
        Application.Current.Dispatcher.Invoke(() => Items.Clear());
        SortingHelper.PopulateLoadOrdersViewModel(this, groupSet, loadOut, suppress997, isCached);
    }

    public void RefreshData()
    {
        // Derive parameters from the instance properties
        LoadData(SelectedGroupSet, SelectedLoadOut, Suppress997, IsCached);
    }

    public void OnDataRefreshed(object? sender, EventArgs e)
    {
        // Reload the underlying data for the main LoadOrders
        RefreshData();
    }
}
