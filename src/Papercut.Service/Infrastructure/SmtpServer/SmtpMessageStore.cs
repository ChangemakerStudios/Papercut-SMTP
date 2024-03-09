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


using System.Buffers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Papercut.Core.Domain.Message;

using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace Papercut.Service.Infrastructure.SmtpServer;

public class SmtpMessageStore : MessageStore
{
    #region Fields

    private readonly IReceivedDataHandler _receivedDataHandler;

    #endregion

    #region Constructors and Destructors

    public SmtpMessageStore(IReceivedDataHandler receivedDataHandler)
    {
            this._receivedDataHandler = receivedDataHandler;
        }

    #endregion

    #region Public Methods and Operators

    public override Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
            this._receivedDataHandler.HandleReceived(
                buffer.ToArray(),
                transaction.To.Select(s => s.AsAddress()).ToArray());

            return Task.FromResult(SmtpResponse.Ok);
        }

    #endregion
}