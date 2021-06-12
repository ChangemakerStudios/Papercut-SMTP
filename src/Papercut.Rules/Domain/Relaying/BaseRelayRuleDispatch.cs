// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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


namespace Papercut.Rules.Domain.Relaying
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using MimeKit;

    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Message;
    using Papercut.Core.Domain.Rules;
    using Papercut.Message;
    using Papercut.Message.Helpers;

    using Serilog;

    public abstract class BaseRelayRuleDispatch<T> : IRuleDispatcher<T>
        where T : RelayRule
    {
        private readonly Lazy<MimeMessageLoader> _mimeMessageLoader;

        protected BaseRelayRuleDispatch(Lazy<MimeMessageLoader> mimeMessageLoader, ILogger logger)
        {
            this.Logger = logger;
            this._mimeMessageLoader = mimeMessageLoader;
        }

        protected ILogger Logger { get; }

        public virtual async Task DispatchAsync([NotNull] T rule, [NotNull] MessageEntry messageEntry, CancellationToken token = default)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (messageEntry == null) throw new ArgumentNullException(nameof(messageEntry));

            var mimeMessage = await this._mimeMessageLoader.Value.GetClonedAsync(messageEntry, token);

            if (!this.RuleMatches(rule, mimeMessage)) return;

            try
            {
                using (var client = await rule.CreateConnectedSmtpClientAsync(token))
                {
                    rule.PopulateFromRule(mimeMessage);
                    await client.SendAsync(mimeMessage, token);
                    await client.DisconnectAsync(true, token);
                }
            }
            catch (Exception ex)
            {
                this.HandleSendFailure(rule, messageEntry, ex);
            }
        }

        protected virtual bool RuleMatches(T rule, MimeMessage mimeMessage)
        {
            return true;
        }

        protected virtual void HandleSendFailure(T rule, MessageEntry messageEntry, Exception exception)
        {
            // log failure
            this.Logger.Error(exception, "Failure sending {@MessageEntry} for rule {@Rule}", messageEntry, rule);
        }
    }
}