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

namespace Papercut
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Windows;

    using Autofac;

    using Papercut.Common.Domain;
    using Papercut.Core;
    using Papercut.Core.Infrastructure.Container;
    using Papercut.Core.Infrastructure.Lifecycle;

    using Serilog;

    public partial class App : Application
    {
        public const string GlobalName = "Papercut.App";

        public static string ExecutablePath;

        Lazy<ILifetimeScope> _lifetimeScope =
            new Lazy<ILifetimeScope>(
                () => PapercutContainer.Instance.BeginLifetimeScope(PapercutContainer.UIScopeTag));

        static App()
        {
            ExecutablePath = Assembly.GetExecutingAssembly().Location;
        }

        public ILifetimeScope Container => _lifetimeScope.Value;


        protected override void OnStartup(StartupEventArgs e)
        {
            var publishEvent = Container.Resolve<IMessageBus>();

            try
            {
                var appPreStartEvent = new PapercutClientPreStartEvent();
                publishEvent.Publish(appPreStartEvent);

                if (appPreStartEvent.CancelStart)
                {
                    // force shut down...
                    publishEvent.Publish(new AppForceShutdownEvent());
                    return;
                }

                base.OnStartup(e);

                // startup app
                publishEvent.Publish(new PapercutClientReadyEvent());
            }
            catch (Exception ex)
            {
                Container.Resolve<ILogger>().Fatal(ex, "Fatal Error Starting Papercut");
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Debug.WriteLine("App.OnExit()");

            using (Container)
            {
                Container.Resolve<IMessageBus>().Publish(new PapercutClientExitEvent());
            }

            _lifetimeScope = null;
            base.OnExit(e);
        }
    }
}