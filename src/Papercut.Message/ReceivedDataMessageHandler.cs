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


using Papercut.Common.Domain;

namespace Papercut.Message;

public class ReceivedDataMessageHandler : IReceivedDataHandler
{
    readonly ILogger _logger;

    readonly IMessageBus _messageBus;

    readonly MessageRepository _messageRepository;

    public ReceivedDataMessageHandler(
        MessageRepository messageRepository,
        IMessageBus messageBus,
        ILogger logger)
    {
        this._messageRepository = messageRepository;
        this._messageBus = messageBus;
        this._logger = logger;
    }

    public async Task HandleReceivedAsync(Stream messageData, IEnumerable<string> recipients)
    {
        var message = await MimeMessage.LoadAsync(ParserOptions.Default, messageData, true);

        var lookup = recipients.IfNullEmpty().ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        // remove TO:
        lookup.ExceptWith(message.To.Mailboxes.Select(s => s.Address));

        // remove CC:
        lookup.ExceptWith(message.Cc.Mailboxes.Select(s => s.Address));

        if (lookup.Any())
        {
            // Bcc is remaining, add to message
            foreach (var r in lookup)
            {
                message.Bcc.Add(MailboxAddress.Parse(r));
            }
        }

        var file = await this._messageRepository.SaveMessageAsync(async fs => await message.WriteToAsync(fs));

        try
        {
            if (!string.IsNullOrWhiteSpace(file))
                this._messageBus.Publish(new NewMessageEvent(new MessageEntry(file)));
        }
        catch (Exception ex)
        {
            this._logger.Error(ex, "Unable to publish new message event for message file: {MessageFile}", file);
        }
    }
}