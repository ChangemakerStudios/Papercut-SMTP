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


using System.Reactive.Linq;

namespace Papercut.Core.Infrastructure.Async
{
    public static class AsyncHelpers
    {
        public static IDisposable SubscribeAsync<TResult>(
            this IObservable<TResult> source,
            Func<TResult,Task> action,
            Action<Exception>? onError = null,
            Action? onCompleted = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));

            return source.Select(x => Observable.FromAsync(async () => await action(x)))
                .Concat()
                .Subscribe(
                    s => { },
                    e =>
                    {
                        onError?.Invoke(e);
                    },
                    () =>
                    {
                        onCompleted?.Invoke();
                    });
        }

        /// <summary>
        /// Avoid the 'classic deadlock problem' when blocking on async work from non-async
        /// code by disabling any synchronization context while the async work takes place
        /// </summary>
        public static T RunAsync<T>(this Task<T> task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            var currentSyncContext = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null);

                return task.Result;
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(currentSyncContext);
            }
        }

        /// <summary>
        /// Avoid the 'classic deadlock problem' when blocking on async work from non-async
        /// code by disabling any synchronization context while the async work takes place
        /// </summary>
        public static void RunAsync(this Task task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            var currentSyncContext = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null);

                task.Wait();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(currentSyncContext);
            }
        }
    }
}