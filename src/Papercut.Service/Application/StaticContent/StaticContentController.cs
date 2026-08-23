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


namespace Papercut.Service.Application.StaticContent;

using System.Reflection;
using System.Text.RegularExpressions;

public class StaticContentController : ControllerBase
{
    const string ResourcePath = "{0}.Web.Assets.{1}";

    private static readonly Dictionary<string, string> MimeMapping = new()
    {
        { "htm", "text/html" },
        { "html", "text/html" },
        { "txt", "text/plain" },
        { "js", "text/javascript" },
        { "css", "text/css" },
        { "ico", "image/x-icon" },
        { "png", "image/png" },
        { "jpeg", "image/jpeg" },
        { "jpg", "image/jpeg" },
        { "gif", "image/gif" },
        { "svg", "image/svg+xml" },
        { "ttf", "application/x-font-ttf" },
        { "woff", "application/font-woff" },
        { "woff2", "application/font-woff2" },
        { "json", "application/json" },
        { "webmanifest", "application/manifest+json" },
    };

    /// <summary>
    ///     The SPA ships with a root base href. When the service is mounted under an
    ///     HttpPathPrefix that is wrong for every url the app builds from it, so the
    ///     tag is rewritten on the way out to whatever prefix this request arrived on.
    /// </summary>
    static readonly Regex _baseHrefRegex = new(
        @"<base\s+href\s*=\s*([""'])[^""']*\1",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    const string IndexResource = "index.html";

    [HttpGet("{*anything}", Order = short.MaxValue)]
    [ResponseCache(
#if DEBUG
        Duration = 30
#else
        Duration = 600
#endif
    )]
    public async Task<IActionResult> Get()
    {
        var resourceName = GetRequestedResourceName(Request.Path);
        var resourceContent = GetResourceStream(resourceName);

        if (resourceContent == null && !Path.HasExtension(Request.Path.Value))
        {
            // deep links into the Angular SPA (e.g. /message/{id}) fall back to index.html
            // so the client-side router can handle the route
            resourceName = IndexResource;
            resourceContent = GetResourceStream(resourceName);
        }

        if (resourceContent == null)
        {
            return NotFound();
        }

        if (string.Equals(resourceName, IndexResource, StringComparison.OrdinalIgnoreCase))
        {
            return Content(await ReadIndexWithBaseHrefAsync(resourceContent), "text/html", Encoding.UTF8);
        }

        return new FileStreamResult(resourceContent, GetMimeType(resourceName));
    }

    async Task<string> ReadIndexWithBaseHrefAsync(Stream content)
    {
        using var reader = new StreamReader(content, Encoding.UTF8);
        var html = await reader.ReadToEndAsync();

        // PathBase is what UsePathBase stripped off, i.e. exactly the prefix this
        // request came in on. Empty when there is no prefix, giving "/" as before.
        var baseHref = $"{Request.PathBase.Value?.TrimEnd('/')}/";

        return _baseHrefRegex.Replace(html, match => $"<base href=\"{baseHref}\"", 1);
    }

    static string GetRequestedResourceName(string requestUri)
    {
        var filename = requestUri
            .TrimStart('/')
            .TrimStart('.')
            .Replace("%", "")
            .Replace("$", "")
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace(Path.DirectorySeparatorChar, '.');

        if (string.IsNullOrEmpty(filename))
        {
            filename = "index.html";
        }

        return filename;
    }

    static Stream? GetResourceStream(string relativePath)
    {
        var currentAssembly = typeof(StaticContentController).GetTypeInfo().Assembly;
        var resource = string.Format(ResourcePath, currentAssembly.GetName().Name, relativePath);

        return currentAssembly.GetManifestResourceStream(resource);
    }

    static string GetMimeType(string filename)
    {
        var extension = Path.GetExtension(filename)?.TrimStart('.');
        if (extension == null || !MimeMapping.TryGetValue(extension, out var mimeType))
        {
            mimeType = "application/octet-stream";
        }
        return mimeType;
    }
}
