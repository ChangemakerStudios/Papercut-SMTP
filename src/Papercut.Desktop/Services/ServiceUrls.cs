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


namespace Papercut.Desktop.Services;

internal static class ServiceUrls
{
    /// <summary>
    /// First entry of a ';'-separated Kestrel "Urls" value, with wildcard hosts swapped for
    /// localhost: "http://0.0.0.0:8080" is something Kestrel binds, not something a client can open.
    /// Keeps a path prefix ("http://host:8080/papercut/") and always ends with '/'.
    /// </summary>
    public static Uri? Normalize(string? urls)
    {
        var first = urls?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

        if (first == null) return null;

        foreach (var wildcard in new[] { "//0.0.0.0", "//+", "//*", "//[::]" })
        {
            if (!first.Contains(wildcard, StringComparison.Ordinal)) continue;

            first = first.Replace(wildcard, "//localhost", StringComparison.Ordinal);
            break;
        }

        if (!Uri.TryCreate(first, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return null;

        var path = uri.AbsolutePath.EndsWith('/') ? uri.AbsolutePath : uri.AbsolutePath + "/";

        return new UriBuilder(uri.Scheme, uri.Host, uri.Port, path).Uri;
    }
}
