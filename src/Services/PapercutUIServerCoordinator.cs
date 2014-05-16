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

    using Papercut.Core.Events;
    using Papercut.Core.Network;

    using Serilog;

    public class PapercutUIServerCoordinator : IHandleEvent<AppReadyEvent>,
        IHandleEvent<AppExitEvent>
    {
        readonly ILogger _logger;

        readonly IServer _papercutServer;

        public PapercutUIServerCoordinator(
            Func<ServerProtocolType, IServer> serverFactory,
            ILogger logger)
        {
            _logger = logger;
            _papercutServer = serverFactory(ServerProtocolType.Papercut);
        }

        public void Handle(AppExitEvent @event)
        {
            _papercutServer.Stop();
        }

        public void Handle(AppReadyEvent @event)
        {
            try
            {
                _papercutServer.Listen(PapercutClient.Localhost, PapercutClient.UIPort);
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    ex,
                    "Failed to bind to the {Address} {Port} specified. The port may already be in use by another process.",
                    PapercutClient.Localhost,
                    PapercutClient.UIPort);
            }
        }
    }
}