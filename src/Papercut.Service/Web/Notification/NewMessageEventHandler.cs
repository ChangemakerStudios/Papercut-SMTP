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


using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Papercut.Common.Domain;
using Papercut.Core.Domain.Message;
using Papercut.Message;
using Papercut.Message.Helpers;
using Papercut.Service.Web.Models;

namespace Papercut.Service.Web.Notification;

public class NewMessageEventHandler: IEventHandler<NewMessageEvent>
{
    private readonly IHubContext<NewMessagesHub> _hubContext;

    private readonly ILogger _logger;

    private readonly MimeMessageLoader _messageLoader;

    public NewMessageEventHandler(IHubContext<NewMessagesHub> hubContext, MimeMessageLoader messageLoader, ILogger<NewMessageEventHandler> logger)
    {
        this._hubContext = hubContext;
        this._messageLoader = messageLoader;
        this._logger = logger;
        }

    public void Handle(NewMessageEvent messageEvent)
    {
            var newMessageObj = MimeMessageEntry.RefDto.CreateFrom(
                                    new MimeMessageEntry(messageEvent.NewMessage,
                                        this._messageLoader.LoadMailMessage(messageEvent.NewMessage)));

            this._logger.LogInformation($"New message '{newMessageObj.Id}' has received. Notifying subscribed clients.");
            this._hubContext.Clients.All.SendAsync("new-message-received", newMessageObj);
        }
}