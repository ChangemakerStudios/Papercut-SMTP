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

    using MailKit.Net.Smtp;

    using MimeKit;

    using Papercut.Core.Annotations;
    using Papercut.Core.Helper;
    using Papercut.Core.Message;

    public class ForwardRuleDispatch : IRuleDispatcher<ForwardRule>
    {
        readonly MimeMessageLoader _mimeMessageLoader;

        public ForwardRuleDispatch(MimeMessageLoader mimeMessageLoader)
        {
            _mimeMessageLoader = mimeMessageLoader;
        }

        public void Dispatch([NotNull] ForwardRule rule, [NotNull] MessageEntry messageEntry)
        {
            if (rule == null) throw new ArgumentNullException("rule");
            if (messageEntry == null) throw new ArgumentNullException("messageEntry");

            _mimeMessageLoader.Get(messageEntry)
                .Subscribe(
                    readOnlyMessage =>
                    {
                        MimeMessage message = readOnlyMessage.CloneMessage();

                        using (var client = new SmtpClient())
                        {
                            client.Connect(rule.SMTPServer, rule.SMTPPort, rule.SmtpUseSSL);

                            // Note: since we don't have an OAuth2 token, disable
                            // the XOAUTH2 authentication mechanism.
                            client.AuthenticationMechanisms.Remove("XOAUTH2");

                            if (!string.IsNullOrWhiteSpace(rule.SMTPPassword)
                                && !string.IsNullOrWhiteSpace(rule.SMTPUsername))
                            {
                                // Note: only needed if the SMTP server requires authentication
                                client.Authenticate(rule.SMTPUsername, rule.SMTPPassword);
                            }

                            if (!string.IsNullOrWhiteSpace(rule.FromEmail))
                            {
                                message.From.Clear();
                                message.From.Add(new MailboxAddress(rule.FromEmail, rule.FromEmail));
                            }

                            if (!string.IsNullOrWhiteSpace(rule.ToEmail))
                            {
                                message.To.Clear();
                                message.To.Add(new MailboxAddress(rule.ToEmail, rule.ToEmail));
                            }

                            client.Send(message);
                            client.Disconnect(true);
                        }
                    });
        }
    }
}