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
using Papercut.Service.Infrastructure.Configuration;

namespace Papercut.Service.Infrastructure.Servers;

public class SmtpServerManager : IEventHandler<SmtpServerBindEvent>, IEventHandler<PapercutServiceReadyEvent>
{
    private readonly PapercutSmtpServer _smtpServer;

    private readonly SmtpServerOptionsMerger _settingsMerger;

    private SmtpServerOptions _smtpServerOptions;

    private readonly IPAllowedList _ipAllowedList;

    private readonly ISettingStore _settingStore;

    private readonly ILogger _logger;

    public SmtpServerManager(PapercutSmtpServer smtpServer,
        SmtpServerOptionsMerger settingsMerger,
        SmtpServerOptions smtpServerOptions,
        IPAllowedList ipAllowedList,
        ISettingStore settingStore,
        ILogger logger)
    {
        _smtpServer = smtpServer;
        _settingsMerger = settingsMerger;
        _smtpServerOptions = smtpServerOptions;
        _ipAllowedList = ipAllowedList;
        _settingStore = settingStore;
        _logger = logger;
    }

    public async Task HandleAsync(PapercutServiceReadyEvent @event, CancellationToken token = default)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500), token);
        await BindSMTPServer();
    }

    public async Task HandleAsync(SmtpServerBindEvent @event, CancellationToken token = default)
    {
        _logger.Information(
            "Received New Smtp Server Binding Settings from UI {@Event}",
            @event);

        // update settings...
        if (@event.IP.IsSet() && @event.Port.HasValue)
        {
            _smtpServerOptions.IP = @event.IP;
            _smtpServerOptions.Port = @event.Port.Value;
        }

        // persist the settings to Settings.json so they survive restarts
        try
        {
            var persistedFields = new List<string>();

            if (@event.IP.IsSet())
            {
                _settingStore.Set("IP", @event.IP);
                _smtpServerOptions.IP = @event.IP;
                persistedFields.Add($"IP={@event.IP}");
            }

            if (@event.Port.HasValue)
            {
                _settingStore.Set("Port", @event.Port.ToString());
                _smtpServerOptions.Port = @event.Port.Value;
                persistedFields.Add($"Port={@event.Port}");
            }

            if (persistedFields.Count > 0)
            {
                _settingStore.Save();
                _logger.Information("Persisted SMTP Server settings: {Settings}", string.Join(", ", persistedFields));
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to persist SMTP server settings");
        }

        // rebind the server...
        await BindSMTPServer();
    }

    private async Task BindSMTPServer()
    {
        try
        {
            await _smtpServer.StopAsync();

            var endpoint = GetSmtpEndpoint();

            var tlsStatus = string.IsNullOrWhiteSpace(_smtpServerOptions.CertificateFindValue)
                ? "Disabled"
                : $"Enabled (Cert: {_smtpServerOptions.CertificateFindValue})";

            _logger.Information(
                "SMTP Server Configuration: Address={Address}, TLS={TlsStatus}, Allow={AllowList}",
                endpoint.Address,
                tlsStatus,
                _ipAllowedList);

            await _smtpServer.StartAsync(endpoint);
        }
        catch (Exception ex)
        {
            _logger.Warning(
                ex,
                "Unable to Create SMTP Server Listener on {IP}:{Port}. After 5 Retries. Failing",
                _smtpServerOptions.IP,
                _smtpServerOptions.Port);
        }
    }

    private EndpointDefinition GetSmtpEndpoint()
    {
        // Check if TLS/STARTTLS should be enabled via certificate configuration
        if (string.IsNullOrWhiteSpace(_smtpServerOptions.CertificateFindValue))
        {
            _logger.Debug("SMTP server configured without TLS (plain text mode)");
        }
        else
        {
            _logger.Debug(
                "Configuring SMTP server with TLS certificate: {FindType}={FindValue} from {StoreLocation}\\{StoreName}",
                _smtpServerOptions.CertificateFindType,
                _smtpServerOptions.CertificateFindValue,
                _smtpServerOptions.CertificateStoreLocation,
                _smtpServerOptions.CertificateStoreName);

            try
            {
                // Parse certificate configuration with safe TryParse
                if (!Enum.TryParse<X509FindType>(_smtpServerOptions.CertificateFindType, ignoreCase: true, out var findType))
                {
                    _logger.Warning(
                        "Invalid CertificateFindType '{FindType}'. Falling back to plain SMTP without TLS.",
                        _smtpServerOptions.CertificateFindType);
                }
                else if (!Enum.TryParse<StoreLocation>(_smtpServerOptions.CertificateStoreLocation, ignoreCase: true, out var storeLocation))
                {
                    _logger.Warning(
                        "Invalid CertificateStoreLocation '{StoreLocation}'. Falling back to plain SMTP without TLS.",
                        _smtpServerOptions.CertificateStoreLocation);
                }
                else if (!Enum.TryParse<StoreName>(_smtpServerOptions.CertificateStoreName, ignoreCase: true, out var storeName))
                {
                    _logger.Warning(
                        "Invalid CertificateStoreName '{StoreName}'. Falling back to plain SMTP without TLS.",
                        _smtpServerOptions.CertificateStoreName);
                }
                else
                {
                    // Attempt to load certificate - wrapped in try/catch for defensive handling
                    try
                    {
                        var endpoint = new EndpointDefinition(
                            _smtpServerOptions.IP,
                            _smtpServerOptions.Port,
                            findType,
                            _smtpServerOptions.CertificateFindValue,
                            storeLocation,
                            storeName);

                        _logger.Debug("TLS/STARTTLS support enabled for SMTP server");

                        return endpoint;
                    }
                    catch (Exception certEx)
                    {
                        _logger.Warning(
                            certEx,
                            "Failed to load TLS certificate ({FindType}={FindValue} from {StoreLocation}\\{StoreName}). Falling back to plain SMTP without TLS.",
                            findType,
                            _smtpServerOptions.CertificateFindValue,
                            storeLocation,
                            storeName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Unexpected error during TLS configuration. Falling back to plain SMTP without TLS.");
            }
        }

        return new EndpointDefinition(_smtpServerOptions.IP, _smtpServerOptions.Port);
    }

    #region Begin Static Container Registrations

    private static void Register(ContainerBuilder builder)
    {
        builder.RegisterType<SmtpServerManager>().AsImplementedInterfaces().AsSelf()
            .InstancePerLifetimeScope();
    }

    #endregion
}