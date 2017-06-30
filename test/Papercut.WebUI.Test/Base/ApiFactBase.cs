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
    using System.IO;
    using System.Net;
    using System.Net.Http;

    using Autofac;

    using Common.Domain;

    using Core.Domain.Application;
    using Core.Domain.Paths;
    using Core.Infrastructure.Container;
    using Core.Infrastructure.Lifecycle;

    using Newtonsoft.Json;

    using WebServerFacts;

    public class ApiFactBase : IDisposable
    {
        protected ILifetimeScope Scope;
        protected string BaseAddress;
        readonly WebClient Client;

        public ApiFactBase()
        {
            BaseAddress = "http://localhost:6789";
            Client = new WebClient();

            Scope = BuildContainer(MockDependencies).BeginLifetimeScope();
            Scope.Resolve<IMessageBus>().Publish(new PapercutServiceReadyEvent {AppMeta = Scope.Resolve<IAppMeta>()});
        }

        void IDisposable.Dispose()
        {
            Scope.Dispose();
        }

        static IContainer BuildContainer(Action<ContainerBuilder> configurer = null)
        {
            PapercutContainer.SpecifiedEntryAssembly = typeof(WebUiWebServerApiFact).Assembly;

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

        protected string Get(string uri)
        {
            try
            {
                return Client.DownloadString($"{BaseAddress}/{uri.TrimStart('/')}");
            }
            catch (WebException exception)
            {
                Console.WriteLine(GetResposneContent(exception.Response.GetResponseStream(), exception.Response.ContentLength));
                throw;
            }
        }

        protected T Get<T>(string uri)
        {
            return JsonConvert.DeserializeObject<T>(Get(uri));
        }



        string GetResposneContent(Stream responseStream, long length)
        {
            using (responseStream)
            {
                var buffer = new byte[length];
                responseStream.Read(buffer, 0, (int)length);
                return System.Text.Encoding.UTF8.GetString(buffer);
            }
        }
    }
}