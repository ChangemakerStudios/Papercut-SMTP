// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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


using Papercut.Common.Domain;
using Papercut.Common.Extensions;
using Papercut.Core;
using Papercut.Core.Infrastructure.Json;
using Papercut.Core.Infrastructure.MessageBus;
using Papercut.Infrastructure.IPComm.Protocols;

namespace Papercut.Infrastructure.IPComm.Network;

public class PapercutIPCommProtocol(ILogger logger, IMessageBus messageBus) : StringCommandProtocol(logger)
{
    public Connection? Connection { get; protected set; }

    public override async Task BeginAsync(
        Connection connection,
        CancellationToken token = default)
    {
        Connection = connection;
        Logger.ForContext("ConnectionId", Connection.Id);
        await Connection.SendLineAsync(AppConstants.ApplicationName.ToUpper());
        var response = await Connection.ReceiveDataAsync();
        await ProcessIncomingBufferAsync(response, Encoding.UTF8, token);
    }

    protected override async Task ProcessRequest(
        string incomingRequest,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(Connection, nameof(Connection));

        try
        {
            var request = incomingRequest.FromJson<PapercutIPCommRequest>();

            Logger.Verbose("Incoming Request Received {@Request}", request);

            await Connection.SendAsync("ACK");

            if (request.CommandType.IsAny(
                    PapercutIPCommCommandType.Publish,
                    PapercutIPCommCommandType.Exchange))
            {
                var remoteObjectBuffer = await Connection.ReceiveDataAsync();

                if (remoteObjectBuffer == null) return;

                var remoteEvent = PapercutIPCommSerializer.FromJson(
                    request.Type,
                    Encoding.UTF8.GetString(remoteObjectBuffer));

                Logger.Information(
                    "Publishing Event Received {@Event} from Remote",
                    remoteEvent);

                await messageBus.PublishObjectAsync(
                    remoteEvent,
                    request.Type,
                    token: token);

                if (request.CommandType == PapercutIPCommCommandType.Exchange)
                {
                    // send response back...
                    Logger.Information(
                        "Exchanging Event {@Event} -- Pushing to Remote",
                        remoteEvent);

                    await Connection.SendJsonAsync(request.Type, remoteEvent);
                }
            }
        }
        catch (IOException e)
        {
            Logger.Error(e, "IOException received. Closing this connection.");
            Connection.Close();
        }
    }
}