using System.Collections.ObjectModel;
using ZO.LoadOrderManager;

public class LoadOrdersViewModel : ViewModelBase
{
    private ObservableCollection<LoadOrderItemViewModel> items;

    public ObservableCollection<LoadOrderItemViewModel> Items
    {
        get => items;
        set => SetProperty(ref items, value);
    }

    public LoadOrdersViewModel()
    {
        Items = new ObservableCollection<LoadOrderItemViewModel>();
    }

    public void LoadData(GroupSet groupSet, LoadOut loadOut, bool suppress997 = false)
    {
        Items.Clear();
        SortingHelper.PopulateLoadOrdersViewModel(this, groupSet, loadOut, suppress997);
    }

    public void SortItems(GroupSet groupSet)
    {
        // Re-sort the items based on a given GroupSet without clearing them
        SortingHelper.UpdateLoadOrdersViewModel(this, groupSet);
    }
}
