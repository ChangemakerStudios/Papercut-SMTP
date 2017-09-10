// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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

namespace Papercut.Services
{
    using System;
    using System.Threading;

    using Papercut.Common.Domain;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Events;
    using Papercut.Network;

    using Serilog;

    public class SingleInstanceService : IDisposable, IEventHandler<PapercutClientPreStartEvent>
    {
        readonly Mutex _appMutex = new Mutex(false, App.GlobalName);

        readonly PapercutClient _papercutClient;

        public SingleInstanceService(PapercutClient papercutClient, ILogger logger)
        {
            Logger = logger;
            _papercutClient = papercutClient;
        }

        public ILogger Logger { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _appMutex?.Dispose();
                }
                catch
                {
                }
            }
        }

        public void Handle(PapercutClientPreStartEvent @event)
        {
            // papercut is not already running...
            if (_appMutex.WaitOne(0, false)) return;

            Logger.Debug(
                "Second process run. Shutting this process and pushing show event to other process.");

            // papercut is already running, push event to other process
            _papercutClient.PublishEventServer(new ShowMainWindowEvent());

            // no need to go further
            @event.CancelStart = true;
        }
    }
}