// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2015 Jaben Cargman
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

namespace Papercut.Core.Network
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    using Papercut.Core.Helper;

    using Serilog;

    public class SmtpProtocol : StringCommandProtocol
    {
        protected IList<ISmtpCommand> Commands;

        public SmtpProtocol(Func<IEnumerable<ISmtpCommand>> smtpCommandsFactory, ILogger logger)
            : base(logger)
        {
            Commands = smtpCommandsFactory().ToList();
        }

        protected Connection Connection { get; set; }

        protected SmtpSession Session { get; set; }

        protected SmtpContext Context { get; set; }

        public override void Begin(Connection connection)
        {
            Connection = connection;
            _logger = _logger.ForContext("ConnectionId", Connection.Id);
            Session = new SmtpSession();
            Context = new SmtpContext(connection, Session);

            Connection.SendLine("220 {0}", NetworkHelper.GetLocalDnsHostName());
        }

        protected override void ProcessRequest(string request)
        {
            string[] parts = request.Split(' ');
            if (parts.Length == 0) return;

            var commandText = parts[0].ToUpper().Trim();
            var args = parts.Skip(1).ToArray();

            foreach (var command in Commands)
            {
                command.CommandContext = Context;
                if (command.Execute(commandText, args) == SmtpCommandResult.Done)
                {
                    //_logger.Debug("Executed {CommandType} with {CommandText} {Request}", command.GetType(), commandText, request);
                    return;
                }
            }

            Connection.SendLine("500 Command not recognized");
        }
    }
}