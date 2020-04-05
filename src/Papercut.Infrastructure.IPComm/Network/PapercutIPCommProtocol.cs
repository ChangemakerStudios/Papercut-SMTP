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
    using System.IO;
    using System.Net.Sockets;

    using Common;
    using Common.Domain;
    using Common.Extensions;

    using Core;
    using Core.Infrastructure.Json;
    using Core.Infrastructure.MessageBus;

    using Protocols;

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

        public override void Begin(Connection connection)
        {
            this.Connection = connection;
            this.Logger.ForContext("ConnectionId", this.Connection.Id);

            this.Connection.SendLine(AppConstants.ApplicationName.ToUpper()).Wait();
        }

        protected override void ProcessRequest(string incomingRequest)
        {
            try
            {
                var request = incomingRequest.FromJson<PapercutIPCommRequest>();

                this.Logger.Verbose("Incoming Request Received {@Request}", request);

                this.Connection.Send("ACK").Wait();

                if (request.CommandType.IsAny(PapercutIPCommCommandType.Publish, PapercutIPCommCommandType.Exchange))
                {
                    // read the rest of the object...
                    object remoteEvent = null;

                    using (var stream = new NetworkStream(this.Connection.Client, false))
                    {
                        remoteEvent = stream.ReadJsonBuffered(request.Type, request.ByteSize);
                    }

                    this.Logger.Information("Publishing Event Received {@Event} from Remote", remoteEvent);

                    this._messageBus.PublishObject(remoteEvent, request.Type);

                    if (request.CommandType == PapercutIPCommCommandType.Exchange)
                    {
                        // send response back...
                        this.Logger.Information("Exchanging Event {@Event} -- Pushing to Remote", remoteEvent);

                        this.Connection.SendJson(request.Type, remoteEvent).Wait();
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