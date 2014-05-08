/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

namespace Papercut.Services
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using System.Windows.Threading;

    using Papercut.Core.Events;
    using Papercut.Events;

    using Application = System.Windows.Application;

    public class NotificationMenu : IDisposable, IHandleEvent<AppReadyEvent>,
        IHandleEvent<ShowBallonTip>
    {
        readonly IPublishEvent _publishEvent;

        readonly AppResourceLocator _resourceLocator;

        NotifyIcon _notification;

        public NotificationMenu(AppResourceLocator resourceLocator, IPublishEvent publishEvent)
        {
            _resourceLocator = resourceLocator;
            _publishEvent = publishEvent;
        }

        public void Handle(AppReadyEvent message)
        {
            if (_notification != null) return;

            // Set up the notification icon
            _notification = new NotifyIcon
            {
                Icon = new Icon(_resourceLocator.GetResource("App.ico").Stream),
                Text = "Papercut",
                Visible = true
            };

            _notification.Click +=
                (sender, args) => _publishEvent.Publish(new ShowMainWindowEvent());

            _notification.BalloonTipClicked += (sender, args) => _publishEvent.Publish(new ShowMainWindowEvent() { SelectMostRecentMessage = true });

            _notification.ContextMenu =
                new ContextMenu(
                    new[]
                    {
                        new MenuItem(
                            "Show",
                            (sender, args) => _publishEvent.Publish(new ShowMainWindowEvent()))
                        {
                            DefaultItem = true
                        },
                        new MenuItem(
                            "Shutdown",
                            (sender, args) => _publishEvent.Publish(new AppForceShutdownEvent()))
                    });
        }

        public void Handle(ShowBallonTip @event)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(
                new Action(
                    () =>
                    _notification.ShowBalloonTip(
                        @event.Timeout,
                        @event.TipTitle,
                        @event.TipText,
                        @event.ToolTipIcon)));
        }

        public void Dispose()
        {
            if (_notification != null)
            {
                _notification.Dispose();
                _notification = null;
            }
        }
    }
}