// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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


namespace Papercut.WebUI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Web.Http;
    using System.Web.Http.Dependencies;
    using System.Web.Http.SelfHost;

    using Autofac;
    using Autofac.Integration.WebApi;
    using Common.Domain;

    using Core.Infrastructure.Lifecycle;

    class WebServer : IEventHandler<PapercutServiceReadyEvent>
    {
        readonly ILifetimeScope container;
        public WebServer(ILifetimeScope container)
        {
            this.container = container;
        }


        const string BaseAddress = "http://localhost:6789";

        public void Handle(PapercutServiceReadyEvent @event)
        {
            Console.WriteLine("WebUI Server Start ...");

            var config = new HttpSelfHostConfiguration(BaseAddress);
            config.DependencyResolver =  new AutofacWebApiDependencyResolver(this.container);

            config.Routes.MapHttpRoute("health", "health", new {controller = "Health"});
            config.Routes.MapHttpRoute("load all message", "messages", new {controller = "Message", action = "GetAll"});

            var server = new HttpSelfHostServer(config);
            server.OpenAsync().Wait();
        }


        public static void RegisterControllerTypes(ContainerBuilder builder)
        {
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
        }
    }
}