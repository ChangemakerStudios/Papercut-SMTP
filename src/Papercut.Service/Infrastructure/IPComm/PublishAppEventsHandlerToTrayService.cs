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

namespace Papercut.Service.Infrastructure.IPComm;

/// <summary>
/// Forwards NewMessageEvent to connected tray notification clients via IPComm
/// </summary>
public class PublishAppEventsHandlerToTrayService(
    PapercutIPCommClientFactory ipCommClientFactory,
    ILogger logger) : PublishAppEventBase(ipCommClientFactory, logger), IEventHandler<NewMessageEvent>, IEventHandler<PapercutServiceReadyEvent>
{
    protected override PapercutIPCommClientConnectTo ConnectTo => PapercutIPCommClientConnectTo.ServiceTray;

    public Task HandleAsync(NewMessageEvent @event, CancellationToken token = default)
    {
        return PublishAsync(@event, token);
    }

    public Task HandleAsync(PapercutServiceReadyEvent @event, CancellationToken token = default)
    {
        return PublishAsync(@event, token);
    }

    #region Begin Static Container Registrations

    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        builder.RegisterType<PublishAppEventsHandlerToTrayService>().AsImplementedInterfaces().AsSelf()
            .InstancePerLifetimeScope();
    }

    #endregion
}
