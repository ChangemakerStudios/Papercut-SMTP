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
            this.Connection = connection;
            this.Session = new SmtpSession();
            this.Connection.Send("220 {0}", Dns.GetHostName().ToLower());
        }

        public void Process(object data)
        {
            var bytes = data as byte[];

            if (bytes != null)
            {
                this.Session.Message = bytes;
                this.Connection.Send("250 OK");
            }
            else
            {
                // Command mode
                var command = (string)data;
                string[] parts = command.Split(' ');

                switch (parts[0].ToUpper())
                {
                    case "HELO":
                        this.HELO(parts);
                        break;

                    case "EHLO":
                        this.EHLO(parts);
                        break;

                    case "SEND":
                    case "SOML":
                    case "SAML":
                    case "MAIL":
                        this.MAIL(parts);
                        break;

                    case "RCPT":
                        this.RCPT(parts);
                        break;

                    case "DATA":
                        this.DATA();
                        break;

                    case "VRFY":
                        this.Connection.Send("252 Cannot VRFY user, but will accept message and attempt delivery");
                        break;

                    case "EXPN":
                        this.Connection.Send("252 Cannot expand upon list");
                        break;

                    case "RSET":
                        this.Session.Reset();
                        this.Connection.Send("250 OK");
                        break;

                    case "NOOP":
                        this.Connection.Send("250 OK");
                        break;

                    case "QUIT":
                        this.Connection.Send("221 Goodbye!");
                        this.Connection.Close();
                        break;

                    case "HELP":
                    case "TURN":
                        this.Connection.Send("502 Command not implemented");
                        break;

                    default:
                        this.Connection.Send("500 Command not recognized");
                        break;
                }
            }
        }

        #endregion

        #region Methods

        private void DATA()
        {
            // Check command order
            if (this.Session.Sender == null || this.Session.MailFrom == null || this.Session.Recipients.Count == 0)
            {
                this.Connection.Send("503 Bad sequence of commands");
                return;
            }

            string file = null;

            try
            {
                Stream networkStream = new NetworkStream(this.Connection.Client, false);

                var output = new List<string>();

                using (var reader = new StreamReader(networkStream))
                {
                    string line;
                    this.Connection.Send("354 Start mail input; end with <CRLF>.<CRLF>").AsyncWaitHandle.WaitOne();

                    while ((line = reader.ReadLine()) != ".")
                    {
                        // reverse any dot-stuffing per RFC 2821, section 4.5.2
                        if (line.StartsWith(".") && line.Length > 1)
                        {
                            line = line.Substring(1);
                        }

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
                    this.Connection.ConnectionId);

                this.Connection.Close();
                return;
            }

            this.Connection.Send("250 OK");
        }

        private void EHLO(string[] parts)
        {
            this.Session.Sender = parts.Length < 2 ? string.Empty : parts[1];
            this.Connection.Send("250-{0}", Dns.GetHostName().ToLower());
            this.Connection.Send("250-8BITMIME");
            this.Connection.Send("250 OK");
        }

        private void HELO(string[] parts)
        {
            this.Session.Sender = parts.Length < 2 ? string.Empty : parts[1];
            this.Connection.Send("250 {0}", Dns.GetHostName().ToLower());
        }

        private void MAIL(string[] parts)
        {
            string line = string.Join(" ", parts);

            // Check for the right number of parameters
            if (parts.Length < 2)
            {
                this.Connection.Send("504 Command parameter not implemented");
                return;
            }

            // Check for the ":"
            if (!parts[1].ToUpper().StartsWith("FROM") || !line.Contains(":"))
            {
                this.Connection.Send("504 Command parameter not implemented");
                return;
            }

            // Check command order
            if (this.Session.Sender == null)
            {
                this.Connection.Send("503 Bad sequence of commands");
                return;
            }

            // Set the from settings
            this.Session.Reset();

            string address =
                line.Substring(line.IndexOf(":") + 1).Replace("<", string.Empty).Replace(">", string.Empty).Trim();

            this.Session.MailFrom = address;

            // Check for encoding
            foreach (string part in parts.Where(part => part.ToUpper().StartsWith("BODY=")))
            {
                switch (part.ToUpper().Replace("BODY=", string.Empty).Trim())
                {
                    case "8BITMIME":
                        this.Session.UseUtf8 = true;
                        break;
                    default:
                        this.Session.UseUtf8 = false;
                        break;
                }

                break;
            }

            this.Connection.Send("250 <{0}> OK", address);
        }

        private void RCPT(string[] parts)
        {
            string line = string.Join(" ", parts);

            // Check for the ":"
            if (!line.ToUpper().StartsWith("RCPT TO") || !line.Contains(":"))
            {
                this.Connection.Send("504 Command parameter not implemented");
                return;
            }

            // Check command order
            if (this.Session.Sender == null || this.Session.MailFrom == null)
            {
                this.Connection.Send("503 Bad sequence of commands");
                return;
            }

            string address =
                line.Substring(line.IndexOf(":") + 1).Replace("<", string.Empty).Replace(">", string.Empty).Trim();
            if (!this.Session.Recipients.Contains(address))
            {
                this.Session.Recipients.Add(address);
            }

            this.Connection.Send("250 <{0}> OK", address);
        }

        #endregion
    }
}