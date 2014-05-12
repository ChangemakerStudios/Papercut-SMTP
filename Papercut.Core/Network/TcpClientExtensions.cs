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
    using System;
    using System.Net.Sockets;
    using System.Text;

    public static class TcpClientExtensions
    {
        public static string ReadString(this TcpClient client)
        {
            if (client == null) throw new ArgumentNullException("client");

            const int BufferSize = 1024;
            var serverbuff = new byte[BufferSize];

            int count = client.GetStream().Read(serverbuff, 0, BufferSize);
            return count == 0 ? string.Empty : new ASCIIEncoding().GetString(serverbuff, 0, count);
        }

        public static void WriteFormat(this TcpClient client, string format, params object[] args)
        {
            if (client == null) throw new ArgumentNullException("client");

            client.WriteString(string.Format(format, args));
        }

        public static void WriteString(this TcpClient client, string str)
        {
            if (client == null) throw new ArgumentNullException("client");

            client.WriteBytes(new ASCIIEncoding().GetBytes(str));
        }

        public static void WriteBytes(this TcpClient client, byte[] data)
        {
            client.GetStream().Write(data, 0, data.Length);
        }
    }
}