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


using Autofac;

using MimeKit;
using Papercut.Core.Domain.Message;
using Papercut.Core.Domain.Rules;
using Papercut.Message;
using Papercut.Message.Helpers;
using Papercut.Rules.App.Relaying;
using Papercut.Rules.Domain.Conditional.Forwarding;

using Polly;

namespace Papercut.Rules.App.Conditional.Forwarding;

public class ConditionalForwardWithRetryRuleDispatch : IRuleDispatcher<ConditionalForwardWithRetryRule>
{
    private readonly ILogger _logger;

    private readonly Lazy<MimeMessageLoader> _mimeMessageLoader;

    public ConditionalForwardWithRetryRuleDispatch(Lazy<MimeMessageLoader> mimeMessageLoader, ILogger logger)
    {
        _mimeMessageLoader = mimeMessageLoader;
        _logger = logger;
    }

    public async Task DispatchAsync(ConditionalForwardWithRetryRule rule, MessageEntry messageEntry, CancellationToken token)
    {
        if (rule == null) throw new ArgumentNullException(nameof(rule));
        if (messageEntry == null) throw new ArgumentNullException(nameof(messageEntry));

        var message = await _mimeMessageLoader.Value.GetClonedAsync(messageEntry, token);

        if (!RuleMatches(rule, message))
        {
            return;
        }

        rule.PopulateFromRule(message);

        var polly = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                rule.RetryAttempts,
                (attempt) => TimeSpan.FromSeconds(rule.RetryAttemptDelaySeconds),
                (exception, span) =>
                {
                    _logger.Error(
                        exception,
                        "Failed to send {@MessageEntry} after {RetryAttempts}",
                        messageEntry,
                        rule.RetryAttempts);
                });

        async Task SendMessage()
        {
            using (var client = await rule.CreateConnectedSmtpClientAsync(token))
            {
                await client.SendAsync(message, token);
                await client.DisconnectAsync(true, token);
            }
        }

        await polly.ExecuteAsync(async () => await SendMessage());
    }

    protected virtual bool RuleMatches(ConditionalForwardWithRetryRule rule, MimeMessage mimeMessage)
    {
        return rule.IsConditionalForwardRuleMatch(mimeMessage);
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    static void Register(ContainerBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.RegisterType<ConditionalForwardWithRetryRuleDispatch>()
            .As<IRuleDispatcher<ConditionalForwardWithRetryRule>>().AsSelf().InstancePerDependency();
    }

    #endregion
}