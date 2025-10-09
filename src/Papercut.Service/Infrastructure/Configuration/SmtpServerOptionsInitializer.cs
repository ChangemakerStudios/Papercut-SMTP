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
/// Initializes SmtpServerOptions by merging appsettings.json defaults with persisted settings from legacy Settings.json
/// This allows appsettings.json to provide defaults (good for Docker) while Settings.json provides overrides (good for UI changes)
/// </summary>
public class SmtpServerOptionsInitializer : IHostedService
{
    private readonly SmtpServerOptions _smtpServerOptions;
    private readonly ISettingStore _settingStore;
    private readonly ILogger _logger;

    public SmtpServerOptionsInitializer(
        SmtpServerOptions smtpServerOptions,
        ISettingStore settingStore,
        ILogger logger)
    {
        _smtpServerOptions = smtpServerOptions;
        _settingStore = settingStore;
        _logger = logger;
    }

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

            _logger.Information(
                "SMTP Server Configuration Initialized: IP={IP}, Port={Port}",
                _smtpServerOptions.IP,
                _smtpServerOptions.Port);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to initialize SMTP server options from persisted settings");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
