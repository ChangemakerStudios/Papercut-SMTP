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


using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MimeKit;

using Papercut.Common.Domain;
using Papercut.Common.Extensions;
using Papercut.Core.Domain.Message;
using Papercut.Message;

using Serilog;

using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace Papercut.Service.Infrastructure.SmtpServer;

public class SmtpMessageStore : MessageStore
{
    private readonly ILogger _logger;

    private readonly IMessageBus _messageBus;

    private readonly MessageRepository _messageRepository;

    public SmtpMessageStore(MessageRepository messageRepository, IMessageBus messageBus, ILogger logger)
    {
        this._messageRepository = messageRepository;
        this._messageBus = messageBus;
        this._logger = logger;
    }

    public async Task HandleReceived(Stream messageData, IEnumerable<string> recipients)
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

    public override async Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        this._logger.Debug("Saving Message in the Message Store...");

        await using var stream = new MemoryStream();

        var position = buffer.GetPosition(0);

        while (buffer.TryGet(ref position, out var memory))
        {
            await stream.WriteAsync(memory, cancellationToken);
        }

        stream.Position = 0;

        await this.HandleReceived(
            stream,
            transaction.To.Select(s => s.AsAddress()));

        return SmtpResponse.Ok;
    }
}