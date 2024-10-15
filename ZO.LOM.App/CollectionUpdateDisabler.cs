using System.Collections.ObjectModel;
using System.Collections.Specialized;
using ZO.LoadOrderManager;

public class CollectionUpdateDisabler<T> : IDisposable
{
    private readonly ObservableCollection<T> _collection;
    private readonly NotifyCollectionChangedEventHandler _eventHandler;

    public CollectionUpdateDisabler(ObservableCollection<T> collection)
    {
        _collection = collection;
        _eventHandler = (NotifyCollectionChangedEventHandler)Delegate.CreateDelegate(
            typeof(NotifyCollectionChangedEventHandler), _collection, "CollectionChanged");

        // Disable notifications
        _collection.CollectionChanged -= _eventHandler;
    }

    public void Dispose()
    {
        // Re-enable notifications
        _collection.CollectionChanged += _eventHandler;
    }
}


public class UniversalCollectionDisabler : IDisposable
{
    private readonly List<IDisposable> _disablers = new List<IDisposable>();

    public UniversalCollectionDisabler(AggLoadInfo aggLoadInfo)
    {
        // Suppress notifications for each collection in AggLoadInfo
        _disablers.Add(new CollectionUpdateDisabler<Plugin>(aggLoadInfo.Plugins));
        _disablers.Add(new CollectionUpdateDisabler<ModGroup>(aggLoadInfo.Groups));
        _disablers.Add(new CollectionUpdateDisabler<LoadOut>(aggLoadInfo.LoadOuts));
        _disablers.Add(new CollectionUpdateDisabler<GroupSet>(AggLoadInfo.GroupSets));
        _disablers.Add(new CollectionUpdateDisabler<(long groupID, long groupSetID, long? parentID, long Ordinal)>(aggLoadInfo.GroupSetGroups.Items));
        _disablers.Add(new CollectionUpdateDisabler<(long groupSetID, long groupID, long pluginID, long Ordinal)>(aggLoadInfo.GroupSetPlugins.Items));

        // Handle ObservableHashSet separately since it's not an ObservableCollection
        if (aggLoadInfo.ProfilePlugins.Items is ObservableHashSet<(long ProfileID, long PluginID)> hashSet)
        {
            _disablers.Add(new ObservableHashSetDisabler<(long ProfileID, long PluginID)>(hashSet));
        }
    }

    public void Dispose()
    {
        // Dispose each CollectionUpdateDisabler or custom disabler
        foreach (var disabler in _disablers)
        {
            disabler.Dispose();
        }
    }
}

public class ObservableHashSetDisabler<T> : IDisposable
{
    private readonly ObservableHashSet<T> _hashSet;
    private readonly NotifyCollectionChangedEventHandler _eventHandler;

    public ObservableHashSetDisabler(ObservableHashSet<T> hashSet)
    {
        _hashSet = hashSet;
        _eventHandler = (NotifyCollectionChangedEventHandler)Delegate.CreateDelegate(
            typeof(NotifyCollectionChangedEventHandler), _hashSet, "CollectionChanged");

        // Disable notifications
        _hashSet.CollectionChanged -= _eventHandler;
    }

    public void Dispose()
    {
        // Re-enable notifications
        _hashSet.CollectionChanged += _eventHandler;
    }
}