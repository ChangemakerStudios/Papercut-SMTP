// Papercut SMTP
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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

namespace Papercut.Common.Extensions;

public static class CollectionExtensions
{
    public static void AddRange<TValue>(
        this ICollection<TValue> destinationCollection,
        IEnumerable<TValue> sourceCollection)
    {
        if (destinationCollection == null) throw new ArgumentNullException(nameof(destinationCollection));

        if (sourceCollection == null) throw new ArgumentNullException(nameof(sourceCollection));

        foreach (var item in sourceCollection as IList<TValue> ?? sourceCollection.ToList()) destinationCollection.Add(item);
    }

    public static void AddRange(this IList destinationList, IEnumerable sourceList)
    {
        if (destinationList == null) throw new ArgumentNullException(nameof(destinationList));

        if (sourceList == null) throw new ArgumentNullException(nameof(sourceList));

        foreach (var item in sourceList.Cast<object>().ToList()) destinationList.Add(item);
    }

    public static int FindIndex<T>(
        [NotNull] this IEnumerable<T> items,
        [NotNull] Predicate<T> predicate)
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

    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> act)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        foreach (T element in (source as IList<T> ?? source.ToList()))
        {
            act(element);
        }

        return source;
    }
}