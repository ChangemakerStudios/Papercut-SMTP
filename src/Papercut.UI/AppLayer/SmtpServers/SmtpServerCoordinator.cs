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


namespace Papercut.AppLayer.SmtpServers;

using Papercut.Core.Domain.Network;
using Papercut.Core.Domain.Network.Smtp;
using Papercut.Infrastructure.Smtp;

public class SmtpServerCoordinator(
    PapercutSmtpServer smtpServer,
    ILogger logger,
    IMessageBus messageBus)
    : Disposable,
        IEventHandler<SettingsUpdatedEvent>,
        IEventHandler<PapercutServiceStatusEvent>
{
    public bool IsServerActive => smtpServer.IsActive;

    public async Task HandleAsync(PapercutServiceStatusEvent @event, CancellationToken token = default)
    {
        if (@event.PapercutServiceStatus == PapercutServiceStatusType.Online)
        {
            logger.Information(
                "Papercut Backend Service is running. SMTP disabled in UI.");

            if (IsServerActive) await StopServerAsync();
        }
        else if (@event.PapercutServiceStatus == PapercutServiceStatusType.Offline)
        {
            logger.Information(
                "Papercut Backend Service is not running. SMTP enabled in UI.");

            // give it a half second though
            await Task.Delay(TimeSpan.FromMilliseconds(500), token);

            if (!IsServerActive) await ListenServerAsync();
        }
    }

    public async Task HandleAsync(SettingsUpdatedEvent @event, CancellationToken token)
    {
        if (!IsServerActive) return;

        if (@event.PreviousSettings.IP == @event.NewSettings.IP && @event.PreviousSettings.Port == @event.NewSettings.Port) return;

        await ListenServerAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            if (smtpServer != null)
            {
                await StopServerAsync();
                await smtpServer.DisposeAsync();
            }
        }
    }

    private async Task StopServerAsync()
    {
        await smtpServer.StopAsync();
    }

    private async Task ListenServerAsync()
    {
        try
        {
            await smtpServer.StopAsync();
            await smtpServer.StartAsync(new EndpointDefinition(Properties.Settings.Default.IP, Properties.Settings.Default.Port));
            await messageBus.PublishAsync(new SmtpServerBindEvent(Properties.Settings.Default.IP, Properties.Settings.Default.Port));
        }
        catch (Exception ex)
        {
            logger.Warning(
                ex,
                "Failed to bind SMTP to the {Address} {Port} specified. The port may already be in use by another process.",
                Properties.Settings.Default.IP,
                Properties.Settings.Default.Port);

            await messageBus.PublishAsync(new SmtpServerBindFailedEvent());
        }
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterType<SmtpServerCoordinator>().AsSelf()
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
    }

    #endregion
}