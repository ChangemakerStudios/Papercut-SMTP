// Papercut
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


using Microsoft.Extensions.Hosting;

using Papercut.Infrastructure.IPComm.Network;

namespace Papercut.Service.Infrastructure.Servers
{
    public class PapercutServerHostedService(
        PapercutIPCommServer ipCommServer,
        PapercutSmtpServer smtpServer,
        PapercutIPCommEndpoints papercutIpCommEndpoints,
        IAppMeta applicationMetaData,
        ILogger logger,
        IMessageBus messageBus)
        : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await messageBus.PublishAsync(
                new PapercutServicePreStartEvent { AppMeta = applicationMetaData },
                cancellationToken);

            try
            {
                await ipCommServer.StopAsync();
                await ipCommServer.StartAsync(papercutIpCommEndpoints.Service);
            }
            catch (Exception ex)
            {
                logger.Warning(
                    ex,
                    "Unable to Create Papercut IPComm Server Listener on {IP}:{Port}. After 5 Retries. Failing",
                    ipCommServer.ListenIpAddress,
                    ipCommServer.ListenPort);
            }

            await messageBus.PublishAsync(
                new PapercutServiceReadyEvent { AppMeta = applicationMetaData },
                cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.WhenAll(smtpServer.StopAsync(), ipCommServer.StopAsync()).WaitAsync(cancellationToken);

            await messageBus.PublishAsync(new PapercutServiceExitEvent { AppMeta = applicationMetaData }, cancellationToken);
        }
    }
}