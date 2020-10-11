using System;
using System.Collections.Concurrent;

namespace CUE4Parse.Utils
{
    public static class DictUtils
    {
        public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary,
            TKey key, Func<TValue> valueCreator)
        {
            if (!dictionary.TryGetValue(key, out var value))
            {
                value = valueCreator();
                dictionary.TryAdd(key, value);
            }
            return value;
        }

        public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary,
            TKey key) where TValue : new()
        {
            return dictionary.GetOrAdd(key, () => new TValue());
        }
    }
}