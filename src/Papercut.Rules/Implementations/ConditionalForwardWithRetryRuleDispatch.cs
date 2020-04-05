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

namespace Papercut.Rules.Implementations
{
    using System;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;

    using MimeKit;

    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Message;
    using Papercut.Core.Domain.Rules;
    using Papercut.Message;
    using Papercut.Message.Helpers;
    using Papercut.Rules.Helpers;

    using Serilog;

    [UsedImplicitly]
    public class ConditionalForwardWithRetryRuleDispatch : IRuleDispatcher<ConditionalForwardWithRetryRule>
    {
        private readonly Lazy<MimeMessageLoader> _mimeMessageLoader;
        private readonly ILogger _logger;

        public ConditionalForwardWithRetryRuleDispatch(Lazy<MimeMessageLoader> mimeMessageLoader, ILogger logger)
        {
            _mimeMessageLoader = mimeMessageLoader;
            _logger = logger;
        }

        public void Dispatch(ConditionalForwardWithRetryRule rule, MessageEntry messageEntry)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (messageEntry == null) throw new ArgumentNullException(nameof(messageEntry));

            var messageSource = _mimeMessageLoader.Value.GetObservable(messageEntry)
                .Select(m => m.CloneMessage())
                .Where(m => RuleMatches(rule, m))
                .Select(
                    m =>
                    {
                        rule.PopulateFromRule(m);
                        return m;
                    });

            var sendObservable = Observable.Create<bool>(o =>
            {
                IDisposable subscription = null;
                subscription = messageSource.Subscribe(x =>
                {
                    try
                    {
                        using (var client = rule.CreateConnectedSmtpClient())
                        {
                            client.Send(x);
                            client.Disconnect(true);
                            o.OnNext(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        o.OnError(ex);
                        subscription?.Dispose();
                    }
                }, o.OnError, o.OnCompleted);

                return subscription;
            });

            sendObservable.RetryWithDelay(rule.RetryAttempts, TimeSpan.FromSeconds(rule.RetryAttemptDelaySeconds))
                .Subscribe(
                    s =>
                    {
                        // success!
                    },
                    e =>
                    {
                        this._logger.Error(e, "Failed to send {@MessageEntry} after {RetryAttempts}", messageEntry, rule.RetryAttempts);
                    });
        }

        protected virtual bool RuleMatches(ConditionalForwardWithRetryRule rule, MimeMessage mimeMessage)
        {
            return rule.IsConditionalForwardRuleMatch(mimeMessage);
        }
    }
}