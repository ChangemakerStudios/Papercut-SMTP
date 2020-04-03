// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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
namespace Papercut.App.WebApi.Tests.Base
{
    using System.Collections.Generic;

    using Autofac;

    using Papercut.Core.Domain.Application;
    using Papercut.Core.Domain.Paths;
    using Papercut.Message;

    public class ApiTestModule : Module
    {
        private IEnumerable<Module> GetPapercutServiceModules()
        {
            yield return new PapercutMessageModule();
            yield return new PapercutWebApiModule();
        }

        protected override void Load(ContainerBuilder builder)
        {
            foreach (var module in this.GetPapercutServiceModules())
            {
                builder.RegisterModule(module);
            }

            builder.Register(c => new ApplicationMeta("Papercut.App.WebApi.Tests")).As<IAppMeta>().SingleInstance();
            builder.RegisterType<ServerPathTemplateProviderService>().As<IPathTemplatesProvider>().SingleInstance();
        }
    }
}