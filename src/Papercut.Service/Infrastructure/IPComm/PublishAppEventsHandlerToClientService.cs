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

namespace Papercut.Service.Infrastructure.IPComm;

public class PublishAppEventsHandlerToClientService(
    PapercutIPCommClientFactory ipCommClientFactory,
    ILogger logger) : IEventHandler<PapercutServiceExitEvent>,
    IEventHandler<PapercutServiceReadyEvent>,
    IEventHandler<PapercutServicePreStartEvent>
{
    public Task HandleAsync(PapercutServiceExitEvent @event, CancellationToken token = default)
    {
        return this.PublishAsync(@event);
    }

    public Task HandleAsync(PapercutServicePreStartEvent @event, CancellationToken token = default)
    {
        return this.PublishAsync(@event);
    }

    public Task HandleAsync(PapercutServiceReadyEvent @event, CancellationToken token = default)
    {
        return this.PublishAsync(@event);
    }

    public async Task PublishAsync<T>(T @event)
        where T : IEvent
    {
        var ipCommClient = ipCommClientFactory.GetClient(PapercutIPCommClientConnectTo.UI);

        try
        {
            logger.Information(
                $"Publishing {{@{@event.GetType().Name}}} to the Papercut Client",
                @event);

            await ipCommClient.PublishEventServer(@event, TimeSpan.FromMilliseconds(500));
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.Warning(
                ex,
                "Failed to publish {Endpoint} specified. Papercut UI is most likely not running.",
                ipCommClient.Endpoint);
        }
    }

    #region Begin Static Container Registrations

    static void Register(ContainerBuilder builder)
    {
        builder.RegisterType<PublishAppEventsHandlerToClientService>().AsImplementedInterfaces().AsSelf()
            .InstancePerLifetimeScope();
    }

    #endregion
}