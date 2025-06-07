
using System.Runtime.Caching;

namespace Papercut.Common.Extensions;

public static class MemoryCacheExtensions
{
    const int LockObjectCount = 0xFF;

    static readonly SemaphoreSlim?[] LockCacheItems;

    static MemoryCacheExtensions()
    {
        LockCacheItems = new SemaphoreSlim[LockObjectCount + 1];
    }

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

            cachedItem = await getValue();

            if (!cachedItem.IsDefault())
            {
                addToCacheFunction(cachedItem);
            }
        }
        finally
        {
            lockObject.Release();
        }

        return cachedItem;
    }

    #region Methods

    static SemaphoreSlim GetLockObject(string originalKey)
    {
        if (originalKey == null) throw new ArgumentNullException(nameof(originalKey));

        int keyHash = originalKey.GetHashCode();

        if (keyHash < 0) keyHash = -keyHash;

        int lockItemId = keyHash % LockObjectCount;

        return LockCacheItems[lockItemId] ?? (LockCacheItems[lockItemId] = new SemaphoreSlim(1));
    }

    #endregion
}