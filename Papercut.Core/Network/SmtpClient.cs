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

namespace Papercut.Core.Network
{
    #region Using

    using System.Net.Mail;
    using System.Net.Sockets;

    #endregion

    /// <summary>
    ///     The smtp client.
    /// </summary>
    public class SmtpClient : TcpClient
    {
        readonly SmtpSession _session;

        public SmtpClient(SmtpSession session)
        {
            _session = session;
        }

        public void Send()
        {
            string response;

            Connect(_session.Sender, 25);
            response = this.ReadString();
            IsValidResponse(response);

            this.WriteFormat("HELO {0}\r\n", GeneralExtensions.GetIPAddress());
            response = this.ReadString();
            IsValidResponse(response);

            this.WriteFormat("MAIL FROM:<{0}>\r\n", _session.MailFrom);
            response = this.ReadString();
            IsValidResponse(response);

            _session.Recipients.ForEach(
                address =>
                {
                    this.WriteFormat("RCPT TO:<{0}>\r\n", address);
                    response = this.ReadString();
                    IsValidResponse(response);
                });

            this.WriteFormat("DATA\r\n");
            response = this.ReadString();
            IsValidResponse(response, "354");

            this.WriteBytes(_session.Message);

            this.WriteFormat("\r\n.\r\n");
            response = this.ReadString();
            IsValidResponse(response);

            this.WriteFormat("QUIT\r\n");
            response = this.ReadString();
            if (response.IndexOf("221") == -1) throw new SmtpException(response);
        }

        static void IsValidResponse(string response, string correctResponse = "250")
        {
            if (response.Substring(0, 3) != correctResponse) throw new SmtpException(response);
        }
    }
}