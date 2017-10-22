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
    using Autofac;

    using Core.Domain.Application;
    using Core.Domain.Paths;
    using Core.Infrastructure.Container;

    using WebServerFacts;
    using System.Reflection;
    using NUnit.Framework;
    using System.IO;
    using System.Linq;

    public class TestBase : IDisposable
    {
        protected ILifetimeScope Scope;

        public TestBase()
        {
            Scope = BuildContainer(MockDependencies).BeginLifetimeScope();
        }

        void IDisposable.Dispose()
        {
            Scope.Dispose();
        }

        [TearDown]
        public void Cleanup(){
            Scope.Resolve<IMessagePathConfigurator>()
                .LoadPaths
                .SelectMany(path => Directory.GetFiles(path))
                .ToList()
                .ForEach(File.Delete);
        }

        static IContainer BuildContainer(Action<ContainerBuilder> configurer = null)
        {
            PapercutCoreModule.SpecifiedEntryAssembly = typeof(WebUiWebServerApiFact).GetTypeInfo().Assembly;

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

    }

    static class ObjectExtensions
    {
        public static T AccessProptery<T>(this object obj, string name)
        {
            T defaultVal = default(T);
            if (obj == null)
            {
                return defaultVal;
            }

            try
            {
                var property = obj.GetType().GetProperty(name);
                return (T)(property.GetValue(obj));
            }
            catch
            {
                return defaultVal;
            }
        }
    }
}