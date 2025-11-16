// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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


using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

using Papercut.Core.Infrastructure.Network;

namespace Papercut.Service.Infrastructure.IPComm;

public class ReplyWithWebSettingsService(IServer server, ILogger logger)
    : IEventHandler<ServiceWebUISettingsExchangeEvent>
{
    private const string Localhost = "localhost";

    public Task HandleAsync(ServiceWebUISettingsExchangeEvent @event, CancellationToken token = default)
    {
        try
        {
            // Get the server addresses feature from Kestrel
            var addressesFeature = server.Features.Get<IServerAddressesFeature>();

            if (addressesFeature?.Addresses != null && addressesFeature.Addresses.Any())
            {
                // Get the first address (typically the primary binding)
                var firstAddress = addressesFeature.Addresses.First();
                var uri = new Uri(firstAddress);

                @event.IP = uri.Host is "+" or "*" or "[::]" ? Localhost : uri.Host;
                @event.Port = uri.Port;

                logger.Debug(
                    "Replying to ServiceWebUISettingsExchangeEvent with IP: {IP}, Port: {Port}",
                    @event.IP,
                    @event.Port);
            }
            else
            {
                logger.Warning("No server addresses found, using fallback");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to get web server addresses, using fallback");
        }

        @event.IP = Localhost;
        @event.Port = 8080;

        return Task.CompletedTask;
    }

    #region Begin Static Container Registrations

    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        builder.RegisterType<ReplyWithWebSettingsService>().AsImplementedInterfaces().AsSelf()
            .InstancePerLifetimeScope();
    }

    #endregion
}
