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


namespace Papercut.WebUI.Test.Base
{
    using System;
    using System.Net.Http;

    using Autofac;

    using Core.Domain.Application;
    using Core.Domain.Paths;
    using Core.Infrastructure.Container;

    using WebServerFacts;
    using System.Reflection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.PlatformAbstractions;
    using Newtonsoft.Json;

    public class ApiTestBase : IDisposable
    {
        protected ILifetimeScope Scope;
        protected string BaseAddress;
        protected readonly HttpClient Client;

        public ApiTestBase()
        {
            BaseAddress = "http://webui.papercut.com";
            Scope = BuildContainer(MockDependencies).BeginLifetimeScope();
            Client = BuildClient();
        }

        HttpClient BuildClient()
        {
            WebServer.WebStartup.Scope = this.Scope;

            var hostBuilder = new WebHostBuilder()
                .UseContentRoot(PlatformServices.Default.Application.ApplicationBasePath)
                .UseEnvironment("Development")
                .UseStartup<WebServer.WebStartup>();

            var testServer = new TestServer(hostBuilder);
            return testServer.CreateClient();            
        }

        void IDisposable.Dispose()
        {
            Client.Dispose();
            Scope.Dispose();
        }

        static IContainer BuildContainer(Action<ContainerBuilder> configurer = null)
        {
            PapercutContainer.SpecifiedEntryAssembly = typeof(WebUiWebServerApiFact).GetTypeInfo().Assembly;

            var builder = new ContainerBuilder();
            builder.RegisterModule<PapercutCoreModule>();

            configurer?.Invoke(builder);
            return builder.Build();
        }

        protected virtual void MockDependencies(ContainerBuilder builder)
        {
            builder.Register(c => new ApplicationMeta("Papercut.WebUI.Tests")).As<IAppMeta>().SingleInstance();
            builder.RegisterType<ServerPathTemplateProviderService>().As<IPathTemplatesProvider>().SingleInstance();
        }

        protected HttpResponseMessage Get(string uri)
        {
            return Client.GetAsync(uri).Result;
        }

        protected HttpResponseMessage Delete(string uri)
        {
            return Client.DeleteAsync(uri).Result;
        }

        protected T Get<T>(string uri)
        {
            var response = Get(uri).Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<T>(response);
        }
    }
}