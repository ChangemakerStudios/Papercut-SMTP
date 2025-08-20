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

using Domain.Messages;

using Microsoft.AspNetCore.SignalR;

public class NewMessageEventHandler(
    IHubContext<MessagesHub> hubContext,
    MimeMessageLoader messageLoader,
    ILogger logger)
    : IEventHandler<NewMessageEvent>
{
    public async Task HandleAsync(NewMessageEvent @event, CancellationToken token = default)
    {
        try
        {
            // Load the message to create a RefDto
            var mimeMessage = await messageLoader.GetAsync(@event.NewMessage, token);
            if (mimeMessage == null)
            {
                logger.Warning("Failed to load message for SignalR notification: {MessageFile}", @event.NewMessage.File);
                return;
            }

            var messageEntry = new MimeMessageEntry(@event.NewMessage, mimeMessage);
            var messageDto = RefDto.CreateFrom(messageEntry);

            logger.Information("New message '{MessageId}' received. Notifying SignalR clients.", messageDto.Id);

            // Send notification to all clients in the Messages group
            await hubContext.Clients.Group("Messages").SendAsync("NewMessageReceived", messageDto, token);
            
            // Also send a general refresh signal
            await hubContext.Clients.Group("Messages").SendAsync("MessageListChanged", token);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error sending SignalR notification for new message: {MessageFile}", @event.NewMessage.File);
        }
    }
}