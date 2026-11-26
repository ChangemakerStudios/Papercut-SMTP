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

using Papercut.Core.Domain.Rules;
using Papercut.Rules.Domain.Invoking;
using Papercut.Rules.Domain.Relaying;

namespace Papercut.Rules.App;

/// <summary>
///     Checks applied when a rule set arrives from outside the process (the
///     service's rules API). The service has no authentication, so a rule
///     update can come from anyone who reaches its HTTP port.
/// </summary>
public static class RuleUpdatePolicy
{
    /// <summary>
    ///     Invoke Process rules in <paramref name="incoming" /> that do not
    ///     exactly match one already in <paramref name="existing" />. Keeping
    ///     an unchanged stored rule is allowed so the rest of the set can
    ///     still be edited; adding or changing one is what the setting gates.
    /// </summary>
    public static IReadOnlyList<InvokeProcessRule> FindNewInvokeProcessRules(
        IEnumerable<IRule> incoming,
        IEnumerable<IRule> existing)
    {
        var stored = existing.OfType<InvokeProcessRule>().ToList();

        return incoming.OfType<InvokeProcessRule>()
            .Where(rule => !stored.Any(s => IsSameInvocation(s, rule)))
            .ToList();
    }

    /// <summary>
    ///     The stored password for a relay-family rule that was sent without
    ///     one, or null. It is only reused when the rule still connects to the
    ///     same server, port, SSL mode and username: otherwise a caller could
    ///     point the rule at their own server and receive the password in the
    ///     SMTP AUTH exchange.
    /// </summary>
    public static string? FindStoredSmtpPassword(
        RelayRule incoming,
        Guid? incomingId,
        IEnumerable<IRule> existing)
    {
        var candidates = existing.OfType<RelayRule>()
            .Where(s => !string.IsNullOrEmpty(s.SmtpPassword) && IsSameConnection(s, incoming))
            .ToList();

        var match = candidates.FirstOrDefault(s => incomingId.HasValue && s.Id == incomingId.Value)
                    ?? candidates.FirstOrDefault();

        return match?.SmtpPassword;
    }

    /// <summary>
    ///     Removes Invoke Process rules unless they are enabled, so a rule
    ///     stored before the setting existed (or arriving by another path)
    ///     does not run.
    /// </summary>
    public static TRule[] WithoutInvokeProcessRules<TRule>(IEnumerable<TRule> rules, bool invokeProcessEnabled)
        where TRule : IRule
    {
        return rules.Where(r => invokeProcessEnabled || r is not InvokeProcessRule).ToArray();
    }

    private static bool IsSameInvocation(InvokeProcessRule a, InvokeProcessRule b)
    {
        return string.Equals(a.ProcessToRun ?? string.Empty, b.ProcessToRun ?? string.Empty, StringComparison.Ordinal)
               && string.Equals(a.ProcessCommandLine ?? string.Empty, b.ProcessCommandLine ?? string.Empty, StringComparison.Ordinal);
    }

    private static bool IsSameConnection(RelayRule a, RelayRule b)
    {
        return string.Equals((a.SmtpServer ?? string.Empty).Trim(), (b.SmtpServer ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)
               && a.SmtpPort == b.SmtpPort
               && a.SmtpUseSSL == b.SmtpUseSSL
               && string.Equals(a.SmtpUsername ?? string.Empty, b.SmtpUsername ?? string.Empty, StringComparison.Ordinal);
    }
}
