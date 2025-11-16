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


using Papercut.Core.Domain.Network;
using Papercut.Core.Infrastructure.Network;
using Papercut.Infrastructure.IPComm.Network;

namespace Papercut.Service.TrayNotification;

/// <summary>
/// Handles communication with the Papercut SMTP Service via IPComm protocol
/// </summary>
public class ServiceCommunicator
{
    private const string FallbackWebUrl = "http://localhost:8080";

    private readonly TimeSpan _urlCacheExpiration = TimeSpan.FromMinutes(1);

    private string? _cachedWebUrl;

    private DateTime _lastUrlCheck = DateTime.MinValue;

    /// <summary>
    /// Gets the web UI URL from the service using IPComm, or returns fallback if service is not available
    /// </summary>
    /// <remarks>
    /// TODO: Currently uses HTTP probing to find the web UI port.
    /// Need to add a new IPComm exchange event to the service that returns web server configuration.
    /// AppProcessExchangeEvent only returns SMTP server bindings (port 25), not web UI bindings.
    /// PapercutWebServerReadyEvent is only published on startup, can't be requested on demand.
    /// </remarks>
    public async Task<string> GetWebUIUrlAsync()
    {
        // Return cached URL if still valid
        if (_cachedWebUrl != null && DateTime.Now - _lastUrlCheck < _urlCacheExpiration)
        {
            return _cachedWebUrl;
        }

        try
        {
            // Try common web UI ports via HTTP probing
            var portsToTry = new[] { 8080, 80, 5000, 37404 };

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };

            foreach (var port in portsToTry)
            {
                var testUrl = $"http://localhost:{port}";
                try
                {
                    var response = await client.GetAsync(testUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        _cachedWebUrl = testUrl;
                        _lastUrlCheck = DateTime.Now;
                        Log.Debug("Found web UI at {Url} via HTTP probe", testUrl);
                        return testUrl;
                    }
                }
                catch
                {
                    // Continue to next port
                }
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to probe for web URL");
        }

        // Fallback to default
        Log.Debug("Using fallback web URL: {Url}", FallbackWebUrl);
        _cachedWebUrl = FallbackWebUrl;
        _lastUrlCheck = DateTime.Now;
        return FallbackWebUrl;
    }

    /// <summary>
    /// Checks if the service is responding via IPComm
    /// </summary>
    public async Task<bool> IsServiceRespondingAsync()
    {
        try
        {
            var endpoint = new EndpointDefinition(
                PapercutIPCommConstants.Localhost,
                PapercutIPCommConstants.ServiceListeningPort);

            var ipCommClient = new PapercutIPCommClient(endpoint, Log.Logger);
            var requestEvent = new AppProcessExchangeEvent();

            var responseEvent = await ipCommClient.ExchangeEventServer(
                requestEvent,
                TimeSpan.FromSeconds(1));

            return responseEvent != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Clears the cached web URL, forcing a re-check on next access
    /// </summary>
    public void InvalidateCache()
    {
        _cachedWebUrl = null;
        _lastUrlCheck = DateTime.MinValue;
    }
}
