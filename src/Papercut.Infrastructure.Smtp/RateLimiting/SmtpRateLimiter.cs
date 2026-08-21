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


namespace Papercut.Infrastructure.Smtp.RateLimiting;

/// <summary>
/// Counts messages accepted within a fixed window and decides whether the next one fits.
///
/// The count is global to the server rather than per-client: Papercut is a single
/// developer's mailbox, so "how many did my app send" is the question being asked.
/// The window is fixed rather than sliding -- once it expires the count resets
/// wholesale, which is how most hosted providers describe their own quotas.
///
/// Registered as a singleton; instances are safe to share across sessions.
/// </summary>
public sealed class SmtpRateLimiter(SmtpRateLimit rateLimit, TimeProvider timeProvider)
{
    private readonly Lock _sync = new();

    private int _count;

    private DateTimeOffset _windowStart;

    public SmtpRateLimiter(SmtpRateLimit rateLimit)
        : this(rateLimit, TimeProvider.System)
    {
    }

    public SmtpRateLimit Limit { get; } = rateLimit;

    /// <summary>
    /// Records an accepted message against the current window, or reports that the
    /// window is full. Callers that reject a message must not call this.
    /// </summary>
    public RateLimitDecision TryAcquire()
    {
        if (this.Limit.IsUnlimited)
        {
            return new RateLimitDecision(true, 0, TimeSpan.Zero);
        }

        var now = timeProvider.GetUtcNow();

        lock (this._sync)
        {
            var elapsed = now - this._windowStart;

            if (this._count == 0 || elapsed >= this.Limit.Window)
            {
                this._windowStart = now;
                this._count = 0;
                elapsed = TimeSpan.Zero;
            }

            if (this._count >= this.Limit.MaxMessages)
            {
                return new RateLimitDecision(false, this._count, this.Limit.Window - elapsed);
            }

            this._count++;

            return new RateLimitDecision(true, this._count, TimeSpan.Zero);
        }
    }
}
