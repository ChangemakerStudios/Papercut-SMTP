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


using Autofac;
using Papercut.Common.Helper;
using Papercut.Core.Infrastructure.Async;
using Papercut.Core.Infrastructure.Network;
using Papercut.Infrastructure.IPComm;

namespace Papercut.Service.TrayNotification.Infrastructure;

/// <summary>
/// Handles communication with the Papercut SMTP Service via IPComm protocol
/// </summary>
public class ServiceCommunicator(PapercutIPCommClientFactory ipCommClientFactory, ILogger logger) : IStartable
{
    private const string FallbackWebUrl = "http://localhost:8080";

    private readonly TimeSpan _urlCacheExpiration = TimeSpan.FromMinutes(5);

    private string? _cachedWebUrl;

    private DateTime _lastUrlCheck = DateTime.MinValue;

    /// <summary>
    /// Gets the cached web URL without making an async call.
    /// Returns null if not yet cached or cache expired.
    /// </summary>
    public string? CachedWebUrl =>
        _cachedWebUrl != null && DateTime.Now - _lastUrlCheck < _urlCacheExpiration
            ? _cachedWebUrl
            : null;

    public async Task<string> GetWebUIUrlAsync()
    {
        // Return cached URL if still valid
        if (_cachedWebUrl != null && DateTime.Now - _lastUrlCheck < _urlCacheExpiration)
        {
            return _cachedWebUrl;
        }
        
        try
        {
            var serviceClient = ipCommClientFactory.GetClient(PapercutIPCommClientConnectTo.Service);

            var exchangeEvent = new ServiceWebUISettingsExchangeEvent();

            var serviceWebUiSettings = await serviceClient.ExchangeEventServer(exchangeEvent, TimeSpan.FromSeconds(2));

            if (serviceWebUiSettings != null && serviceWebUiSettings.IP.IsSet() && serviceWebUiSettings.Port.HasValue)
            {
                _cachedWebUrl = $"http://{serviceWebUiSettings.IP}:{serviceWebUiSettings.Port}";
                _lastUrlCheck = DateTime.Now;

                return _cachedWebUrl;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to probe for Papercut Service Listening Web Url");
        }

        // Fallback to default
        Log.Debug("Using fallback web URL: {Url}", FallbackWebUrl);
        _cachedWebUrl = FallbackWebUrl;
        _lastUrlCheck = DateTime.Now;

        return FallbackWebUrl;
    }

    public void Start()
    {
        logger.Debug("Startup: Attempting to IPComm to Service to get the Web UI Url...");
        this.GetWebUIUrlAsync().RunAsync();
    }


    /// <summary>
    /// Clears the cached web URL, forcing a re-check on next access
    /// </summary>
    public void InvalidateCache()
    {
        _cachedWebUrl = null;
        _lastUrlCheck = DateTime.MinValue;
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

        builder.RegisterType<ServiceCommunicator>().AsSelf().AsImplementedInterfaces().SingleInstance();
    }

    #endregion
}
