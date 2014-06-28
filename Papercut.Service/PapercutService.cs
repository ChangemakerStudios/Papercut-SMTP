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

namespace Papercut.Service
{
    using System;

    using Papercut.Core.Events;
    using Papercut.Core.Network;
    using Papercut.Service.Properties;

    using Serilog;

    public class PapercutService : IHandleEvent<SmtpServerBindEvent>
    {
        readonly ILogger _logger;

        readonly IServer _papercutServer;

        readonly IServer _smtpServer;

        public PapercutService(Func<ServerProtocolType, IServer> serverFactory, ILogger logger)
        {
            _logger = logger;
            _smtpServer = serverFactory(ServerProtocolType.Smtp);
            _papercutServer = serverFactory(ServerProtocolType.Papercut);
        }

        public void Handle(SmtpServerBindEvent @event)
        {
            _logger.Information(
                "Received New Smtp Server Binding Settings from UI {@Event}",
                @event);

            // update settings...
            Settings.Default.IP = @event.IP;
            Settings.Default.Port = @event.Port;
            Settings.Default.Save();

            // rebind the server...
            BindSMTPServer();
        }

        public void Start()
        {
            BindSMTPServer();
            BindPapercutServer();
        }

        void BindSMTPServer()
        {
            _smtpServer.Stop();
            _smtpServer.Listen(Settings.Default.IP, Settings.Default.Port);
        }

        void BindPapercutServer()
        {
            _papercutServer.Stop();
            _papercutServer.Listen(PapercutClient.Localhost, PapercutClient.ServerPort);
        }

        public void Stop()
        {
            _smtpServer.Stop();
            _papercutServer.Stop();
        }
    }
}