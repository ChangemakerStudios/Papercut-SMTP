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

namespace Papercut.Service
{
    using System;

    using Autofac;

    using Papercut.Core;
    using Papercut.Core.Infrastructure.Container;
    using Papercut.Service.Services;

    using Serilog;

    using Topshelf;
    using Topshelf.HostConfigurators;

    class RunServiceApp
    {
        ILifetimeScope _container;

        public void Run()
        {
            _container = PapercutContainer.Instance.BeginLifetimeScope();
            try
            {
                HostFactory.Run(ConfigureHost);
            }
            catch (Exception ex)
            {
                _container.Resolve<ILogger>().Fatal(ex, "Unhandled Exception");
                throw;
            }
        }

        void ConfigureHost(HostConfigurator x)
        {
            x.UseSerilog();
            x.Service<PapercutServerService>(
                s =>
                {
                    s.ConstructUsing(serviceFactory => _container.Resolve<PapercutServerService>());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                    s.WhenShutdown(ts => _container.Dispose());
                });

            x.RunAsLocalSystem();

            x.SetDescription("Papercut SMTP Backend Service");
            x.SetDisplayName("Papercut SMTP Service");
            x.SetServiceName("PapercutServerService");
        }
    }
}