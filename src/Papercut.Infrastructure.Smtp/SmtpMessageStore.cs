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


namespace Papercut.Infrastructure.Smtp
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Papercut.Common.Extensions;
    using Papercut.Core.Domain.Message;

    using global::SmtpServer;
    using global::SmtpServer.Mail;
    using global::SmtpServer.Protocol;
    using global::SmtpServer.Storage;

    using Papercut.Core.Annotations;

    public class SmtpMessageStore : MessageStore
    {
        private readonly IReceivedDataHandler _receivedDataHandler;

        public SmtpMessageStore(IReceivedDataHandler receivedDataHandler)
        {
            this._receivedDataHandler = receivedDataHandler;
        }

        public override async Task<SmtpResponse> SaveAsync(
            ISessionContext context,
            IMessageTransaction transaction,
            CancellationToken cancellationToken)
        {
            var textMessage = (ITextMessage)transaction.Message;

            this._receivedDataHandler.HandleReceived(
                await ToArrayAsync(textMessage.Content),
                transaction.To.Select(s => s.AsAddress()).ToArray());

            return SmtpResponse.Ok;
        }

        public static async Task<byte[]> ToArrayAsync([NotNull] Stream input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            using (MemoryStream ms = new MemoryStream())
            {
                await input.CopyToAsync(ms);

                return ms.ToArray();
            }
        }
    }
}