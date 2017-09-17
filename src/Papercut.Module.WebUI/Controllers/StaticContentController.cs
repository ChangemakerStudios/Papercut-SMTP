

namespace Papercut.Module.WebUI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http.Headers;
    using System.Reflection;
    // using WebApi.OutputCache.V2;

    public class StaticContentController: ControllerBase
    {

        [HttpGet("{*anything}", Order = short.MaxValue)]
//        [CacheOutput(
//#if DEBUG
//        ClientTimeSpan = 30,
//#else
//        ClientTimeSpan = 600,
//#endif
//        ServerTimeSpan = 86400, CacheKeyGenerator= typeof(PapercutResourceKeyGenerator))]
        public IActionResult Get()
        {
            var resourceName = GetRequetedResourceName(Request.Path);
            var resourceContent = GetResourceStream(resourceName);
            if (resourceContent == null)
            {
                return NotFound();
            }

            return new FileStreamResult(resourceContent, GetMimeType(resourceName));
        }

        static string GetRequetedResourceName(string requestUri)
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

        static Stream GetResourceStream(string relativePath)
        {
            var currentAssembly = typeof(StaticContentController).GetTypeInfo().Assembly;
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

        const string ResourcePath = "{0}.assets.{1}";
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

        //class PapercutResourceKeyGenerator : DefaultCacheKeyGenerator
        //{
        //    static string AssemblyVersion;
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
