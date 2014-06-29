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
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    public static class ConnectionExtensions
    {
        public static TOut ReadTextStream<TOut>(
            this Socket socket,
            Func<StreamReader, TOut> read)
        {
            TOut output;

            using (var networkStream = new NetworkStream(socket, false))
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

        public static Task<int> SendLine(this Connection connection, string message)
        {
            connection.Logger.Debug("Sending {Message}", message);
            return connection.Send(Encoding.ASCII.GetBytes(message + "\r\n"));
        }

        public static Task<int> SendLine(
            this Connection connection,
            string message,
            params object[] args)
        {
            return connection.SendLine(string.Format(message, args));
        }

        public static Task<int> Send(
            this Connection connection,
            string message,
            params object[] args)
        {
            return connection.Send(Encoding.ASCII.GetBytes(string.Format(message, args)));
        }

        public static Task<int> Send(this Connection connection, byte[] data)
        {
            connection.Logger.Debug("Sending byte[] length of {ByteArrayLength}", data.Length);

            return connection.Client.Send(data, 0, data.Length);
        }

        public static Task<int> Send(
            this Socket socket,
            byte[] buffer,
            int offset,
            int size,
            SocketFlags flags = SocketFlags.None)
        {
            AsyncCallback nullOp = i => { };
            IAsyncResult result = socket.BeginSend(buffer, offset, size, flags, nullOp, socket);

            // Use overload that takes an IAsyncResult directly
            try
            {
                return Task.Factory.FromAsync(result, r => socket.Connected ? socket.EndSend(r) : 0);
            }
            catch (ObjectDisposedException)
            {
                // sometimes happens when the socket has already been closed.   
            }

            return null;
        }
    }
}