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
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    using Papercut.Core;

    public static class ConnectionExtensions
    {
        public static TOut ReadStream<TOut>(this IClient client, Func<StreamReader, TOut> read)
        {
            TOut output;

            using (Stream networkStream = new NetworkStream(client.Client, false))
            {
                using (var reader = new StreamReader(networkStream))
                {
                    output = read(reader);
                    reader.Close();
                }

                networkStream.Close();
            }

            return output;
        }

        public static Task<int> Send(this IClient client, string message)
        {
            Logger.WriteDebug("Sending: " + message, client.Id);
            return client.Send(Encoding.ASCII.GetBytes(message + "\r\n"));
        }

        public static Task<int> Send(this IClient client, string message, params object[] args)
        {
            return client.Send(string.Format(message, args));
        }

        public static Task<int> Send(this IClient client, byte[] data)
        {
            Logger.WriteDebug("Sending byte array of " + data.Length + " bytes");
            return client.Send(data, 0, data.Length);
        }

        public static Task<int> Send(
            this IClient client,
            byte[] buffer,
            int offset,
            int size,
            SocketFlags flags = SocketFlags.None)
        {
            AsyncCallback nullOp = i => { };
            IAsyncResult result = client.Client.BeginSend(
                buffer,
                offset,
                size,
                flags,
                nullOp,
                client.Client);

            // Use overload that takes an IAsyncResult directly
            return Task.Factory.FromAsync(result, r => client.Client.EndSend(r));
        }
    }
}