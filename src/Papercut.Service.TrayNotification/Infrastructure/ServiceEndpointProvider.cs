// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
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


using System.Text.Json;

using Autofac;

using Microsoft.Win32;

namespace Papercut.Service.TrayNotification.Infrastructure;

/// <summary>
/// Resolves the base URL the Papercut service is listening on.
///
/// This used to be an IPComm round-trip to the service. That required the service
/// to be up before the tray could learn where it was, which is backwards -- the
/// tray needs the address precisely so it can connect and notice when the service
/// comes up. Reading the same "Urls" configuration the service reads has no such
/// ordering problem and works while the service is stopped.
/// </summary>
public class ServiceEndpointProvider
{
    public const string FallbackBaseUrl = "http://localhost:8080";

    private const string ServiceName = "Papercut.SMTP.Service";

    private const string UrlOverrideVariable = "PAPERCUT_SERVICE_URL";

    private readonly ILogger _logger;

    private string? _cachedBaseUrl;

    public ServiceEndpointProvider(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// The resolved base url, e.g. "http://localhost:8080". Resolution is cheap
    /// (config file read) and the result is cached until <see cref="InvalidateCache" />.
    /// </summary>
    public string BaseUrl => _cachedBaseUrl ??= Resolve();

    /// <summary>
    /// Absolute url of the messages SignalR hub.
    /// </summary>
    public string MessagesHubUrl => $"{BaseUrl.TrimEnd('/')}/hubs/messages";

    /// <summary>
    /// Clears the cached url, forcing a re-read on next access. Called after a
    /// service restart, since the configuration may have changed with it.
    /// </summary>
    public void InvalidateCache()
    {
        _cachedBaseUrl = null;
    }

    private string Resolve()
    {
        var resolved = FromEnvironment() ?? FromServiceDirectory() ?? FromTrayDirectory();

        if (resolved == null)
        {
            _logger.Debug("Could not locate service configuration, using fallback {Url}", FallbackBaseUrl);
            return FallbackBaseUrl;
        }

        _logger.Debug("Resolved Papercut service base url {Url}", resolved);

        return resolved;
    }

    private string? FromEnvironment()
    {
        var overrideUrl = Environment.GetEnvironmentVariable(UrlOverrideVariable);

        if (string.IsNullOrWhiteSpace(overrideUrl)) return null;

        var normalized = NormalizeUrl(overrideUrl);

        if (normalized != null)
        {
            _logger.Information(
                "Using Papercut service url from {Variable}: {Url}",
                UrlOverrideVariable,
                normalized);
        }

        return normalized;
    }

    /// <summary>
    /// Reads appsettings.json next to the installed service executable, located
    /// via the registered Windows service ImagePath.
    /// </summary>
    private string? FromServiceDirectory()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{ServiceName}");

            if (key?.GetValue("ImagePath") is not string imagePath || string.IsNullOrWhiteSpace(imagePath))
                return null;

            var executablePath = UnquoteImagePath(imagePath);
            var directory = Path.GetDirectoryName(executablePath);

            return directory == null ? null : FromSettingsFile(Path.Combine(directory, "appsettings.json"));
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Unable to read the Papercut service ImagePath from the registry");
            return null;
        }
    }

    /// <summary>
    /// Reads appsettings.json next to the tray executable, which is where it
    /// lands when the tray and service are installed side by side.
    /// </summary>
    private string? FromTrayDirectory()
    {
        return FromSettingsFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
    }

    private string? FromSettingsFile(string settingsPath)
    {
        try
        {
            if (!File.Exists(settingsPath)) return null;

            using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));

            if (!document.RootElement.TryGetProperty("Urls", out var urls)) return null;

            return NormalizeUrl(urls.GetString());
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Unable to read Urls from {SettingsPath}", settingsPath);
            return null;
        }
    }

    /// <summary>
    /// Takes the first entry of a semicolon-delimited Urls value and rewrites
    /// wildcard hosts to localhost -- "http://0.0.0.0:8080" is something Kestrel
    /// binds, not something a client can connect to.
    /// </summary>
    internal static string? NormalizeUrl(string? urlsValue)
    {
        if (string.IsNullOrWhiteSpace(urlsValue)) return null;

        var first = urlsValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (first == null) return null;

        // Uri cannot parse the wildcard hosts Kestrel accepts, so swap them before parsing
        foreach (var wildcard in new[] { "//0.0.0.0", "//+", "//*", "//[::]" })
        {
            if (first.Contains(wildcard, StringComparison.Ordinal))
            {
                first = first.Replace(wildcard, "//localhost", StringComparison.Ordinal);
                break;
            }
        }

        if (!Uri.TryCreate(first, UriKind.Absolute, out var uri)) return null;

        // Uri already resolves the scheme's default port (80/443); it only reports
        // -1 for schemes that have none, which is not something Kestrel would bind
        var port = uri.Port >= 0 ? uri.Port : 8080;

        return $"{uri.Scheme}://{uri.Host}:{port}";
    }

    private static string UnquoteImagePath(string imagePath)
    {
        imagePath = imagePath.Trim();

        // a quoted path may be followed by service arguments
        if (imagePath.StartsWith('"'))
        {
            var closing = imagePath.IndexOf('"', 1);
            return closing > 0 ? imagePath[1..closing] : imagePath.Trim('"');
        }

        return imagePath;
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

        builder.RegisterType<ServiceEndpointProvider>().AsSelf().SingleInstance();
    }

    #endregion
}
