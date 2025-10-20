// Papercut
// 
// Copyright � 2008 - 2012 Ken Robertson
// Copyright � 2013 - 2025 Jaben Cargman
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


using MimeKit;
using Papercut.Core.Domain.Message;
using Papercut.Core.Domain.Rules;
using Papercut.Message;
using Papercut.Message.Helpers;
using Papercut.Rules.Domain.Relaying;

namespace Papercut.Rules.App.Relaying;

public abstract class BaseRelayRuleDispatch<T> : IRuleDispatcher<T>
    where T : RelayRule
{
    private readonly Lazy<IMimeMessageLoader> _mimeMessageLoader;

    protected BaseRelayRuleDispatch(Lazy<IMimeMessageLoader> mimeMessageLoader, ILogger logger)
    {
        Logger = logger;
        _mimeMessageLoader = mimeMessageLoader;
    }

    protected ILogger Logger { get; }

    public virtual async Task DispatchAsync(T rule, MessageEntry? messageEntry = null, CancellationToken token = default)
    {
        if (rule == null) throw new ArgumentNullException(nameof(rule));

        // Skip processing when messageEntry is null (e.g., periodic rules)
        if (messageEntry == null) return;

        var mimeMessage = await _mimeMessageLoader.Value.GetClonedAsync(messageEntry, token);

        if (!RuleMatches(rule, mimeMessage)) return;

        try
        {
            using var client = await rule.CreateConnectedSmtpClientAsync(token);

            rule.PopulateFromRule(mimeMessage);

            await client.SendAsync(mimeMessage, token);
            await client.DisconnectAsync(true, token);
        }
        catch (Exception ex)
        {
            HandleSendFailure(rule, messageEntry, ex);
        }
    }

    protected virtual bool RuleMatches(T rule, MimeMessage mimeMessage)
    {
        return true;
    }

    protected virtual void HandleSendFailure(T rule, MessageEntry messageEntry, Exception exception)
    {
        // log failure
        Logger.Error(exception, "Failure sending {@MessageEntry} for rule {@Rule}", messageEntry, rule);
    }
}