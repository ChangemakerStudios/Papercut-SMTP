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
    using System.Threading;

    public class SafeReadWriteProvider<T>
        where T : class
    {
        readonly Func<T> _create;

        readonly ReaderWriterLockSlim _slimLock = new ReaderWriterLockSlim();

        T _instance;

        public SafeReadWriteProvider(Func<T> create)
        {
            _create = create;
        }

        public bool Created
        {
            get
            {
                _slimLock.EnterReadLock();
                try
                {
                    var readValue = _instance;
                    return (readValue != null);
                }
                finally
                {
                    _slimLock.ExitReadLock();
                }
            }
        }

        public T Instance
        {
            get
            {
                T returnInstance = null;

                _slimLock.EnterUpgradeableReadLock();
                try
                {
                    returnInstance = _instance;
                    if (returnInstance == null)
                    {
                        returnInstance = _create();
                        // call this setter...
                        Instance = returnInstance;
                    }
                }
                finally
                {
                    _slimLock.ExitUpgradeableReadLock();
                }

                return returnInstance;
            }
            set
            {
                _slimLock.EnterWriteLock();

                try
                {
                    _instance = value;
                }
                finally
                {
                    _slimLock.ExitWriteLock();
                }
            }
        }
    }
}