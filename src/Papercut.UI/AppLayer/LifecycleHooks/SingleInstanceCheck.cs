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


namespace Papercut.AppLayer.LifecycleHooks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;

    using Papercut.Core.Annotations;
    using Papercut.Domain.LifecycleHooks;
    using Papercut.Domain.UiCommands.Commands;
    using Papercut.Infrastructure.IPComm;

    using Serilog;

    public class SingleInstanceCheck : IDisposable, IAppLifecyclePreStart
    {
        readonly Mutex _appMutex = new Mutex(false, App.GlobalName);

        readonly PapercutIPCommClientFactory _ipCommClientFactory;

        public SingleInstanceCheck(PapercutIPCommClientFactory ipCommClientFactory,
            ILogger logger)
        {
            this.Logger = logger;
            this._ipCommClientFactory = ipCommClientFactory;
        }

        public ILogger Logger { get; set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public AppLifecycleActionResultType OnPreStart()
        {
            // papercut is not already running...
            if (this._appMutex.WaitOne(0, false)) return AppLifecycleActionResultType.Continue;

            this.Logger.Debug("Second process run. Shutting this process down and pushing show event to other process");

            // papercut is already running, push event to other UI process
            //Task.Run(
            //    () => this._ipCommClientFactory.GetClient(PapercutIPCommClientConnectTo.UI)
            //        .PublishEventServer(new ShowMainWindowCommand())).RunSynchronously();

            // no need to go further
            return AppLifecycleActionResultType.Cancel;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    this._appMutex?.Dispose();
                }
                catch
                {
                }
            }
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register([NotNull] ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterType<SingleInstanceCheck>().As<IAppLifecyclePreStart>().SingleInstance();
        }

        #endregion
    }
}