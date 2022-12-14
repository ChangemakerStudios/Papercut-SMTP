// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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
    using Papercut.Core.Infrastructure.Container;
    using Papercut.Core.Infrastructure.Logging;
    using Papercut.Domain.LifecycleHooks;
    using Papercut.Infrastructure;
    using Papercut.Infrastructure.LifecycleHooks;

    using Serilog;

    public partial class App : Application
    {
        public const string GlobalName = "Papercut.App";

        Lazy<ILifetimeScope> _lifetimeScope =
            new Lazy<ILifetimeScope>(
                () => RootContainer.BeginLifetimeScope(ContainerScope.UIScopeTag));

        static App()
        {
            BootstrapLogger.SetRootGlobal();

            ExecutablePath = Assembly.GetExecutingAssembly().Location;
            RootContainer = new SimpleContainer<PapercutUIModule>().Build();
        }

        public static string ExecutablePath { get; }

        public static IContainer RootContainer { get; }

        public ILifetimeScope Container => this._lifetimeScope.Value;

        protected override void OnExit(ExitEventArgs e)
        {
            Debug.WriteLine("App.OnExit()");

            // run pre-exit
            AppLifecycleActionResultType runPreExit = AppLifecycleActionResultType.Continue;
            
            Task.Run(async () =>
            {
                runPreExit = await this.Container.RunPreExit();
            }).Wait();

            if (runPreExit == AppLifecycleActionResultType.Cancel)
            {
                // cancel exit
                return;
            }

            try
            {
                this.Container.Dispose();

                this._lifetimeScope = null;

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