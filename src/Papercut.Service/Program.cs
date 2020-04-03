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

namespace Papercut.Service
{
    using Autofac;

    using Papercut.Core.Infrastructure.Container;
    using Papercut.Core.Infrastructure.Logging;
    using Papercut.Service.Services;

    using Topshelf;
    using Topshelf.HostConfigurators;

    internal class Program
    {
        private static IContainer _container;

        static void Main(string[] args)
        {
            BootstrapLogger.SetRootGlobal();

            using (_container = new SimpleContainer<PapercutServiceModule>().Build())
            {
                HostFactory.Run(ConfigureHost);
            }
        }

        static void ConfigureHost(HostConfigurator x)
        {
            x.UseSerilog();
            x.Service<ILifetimeScope>(
                s =>
                {
                    s.ConstructUsing(serviceFactory => _container.BeginLifetimeScope());
                    s.WhenStarted(scope => scope.Resolve<PapercutServerService>().Start());
                    s.WhenStopped(scope => scope.Resolve<PapercutServerService>().Stop());
                    s.WhenShutdown(scope => scope.Dispose());
                });

            x.RunAsLocalSystem();

            x.SetDescription("Papercut SMTP Backend Service");
            x.SetDisplayName("Papercut SMTP Service");
            x.SetServiceName("PapercutServerService");
        }
    }
}