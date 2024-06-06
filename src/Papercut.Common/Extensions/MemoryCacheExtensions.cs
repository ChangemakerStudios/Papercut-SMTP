﻿// Papercut
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


using System.Runtime.Caching;

namespace Papercut.Common.Extensions
{
    public static class MemoryCacheExtensions
    {
        #region Constants

        /// <summary>
        ///     The lock object count.
        /// </summary>
        const int LockObjectCount = 0xFF;

        #endregion

        #region Static Fields

        /// <summary>
        ///     The lock cache items.
        /// </summary>
        static readonly SemaphoreSlim[] _lockCacheItems;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="MemoryCacheExtensions" /> class.
        /// </summary>
        static MemoryCacheExtensions()
        {
            _lockCacheItems = new SemaphoreSlim[LockObjectCount + 1];
        }

        #endregion

        #region Public Methods and Operators

        public static async Task<T> GetOrSetAsync<T>(
            this MemoryCache cache,
            string key,
            Func<Task<T>> getValue,
            Action<T> addToCacheFunction)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (getValue == null) throw new ArgumentNullException(nameof(getValue));
            if (addToCacheFunction == null) throw new ArgumentNullException(nameof(addToCacheFunction));

            var cachedItem = (T)cache[key];

            if (!cachedItem.IsDefault()) return cachedItem;

            var lockObject = GetLockObject(key);

            try
            {
                await lockObject.WaitAsync().ConfigureAwait(false);

                cachedItem = (T)cache[key];

                if (!cachedItem.IsDefault()) return cachedItem;

                // materialize the query
                cachedItem = await getValue();

                if (!cachedItem.IsDefault())
                {
                    // add to cache...
                    addToCacheFunction(cachedItem);
                }
            }
            finally
            {
                lockObject.Release();
            }

            return cachedItem;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     The get.
        /// </summary>
        /// <param name="originalKey">
        ///     The key.
        /// </param>
        /// <returns>
        ///     The get.
        /// </returns>
        static SemaphoreSlim GetLockObject(string originalKey)
        {
            if (originalKey == null) throw new ArgumentNullException(nameof(originalKey));

            int keyHash = originalKey.GetHashCode();

            // make positive if negative...
            if (keyHash < 0) keyHash = -keyHash;

            // get the lock item id (value between 0 and objectCount)
            int lockItemId = keyHash % LockObjectCount;

            // init the lock object if it hasn't been created yet...
            return _lockCacheItems[lockItemId] ?? (_lockCacheItems[lockItemId] = new SemaphoreSlim(1));
        }

        #endregion
    }
}