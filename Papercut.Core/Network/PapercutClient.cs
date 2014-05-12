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

    using Newtonsoft.Json;

    using Papercut.Core.Events;

    using Serilog;

    public class PapercutClient : IDisposable
    {
        public const string Localhost = "127.0.0.1";

        public const int Port = 37402;

        public PapercutClient(ILogger logger)
        {
            Logger = logger;
            Client = new TcpClient();
        }

        public ILogger Logger { get; private set; }

        public TcpClient Client { get; private set; }

        public void Dispose()
        {
            Client.Close();
        }

        public bool PublishRemoteEvent<TEvent>(TEvent @event) where TEvent : IDomainEvent
        {
            try
            {
                Client.Connect(Localhost, Port);
            }
            catch (SocketException)
            {
                // no listener
                return false;
            }

            string response = Client.ReadString().Trim();
            if (response != "PAPERCUT") return false;

            Logger.Debug("Publishing {@Event} to Socket", @event);

            string json = JsonConvert.SerializeObject(@event);
            Client.WriteFormat("PUBLISH\t{0}\t{1}\r\n", @event.GetType().AssemblyQualifiedName, json.Length);
            Logger.Debug("PUBLISH\t{0:l}\t{1}\r\n", @event.GetType().AssemblyQualifiedName, json.Length);

            response = Client.ReadString().Trim();
            if (response == "READY") Client.WriteString(json);

            Client.Close();

            return true;
        }
    }
}