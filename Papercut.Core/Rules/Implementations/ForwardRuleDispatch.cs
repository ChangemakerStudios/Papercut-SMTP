// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2015 Jaben Cargman
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
    using System.Reactive.Linq;

    using MailKit.Net.Smtp;

    using Papercut.Core.Annotations;
    using Papercut.Core.Helper;
    using Papercut.Core.Message;

    public class ForwardRuleDispatch : IRuleDispatcher<ForwardRule>
    {
        readonly Lazy<MimeMessageLoader> _mimeMessageLoader;

        public ForwardRuleDispatch(Lazy<MimeMessageLoader> mimeMessageLoader)
        {
            _mimeMessageLoader = mimeMessageLoader;
        }

        public void Dispatch([NotNull] ForwardRule rule, [NotNull] MessageEntry messageEntry)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (messageEntry == null) throw new ArgumentNullException(nameof(messageEntry));

            _mimeMessageLoader.Value.Get(messageEntry)
                .Select(m => m.CloneMessage())
                .Subscribe(
                    m =>
                    {
                        using (SmtpClient client = rule.CreateConnectedSmtpClient())
                        {
                            m.PopulateFromRule(rule);
                            client.Send(m);
                            client.Disconnect(true);
                        }
                    },
                    e =>
                    {
                        // NOOP
                    });
        }
    }
}