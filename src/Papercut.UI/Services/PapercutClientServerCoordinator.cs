// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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
    using System.Threading.Tasks;

    using Papercut.Common.Domain;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Infrastructure.IPComm.Network;

    using Serilog;

    public class PapercutClientServerCoordinator : IEventHandler<PapercutClientReadyEvent>,
        IEventHandler<PapercutClientExitEvent>
    {
        readonly ILogger _logger;

        private readonly PapercutIPCommEndpoints _papercutIpCommEndpoints;

        readonly PapercutIPCommServer _papercutIpCommServer;

        public PapercutClientServerCoordinator(
            PapercutIPCommEndpoints papercutIpCommEndpoints,
            PapercutIPCommServer ipCommServer,
            ILogger logger)
        {
            this._papercutIpCommEndpoints = papercutIpCommEndpoints;
            this._logger = logger;
            this._papercutIpCommServer = ipCommServer;
        }

        public async Task HandleAsync(PapercutClientExitEvent @event)
        {
            await this._papercutIpCommServer.StopAsync();
        }

        public async Task HandleAsync(PapercutClientReadyEvent @event)
        {
            await this._papercutIpCommServer.StopAsync();

            try
            {
                await this._papercutIpCommServer.StartAsync(this._papercutIpCommEndpoints.UI);
            }
            catch (Exception ex)
            {
                this._logger.Warning(
                    ex,
                    "Papercut IPComm Server failed to bind to the {Address} {Port} specified. The port may already be in use by another process.",
                    this._papercutIpCommServer.ListenIpAddress,
                    this._papercutIpCommServer.ListenPort);
            }
        }
    }
}