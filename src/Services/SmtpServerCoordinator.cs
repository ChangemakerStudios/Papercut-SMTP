// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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

    using Papercut.Core.Events;
    using Papercut.Core.Network;
    using Papercut.Events;
    using Papercut.Properties;

    using Serilog;

    public class SmtpServerCoordinator : IHandleEvent<AppReadyEvent>,
        IHandleEvent<AppExitEvent>,
        IHandleEvent<SettingsUpdatedEvent>
    {
        readonly ILogger _logger;

        readonly IPublishEvent _publishEvent;

        readonly IServer _smtpServer;

        bool _smtpServerEnabled = true;

        public SmtpServerCoordinator(
            Func<ServerProtocolType, IServer> serverFactory,
            ILogger logger,
            IPublishEvent publishEvent)
        {
            _smtpServer = serverFactory(ServerProtocolType.Smtp);
            _logger = logger;
            _publishEvent = publishEvent;
        }

        public bool SmtpServerEnabled
        {
            get { return _smtpServerEnabled; }
            set { _smtpServerEnabled = value; }
        }

        public void Handle(AppExitEvent @event)
        {
            _smtpServer.Stop();
        }

        public void Handle(AppReadyEvent @event)
        {
            if (SmtpServerEnabled) ListenSmtpServer();
        }

        public void Handle(SettingsUpdatedEvent @event)
        {
            if (SmtpServerEnabled) ListenSmtpServer();
        }

        public bool ListenSmtpServer()
        {
            try
            {
                _smtpServer.Stop();
                _smtpServer.Listen(Settings.Default.IP, Settings.Default.Port);
                _publishEvent.Publish(
                    new SmtpServerBindEvent(Settings.Default.IP, Settings.Default.Port));

                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    ex,
                    "Failed to bind to the {Address} {Port} specified. The port may already be in use by another process.",
                    Settings.Default.IP,
                    Settings.Default.Port);

                _publishEvent.Publish(new SmtpServerBindFailedEvent());
            }

            return false;
        }
    }
}