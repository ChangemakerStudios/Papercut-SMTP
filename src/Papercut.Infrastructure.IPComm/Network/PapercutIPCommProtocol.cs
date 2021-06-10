// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Papercut.Common.Domain;
    using Papercut.Common.Extensions;
    using Papercut.Core;
    using Papercut.Core.Infrastructure.Json;
    using Papercut.Core.Infrastructure.MessageBus;
    using Papercut.Infrastructure.IPComm.Protocols;

    using Serilog;

    public class PapercutIPCommProtocol : StringCommandProtocol
    {
        readonly IMessageBus _messageBus;

        public PapercutIPCommProtocol(ILogger logger, IMessageBus messageBus)
            : base(logger)
        {
            this._messageBus = messageBus;
        }

        public Connection Connection { get; protected set; }

        public override async Task BeginAsync(
            Connection connection,
            CancellationToken token = default)
        {
            this.Connection = connection;
            this.Logger.ForContext("ConnectionId", this.Connection.Id);
            await this.Connection.SendLineAsync(AppConstants.ApplicationName.ToUpper());
            var response = await this.Connection.ReceiveDataAsync();
            await this.ProcessIncomingBufferAsync(response, Encoding.UTF8, token);
        }

        protected override async Task ProcessRequest(
            string incomingRequest,
            CancellationToken token = default)
        {
            try
            {
                var request = incomingRequest.FromJson<PapercutIPCommRequest>();

                this.Logger.Verbose("Incoming Request Received {@Request}", request);

                await this.Connection.SendAsync("ACK");

                if (request.CommandType.IsAny(
                    PapercutIPCommCommandType.Publish,
                    PapercutIPCommCommandType.Exchange))
                {
                    var remoteObjectBuffer = await this.Connection.ReceiveDataAsync();

                    var remoteEvent = PapercutIPCommSerializer.FromJson(
                        request.Type,
                        Encoding.UTF8.GetString(remoteObjectBuffer));

                    this.Logger.Information(
                        "Publishing Event Received {@Event} from Remote",
                        remoteEvent);

                    await this._messageBus.PublishObjectAsync(
                        remoteEvent,
                        request.Type,
                        token: token);

                    if (request.CommandType == PapercutIPCommCommandType.Exchange)
                    {
                        // send response back...
                        this.Logger.Information(
                            "Exchanging Event {@Event} -- Pushing to Remote",
                            remoteEvent);

                        await this.Connection.SendJsonAsync(request.Type, remoteEvent);
                    }
                }
            }
            catch (IOException e)
            {
                this.Logger.Error(e, "IOException received. Closing this connection.");
                this.Connection.Close();
            }
        }
    }
}