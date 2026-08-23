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


namespace Papercut.Service.Application.Messages;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// SignalR hub for real-time message notifications
/// </summary>
public class MessagesHub(ILogger logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        logger.Debug("SignalR client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.Debug("SignalR client disconnected: {ConnectionId}", Context.ConnectionId);
        if (exception != null)
        {
            logger.Warning(exception, "SignalR client disconnected with exception: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join the messages group to receive message notifications
    /// </summary>
    public async Task JoinMessagesGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Messages");
        logger.Debug("Client {ConnectionId} joined Messages group", Context.ConnectionId);
    }

    /// <summary>
    /// Leave the messages group
    /// </summary>
    public async Task LeaveMessagesGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Messages");
        logger.Debug("Client {ConnectionId} left Messages group", Context.ConnectionId);
    }
}
