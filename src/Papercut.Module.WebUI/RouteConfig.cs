using System.Web.Http;

namespace Papercut.Module.WebUI
{
    using Autofac;
    using Autofac.Integration.WebApi;

    public static class RouteConfig
    {
        public static void Init(HttpConfiguration config, ILifetimeScope scope)
        {
            config.DependencyResolver = new AutofacWebApiDependencyResolver(scope);

            config.Routes.MapHttpRoute("health", "health", new {controller = "Health"});

            config.Routes.MapHttpRoute("load all messages",
                "api/messages",
                new {controller = "Message", action = "GetAll"});

            config.Routes.MapHttpRoute("load message detail",
                "api/messages/{id}",
                new {controller = "Message", action = "Get"});

            config.Routes.MapHttpRoute("download section by content id",
                "api/messages/{messageId}/contents/{contentId}",
                new {controller = "Message", action = "DownloadSectionContent" });

            config.Routes.MapHttpRoute("download section by index",
                "api/messages/{messageId}/sections/{index}",
                new {controller = "Message", action = "DownloadSection" });

            config.Routes.MapHttpRoute("Serve other requests as static content",
                "{*anything}",
                new {controller = "StaticContent", anything = RouteParameter.Optional});
        }
    }
}