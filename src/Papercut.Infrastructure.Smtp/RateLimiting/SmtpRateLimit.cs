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


using System.Globalization;

using Papercut.Common.Domain;

using SmtpServer.Protocol;

namespace Papercut.Infrastructure.Smtp.RateLimiting;

/// <summary>
/// Domain object representing a message reception rate limit for the SMTP server.
/// Immutable value object parsed from a "&lt;count&gt;/&lt;window&gt;" specification,
/// where window is a number followed by s, m, or h -- for example "500/1h" or "5/10m".
/// </summary>
public sealed class SmtpRateLimit
{
    private SmtpRateLimit()
    {
        this.IsUnlimited = true;
    }

    private SmtpRateLimit(int maxMessages, TimeSpan window, SmtpReplyCode replyCode)
    {
        this.MaxMessages = maxMessages;
        this.Window = window;
        this.ReplyCode = replyCode;
    }

    /// <summary>
    /// A rate limit that never rejects. This is the default.
    /// </summary>
    public static SmtpRateLimit Unlimited { get; } = new();

    public bool IsUnlimited { get; }

    /// <summary>
    /// Maximum messages accepted per window. Meaningless when <see cref="IsUnlimited" />.
    /// </summary>
    public int MaxMessages { get; }

    /// <summary>
    /// The length of the counting window. Meaningless when <see cref="IsUnlimited" />.
    /// </summary>
    public TimeSpan Window { get; }

    /// <summary>
    /// The SMTP reply code returned once the limit is hit. Defaults to 451.
    /// </summary>
    public SmtpReplyCode ReplyCode { get; } = SmtpReplyCode.Aborted;

    /// <summary>
    /// The text returned alongside <see cref="ReplyCode" />. 4xx codes are transient
    /// failures and get a 4.7.1 enhanced status; 5xx codes are permanent and get 5.7.1.
    /// </summary>
    public string ReplyMessage =>
        $"{((int)this.ReplyCode < 500 ? "4.7.1" : "5.7.1")} Message rate limit exceeded ({this.MaxMessages} per {DescribeWindow(this.Window)})";

    /// <summary>
    /// Creates an SmtpRateLimit from a string specification.
    /// </summary>
    /// <param name="rateLimitSpec">
    /// Rate limit in "&lt;count&gt;/&lt;window&gt;" form, where window is a number
    /// followed by s (seconds), m (minutes), or h (hours).
    /// Use "*", empty, or null for no limit (default).
    /// Examples: "500/1h", "5/10m", "100/30s"
    /// </param>
    /// <param name="replyCode">
    /// SMTP reply code returned when the limit is hit. Must be a 4xx or 5xx code.
    /// Defaults to 451 (transient, tells the client to retry later).
    /// </param>
    /// <returns>ExecutionResult containing the SmtpRateLimit or error details</returns>
    public static ExecutionResult<SmtpRateLimit> Create(string? rateLimitSpec, int replyCode = 451)
    {
        rateLimitSpec = string.IsNullOrWhiteSpace(rateLimitSpec) ? "*" : rateLimitSpec.Trim();

        if (rateLimitSpec == "*")
        {
            return ExecutionResult.Success(Unlimited);
        }

        var parts = rateLimitSpec.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 2)
        {
            return ExecutionResult.Failure<SmtpRateLimit>(
                $"Invalid rate limit '{rateLimitSpec}'. Expected '<count>/<window>' such as '500/1h', or '*' for no limit.");
        }

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var maxMessages)
            || maxMessages <= 0)
        {
            return ExecutionResult.Failure<SmtpRateLimit>(
                $"Invalid rate limit count '{parts[0]}'. Expected a positive whole number.");
        }

        var windowResult = ParseWindow(parts[1]);

        if (windowResult.IsFailed)
        {
            return ExecutionResult.Failure<SmtpRateLimit>(windowResult.Errors.ToArray());
        }

        if (replyCode is < 400 or > 599)
        {
            return ExecutionResult.Failure<SmtpRateLimit>(
                $"Invalid rate limit reply code '{replyCode}'. Expected a 4xx or 5xx SMTP reply code such as 421, 451, 452 or 550.");
        }

        return ExecutionResult.Success(
            new SmtpRateLimit(maxMessages, windowResult.Value, (SmtpReplyCode)replyCode));
    }

    private static ExecutionResult<TimeSpan> ParseWindow(string window)
    {
        var unit = window[^1];
        var value = window[..^1];

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            return ExecutionResult.Failure<TimeSpan>(
                $"Invalid rate limit window '{window}'. Expected a positive number followed by s, m, or h -- for example '10m'.");
        }

        return char.ToLowerInvariant(unit) switch
        {
            's' => FromUnits(amount, TimeSpan.TicksPerSecond, window),
            'm' => FromUnits(amount, TimeSpan.TicksPerMinute, window),
            'h' => FromUnits(amount, TimeSpan.TicksPerHour, window),
            _ => ExecutionResult.Failure<TimeSpan>(
                $"Invalid rate limit window unit '{unit}'. Expected s (seconds), m (minutes), or h (hours).")
        };
    }

    /// <summary>
    /// Converts a whole number of units into a TimeSpan, reporting a failure rather than
    /// throwing when the result would not fit. TimeSpan.FromHours(int.MaxValue) overflows,
    /// and an exception escaping here would take down service startup instead of falling
    /// back to no limit.
    /// </summary>
    private static ExecutionResult<TimeSpan> FromUnits(int amount, long ticksPerUnit, string window)
    {
        if (amount > TimeSpan.MaxValue.Ticks / ticksPerUnit)
        {
            return ExecutionResult.Failure<TimeSpan>(
                $"Rate limit window '{window}' is too large.");
        }

        return ExecutionResult.Success(new TimeSpan(amount * ticksPerUnit));
    }

    private static string DescribeWindow(TimeSpan window)
    {
        if (window.TotalHours >= 1 && window.TotalHours % 1 == 0)
        {
            return window.TotalHours == 1 ? "hour" : $"{window.TotalHours:0} hours";
        }

        if (window.TotalMinutes >= 1 && window.TotalMinutes % 1 == 0)
        {
            return window.TotalMinutes == 1 ? "minute" : $"{window.TotalMinutes:0} minutes";
        }

        return window.TotalSeconds == 1 ? "second" : $"{window.TotalSeconds:0} seconds";
    }

    public override string ToString()
    {
        return this.IsUnlimited
            ? "Unlimited"
            : $"{this.MaxMessages}/{DescribeWindow(this.Window)} ({(int)this.ReplyCode})";
    }
}
