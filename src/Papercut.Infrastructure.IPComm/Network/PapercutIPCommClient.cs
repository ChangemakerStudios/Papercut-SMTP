// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


using System.Net.Sockets;

using Papercut.Common.Domain;
using Papercut.Core;
using Papercut.Core.Domain.Network;

namespace Papercut.Infrastructure.IPComm.Network
{
    public class PapercutIPCommClient(EndpointDefinition endpointDefinition, ILogger logger)
    {
        public EndpointDefinition Endpoint { get; } = endpointDefinition;

        private async Task<T> TryConnect<T>(Func<TcpClient, Task<T>> doOperation, TimeSpan connectTimeout)
        {
            using var source = new CancellationTokenSource(connectTimeout);
            using var client = new TcpClient();

            try
            {
                await client.ConnectAsync(
                    this.Endpoint.Address,
                    this.Endpoint.Port,
                    source.Token);

                if (client.Connected)
                {
                    return await doOperation(client);
                }
            }
            catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                // already disposed or no listener
            }
            catch (Exception e)
            {
                logger.Information(e, "Caught IP Comm Client Exception");
            }

            return default;
        }

        public async Task<TEvent?> ExchangeEventServer<TEvent>(TEvent @event, TimeSpan connectTimeout) where TEvent : IEvent
        {
            async Task<TEvent?> DoOperation(TcpClient client)
            {
                TEvent? returnEvent = default;

                await using var stream = client.GetStream();
                logger.Debug("Exchanging {@Event} with Remote", @event);

                var isSuccessful = await this.HandlePublishEvent(
                                       stream,
                                       @event,
                                       PapercutIPCommCommandType.Exchange);

                if (isSuccessful)
                {
                    returnEvent = (TEvent)await stream.ReadJsonBufferedAsync(typeof(TEvent));
                }

                await stream.FlushAsync();

                return returnEvent;
            }

            return await this.TryConnect(DoOperation, connectTimeout);
        }

        public async Task<bool> PublishEventServer<TEvent>(TEvent @event, TimeSpan connectTimeout) where TEvent : IEvent
        {
            async Task<bool> DoOperation(TcpClient client)
            {
                await using var stream = client.GetStream();
                logger.Debug("Publishing {@Event} to Remote", @event);

                var isSuccessful = await this.HandlePublishEvent(
                                       stream,
                                       @event,
                                       PapercutIPCommCommandType.Publish);

                await stream.FlushAsync();

                return isSuccessful;
            }

            return await this.TryConnect(DoOperation, connectTimeout);
        }

        async Task<bool> HandlePublishEvent<TEvent>(
            NetworkStream stream,
            TEvent @event,
            PapercutIPCommCommandType protocolCommandType) where TEvent : IEvent
        {
            string response = (await stream.ReadStringBufferedAsync()).Trim();

            if (response != AppConstants.ApplicationName.ToUpper()) return false;

            var eventJson = PapercutIPCommSerializer.ToJson(@event);

            var requestJson = PapercutIPCommSerializer.ToJson(
                new PapercutIPCommRequest()
                {
                    CommandType = protocolCommandType,
                    Type = @event.GetType(),
                    ByteSize = Encoding.UTF8.GetBytes(eventJson).Length
                });

            await stream.WriteLineAsync(requestJson);

            response = (await stream.ReadStringBufferedAsync()).Trim();
            if (response == "ACK") await stream.WriteStrAsync(eventJson);

            return true;
        }
    }
}