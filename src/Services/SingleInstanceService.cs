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


namespace Papercut.Services
{
    using System;
    using System.Threading;

    using Papercut.Core.Events;

    public class SingleInstanceService : IDisposable, IHandleEvent<AppReadyEvent>
    {
        public class ProcessMessage
        {
            
        }

        const string GlobalPapercutAppName = "Papercut.App";

        Mutex _appMutex = new Mutex(false, GlobalPapercutAppName);

        public SingleInstanceService()
        {
        }

        public void Dispose()
        {
            try
            {
                _appMutex.Close();
                _appMutex.Dispose();
            }
            catch
            {
            }
        }

        public void Handle(AppReadyEvent @event)
        {
            if (!_appMutex.WaitOne(0, false))
            {
                // papercut is already running...

            }
        }
    }
}