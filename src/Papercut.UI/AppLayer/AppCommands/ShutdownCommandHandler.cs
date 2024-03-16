// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;

using Autofac;
using Autofac.Util;

using Papercut.Common.Domain;
using Papercut.Core.Domain.Application;
using Papercut.Core.Infrastructure.Lifecycle;
using Papercut.Domain.AppCommands;

namespace Papercut.AppLayer.AppCommands
{
    public class ShutdownCommandHandler : Disposable, IStartable
    {
        private readonly IAppCommandHub _appCommandHub;

        private readonly IAppMeta _appMeta;

        private readonly ILogger _logger;

        private readonly IMessageBus _messageBus;

        private IDisposable _shutdownObservable;

        public ShutdownCommandHandler(IAppCommandHub appCommandHub, IMessageBus messageBus, IAppMeta appMeta, ILogger logger)
        {
            this._appCommandHub = appCommandHub;
            this._messageBus = messageBus;
            this._appMeta = appMeta;
            this._logger = logger.ForContext<ShutdownCommandHandler>();
        }

        public void Start()
        {
            this.InitObservables();
        }

        private void InitObservables()
        {
            this._shutdownObservable = this._appCommandHub.OnShutdown
                .ObserveOn(Dispatcher.CurrentDispatcher)
                .Subscribe(
                    async @event =>
                    {
                        this._logger.Information("Shutdown Executed {ExitCode}", @event.ExitCode);
                        
                        // fire shutdown event
                        await this._messageBus.PublishAsync(
                            new PapercutClientExitEvent() { AppMeta = this._appMeta });

                        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                        Application.Current.Shutdown(@event.ExitCode);
                    });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._shutdownObservable?.Dispose();
            }
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register(ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterType<ShutdownCommandHandler>().AsImplementedInterfaces().SingleInstance();
        }

        #endregion
    }
}