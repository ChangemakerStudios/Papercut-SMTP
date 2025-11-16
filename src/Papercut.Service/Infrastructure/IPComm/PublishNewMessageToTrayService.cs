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


using Papercut.Core.Domain.Message;
using Papercut.Infrastructure.IPComm;

namespace Papercut.Service.Infrastructure.IPComm;

/// <summary>
/// Forwards NewMessageEvent to connected tray notification clients via IPComm
/// </summary>
public class PublishNewMessageToTrayService(
    PapercutIPCommClientFactory ipCommClientFactory,
    ILogger logger) : IEventHandler<NewMessageEvent>
{
    public async Task HandleAsync(NewMessageEvent @event, CancellationToken token = default)
    {
        try
        {
            var trayClient = ipCommClientFactory.GetClient(PapercutIPCommClientConnectTo.TrayService);

            // Create a simplified event for IPComm transmission
            var trayEvent = new NewMessageEvent(@event.NewMessage);

            // Fire and forget - don't wait for tray response
            _ = Task.Run(async () =>
            {
                try
                {
                    await trayClient.PublishEventServer(trayEvent, TimeSpan.FromSeconds(1), token);
                    logger.Debug("Published new message notification to tray service");
                }
                catch (Exception ex)
                {
                    // Tray might not be running - this is expected, so just debug log
                    logger.Debug(ex, "Failed to publish new message to tray service (tray may not be running)");
                }
            }, token);
        }
        catch (Exception ex)
        {
            logger.Debug(ex, "Failed to get tray client for new message notification");
        }

        await Task.CompletedTask;
    }

    #region Begin Static Container Registrations

    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        builder.RegisterType<PublishNewMessageToTrayService>().AsImplementedInterfaces().AsSelf()
            .InstancePerLifetimeScope();
    }

    #endregion
}
