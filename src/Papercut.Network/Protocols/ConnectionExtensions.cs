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


namespace Papercut.Network.Protocols
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    using Papercut.Core.Domain.Network;

    public static class ConnectionExtensions
    {
        public static Task SendLine(this IConnection connection, string message)
        {
            connection.Logger.Debug("Sending Line {Message}", message);

            return connection.SendData(connection.Encoding.GetBytes(message + "\r\n"));
        }

        public static Task SendLine(
            this IConnection connection,
            string message,
            params object[] args)
        {
            return connection.SendLine(string.Format(message, args));
        }

        public static Task Send(
            this IConnection connection,
            string message,
            params object[] args)
        {
            return connection.SendData(connection.Encoding.GetBytes(string.Format(message, args)));
        }

        public static TOut ReadTextStream<TOut>(
            this Socket socket,
            Func<StreamReader, TOut> read)
        {
            TOut output;
            NetworkStream networkStream = null;

            try
            {
                networkStream = new NetworkStream(socket, false);
                using (var reader = new StreamReader(networkStream))
                {
                    output = read(reader);
                    networkStream.Close();
                }
            }
            finally
            {
                networkStream?.Dispose();
            }

            return output;
        }
    }
}