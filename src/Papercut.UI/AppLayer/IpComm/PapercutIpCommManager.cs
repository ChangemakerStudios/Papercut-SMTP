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


namespace Papercut.AppLayer.IpComm
{
    using System;
    using System.Threading.Tasks;

    using Autofac;
    using Autofac.Util;

    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Network;
    using Papercut.Core.Domain.Settings;
    using Papercut.Domain.LifecycleHooks;
    using Papercut.Infrastructure.IPComm.Network;

    using Serilog;

    public class PapercutIpCommManager : Disposable, IAppLifecycleStarted
    {
        readonly ILogger _logger;

        private readonly PapercutIPCommEndpoints _papercutIpCommEndpoints;

        readonly PapercutIPCommServer _papercutIpCommServer;
        private readonly ISettingStore _settingStore;

        public PapercutIpCommManager(
            PapercutIPCommEndpoints papercutIpCommEndpoints,
            PapercutIPCommServer ipCommServer,
            ISettingStore settingStore,
            ILogger logger)
        {
            this._papercutIpCommEndpoints = papercutIpCommEndpoints;
            this._logger = logger;
            this._settingStore = settingStore;
            this._papercutIpCommServer = ipCommServer;
        }

        public async Task OnStartedAsync()
        {
            await this._papercutIpCommServer.StopAsync();

            try
            {
                if (_settingStore == null) throw new ArgumentNullException(nameof(_settingStore));

                var serverAddress = _settingStore.GetOrSet("IPCommServerIPAddress", PapercutIPCommConstants.Localhost,
                    $"The IP Comm Server IP address (Defaults to {PapercutIPCommConstants.Localhost}).");

                var serverPort = _settingStore.GetOrSet("IPCommServerPort", PapercutIPCommConstants.UiListeningPort,
                    $"The IP Comm Server listening port (Defaults to {PapercutIPCommConstants.UiListeningPort}).");

                var settings = new EndpointDefinition(serverAddress, serverPort);

                await this._papercutIpCommServer.StartAsync(settings);
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

        protected override async ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                await this._papercutIpCommServer.StopAsync();
            }
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register([NotNull] ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterType<PapercutIpCommManager>().AsImplementedInterfaces()
                .SingleInstance();
        }

        #endregion
    }
}