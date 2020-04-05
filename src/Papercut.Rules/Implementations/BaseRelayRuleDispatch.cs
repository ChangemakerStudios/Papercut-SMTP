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
    using System.Linq;
    using System.Reactive.Linq;

    using Common.Extensions;

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
            Logger = logger;
            _mimeMessageLoader = mimeMessageLoader;
        }

        protected ILogger Logger { get; }

        public virtual void Dispatch([NotNull] T rule, [NotNull] MessageEntry messageEntry)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (messageEntry == null) throw new ArgumentNullException(nameof(messageEntry));

            var mimeMessage = _mimeMessageLoader.Value.Get(messageEntry).CloneMessage();

            if (!RuleMatches(rule, mimeMessage)) return;

            try
            {
                using (var client = rule.CreateConnectedSmtpClient())
                {
                    rule.PopulateFromRule(mimeMessage);
                    client.Send(mimeMessage);
                    client.Disconnect(true);
                }
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
}