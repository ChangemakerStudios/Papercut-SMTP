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


using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Storage;

namespace Papercut.Infrastructure.Smtp;

/// <summary>
/// Runs several mailbox filters in order, stopping at the first rejection.
///
/// SmtpServer ships an equivalent internally but does not expose it, and the
/// DelegatingMailboxFilterFactory hook Papercut uses supplies exactly one filter.
/// </summary>
internal sealed class ChainedMailboxFilter(IReadOnlyList<IMailboxFilter> filters) : IMailboxFilter
{
    public async Task<bool> CanAcceptFromAsync(
        ISessionContext context,
        IMailbox from,
        int size,
        CancellationToken cancellationToken)
    {
        foreach (var filter in filters)
        {
            if (!await filter.CanAcceptFromAsync(context, from, size, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }
        }

        return true;
    }

    public async Task<bool> CanDeliverToAsync(
        ISessionContext context,
        IMailbox to,
        IMailbox from,
        CancellationToken cancellationToken)
    {
        foreach (var filter in filters)
        {
            if (!await filter.CanDeliverToAsync(context, to, from, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }
        }

        return true;
    }
}
