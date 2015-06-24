// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2015 Jaben Cargman
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

namespace Papercut.Core.Helper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using Papercut.Core.Annotations;

    public static class CollectionExtensions
    {
        public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            [NotNull] this IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            if (keyValuePairs == null) throw new ArgumentNullException("keyValuePairs");

            return keyValuePairs.ToDictionary(s => s.Key, s => s.Value);
        }

        public static IDictionary<TKey, TValue> FlattenToDictionary<TKey, TValue>(
            [NotNull] this ILookup<TKey, TValue> lookup, Func<IEnumerable<TValue>, TValue> flattenFunc = null)
        {
            if (lookup == null) throw new ArgumentNullException("nameValueCollection");

            flattenFunc = flattenFunc ?? (v => v.FirstOrDefault());

            return lookup.ToDictionary(k => k.Key, v => flattenFunc(v));
        }

        public static void AddRange<TValue>(
            this ICollection<TValue> destinationCollection,
            IEnumerable<TValue> sourceCollection)
        {
            if (destinationCollection == null) throw new ArgumentNullException("destinationCollection");

            if (sourceCollection == null) throw new ArgumentNullException("sourceCollection");

            foreach (TValue item in (sourceCollection as IList<TValue> ?? sourceCollection.ToList())
                )
            {
                destinationCollection.Add(item);
            }
        }

        public static void AddRange(this IList destinationList, IEnumerable sourceList)
        {
            if (destinationList == null) throw new ArgumentNullException("destinationList");

            if (sourceList == null) throw new ArgumentNullException("sourceList");

            foreach (object item in sourceList.Cast<object>().ToList())
            {
                destinationList.Add(item);
            }
        }

        public static int FindIndex<T>(
            [NotNull] this IEnumerable<T> items,
            [NotNull] Predicate<T> predicate)
        {
            if (items == null) throw new ArgumentNullException("items");
            if (predicate == null) throw new ArgumentNullException("predicate");

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
            if (source == null) throw new ArgumentNullException("source");

            foreach (T element in (source as IList<T> ?? source.ToList()))
            {
                act(element);
            }

            return source;
        }
    }
}