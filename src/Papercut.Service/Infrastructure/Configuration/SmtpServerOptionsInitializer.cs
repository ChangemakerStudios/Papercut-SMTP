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


using Microsoft.Extensions.Hosting;
using Papercut.Core.Domain.Settings;

namespace Papercut.Service.Infrastructure.Configuration;

/// <summary>
/// Initializes SmtpServerOptions by merging appsettings.json defaults with persisted settings from legacy Settings.json.
/// This allows appsettings.json to provide defaults (good for Docker) while Settings.json provides overrides (good for UI changes).
/// Configuration precedence: Settings.json > appsettings.{Environment}.json > appsettings.json > code defaults.
/// </summary>
public class SmtpServerOptionsInitializer : IHostedService
{
    private readonly SmtpServerOptions _smtpServerOptions;
    private readonly ISettingStore _settingStore;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmtpServerOptionsInitializer"/> class.
    /// </summary>
    /// <param name="smtpServerOptions">The SMTP server options to initialize from appsettings.json.</param>
    /// <param name="settingStore">The setting store for persisted user settings.</param>
    /// <param name="logger">The logger instance.</param>
    public SmtpServerOptionsInitializer(
        SmtpServerOptions smtpServerOptions,
        ISettingStore settingStore,
        ILogger logger)
    {
        _smtpServerOptions = smtpServerOptions;
        _settingStore = settingStore;
        _logger = logger;
    }

    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// Loads persisted settings from Settings.json and merges them with appsettings.json defaults.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Load persisted settings from Settings.json if they exist
            // These override the defaults from appsettings.json
            // GetOrSet will use the appsettings.json value as default if not already persisted

            var persistedIp = _settingStore.GetOrSet("IP", _smtpServerOptions.IP, "SMTP Server listening IP. 'Any' is the default and it means '0.0.0.0'.");
            var persistedPort = _settingStore.GetOrSet("Port", _smtpServerOptions.Port.ToString(), "SMTP Server listening Port. Default is 25.");
            var persistedMessagePath = _settingStore.GetOrSet("MessagePath", _smtpServerOptions.MessagePath, "Base path where incoming emails are written.");
            var persistedLoggingPath = _settingStore.GetOrSet("LoggingPath", _smtpServerOptions.LoggingPath, "Base path where logs are written.");

            // TLS/Certificate settings
            var persistedCertFindType = _settingStore.GetOrSet("CertificateFindType", _smtpServerOptions.CertificateFindType, "Certificate search method (FindBySubjectName, FindByThumbprint, etc.).");
            var persistedCertFindValue = _settingStore.GetOrSet("CertificateFindValue", _smtpServerOptions.CertificateFindValue, "Certificate identifier (e.g., 'localhost' or thumbprint). Leave empty to disable TLS.");
            var persistedCertStoreLocation = _settingStore.GetOrSet("CertificateStoreLocation", _smtpServerOptions.CertificateStoreLocation, "Certificate store location (LocalMachine or CurrentUser).");
            var persistedCertStoreName = _settingStore.GetOrSet("CertificateStoreName", _smtpServerOptions.CertificateStoreName, "Certificate store name (My, Root, etc.).");

            // Save settings file to ensure defaults are persisted
            _settingStore.Save();

            // Apply persisted settings
            if (!string.IsNullOrWhiteSpace(persistedIp))
            {
                _smtpServerOptions.IP = persistedIp;
            }

            if (int.TryParse(persistedPort, out var port) && port > 0)
            {
                _smtpServerOptions.Port = port;
            }

            if (!string.IsNullOrWhiteSpace(persistedMessagePath))
            {
                _smtpServerOptions.MessagePath = persistedMessagePath;
            }

            if (!string.IsNullOrWhiteSpace(persistedLoggingPath))
            {
                _smtpServerOptions.LoggingPath = persistedLoggingPath;
            }

            if (!string.IsNullOrWhiteSpace(persistedCertFindType))
            {
                _smtpServerOptions.CertificateFindType = persistedCertFindType;
            }

            // CertificateFindValue can be empty (TLS disabled), so always apply it
            _smtpServerOptions.CertificateFindValue = persistedCertFindValue ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(persistedCertStoreLocation))
            {
                _smtpServerOptions.CertificateStoreLocation = persistedCertStoreLocation;
            }

            if (!string.IsNullOrWhiteSpace(persistedCertStoreName))
            {
                _smtpServerOptions.CertificateStoreName = persistedCertStoreName;
            }

            var tlsStatus = string.IsNullOrWhiteSpace(_smtpServerOptions.CertificateFindValue)
                ? "Disabled"
                : $"Enabled (Cert: {_smtpServerOptions.CertificateFindValue})";

            _logger.Information(
                "SMTP Server Configuration Initialized: IP={IP}, Port={Port}, TLS={TlsStatus}",
                _smtpServerOptions.IP,
                _smtpServerOptions.Port,
                tlsStatus);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to initialize SMTP server options from persisted settings");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
