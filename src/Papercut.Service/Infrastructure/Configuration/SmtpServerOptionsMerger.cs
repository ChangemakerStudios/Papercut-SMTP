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


namespace Papercut.Service.Infrastructure.Configuration;

/// <summary>
/// Initializes SmtpServerOptions by merging appsettings.json defaults with persisted settings from legacy Settings.json.
/// This allows appsettings.json to provide defaults (good for Docker) while Settings.json provides overrides (good for UI changes).
/// Configuration precedence: Settings.json > appsettings.{Environment}.json > appsettings.json > code defaults.
/// </summary>
public class SmtpServerOptionsMerger
{
    private readonly Lazy<ILogger> _logger;

    private readonly Lazy<ISettingStore> _settingStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmtpServerOptionsMerger"/> class.
    /// </summary>
    /// <param name="settingStore">The setting store for persisted user settings.</param>
    /// <param name="logger">The logger instance.</param>
    public SmtpServerOptionsMerger(
        Lazy<ISettingStore> settingStore,
        Lazy<ILogger> logger)
    {
        _settingStore = settingStore;
        _logger = logger;
    }

    protected ISettingStore SettingStore => _settingStore.Value;

    protected ILogger Logger => _logger.Value;

    public SmtpServerOptions GetSettings(SmtpServerOptions smtpServerOptions)
    {
        try
        {
            var persistedIp = SettingStore.Get("IP", smtpServerOptions.IP);
            var persistedPort = SettingStore.Get("Port", smtpServerOptions.Port);
            var persistedMessagePath = SettingStore.Get("MessagePath", smtpServerOptions.MessagePath);
            var persistedLoggingPath = SettingStore.Get("LoggingPath", smtpServerOptions.LoggingPath);

            // TLS/Certificate settings
            var persistedCertFindType = SettingStore.Get("CertificateFindType", smtpServerOptions.CertificateFindType);
            var persistedCertFindValue = SettingStore.Get("CertificateFindValue", smtpServerOptions.CertificateFindValue);
            var persistedCertStoreLocation = SettingStore.Get("CertificateStoreLocation", smtpServerOptions.CertificateStoreLocation);
            var persistedCertStoreName = SettingStore.Get("CertificateStoreName", smtpServerOptions.CertificateStoreName);

            // Apply persisted settings
            if (!string.IsNullOrWhiteSpace(persistedIp))
            {
                smtpServerOptions.IP = persistedIp;
            }

            if (persistedPort > 0)
            {
                smtpServerOptions.Port = persistedPort;
            }

            if (!string.IsNullOrWhiteSpace(persistedMessagePath))
            {
                smtpServerOptions.MessagePath = persistedMessagePath;
            }

            if (!string.IsNullOrWhiteSpace(persistedLoggingPath))
            {
                smtpServerOptions.LoggingPath = persistedLoggingPath;
            }

            if (!string.IsNullOrWhiteSpace(persistedCertFindType))
            {
                smtpServerOptions.CertificateFindType = persistedCertFindType;
            }

            // CertificateFindValue can be empty (TLS disabled), so always apply it
            smtpServerOptions.CertificateFindValue = persistedCertFindValue ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(persistedCertStoreLocation))
            {
                smtpServerOptions.CertificateStoreLocation = persistedCertStoreLocation;
            }

            if (!string.IsNullOrWhiteSpace(persistedCertStoreName))
            {
                smtpServerOptions.CertificateStoreName = persistedCertStoreName;
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to initialize SMTP server options from persisted settings");
        }

        return smtpServerOptions;
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.RegisterType<SmtpServerOptionsMerger>().AsSelf().SingleInstance();
    }

    #endregion
}
