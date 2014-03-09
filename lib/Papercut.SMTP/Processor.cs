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

namespace Papercut.SMTP
{
    #region Using

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    using Papercut.SMTP;

    #endregion

    /// <summary>
    /// The processor.
    /// </summary>
    public class Processor
    {
        #region Events

        /// <summary>
        ///   The message received.
        /// </summary>
        public static event EventHandler<MessageEventArgs> MessageReceived;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The on message received.
        /// </summary>
        /// <param name="connection">
        /// The connection. 
        /// </param>
        /// <param name="file">
        /// The file. 
        /// </param>
        public static void OnMessageReceived(Connection connection, string file)
        {
            if (MessageReceived != null)
            {
                MessageReceived(connection, new MessageEventArgs(new MessageEntry(file)));
            }
        }

        /// <summary>
        /// The process command.
        /// </summary>
        /// <param name="connection">
        /// The connection. 
        /// </param>
        /// <param name="data">
        /// The data. 
        /// </param>
        public static void ProcessCommand(Connection connection, object data)
        {
            var bytes = data as byte[];

            if (bytes != null)
            {
                connection.Session.Message = bytes;
                connection.Send("250 OK");
            }
            else
            {
                // Command mode
                var command = (string)data;
                string[] parts = command.Split(' ');

                switch (parts[0].ToUpper())
                {
                    case "HELO":
                        HELO(connection, parts);
                        break;

                    case "EHLO":
                        EHLO(connection, parts);
                        break;

                    case "SEND":
                    case "SOML":
                    case "SAML":
                    case "MAIL":
                        MAIL(connection, parts);
                        break;

                    case "RCPT":
                        RCPT(connection, parts);
                        break;

                    case "DATA":
                        DATA(connection);
                        break;

                    case "VRFY":
                        connection.Send("252 Cannot VRFY user, but will accept message and attempt delivery");
                        break;

                    case "EXPN":
                        connection.Send("252 Cannot expand upon list");
                        break;

                    case "RSET":
                        connection.Session.Reset();
                        connection.Send("250 OK");
                        break;

                    case "NOOP":
                        connection.Send("250 OK");
                        break;

                    case "QUIT":
                        connection.Send("221 Goodbye!");
                        connection.Close();
                        break;

                    case "HELP":
                    case "TURN":
                        connection.Send("502 Command not implemented");
                        break;

                    default:
                        connection.Send("500 Command not recognized");
                        break;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The data.
        /// </summary>
        /// <param name="connection">
        /// The connection. 
        /// </param>
        private static void DATA(Connection connection)
        {
            // Check command order
            if (connection.Session.Sender == null || connection.Session.MailFrom == null
                    || connection.Session.Recipients.Count == 0)
            {
                connection.Send("503 Bad sequence of commands");
                return;
            }

            string file = null;

            try
            {
                Stream networkStream = new NetworkStream(connection.Client, false);

                var output = new List<string>();

                using (var reader = new StreamReader(networkStream))
                {
                    string line;
                    connection.Send("354 Start mail input; end with <CRLF>.<CRLF>").AsyncWaitHandle.WaitOne();

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

                file = MessageFileService.SaveMessage(output);
            }
            catch (IOException e)
            {
                Logger.WriteWarning(
                    "IOException received in Processor.DATA while reading message.  Closing connection.  Message: " + e.Message,
                    connection.ConnectionId);
                connection.Close();
                return;
            }

            OnMessageReceived(connection, file);

            connection.Send("250 OK");
        }

        /// <summary>
        /// The ehlo.
        /// </summary>
        /// <param name="connection">
        /// The connection. 
        /// </param>
        /// <param name="parts">
        /// The parts. 
        /// </param>
        private static void EHLO(Connection connection, string[] parts)
        {
            connection.Session.Sender = parts.Length < 2 ? string.Empty : parts[1];
            connection.Send("250-{0}", Dns.GetHostName().ToLower());
            connection.Send("250-8BITMIME");
            connection.Send("250 OK");
        }

        /// <summary>
        /// The helo.
        /// </summary>
        /// <param name="connection">
        /// The connection. 
        /// </param>
        /// <param name="parts">
        /// The parts. 
        /// </param>
        private static void HELO(Connection connection, string[] parts)
        {
            connection.Session.Sender = parts.Length < 2 ? string.Empty : parts[1];
            connection.Send("250 {0}", Dns.GetHostName().ToLower());
        }

        /// <summary>
        /// The mail.
        /// </summary>
        /// <param name="connection">
        /// The connection. 
        /// </param>
        /// <param name="parts">
        /// The parts. 
        /// </param>
        private static void MAIL(Connection connection, string[] parts)
        {
            string line = string.Join(" ", parts);

            // Check for the right number of parameters
            if (parts.Length < 2)
            {
                connection.Send("504 Command parameter not implemented");
                return;
            }

            // Check for the ":"
            if (!parts[1].ToUpper().StartsWith("FROM") || !line.Contains(":"))
            {
                connection.Send("504 Command parameter not implemented");
                return;
            }

            // Check command order
            if (connection.Session.Sender == null)
            {
                connection.Send("503 Bad sequence of commands");
                return;
            }

            // Set the from settings
            connection.Session.Reset();
            string address = line.Substring(line.IndexOf(":") + 1).Replace("<", string.Empty).Replace(">", string.Empty).Trim();
            connection.Session.MailFrom = address;

            // Check for encoding
            foreach (string part in parts.Where(part => part.ToUpper().StartsWith("BODY=")))
            {
                switch (part.ToUpper().Replace("BODY=", string.Empty).Trim())
                {
                    case "8BITMIME":
                        connection.Session.UseUtf8 = true;
                        break;
                    default:
                        connection.Session.UseUtf8 = false;
                        break;
                }

                break;
            }

            connection.Send("250 <{0}> OK", address);
        }

        /// <summary>
        /// The rcpt.
        /// </summary>
        /// <param name="connection">
        /// The connection. 
        /// </param>
        /// <param name="parts">
        /// The parts. 
        /// </param>
        private static void RCPT(Connection connection, string[] parts)
        {
            string line = string.Join(" ", parts);

            // Check for the ":"
            if (!line.ToUpper().StartsWith("RCPT TO") || !line.Contains(":"))
            {
                connection.Send("504 Command parameter not implemented");
                return;
            }

            // Check command order
            if (connection.Session.Sender == null || connection.Session.MailFrom == null)
            {
                connection.Send("503 Bad sequence of commands");
                return;
            }

            string address = line.Substring(line.IndexOf(":") + 1).Replace("<", string.Empty).Replace(">", string.Empty).Trim();
            if (!connection.Session.Recipients.Contains(address))
            {
                connection.Session.Recipients.Add(address);
            }

            connection.Send("250 <{0}> OK", address);
        }

        #endregion
    }

    /// <summary>
    /// The message event args.
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        #region Constants and Fields

        /// <summary>
        ///   The entry.
        /// </summary>
        public MessageEntry Entry;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageEventArgs"/> class.
        /// </summary>
        /// <param name="entry">
        /// The entry. 
        /// </param>
        public MessageEventArgs(MessageEntry entry)
        {
            this.Entry = entry;
        }

        #endregion
    }
}