// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


namespace Papercut.Service.Application.Controllers;

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
                                                                     };

    [HttpGet("{*anything}", Order = short.MaxValue)]
    [ResponseCache(
#if DEBUG
        Duration = 30
#else
        Duration = 600
#endif
    )]
    public IActionResult Get()
    {
        var resourceName = GetRequestedResourceName(this.Request.Path);
        var resourceContent = GetResourceStream(resourceName);
        if (resourceContent == null)
        {
            return this.NotFound();
        }

        return new FileStreamResult(resourceContent, GetMimeType(resourceName));
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