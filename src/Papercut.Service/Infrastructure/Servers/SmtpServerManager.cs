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


using System.Security.Cryptography.X509Certificates;

using Papercut.Common.Helper;
using Papercut.Core.Domain.Network;
using Papercut.Core.Domain.Network.Smtp;

namespace Papercut.Service.Infrastructure.Servers;

public class SmtpServerManager(
    PapercutSmtpServer smtpServer,
    SmtpServerOptions smtpServerOptions,
    ISettingStore settingStore,
    ILogger logger)
    : IEventHandler<SmtpServerBindEvent>, IEventHandler<PapercutServiceReadyEvent>
{
    public async Task HandleAsync(PapercutServiceReadyEvent @event, CancellationToken token = default)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500), token);
        await BindSMTPServer();
    }

    public async Task HandleAsync(SmtpServerBindEvent @event, CancellationToken token = default)
    {
        logger.Information(
            "Received New Smtp Server Binding Settings from UI {@Event}",
            @event);

        // update settings...
        if (@event.IP.IsSet() && @event.Port.HasValue)
        {
            smtpServerOptions.IP = @event.IP;
            smtpServerOptions.Port = @event.Port.Value;
        }

        // persist the settings to Settings.json so they survive restarts
        try
        {
            settingStore.Set("IP", @event.IP);
            settingStore.Set("Port", @event.Port.ToString());
            settingStore.Save();

            logger.Information(
                "Persisted SMTP Server settings: IP={IP}, Port={Port}",
                @event.IP,
                @event.Port);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to persist SMTP server settings");
        }

        // rebind the server...
        await BindSMTPServer();
    }

    private async Task BindSMTPServer()
    {
        try
        {
            await smtpServer.StopAsync();

            EndpointDefinition endpoint;

            // Check if TLS/STARTTLS should be enabled via certificate configuration
            if (!string.IsNullOrWhiteSpace(smtpServerOptions.CertificateFindValue))
            {
                logger.Information(
                    "Configuring SMTP server with TLS certificate: {FindType}={FindValue} from {StoreLocation}\\{StoreName}",
                    smtpServerOptions.CertificateFindType,
                    smtpServerOptions.CertificateFindValue,
                    smtpServerOptions.CertificateStoreLocation,
                    smtpServerOptions.CertificateStoreName);

                try
                {
                    // Parse certificate configuration with safe TryParse
                    if (!Enum.TryParse<X509FindType>(smtpServerOptions.CertificateFindType, ignoreCase: true, out var findType))
                    {
                        logger.Warning(
                            "Invalid CertificateFindType '{FindType}'. Falling back to plain SMTP without TLS.",
                            smtpServerOptions.CertificateFindType);
                        endpoint = new EndpointDefinition(smtpServerOptions.IP, smtpServerOptions.Port);
                    }
                    else if (!Enum.TryParse<StoreLocation>(smtpServerOptions.CertificateStoreLocation, ignoreCase: true, out var storeLocation))
                    {
                        logger.Warning(
                            "Invalid CertificateStoreLocation '{StoreLocation}'. Falling back to plain SMTP without TLS.",
                            smtpServerOptions.CertificateStoreLocation);
                        endpoint = new EndpointDefinition(smtpServerOptions.IP, smtpServerOptions.Port);
                    }
                    else if (!Enum.TryParse<StoreName>(smtpServerOptions.CertificateStoreName, ignoreCase: true, out var storeName))
                    {
                        logger.Warning(
                            "Invalid CertificateStoreName '{StoreName}'. Falling back to plain SMTP without TLS.",
                            smtpServerOptions.CertificateStoreName);
                        endpoint = new EndpointDefinition(smtpServerOptions.IP, smtpServerOptions.Port);
                    }
                    else
                    {
                        // Attempt to load certificate - wrapped in try/catch for defensive handling
                        try
                        {
                            endpoint = new EndpointDefinition(
                                smtpServerOptions.IP,
                                smtpServerOptions.Port,
                                findType,
                                smtpServerOptions.CertificateFindValue,
                                storeLocation,
                                storeName);

                            logger.Information("TLS/STARTTLS support enabled for SMTP server");
                        }
                        catch (Exception certEx)
                        {
                            logger.Warning(
                                certEx,
                                "Failed to load TLS certificate ({FindType}={FindValue} from {StoreLocation}\\{StoreName}). Falling back to plain SMTP without TLS.",
                                findType,
                                smtpServerOptions.CertificateFindValue,
                                storeLocation,
                                storeName);
                            endpoint = new EndpointDefinition(smtpServerOptions.IP, smtpServerOptions.Port);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Warning(
                        ex,
                        "Unexpected error during TLS configuration. Falling back to plain SMTP without TLS.");
                    endpoint = new EndpointDefinition(smtpServerOptions.IP, smtpServerOptions.Port);
                }
            }
            else
            {
                // No certificate configured - plain SMTP without TLS
                endpoint = new EndpointDefinition(smtpServerOptions.IP, smtpServerOptions.Port);
                logger.Information("SMTP server configured without TLS (plain text mode)");
            }

            await smtpServer.StartAsync(endpoint);
        }
        catch (Exception ex)
        {
            logger.Warning(
                ex,
                "Unable to Create SMTP Server Listener on {IP}:{Port}. After 5 Retries. Failing",
                smtpServerOptions.IP,
                smtpServerOptions.Port);
        }
    }

    #region Begin Static Container Registrations

    private static void Register(ContainerBuilder builder)
    {
        builder.RegisterType<SmtpServerManager>().AsImplementedInterfaces().AsSelf()
            .InstancePerLifetimeScope();
    }

    #endregion
}