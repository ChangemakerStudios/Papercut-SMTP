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


namespace Papercut.Core.Domain.Network;

/// <summary>
/// Decides which web pages may call the service. Captured mail is sensitive and the service
/// runs on a guessable local port, so a page on another site gets nothing by default.
/// </summary>
public static class CrossOriginPolicy
{
    public const string AllowedOriginsSettingKey = "CorsAllowedOrigins";

    public const string AnyOrigin = "*";

    // reserved names, which public DNS can't answer
    private static readonly string[] _privateSuffixes = [".localhost", ".local", ".internal", ".test", ".home.arpa"];

    /// <summary>
    /// Origins separated by ';' or ','. "*" allows any.
    /// </summary>
    public static string[] ParseAllowedOrigins(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];

        return value.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(origin => origin.TrimEnd('/'))
            .ToArray();
    }

    /// <param name="origin">The Origin header. Browsers send it; scripts and tools do not.</param>
    /// <param name="host">The Host header.</param>
    /// <param name="forwardedHost">
    /// X-Forwarded-Host, for a reverse proxy that rewrites Host. Safe to trust here: a page on
    /// another site cannot set it without a preflight, and the preflight has no such header.
    /// </param>
    /// <param name="allowedOrigins">From <see cref="ParseAllowedOrigins" />.</param>
    public static bool IsAllowed(string? origin, string? host, string? forwardedHost, IReadOnlyCollection<string> allowedOrigins)
    {
        if (string.IsNullOrEmpty(origin)) return true;

        if (allowedOrigins.Contains(AnyOrigin)) return true;

        if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)) return true;

        // "null" (sandboxed frames, file:) is not absolute, so it is never same-origin
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri)) return false;

        return IsSameAuthority(originUri, host)
               || IsSameAuthority(originUri, forwardedHost?.Split(',')[0].Trim());
    }

    /// <summary>
    /// Stops DNS rebinding, where a site points its own domain at this machine. Its page is then
    /// same-origin, so Origin proves nothing and same-origin GETs don't send one. Only a public
    /// name can be rebound, so anything else needs no setting.
    /// </summary>
    /// <remarks>Must not read X-Forwarded-Host: a rebound page is same-origin and can set it.</remarks>
    /// <param name="host">The Host header.</param>
    /// <param name="allowedOrigins">From <see cref="ParseAllowedOrigins" />. A listed origin's name is allowed on any port.</param>
    public static bool IsAllowedHost(string? host, IReadOnlyCollection<string> allowedOrigins)
    {
        if (string.IsNullOrEmpty(host) || allowedOrigins.Contains(AnyOrigin)) return true;

        if (!Uri.TryCreate($"http://{host}", UriKind.Absolute, out var hostUri)) return false;

        if (hostUri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6) return true;

        var name = hostUri.Host;

        if (!name.Contains('.')) return true;

        if (_privateSuffixes.Any(suffix => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))) return true;

        return allowedOrigins.Any(
            origin => Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
                      && string.Equals(originUri.Host, name, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSameAuthority(Uri origin, string? host)
    {
        if (string.IsNullOrEmpty(host)) return false;

        // through a Uri so that an explicit default port ("host:443") still matches
        return Uri.TryCreate($"{origin.Scheme}://{host}", UriKind.Absolute, out var hostUri)
               && string.Equals(origin.Authority, hostUri.Authority, StringComparison.OrdinalIgnoreCase);
    }
}
