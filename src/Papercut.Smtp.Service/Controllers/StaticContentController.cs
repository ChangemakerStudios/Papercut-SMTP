// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2022 Jaben Cargman
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


using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Papercut.Smtp.Service.Controllers
{
    public class StaticContentController: Controller
    {
        const string ResourcePath = "{0}.wwwroot.{1}";

        static readonly Dictionary<string, string> MimeMapping = new Dictionary<string, string>()
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

        private readonly IWebHostEnvironment _webHostEnvironment;

        public StaticContentController(IWebHostEnvironment webHostEnvironment)
        {
            this._webHostEnvironment = webHostEnvironment;
        }

        //        [CacheOutput(
        //#if DEBUG
        //        ClientTimeSpan = 30,
        //#else
        //        ClientTimeSpan = 600,
        //#endif
        //        ServerTimeSpan = 86400, CacheKeyGenerator= typeof(PapercutResourceKeyGenerator))]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public ActionResult Get()
        {
            var resourceName = GetRequestedResourceName(new Uri(this.Request.GetDisplayUrl()));
            var contentType = GetMimeType(resourceName);

            if (this._webHostEnvironment.IsDevelopment())
            {
                return this.PhysicalFile(Path.Combine(this._webHostEnvironment.WebRootPath, resourceName), contentType);
            }

            var resourceContent = GetResourceStream(resourceName);
            if (resourceContent == null)
            {
                return this.NotFound("The requested file does not exist.");
            }

            return this.File(resourceContent, contentType);
        }

        static string GetRequestedResourceName(Uri requestUri)
        {
            var filename = requestUri.PathAndQuery
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

        static Stream GetResourceStream(string relativePath)
        {
            var currentAssembly = Assembly.GetExecutingAssembly();
            var resource = string.Format(ResourcePath, currentAssembly.GetName().Name, relativePath);

            return currentAssembly.GetManifestResourceStream(resource);
        }

        static string GetMimeType(string filename)
        {
            var extension = Path.GetExtension(filename)?.TrimStart('.');
            string mimeType;
            if (extension == null || !MimeMapping.TryGetValue(extension, out mimeType))
            {
                mimeType = "application/octet-stream";
            }
            return mimeType;
        }

        //class PapercutResourceKeyGenerator : DefaultCacheKeyGenerator
        //{
        //    static readonly string AssemblyVersion;

        //    static PapercutResourceKeyGenerator()
        //    {
        //        AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        //    }

        //    public override string MakeCacheKey(HttpActionContext context, MediaTypeHeaderValue mediaType, bool excludeQueryString = false)
        //    {
        //        var requstUri = context.Request.RequestUri;
        //        int hashCode = string.Concat("PapercutResource", AssemblyVersion, requstUri).GetHashCode();

        //        return (hashCode ^ 0x10000).ToString("x2");
        //    }
        //}
    }
}
