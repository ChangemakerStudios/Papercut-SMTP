// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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


namespace Papercut.Core.Infrastructure.Async
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Papercut.Core.Annotations;

    public static class AsyncHelpers
    {
        /// <summary>
        /// Avoid the 'classic deadlock problem' when blocking on async work from non-async
        /// code by disabling any synchronization context while the async work takes place
        /// </summary>
        public static T RunAsync<T>([NotNull] this Task<T> task)
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
        public static void RunAsync([NotNull] this Task task)
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