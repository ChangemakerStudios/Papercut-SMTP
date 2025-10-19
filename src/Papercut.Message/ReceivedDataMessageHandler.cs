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


using MimeKit;

using Papercut.Common.Domain;
using Papercut.Core.Domain.Message;

namespace Papercut.Message;

public class ReceivedDataMessageHandler : IReceivedDataHandler
{
    private readonly IMessageRepository _messageRepository;

    private readonly IMessageBus _messageBus;

    private readonly ILogger _logger;

    public ReceivedDataMessageHandler(IMessageRepository messageRepository,
        IMessageBus messageBus,
        ILogger logger)
    {
        _messageRepository = messageRepository;
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task HandleReceivedAsync(
        byte[] messageData,
        string[] recipients)
    {
        ArgumentNullException.ThrowIfNull(messageData);
        ArgumentNullException.ThrowIfNull(recipients);

        string file;

        using (var ms = new MemoryStream(messageData))
        {
            var message = await MimeMessage.LoadAsync(ParserOptions.Default, ms, true);

            var lookup = recipients.ToHashSet(StringComparer.CurrentCultureIgnoreCase);

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

            file = await _messageRepository.SaveMessage(message.Subject ?? string.Empty, async fs => await message.WriteToAsync(fs));
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(file) && File.Exists(file))
                await _messageBus.PublishAsync(new NewMessageEvent(new MessageEntry(file)));
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "Unable to publish new message event for message file: {MessageFile}", file);
        }
    }
}