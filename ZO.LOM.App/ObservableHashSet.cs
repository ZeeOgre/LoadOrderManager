using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

public class ObservableHashSet<T> : ISet<T>, INotifyCollectionChanged
{
    private readonly HashSet<T> _hashSet = new HashSet<T>();
    private readonly ObservableCollection<T> _observableCollection = new ObservableCollection<T>();

    public event NotifyCollectionChangedEventHandler CollectionChanged
    {
        add => _observableCollection.CollectionChanged += value;
        remove => _observableCollection.CollectionChanged -= value;
    }

    public bool Add(T item)
    {
        if (_hashSet.Add(item))
        {
            _observableCollection.Add(item);
            return true;
        }
        return false;
    }

    public bool Remove(T item)
    {
        if (_hashSet.Remove(item))
        {
            _observableCollection.Remove(item);
            return true;
        }
        return false;
    }

    public void Clear()
    {
        _hashSet.Clear();
        _observableCollection.Clear();
    }

    public bool Contains(T item) => _hashSet.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => _hashSet.CopyTo(array, arrayIndex);

    public int Count => _hashSet.Count;

    public bool IsReadOnly => false;

    public IEnumerator<T> GetEnumerator() => _hashSet.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _hashSet.GetEnumerator();

    void ICollection<T>.Add(T item) => Add(item);

    public void UnionWith(IEnumerable<T> other) => _hashSet.UnionWith(other);

    public void IntersectWith(IEnumerable<T> other) => _hashSet.IntersectWith(other);

    public void ExceptWith(IEnumerable<T> other) => _hashSet.ExceptWith(other);

    public void SymmetricExceptWith(IEnumerable<T> other) => _hashSet.SymmetricExceptWith(other);

    public bool IsSubsetOf(IEnumerable<T> other) => _hashSet.IsSubsetOf(other);

    public bool IsSupersetOf(IEnumerable<T> other) => _hashSet.IsSupersetOf(other);

    public bool IsProperSupersetOf(IEnumerable<T> other) => _hashSet.IsProperSupersetOf(other);

    public bool IsProperSubsetOf(IEnumerable<T> other) => _hashSet.IsProperSubsetOf(other);

    public bool Overlaps(IEnumerable<T> other) => _hashSet.Overlaps(other);

    public bool SetEquals(IEnumerable<T> other) => _hashSet.SetEquals(other);
}
