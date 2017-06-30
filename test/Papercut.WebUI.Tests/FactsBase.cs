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


namespace Papercut.WebUI.Test
{
    using System;

    using Autofac;

    using Core.Domain.Application;
    using Core.Infrastructure.Container;

    using WebServerFacts;

    public class FactsBase : IDisposable
    {
        protected ILifetimeScope Scope;
        protected string BaseAddress;

        public FactsBase()
        {
            BaseAddress = "http://localhost:6789";
            Scope = BuildContainer(MockDependencies).BeginLifetimeScope();
        }

        void IDisposable.Dispose()
        {
            Scope.Dispose();
        }

        static IContainer BuildContainer(Action<ContainerBuilder> configurer = null)
        {
            PapercutContainer.SpecifiedEntryAssembly = typeof(WebUIWebServerFacts).Assembly;

            var builder = new ContainerBuilder();

            builder.RegisterModule<PapercutCoreModule>();

            configurer?.Invoke(builder);

            return builder.Build();
        }

        protected virtual void MockDependencies(ContainerBuilder builder)
        {
            builder.Register(c => new ApplicationMeta("Papercut.WebUI.Tests")).As<IAppMeta>().SingleInstance();
        }
    }
}