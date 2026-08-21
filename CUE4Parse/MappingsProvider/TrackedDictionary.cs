using System.Collections;

namespace CUE4Parse.MappingsProvider;

/// <summary>
/// Dictionary used by the mutable mapping builder. Every mutation invalidates
/// the derived identifier indexes, including replacement of an existing key.
/// </summary>
public sealed class TrackedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _values;
    private readonly Action _changed;
    private readonly Action<TValue>? _attachValue;

    public TrackedDictionary(IEqualityComparer<TKey>? comparer, Action changed, Action<TValue>? attachValue = null)
    {
        _values = new Dictionary<TKey, TValue>(comparer);
        _changed = changed;
        _attachValue = attachValue;
    }

    public TrackedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> values, IEqualityComparer<TKey>? comparer,
        Action changed, Action<TValue>? attachValue = null) : this(comparer, changed, attachValue)
    {
        foreach (var (key, value) in values)
        {
            _attachValue?.Invoke(value);
            _values.Add(key, value);
        }
    }

    public TValue this[TKey key]
    {
        get => _values[key];
        set
        {
            _attachValue?.Invoke(value);
            _values[key] = value;
            _changed();
        }
    }

    public ICollection<TKey> Keys => _values.Keys;
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _values.Keys;
    public ICollection<TValue> Values => _values.Values;
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _values.Values;
    public int Count => _values.Count;
    public bool IsReadOnly => false;

    public void Add(TKey key, TValue value)
    {
        _attachValue?.Invoke(value);
        _values.Add(key, value);
        _changed();
    }

    public bool TryAdd(TKey key, TValue value)
    {
        if (_values.ContainsKey(key))
            return false;
        Add(key, value);
        return true;
    }

    public bool Remove(TKey key)
    {
        if (!_values.Remove(key))
            return false;
        _changed();
        return true;
    }

    public void Clear()
    {
        if (_values.Count == 0)
            return;
        _values.Clear();
        _changed();
    }

    public bool ContainsKey(TKey key) => _values.ContainsKey(key);
    public bool TryGetValue(TKey key, out TValue value) => _values.TryGetValue(key, out value!);
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public bool Contains(KeyValuePair<TKey, TValue> item) =>
        ((ICollection<KeyValuePair<TKey, TValue>>) _values).Contains(item);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<TKey, TValue>>) _values).CopyTo(array, arrayIndex);

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        if (!((ICollection<KeyValuePair<TKey, TValue>>) _values).Remove(item))
            return false;
        _changed();
        return true;
    }

    public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => _values.GetEnumerator();
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() =>
        _values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();
}
