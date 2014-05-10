// /*  
//  * Papercut
//  *
//  *  Copyright © 2008 - 2012 Ken Robertson
//  *  Copyright © 2013 - 2014 Jaben Cargman
//  *  
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *  
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *  
//  *  Unless required by applicable law or agreed to in writing, software
//  *  distributed under the License is distributed on an "AS IS" BASIS,
//  *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *  See the License for the specific language governing permissions and
//  *  limitations under the License.
//  *  
//  */


namespace Papercut.Services
{
    using System;
    using System.Diagnostics;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.Threading;

    using NamedPipeWrapper;

    using Papercut.Core.Events;

    public class SingleInstanceService : IDisposable, IHandleEvent<AppReadyEvent>
    {
        public class ProcessMessage
        {
            
        }

        const string GlobalPapercutAppName = "Papercut.App";

        Mutex _appMutex = new Mutex(false, GlobalPapercutAppName);

        NamedPipeServer<ProcessMessage> _namedPipeServer =
            new NamedPipeServer<ProcessMessage>(GlobalPapercutAppName);

        NamedPipeClient<ProcessMessage> _namedPipeClient =
    new NamedPipeClient<ProcessMessage>(GlobalPapercutAppName);

        public SingleInstanceService()
        {
            _namedPipeServer.ClientMessage += HandleClientMessage;
        }

        void HandleClientMessage(NamedPipeConnection<ProcessMessage, ProcessMessage> connection, ProcessMessage message)
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

            try
            {
                _namedPipeServer.Stop();
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
            else
            {
                // run the server...
                _namedPipeServer.Start();
            }
        }
    }
}