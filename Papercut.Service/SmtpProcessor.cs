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

namespace Papercut.Service
{
    #region Using

    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;

    using Autofac;

    using Papercut.Core;
    using Papercut.Core.Message;

    #endregion

    /// <summary>
    ///     The processor.
    /// </summary>
    public class SmtpProcessor : IDataProcessor
    {
        #region Public Properties

        public IConnection Connection { get; protected set; }

        public SmtpSession Session { get; protected set; }

        #endregion

        #region Public Methods and Operators

        public void Begin(IConnection connection)
        {
            Connection = connection;
            Session = new SmtpSession();
            Connection.Send("220 {0}", Dns.GetHostName().ToLower());
        }

        public void Process(object data)
        {
            var bytes = data as byte[];

            if (bytes != null)
            {
                Session.Message = bytes;
                Connection.Send("250 OK");
            }
            else
            {
                // Command mode
                var command = (string)data;
                string[] parts = command.Split(' ');

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
                        Connection.Send("252 Cannot VRFY user, but will accept message and attempt delivery");
                        break;

                    case "EXPN":
                        Connection.Send("252 Cannot expand upon list");
                        break;

                    case "RSET":
                        Session.Reset();
                        Connection.Send("250 OK");
                        break;

                    case "NOOP":
                        Connection.Send("250 OK");
                        break;

                    case "QUIT":
                        Connection.Send("221 Goodbye!");
                        Connection.Close();
                        break;

                    case "HELP":
                    case "TURN":
                        Connection.Send("502 Command not implemented");
                        break;

                    default:
                        Connection.Send("500 Command not recognized");
                        break;
                }
            }
        }

        #endregion

        #region Methods

        void DATA()
        {
            // Check command order
            if (Session.Sender == null || Session.MailFrom == null || Session.Recipients.Count == 0)
            {
                Connection.Send("503 Bad sequence of commands");
                return;
            }

            string file = null;

            try
            {
                Stream networkStream = new NetworkStream(Connection.Client, false);

                var output = new List<string>();

                using (var reader = new StreamReader(networkStream))
                {
                    string line;
                    Connection.Send("354 Start mail input; end with <CRLF>.<CRLF>").AsyncWaitHandle.WaitOne();

                    while ((line = reader.ReadLine()) != ".")
                    {
                        // reverse any dot-stuffing per RFC 2821, section 4.5.2
                        if (line.StartsWith(".") && line.Length > 1) line = line.Substring(1);

                        output.Add(line);
                    }

                    reader.Close();
                }

                file = PapercutContainer.Instance.Resolve<MessageRepository>().SaveMessage(output);
            }
            catch (IOException e)
            {
                Logger.WriteWarning(
                    "IOException received in Processor.DATA while reading message.  Closing this.Connection.  Message: "
                    + e.Message,
                    Connection.ConnectionId);

                Connection.Close();
                return;
            }

            Connection.Send("250 OK");
        }

        void EHLO(string[] parts)
        {
            Session.Sender = parts.Length < 2 ? string.Empty : parts[1];
            Connection.Send("250-{0}", Dns.GetHostName().ToLower());
            Connection.Send("250-8BITMIME");
            Connection.Send("250 OK");
        }

        void HELO(string[] parts)
        {
            Session.Sender = parts.Length < 2 ? string.Empty : parts[1];
            Connection.Send("250 {0}", Dns.GetHostName().ToLower());
        }

        void MAIL(string[] parts)
        {
            string line = string.Join(" ", parts);

            // Check for the right number of parameters
            if (parts.Length < 2)
            {
                Connection.Send("504 Command parameter not implemented");
                return;
            }

            // Check for the ":"
            if (!parts[1].ToUpper().StartsWith("FROM") || !line.Contains(":"))
            {
                Connection.Send("504 Command parameter not implemented");
                return;
            }

            // Check command order
            if (Session.Sender == null)
            {
                Connection.Send("503 Bad sequence of commands");
                return;
            }

            // Set the from settings
            Session.Reset();

            string address =
                line.Substring(line.IndexOf(":") + 1).Replace("<", string.Empty).Replace(">", string.Empty).Trim();

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

            Connection.Send("250 <{0}> OK", address);
        }

        void RCPT(string[] parts)
        {
            string line = string.Join(" ", parts);

            // Check for the ":"
            if (!line.ToUpper().StartsWith("RCPT TO") || !line.Contains(":"))
            {
                Connection.Send("504 Command parameter not implemented");
                return;
            }

            // Check command order
            if (Session.Sender == null || Session.MailFrom == null)
            {
                Connection.Send("503 Bad sequence of commands");
                return;
            }

            string address =
                line.Substring(line.IndexOf(":") + 1).Replace("<", string.Empty).Replace(">", string.Empty).Trim();
            if (!Session.Recipients.Contains(address)) Session.Recipients.Add(address);

            Connection.Send("250 <{0}> OK", address);
        }

        #endregion
    }
}