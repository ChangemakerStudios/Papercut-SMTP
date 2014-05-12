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

    using Papercut.Core.Events;
    using Papercut.Core.Network;
    using Papercut.Properties;

    using Serilog;

    public class SmtpServerCoordinator : IHandleEvent<AppReadyEvent>, IHandleEvent<AppExitEvent>, IHandleEvent<SmtpServerForceRebindEvent>
    {
        public SmtpServerCoordinator(Func<ServerProtocolType,IServer> serverFactory, ILogger logger, IPublishEvent publishEvent)
        {
            SmtpServer = serverFactory(ServerProtocolType.Smtp);
            Logger = logger;
            PublishEvent = publishEvent;
        }

        public IServer SmtpServer { get; set; }

        public ILogger Logger { get; set; }

        public IPublishEvent PublishEvent { get; set; }

        public void Handle(AppReadyEvent @event)
        {
            ListenSmtpServer();
        }

        public bool ListenSmtpServer()
        {
            try
            {
                SmtpServer.Stop();
                SmtpServer.Listen(Settings.Default.IP, Settings.Default.Port);
                PublishEvent.Publish(
                    new SmtpServerBoundEvent(Settings.Default.IP, Settings.Default.Port));

                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning(
                    ex,
                    "Failed to bind to the {Address} {Port} specified. The port may already be in use by another process.",
                    Settings.Default.IP,
                    Settings.Default.Port);

                PublishEvent.Publish(new SmtpServerBindFailedEvent());
            }

            return false;
        }

        public void Handle(AppExitEvent @event)
        {
            SmtpServer.Stop();
        }

        public void Handle(SmtpServerForceRebindEvent @event)
        {
            ListenSmtpServer();
        }
    }
}