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


namespace Papercut.Services
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using Autofac;

    using Papercut.Common.Domain;
    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Network;
    using Papercut.Core.Domain.Network.Smtp;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Events;
    using Papercut.Infrastructure.Smtp;
    using Papercut.Properties;

    using Serilog;

    public class SmtpServerCoordinator : IEventHandler<PapercutClientReadyEvent>,
        IEventHandler<PapercutClientExitEvent>,
        IEventHandler<SettingsUpdatedEvent>,
        INotifyPropertyChanged,
        IAsyncDisposable
    {
        readonly ILogger _logger;

        readonly IMessageBus _messageBus;

        private readonly PapercutSmtpServer _smtpServer;

        //private IDisposable _observeStartServer;

        bool _smtpServerEnabled = true;

        public SmtpServerCoordinator(
            PapercutSmtpServer smtpServer,
            ILogger logger,
            IMessageBus messageBus)
        {
            this._smtpServer = smtpServer;
            this._logger = logger;
            this._messageBus = messageBus;
        }

        public bool SmtpServerEnabled
        {
            get => this._smtpServerEnabled;
            set
            {
                if (value.Equals(this._smtpServerEnabled)) return;
                this._smtpServerEnabled = value;
                this.OnPropertyChanged();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (this._smtpServer != null) await this._smtpServer.DisposeAsync();
        }

        public async Task HandleAsync(PapercutClientExitEvent @event)
        {
            await this.StopSmtpServerAsync();
        }

        public async Task HandleAsync(PapercutClientReadyEvent @event)
        {
            if (this.SmtpServerEnabled) await this.ListenSmtpServer();

            this.PropertyChanged += async (sender, args) =>
            {
                if (args.PropertyName == "StmpServerEnabled")
                {
                    if (this.SmtpServerEnabled && !this._smtpServer.IsActive)
                    {
                        await this.ListenSmtpServer();
                    }
                    else if (!this.SmtpServerEnabled && this._smtpServer.IsActive)
                    {
                        await this.StopSmtpServerAsync();
                    }
                }
            };
        }

        public async Task HandleAsync(SettingsUpdatedEvent @event)
        {
            if (!this.SmtpServerEnabled) return;
            if (@event.PreviousSettings.IP == @event.NewSettings.IP && @event.PreviousSettings.Port == @event.NewSettings.Port) return;

            await this.ListenSmtpServer();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async Task StopSmtpServerAsync()
        {
            //this._observeStartServer?.Dispose();
            await this._smtpServer.StopAsync();
        }

        async Task ListenSmtpServer()
        {
            try
            {
                await this._smtpServer.StopAsync();
                await this._smtpServer.StartAsync(new EndpointDefinition(Settings.Default.IP, Settings.Default.Port));
                await this._messageBus.PublishAsync(new SmtpServerBindEvent(Settings.Default.IP, Settings.Default.Port));
            }
            catch (Exception ex)
            {
                this._logger.Warning(
                    ex,
                    "Failed to bind SMTP to the {Address} {Port} specified. The port may already be in use by another process.",
                    Settings.Default.IP,
                    Settings.Default.Port);

                await this._messageBus.PublishAsync(new SmtpServerBindFailedEvent());
            }

            //this._observeStartServer = this._smtpServer.ObserveStartServer(
            //        Settings.Default.IP,
            //        Settings.Default.Port,
            //        TaskPoolScheduler.Default)
            //    .DelaySubscription(TimeSpan.FromMilliseconds(500)).Retry(5)
            //    .Subscribe(
            //        b => { },
            //        async ex =>
            //        {
            //            this._logger.Warning(
            //                ex,
            //                "Failed to bind SMTP to the {Address} {Port} specified. The port may already be in use by another process.",
            //                Settings.Default.IP,
            //                Settings.Default.Port);

            //            await this._messageBus.PublishAsync(new SmtpServerBindFailedEvent());
            //        },
            //        async () =>
            //            await this._messageBus.PublishAsync(
            //                new SmtpServerBindEvent(Settings.Default.IP, Settings.Default.Port)));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

            builder.RegisterType<SmtpServerCoordinator>().AsImplementedInterfaces()
                .InstancePerLifetimeScope();
        }

        #endregion
    }
}