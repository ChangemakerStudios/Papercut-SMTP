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

    using Papercut.Core.Annotations;
    using Papercut.Core.Message;
    using Papercut.Core.Network;

    public class ForwardRuleDispatch : IRuleDispatcher<ForwardRule>
    {
        readonly MessageRepository _messageRepository;

        public ForwardRuleDispatch(MessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public void Dispatch([NotNull] ForwardRule rule, [NotNull] MessageEntry messageEntry)
        {
            if (rule == null) throw new ArgumentNullException("rule");
            if (messageEntry == null) throw new ArgumentNullException("messageEntry");

            // send message...
            var session = new SmtpSession
            {
                MailFrom = rule.FromEmail,
                Sender = rule.SmtpServer
            };
            session.Recipients.Add(rule.ToEmail);
            session.Message = _messageRepository.GetMessage(messageEntry);

            new SmtpClient(session).Send();
        }
    }
}