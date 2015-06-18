// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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

namespace Papercut.Core.Rules.Implementations
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Text.RegularExpressions;

    using MailKit.Net.Smtp;

    using MimeKit;

    using Papercut.Core.Annotations;
    using Papercut.Core.Helper;
    using Papercut.Core.Message;

    public class ConditionalForwardRuleDispatch : IRuleDispatcher<ConditionalForwardRule>
    {
        readonly Lazy<MimeMessageLoader> _mimeMessageLoader;

        public ConditionalForwardRuleDispatch(Lazy<MimeMessageLoader> mimeMessageLoader)
        {
            _mimeMessageLoader = mimeMessageLoader;
        }

        public void Dispatch([NotNull] ConditionalForwardRule rule, [NotNull] MessageEntry messageEntry)
        {
            if (rule == null)
                throw new ArgumentNullException("rule");
            if (messageEntry == null)
                throw new ArgumentNullException("messageEntry");

            _mimeMessageLoader.Value.Get(messageEntry)
                .Select(m => m.CloneMessage())
                .Where(m => RuleMatches(rule, m))
                .Subscribe(m =>
                {
                    using (SmtpClient client = rule.CreateConnectedSmtpClient())
                    {
                        m.PopulateFromRule(rule);
                        client.Send(m);
                        client.Disconnect(true);
                    }
                }, e => { 
                    // NOOP -- exception is logged in the message loader
                });
        }

        bool RuleMatches(ConditionalForwardRule rule, MimeMessage mimeMessage)
        {
            if (rule.RegexHeaderMatch.IsSet())
            {
                string allHeaders = string.Join("\r\n", mimeMessage.Headers.Select(h => h.ToString()));

                if (!IsMatch(rule.RegexHeaderMatch, allHeaders))
                    return false;
            }

            if (rule.RegexBodyMatch.IsSet())
            {
                string bodyText = string.Join("\r\n",
                    mimeMessage.BodyParts.OfType<TextPart>().Where(s => !s.IsAttachment));

                if (!IsMatch(rule.RegexBodyMatch, bodyText))
                    return false;
            }

            return true;
        }

        public bool IsMatch(string match, string searchText)
        {
            var regex = new Regex(match, RegexOptions.IgnoreCase);
            return regex.IsMatch(searchText);
        }
    }
}