// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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

namespace Papercut.Network.SmtpCommands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Papercut.Core.Domain.Message;
    using Papercut.Network.Protocols;

    using Serilog;

    public class DataSmtpCommand : BaseSmtpCommand
    {
        readonly ILogger _logger;

        readonly IReceivedDataHandler _receivedDataHandler;

        public DataSmtpCommand(IReceivedDataHandler receivedDataHandler, ILogger logger)
        {
            _receivedDataHandler = receivedDataHandler;
            _logger = logger;
        }

        protected override IEnumerable<string> GetMatchCommands()
        {
            return new[] { "DATA" };
        }

        protected override void Run(string command, string[] args)
        {
            // Check request order
            if (Session.Sender == null || Session.MailFrom == null || Session.Recipients.Count == 0)
            {
                Connection.SendLine("503 Bad sequence of commands");
                return;
            }

            List<string> data;
            Task confirmation;

            try
            {
                data = Connection.Client.ReadTextStream(
                    reader =>
                    {
                        var messageLines = new List<string>();

                        string line;
                        Connection.SendLine("354 Start mail input; end with <CRLF>.<CRLF>").Wait();

                        while ((line = reader.ReadLine()) != ".")
                        {
                            // reverse any dot-stuffing per RFC 2821, section 4.5.2
                            if (line?.Length > 1 && line[0] == '.') line = line.Substring(1);
                            messageLines.Add(line);
                        }

                        return messageLines;
                    });

                confirmation = Connection.SendLine("250 OK");

                _receivedDataHandler.HandleReceived(string.Join(Environment.NewLine, data), Session.Recipients.ToArray(), Connection.Encoding);
            }
            catch (IOException e)
            {
                _logger.Warning(e, "IOException received in DATA while reading message. Closing connection.");
                Connection.Close();
                return;
            }

            confirmation.Wait();
        }
    }
}