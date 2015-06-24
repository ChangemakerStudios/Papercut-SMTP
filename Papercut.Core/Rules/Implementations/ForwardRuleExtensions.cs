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

    using MailKit.Net.Smtp;

    using MimeKit;

    using Papercut.Core.Annotations;

    public static class ForwardRuleExtensions
    {
        public static void PopulateFromRule(
            [NotNull] this MimeMessage mimeMessage,
            [NotNull] ForwardRule forwardRule)
        {
            if (forwardRule == null) throw new ArgumentNullException("forwardRule");
            if (mimeMessage == null) throw new ArgumentNullException("mimeMessage");

            if (!string.IsNullOrWhiteSpace(forwardRule.FromEmail))
            {
                mimeMessage.From.Clear();
                mimeMessage.From.Add(
                    new MailboxAddress(forwardRule.FromEmail, forwardRule.FromEmail));
            }

            if (!string.IsNullOrWhiteSpace(forwardRule.ToEmail))
            {
                mimeMessage.To.Clear();
                mimeMessage.Bcc.Clear();
                mimeMessage.Cc.Clear();
                mimeMessage.To.Add(new MailboxAddress(forwardRule.ToEmail, forwardRule.ToEmail));
            }
        }

        public static SmtpClient CreateConnectedSmtpClient([NotNull] this ForwardRule forwardRule)
        {
            if (forwardRule == null) throw new ArgumentNullException("forwardRule");

            var client = new SmtpClient();

            client.Connect(forwardRule.SMTPServer, forwardRule.SMTPPort, forwardRule.SmtpUseSSL);

            // Note: since we don't have an OAuth2 token, disable
            // the XOAUTH2 authentication mechanism.
            client.AuthenticationMechanisms.Remove("XOAUTH2");

            if (!string.IsNullOrWhiteSpace(forwardRule.SMTPPassword)
                && !string.IsNullOrWhiteSpace(forwardRule.SMTPUsername))
            {
                // Note: only needed if the SMTP server requires authentication
                client.Authenticate(forwardRule.SMTPUsername, forwardRule.SMTPPassword);
            }

            return client;
        }
    }
}