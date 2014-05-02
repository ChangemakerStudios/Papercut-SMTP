/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

namespace Papercut.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public static class CollectionExtensions
    {
        /// <summary>
        ///     The add range.
        /// </summary>
        /// <param name="destinationCollection">
        ///     The destination collection.
        /// </param>
        /// <param name="sourceCollection">
        ///     The source collection.
        /// </param>
        /// <typeparam name="TValue">
        /// </typeparam>
        public static void AddRange<TValue>(
            this ICollection<TValue> destinationCollection,
            IEnumerable<TValue> sourceCollection)
        {
            if (destinationCollection == null) throw new ArgumentNullException("destinationCollection");

            if (sourceCollection == null) throw new ArgumentNullException("sourceCollection");

            foreach (var item in sourceCollection.ToList()) destinationCollection.Add(item);
        }

        /// <summary>
        ///     The add range.
        /// </summary>
        /// <param name="destinationList">
        ///     The destination list.
        /// </param>
        /// <param name="sourceList">
        ///     The source list.
        /// </param>
        public static void AddRange(this IList destinationList, IEnumerable sourceList)
        {
            if (destinationList == null) throw new ArgumentNullException("destinationList");

            if (sourceList == null) throw new ArgumentNullException("sourceList");

            foreach (var item in sourceList.Cast<object>().ToList()) destinationList.Add(item);
        }

        /// <summary>
        ///     The for each.
        /// </summary>
        /// <param name="source">
        ///     The source.
        /// </param>
        /// <param name="act">
        ///     The act.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// </returns>
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> act)
        {
            if (source == null) throw new ArgumentNullException("source");

            foreach (T element in source.ToList()) act(element);

            return source;
        }
    }
}