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

namespace Papercut.Core.Network
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;

    using Papercut.Core.Events;
    using Papercut.Core.Message;

    using Serilog;

    public class SmtpProtocol : StringCommandProtocol
    {
        readonly MessageRepository _messageRepository;

        readonly IPublishEvent _publishEvent;

        public SmtpProtocol(MessageRepository messageRepository, IPublishEvent publishEvent, ILogger logger)
            : base(logger)
        {
            _messageRepository = messageRepository;
            _publishEvent = publishEvent;
        }

        public Connection Connection { get; protected set; }

        public SmtpSession Session { get; protected set; }

        public override void Begin(Connection connection)
        {
            Connection = connection;
            _logger = _logger.ForContext("ConnectionId", Connection.Id);
            Session = new SmtpSession();
            Connection.SendLine("220 {0}", Dns.GetHostName().ToLower());
        }

        protected override void ProcessRequest(string request)
        {
            string[] parts = request.Split(' ');

            switch (parts[0].ToUpper())
            {
                case "HELO":
                    HELO(parts);
                    break;

                case "EHLO":
                    EHLO(parts);
                    break;

                case "SEND":
                case "SOML":
                case "SAML":
                case "MAIL":
                    MAIL(parts);
                    break;

                case "RCPT":
                    RCPT(parts);
                    break;

                case "DATA":
                    DATA();
                    break;

                case "VRFY":
                    Connection.SendLine(
                        "252 Cannot VRFY user, but will accept message and attempt delivery");
                    break;

                case "EXPN":
                    Connection.SendLine("252 Cannot expand upon list");
                    break;

                case "RSET":
                    Session.Reset();
                    Connection.SendLine("250 OK");
                    break;

                case "NOOP":
                    Connection.SendLine("250 OK");
                    break;

                case "QUIT":
                    Connection.SendLine("221 Goodbye!");
                    Connection.Close();
                    break;

                case "HELP":
                case "TURN":
                    Connection.SendLine("502 Command not implemented");
                    break;

                default:
                    Connection.SendLine("500 Command not recognized");
                    break;
            }
        }

        void DATA()
        {
            // Check request order
            if (Session.Sender == null || Session.MailFrom == null || Session.Recipients.Count == 0)
            {
                Connection.SendLine("503 Bad sequence of commands");
                return;
            }

            string file;

            try
            {
                List<string> output = Connection.Client.ReadTextStream(
                    reader =>
                    {
                        var messageLines = new List<string>();

                        string line;
                        Connection.SendLine("354 Start mail input; end with <CRLF>.<CRLF>").Wait();

                        while ((line = reader.ReadLine()) != ".")
                        {
                            // reverse any dot-stuffing per RFC 2821, section 4.5.2
                            if (line.StartsWith(".") && line.Length > 1) line = line.Substring(1);

                            messageLines.Add(line);
                        }

                        return messageLines;
                    });

                file = _messageRepository.SaveMessage(output);
            }
            catch (IOException e)
            {
                _logger.Warning(e, "IOException received in DATA while reading message. Closing connection.");
                Connection.Close();
                return;
            }

            Connection.SendLine("250 OK").Wait();

            if (!string.IsNullOrWhiteSpace(file))
            {
                _publishEvent.Publish(new NewMessageEvent(new MessageEntry(file)));
            }
        }

        void EHLO(string[] parts)
        {
            Session.Sender = parts.Length < 2 ? string.Empty : parts[1];
            Connection.SendLine("250-{0}", Dns.GetHostName().ToLower());
            Connection.SendLine("250-8BITMIME");
            Connection.SendLine("250 OK");
        }

        void HELO(string[] parts)
        {
            Session.Sender = parts.Length < 2 ? string.Empty : parts[1];
            Connection.SendLine("250 {0}", Dns.GetHostName().ToLower());
        }

        void MAIL(string[] parts)
        {
            string line = string.Join(" ", parts);

            // Check for the right number of parameters
            if (parts.Length < 2)
            {
                Connection.SendLine("504 Command parameter not implemented");
                return;
            }

            // Check for the ":"
            if (!parts[1].ToUpper().StartsWith("FROM") || !line.Contains(":"))
            {
                Connection.SendLine("504 Command parameter not implemented");
                return;
            }

            // Check request order
            if (Session.Sender == null)
            {
                Connection.SendLine("503 Bad sequence of commands");
                return;
            }

            // Set the from settings
            Session.Reset();

            string address =
                line.Substring(line.IndexOf(":") + 1)
                    .Replace("<", string.Empty)
                    .Replace(">", string.Empty)
                    .Trim();

            Session.MailFrom = address;

            // Check for encoding
            foreach (string part in parts.Where(part => part.ToUpper().StartsWith("BODY=")))
            {
                switch (part.ToUpper().Replace("BODY=", string.Empty).Trim())
                {
                    case "8BITMIME":
                        Session.UseUtf8 = true;
                        break;
                    default:
                        Session.UseUtf8 = false;
                        break;
                }

                break;
            }

            Connection.SendLine("250 <{0}> OK", address);
        }

        void RCPT(string[] parts)
        {
            string line = string.Join(" ", parts);

            // Check for the ":"
            if (!line.ToUpper().StartsWith("RCPT TO") || !line.Contains(":"))
            {
                Connection.SendLine("504 Command parameter not implemented");
                return;
            }

            // Check request order
            if (Session.Sender == null || Session.MailFrom == null)
            {
                Connection.SendLine("503 Bad sequence of commands");
                return;
            }

            string address =
                line.Substring(line.IndexOf(":") + 1)
                    .Replace("<", string.Empty)
                    .Replace(">", string.Empty)
                    .Trim();
            if (!Session.Recipients.Contains(address)) Session.Recipients.Add(address);

            Connection.SendLine("250 <{0}> OK", address);
        }
    }
}