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
    using System.Net.Sockets;

    using Papercut.Core.Events;
    using Papercut.Core.Helper;

    using Serilog;

    public class PapercutClient : IDisposable
    {
        public const string Localhost = "127.0.0.1";

        public const int UIPort = 37402;

        public const int ServerPort = 37403;

        readonly ILogger _logger;

        public PapercutClient(ILogger logger)
        {
            _logger = logger;
            Client = new TcpClient();
            Host = Localhost;
            Port = UIPort;
        }

        public string Host { get; set; }

        public int Port { get; set; }

        public TcpClient Client { get; private set; }

        public void Dispose()
        {
            Client.Close();
        }

        public bool ExchangeEventServer<TEvent>(ref TEvent @event) where TEvent : IDomainEvent
        {
            try
            {
                Client.Connect(Host, Port);
            }
            catch (SocketException)
            {
                // no listener
                return false;
            }

            try
            {
                using (var stream = Client.GetStream())
                {
                    _logger.Debug("Exchanging {@Event} with Remote", @event);

                    var isSuccessful = HandlePublishEvent(
                        stream,
                        @event,
                        ProtocolCommandType.Exchange);

                    if (isSuccessful)
                    {
                        var response = stream.ReadString().Trim();
                        if (response != "REPLY") isSuccessful = false;
                        else
                        {
                            // get exchanged event
                            @event = stream.ReadString().FromJson<TEvent>();
                        }
                    }

                    stream.Flush();
                    stream.Close();

                    return isSuccessful;
                }
            }
            finally
            {
                Client.Close();
            }
        }

        public bool PublishEventServer<TEvent>(TEvent @event) where TEvent : IDomainEvent
        {
            try
            {
                Client.Connect(Host, Port);
            }
            catch (SocketException)
            {
                // no listener
                return false;
            }

            try
            {
                using (var stream = Client.GetStream())
                {
                    _logger.Debug("Publishing {@Event} to Remote", @event);

                    var isSuccessful = HandlePublishEvent(
                        stream,
                        @event,
                        ProtocolCommandType.Publish);

                    stream.Flush();
                    stream.Close();

                    return isSuccessful;
                }
            }
            finally
            {
                Client.Close();
            }
        }

        bool HandlePublishEvent<TEvent>(
            NetworkStream stream,
            TEvent @event,
            ProtocolCommandType protocolCommandType) where TEvent : IDomainEvent
        {
            string response = stream.ReadString().Trim();

            if (response != "PAPERCUT") return false;

            _logger.Debug("Publishing {@Event} to Remote", @event);

            var eventJson = @event.ToJson();

            stream.WriteLine(
                new PapercutProtocolRequest
                {
                    CommandType = protocolCommandType,
                    Type = @event.GetType(),
                    ByteSize = eventJson.Length
                }.ToJson());

            response = stream.ReadString().Trim();
            if (response == "ACK") stream.WriteStr(eventJson);

            return true;
        }
    }
}