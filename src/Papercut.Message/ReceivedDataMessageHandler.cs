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

namespace Papercut.Message
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using MimeKit;

    using Papercut.Common.Domain;
    using Papercut.Common.Extensions;
    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Message;

    using Serilog;

    public class ReceivedDataMessageHandler : IReceivedDataHandler
    {
        readonly ILogger _logger;

        readonly MessageRepository _messageRepository;

        readonly IMessageBus _messageBus;

        public ReceivedDataMessageHandler(MessageRepository messageRepository,
            IMessageBus messageBus,
            ILogger logger)
        {
            _messageRepository = messageRepository;
            this._messageBus = messageBus;
            _logger = logger;
        }

        public void HandleReceived([NotNull] byte[] messageData, [NotNull] string[] recipients)
        {
            if (messageData == null) throw new ArgumentNullException(nameof(messageData));
            if (recipients == null) throw new ArgumentNullException(nameof(recipients));

            string file;

            using (var ms = new MemoryStream(messageData))
            {
                var message = MimeMessage.Load(ParserOptions.Default, ms, true);

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

                file = _messageRepository.SaveMessage(message.Subject, fs => message.WriteTo(fs));
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(file))
                    this._messageBus.Publish(new NewMessageEvent(new MessageEntry(file)));
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Unable to publish new message event for message file: {MessageFile}", file);
            }
        }
    }
}
