// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


using System.Collections;
using System.Collections.ObjectModel;

namespace Papercut.Common.Extensions;

public static class CollectionExtensions
{
    public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs) where TKey : notnull
    {
        if (keyValuePairs == null) throw new ArgumentNullException(nameof(keyValuePairs));

        return keyValuePairs.ToDictionary(s => s.Key, s => s.Value);
    }

    public static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T>? items)
    {
        return new ReadOnlyCollection<T>(items.IfNullEmpty().ToList());
    }

    public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T>? items)
    {
        return items.ToReadOnlyCollection();
    }

    public static int RemoveRange<TValue>(this ICollection<TValue> collection, IEnumerable<TValue> toRemove)
    {
        return toRemove.IfNullEmpty().Count(collection.Remove);
    }

    public static int RemoveRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
    {
        if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
        if (keyValuePairs == null) throw new ArgumentNullException(nameof(keyValuePairs));

        return dictionary.RemoveRange(keyValuePairs.Select(s => s.Key));
    }

    public static int RemoveRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<TKey> keys)
    {
        if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
        if (keys == null) throw new ArgumentNullException(nameof(keys));

        var count = 0;

        foreach (var key in keys.ToList().Where(dictionary.ContainsKey)) {
            dictionary.Remove(key);
            count++;
        }

        return count;
    }

    public static IDictionary<TKey, TValue?> FlattenToDictionary<TKey, TValue>(
        this ILookup<TKey, TValue> lookup,
        Func<IEnumerable<TValue>, TValue?>? flattenFunc = null) where TKey : notnull
    {
        if (lookup == null) throw new ArgumentNullException(nameof(lookup));

        flattenFunc ??= v => v.FirstOrDefault();

        return lookup.ToDictionary(k => k.Key, v => flattenFunc(v));
    }

    public static void AddRange<TValue>(
        this ICollection<TValue> destinationCollection,
        IEnumerable<TValue> sourceCollection)
    {
        if (destinationCollection == null) throw new ArgumentNullException(nameof(destinationCollection));

        if (sourceCollection == null) throw new ArgumentNullException(nameof(sourceCollection));

        foreach (TValue item in sourceCollection as IList<TValue> ?? sourceCollection.ToList())
        {
            destinationCollection.Add(item);
        }
    }

    public static void AddRange(this IList destinationList, IEnumerable sourceList)
    {
        if (destinationList == null) throw new ArgumentNullException(nameof(destinationList));

        if (sourceList == null) throw new ArgumentNullException(nameof(sourceList));

        foreach (object item in sourceList.Cast<object>().ToList())
        {
            destinationList.Add(item);
        }
    }

    public static int FindIndex<T>(
        this IEnumerable<T> items,
        Predicate<T> predicate)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        int index = 0;
        foreach (T item in items)
        {
            if (predicate(item)) return index;
            index++;
        }

        return -1;
    }
}