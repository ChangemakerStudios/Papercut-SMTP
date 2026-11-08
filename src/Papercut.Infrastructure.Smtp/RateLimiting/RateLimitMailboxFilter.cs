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
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace Papercut.Infrastructure.Smtp.RateLimiting;

using ILogger = Serilog.ILogger;

/// <summary>
/// Mailbox filter that enforces a message reception rate limit, letting developers
/// test how their application behaves against a mail server that is throttling them.
///
/// Returning false from CanAcceptFromAsync would reject with a hardcoded 550, so this
/// throws SmtpResponseException instead -- the SmtpServer session loop writes the
/// carried response back to the client verbatim, which is what allows a configurable
/// reply code (421/451/452/550).
/// </summary>
internal sealed class RateLimitMailboxFilter(SmtpRateLimiter rateLimiter, ILogger logger) : IMailboxFilter
{
    public Task<bool> CanAcceptFromAsync(
        ISessionContext context,
        IMailbox from,
        int size,
        CancellationToken cancellationToken)
    {
        var decision = rateLimiter.TryAcquire();

        if (decision.IsAllowed)
        {
            logger.Verbose(
                "SMTP message accepted against rate limit {RateLimit} ({Count} so far this window)",
                rateLimiter.Limit,
                decision.Count);

            return Task.FromResult(true);
        }

        logger.Warning(
            "Rejected SMTP MAIL FROM command from {RemoteIp} with {ReplyCode} - rate limit {RateLimit} reached, resets in {RetryAfter}",
            context.GetRemoteIpAddress(),
            (int)rateLimiter.Limit.ReplyCode,
            rateLimiter.Limit,
            decision.RetryAfter);

        // quit: true closes the session after the reply. Without it the session loop
        // appends ", N retry(ies) remaining." to the message, which is noise in a
        // response the client is meant to parse.
        throw new SmtpResponseException(
            new SmtpResponse(rateLimiter.Limit.ReplyCode, rateLimiter.Limit.ReplyMessage),
            quit: true);
    }

    public Task<bool> CanDeliverToAsync(
        ISessionContext context,
        IMailbox to,
        IMailbox from,
        CancellationToken cancellationToken)
    {
        // The limit counts messages, not recipients -- it is applied once at MAIL FROM.
        return Task.FromResult(true);
    }
}
