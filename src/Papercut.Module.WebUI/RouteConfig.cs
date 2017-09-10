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

            config.Routes.MapHttpRoute("load all messages", "api/messages", new {controller = "Message", action = "GetAll"});
            config.Routes.MapHttpRoute("load message detail", "api/messages/{id}", new { controller = "Message", action = "Get" });

            config.Routes.MapHttpRoute("Serve other requests as static content", routeTemplate: "{*anything}", defaults: new { controller = "StaticContent", uri = RouteParameter.Optional });

        }
    }
}