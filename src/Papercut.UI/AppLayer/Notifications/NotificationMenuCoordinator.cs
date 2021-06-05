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


namespace Papercut.AppLayer.Notifications
{
    using System;
    using System.Drawing;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using Autofac;
    using Autofac.Util;

    using Caliburn.Micro;

    using Papercut.Common.Domain;
    using Papercut.Core;
    using Papercut.Core.Annotations;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Domain.AppCommands;
    using Papercut.Domain.LifecycleHooks;
    using Papercut.Domain.UiCommands;
    using Papercut.Infrastructure.Resources;

    [UsedImplicitly]
    public class NotificationMenuCoordinator : Disposable, IAppLifecyclePreExit,
        IHandle<PapercutClientReadyEvent>
    {
        readonly IMessageBus _messageBus;

        readonly AppResourceLocator _resourceLocator;

        private readonly IAppCommandHub _appCommandHub;

        private readonly IUiCommandHub _uiCommandHub;

        NotifyIcon _notification;

        public NotificationMenuCoordinator(
            IAppCommandHub appCommandHub,
            IUiCommandHub uiCommandHub,
            AppResourceLocator resourceLocator,
            IMessageBus messageBus)
        {
            this._appCommandHub = appCommandHub;
            this._uiCommandHub = uiCommandHub;
            this._resourceLocator = resourceLocator;
            this._messageBus = messageBus;

            this.InitObservables();
        }

        public AppLifecycleActionResultType OnPreExit()
        {
            this.Reset();
            return AppLifecycleActionResultType.Continue;
        }

        public Task HandleAsync(PapercutClientReadyEvent message, CancellationToken cancellationToken)
        {
            if (this._notification == null) this.SetupNotification();

            return Task.CompletedTask;
        }

        void InitObservables()
        {
            this._uiCommandHub.OnShowBalloonTip
                .Sample(TimeSpan.FromSeconds(1), TaskPoolScheduler.Default)
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(
                    @event =>
                    {
                        this._notification.ShowBalloonTip(
                            @event.Timeout,
                            @event.TipTitle,
                            @event.TipText,
                            @event.ToolTipIcon);
                    });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Reset();
            }
        }

        private void Reset()
        {
            this._notification?.Dispose();
            this._notification = null;
        }

        void SetupNotification()
        {
            // Set up the notification icon
            this._notification = new NotifyIcon
                                 {
                                     Icon = new Icon(this._resourceLocator.GetResource("App.ico").Stream),
                                     Text = AppConstants.ApplicationName,
                                     Visible = true
                                 };

            this._notification.Click +=
                (sender, args) => this._uiCommandHub.ShowMainWindow();

            this._notification.BalloonTipClicked +=
                (sender, args) => this._uiCommandHub.ShowMainWindow(true);

            var options = new MenuItem(
                "Options",
                (sender, args) => this._uiCommandHub.ShowOptionWindow())
            {
                DefaultItem = false,
            };

            var menuItems = new[]
            {
                new MenuItem(
                    "Show",
                    (sender, args) => this._uiCommandHub.ShowMainWindow())
                {
                    DefaultItem = true
                },
                options,
                new MenuItem(
                    "Exit",
                    (sender, args) => this._appCommandHub.Shutdown())
            };

            this._notification.ContextMenu = new ContextMenu(menuItems);
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

            builder.RegisterType<NotificationMenuCoordinator>().AsImplementedInterfaces()
                .InstancePerLifetimeScope();
        }

        #endregion
    }
}