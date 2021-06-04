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
    using System.Drawing;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using Autofac.Util;

    using Papercut.Common.Domain;
    using Papercut.Core;
    using Papercut.Core.Annotations;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Events;
    using Papercut.Infrastructure.Resources;

    [UsedImplicitly]
    public class NotificationMenuService : Disposable,
        IUIThreadEventHandler<PapercutClientReadyEvent>,
        IUIThreadEventHandler<PapercutClientExitEvent>,
        IEventHandler<ShowBallonTip>
    {
        readonly IMessageBus _messageBus;

        readonly AppResourceLocator _resourceLocator;

        NotifyIcon _notification;

        Subject<ShowBallonTip> _notificationSubject;

        public NotificationMenuService(
            AppResourceLocator resourceLocator,
            IMessageBus messageBus)
        {
            this._resourceLocator = resourceLocator;
            this._messageBus = messageBus;
            this._notificationSubject = new Subject<ShowBallonTip>();

            this.InitObservables();
        }

        public Task HandleAsync(ShowBallonTip @event)
        {
            this._notificationSubject.OnNext(@event);

            return Task.CompletedTask;
        }

        public Task HandleAsync(PapercutClientExitEvent message)
        {
            if (this._notification == null) return Task.CompletedTask;

            this.Reset();

            return Task.CompletedTask;
        }

        public Task HandleAsync(PapercutClientReadyEvent message)
        {
            if (this._notification == null) this.SetupNotification();

            return Task.CompletedTask;
        }

        void InitObservables()
        {
            this._notificationSubject
                .Sample(TimeSpan.FromSeconds(1), TaskPoolScheduler.Default)
                .ObserveOnDispatcher()
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
            this._notificationSubject?.Dispose();
            this._notificationSubject = null;
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
                (sender, args) => this._messageBus.Publish(new ShowMainWindowEvent());

            this._notification.BalloonTipClicked +=
                (sender, args) =>
                this._messageBus.Publish(new ShowMainWindowEvent { SelectMostRecentMessage = true });

            var options = new MenuItem(
                "Options",
                (sender, args) => this._messageBus.Publish(new ShowOptionWindowEvent()))
            {
                DefaultItem = false,
            };

            var menuItems = new[]
            {
                new MenuItem(
                    "Show",
                    (sender, args) => this._messageBus.Publish(new ShowMainWindowEvent()))
                {
                    DefaultItem = true
                },
                options,
                new MenuItem(
                    "Exit",
                    (sender, args) => this._messageBus.Publish(new AppForceShutdownEvent()))
            };

            this._notification.ContextMenu = new ContextMenu(menuItems);
        }
    }
}