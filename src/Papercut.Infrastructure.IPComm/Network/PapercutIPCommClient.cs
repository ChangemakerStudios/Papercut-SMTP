// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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


namespace Papercut.Infrastructure.IPComm.Network
{
    using System;
    using System.Net.Sockets;
    using System.Text;

    using Common;
    using Common.Domain;

    using Core;
    using Core.Domain.Network;

    using Serilog;

    public class PapercutIPCommClient : IDisposable
    {
        private readonly EndpointDefinition _endpointDefinition;
        readonly ILogger _logger;

        public PapercutIPCommClient(EndpointDefinition endpointDefinition, ILogger logger)
        {
            _endpointDefinition = endpointDefinition;
            _logger = logger;
        }

        public EndpointDefinition Endpoint => this._endpointDefinition;


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        private T TryConnect<T>(Func<TcpClient, T> doOperation)
        {
            IAsyncResult asyncResult = null;

            var result = default(T);

            try
            {
                var client = new TcpClient();

                try
                {
                    asyncResult = client.BeginConnect(this._endpointDefinition.Address, this._endpointDefinition.Port, null, null);

                    var success = asyncResult.AsyncWaitHandle.WaitOne(100);

                    if (success)
                    {
                        result = doOperation(client);
                    }
                }
                finally
                {
                    if (asyncResult != null && client.Connected)
                    {
                        client.EndConnect(asyncResult);
                    }
                }

            }
            catch (ObjectDisposedException)
            {
                // already disposed
            }
            catch (SocketException)
            {
                // no listener
            }

            return result;
        }

        public TEvent ExchangeEventServer<TEvent>(TEvent @event) where TEvent : IEvent
        {
            TEvent DoOperation(TcpClient client)
            {
                TEvent returnEvent = default(TEvent);

                using (var stream = client.GetStream())
                {
                    _logger.Debug("Exchanging {@Event} with Remote", @event);

                    var isSuccessful = HandlePublishEvent(
                        stream,
                        @event,
                        PapercutIPCommCommandType.Exchange);

                    if (isSuccessful)
                    {
                        returnEvent = (TEvent)stream.ReadJsonBuffered(typeof(TEvent));
                    }

                    stream.Flush();

                    return returnEvent;
                }
            }

            return TryConnect(DoOperation);
        }

        public bool PublishEventServer<TEvent>(TEvent @event) where TEvent : IEvent
        {
            bool DoOperation(TcpClient client)
            {
                using (var stream = client.GetStream())
                {
                    _logger.Debug("Publishing {@Event} to Remote", @event);

                    var isSuccessful = HandlePublishEvent(
                        stream,
                        @event,
                        PapercutIPCommCommandType.Publish);

                    stream.Flush();

                    return isSuccessful;
                }
            }

            return TryConnect(DoOperation);
        }

        bool HandlePublishEvent<TEvent>(
            NetworkStream stream,
            TEvent @event,
            PapercutIPCommCommandType protocolCommandType) where TEvent : IEvent
        {
            string response = stream.ReadStringBuffered().Trim();

            if (response != AppConstants.ApplicationName.ToUpper()) return false;

            var eventJson = PapercutIPCommSerializer.ToJson(@event);

            stream.WriteLine(PapercutIPCommSerializer.ToJson(
                new PapercutIPCommRequest()
                {
                    CommandType = protocolCommandType,
                    Type = @event.GetType(),
                    ByteSize = Encoding.UTF8.GetBytes(eventJson).Length
                }));

            response = stream.ReadStringBuffered().Trim();
            if (response == "ACK") stream.WriteStr(eventJson);

            return true;
        }
    }
}