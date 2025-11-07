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


using System.Net;

using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Storage;

namespace Papercut.Infrastructure.Smtp;

using ILogger = Serilog.ILogger;

/// <summary>
/// Mailbox filter that validates IP addresses against the allowlist before accepting SMTP connections.
/// This is the proper pattern for connection rejection in the SmtpServer library.
/// Returning false from CanAcceptFromAsync will reject the connection with an appropriate SMTP error.
/// </summary>
internal sealed class IpAllowlistMailboxFilter(IPAllowedList ipAllowedList, ILogger logger) : IMailboxFilter
{
    public Task<bool> CanAcceptFromAsync(
        ISessionContext context,
        IMailbox from,
        int size,
        CancellationToken cancellationToken)
    {
        var remoteIp = context.GetRemoteIpAddress();

        // Skip validation if we couldn't determine the remote IP (IPAddress.None)
        if (remoteIp.Equals(IPAddress.None))
        {
            logger.Verbose("SMTP connection remote IP unknown, allowing");
            return Task.FromResult(true);
        }

        // Validate IP address against allowlist
        if (!ipAllowedList.IsAllowed(remoteIp))
        {
            logger.Warning(
                "Rejected SMTP MAIL FROM command from {RemoteIp} - IP not in allowlist",
                remoteIp);

            return Task.FromResult(false);
        }

        logger.Verbose("SMTP connection from {RemoteIp} validated against allowlist", remoteIp);
        return Task.FromResult(true);
    }

    public Task<bool> CanDeliverToAsync(
        ISessionContext context,
        IMailbox to,
        IMailbox from,
        CancellationToken cancellationToken)
    {
        // IP validation only applies to the connection, not individual recipients
        // Once MAIL FROM is accepted, allow delivery to any recipient
        return Task.FromResult(true);
    }
}
