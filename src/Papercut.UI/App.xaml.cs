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
    using System.Threading.Tasks;
    using System.Windows;

    using Autofac;

    using Papercut.Common.Domain;
    using Papercut.Core.Infrastructure.Async;
    using Papercut.Core.Infrastructure.Container;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Core.Infrastructure.Logging;

    using Serilog;

    using SmtpServer;

    public partial class App : Application
    {
        public const string GlobalName = "Papercut.App";

        public static string ExecutablePath { get; }

        Lazy<ILifetimeScope> _lifetimeScope =
            new Lazy<ILifetimeScope>(
                () => RootContainer.BeginLifetimeScope(ContainerScope.UIScopeTag));

        static App()
        {
            BootstrapLogger.SetRootGlobal();

            ExecutablePath = Assembly.GetExecutingAssembly().Location;
            RootContainer = new SimpleContainer<PapercutUIModule>().Build();
        }

        public static IContainer RootContainer { get; }

        public ILifetimeScope Container => _lifetimeScope.Value;


        protected override void OnStartup(StartupEventArgs e)
        {
            var messageBus = this.Container.Resolve<IMessageBus>();

            var successfulStartup = this.PapercutStartup(messageBus);

            if (true)
            {
                base.OnStartup(e);

                messageBus.Publish(new PapercutClientReadyEvent());
            }
        }

        private async Task<bool> PapercutStartup(IMessageBus messageBus)
        {
            try
            {
                var appPreStartEvent = new PapercutClientPreStartEvent();

                await messageBus.Publish(appPreStartEvent).ConfigureAwait(false);

                if (!appPreStartEvent.CancelStart)
                {
                    return true;
                }

                // force shut down...
                await messageBus.Publish(new AppForceShutdownEvent()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "Fatal Error Starting Papercut");
            }

            return false;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Debug.WriteLine("App.OnExit()");

            try
            {
                Container.Resolve<IMessageBus>().Publish(new PapercutClientExitEvent()).RunAsync();
                Container.Dispose();

                _lifetimeScope = null;

                RootContainer.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // no bother
            }

            base.OnExit(e);
        }
    }
}