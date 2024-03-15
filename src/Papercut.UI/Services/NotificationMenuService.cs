// Papercut SMTP
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

using System.Drawing;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;

using Autofac.Util;

using Papercut.Common.Domain;
using Papercut.Events;

namespace Papercut.Services;

using Disposable = Disposable;

public class NotificationMenuService : Disposable,
    IUIThreadEventHandler<PapercutClientReadyEvent>,
    IUIThreadEventHandler<PapercutClientExitEvent>,
    IEventHandler<ShowBallonTip>
{
    private readonly IMessageBus _messageBus;

    private readonly AppResourceLocator _resourceLocator;

    private NotifyIcon? _notification;

    private Subject<ShowBallonTip> _notificationSubject;

    public NotificationMenuService(
        AppResourceLocator resourceLocator,
        IMessageBus messageBus)
    {
        this._resourceLocator = resourceLocator;
        this._messageBus = messageBus;
        this._notificationSubject = new Subject<ShowBallonTip>();

        this.InitObservables();
    }

    public void Handle(ShowBallonTip @event)
    {
        this._notificationSubject.OnNext(@event);
    }

    public void Handle(PapercutClientExitEvent message)
    {
        if (this._notification == null) return;

        this.Reset();
    }

    public void Handle(PapercutClientReadyEvent message)
    {
        if (this._notification != null) return;

        this.SetupNotification();
    }

    private void InitObservables()
    {
        this._notificationSubject
            .Sample(TimeSpan.FromSeconds(1), TaskPoolScheduler.Default)
            .ObserveOnDispatcher()
            .Subscribe(
                @event =>
                {
                    this._notification?.ShowBalloonTip(
                        @event.Timeout,
                        @event.TipTitle,
                        @event.TipText,
                        @event.ToolTipIcon);
                });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) this.Reset();
    }

    private void Reset()
    {
        this._notificationSubject?.Dispose();
        this._notificationSubject = null;
        this._notification?.Dispose();
        this._notification = null;
    }

    private void SetupNotification()
    {
        // Set up the notification icon
        this._notification = new NotifyIcon
                             {
                                 Icon = new Icon(this._resourceLocator.GetResource("App.ico").Stream),
                                 Text = "Papercut",
                                 Visible = true
                             };

        this._notification.Click +=
            (sender, args) => this._messageBus.Publish(new ShowMainWindowEvent());

        this._notification.BalloonTipClicked +=
            (sender, args) =>
                this._messageBus.Publish(new ShowMainWindowEvent { SelectMostRecentMessage = true });

        this._notification.ContextMenuStrip = new ContextMenuStrip();

        this._notification.ContextMenuStrip.Items.Add("Show", null, (sender, args) => this._messageBus.Publish(new ShowMainWindowEvent()));
        this._notification.ContextMenuStrip.Items.Add("Options", null, (sender, args) => this._messageBus.Publish(new ShowOptionWindowEvent()));
        this._notification.ContextMenuStrip.Items.Add("Exit", null, (sender, args) => this._messageBus.Publish(new AppForceShutdownEvent()));
    }
}