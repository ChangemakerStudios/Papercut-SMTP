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

    using MailKit.Net.Smtp;
    using MailKit.Security;

    using Papercut.Core.Annotations;

    public static class RelayRuleExtensions
    {
        public static SmtpClient CreateConnectedSmtpClient([NotNull] this RelayRule forwardRule)
        {
            if (forwardRule == null) throw new ArgumentNullException(nameof(forwardRule));

            var client = new SmtpClient();

            client.Connect(forwardRule.SmtpServer, forwardRule.SmtpPort, forwardRule.SmtpUseSSL ? SecureSocketOptions.Auto : SecureSocketOptions.None);

            // Note: since we don't have an OAuth2 token, disable
            // the XOAUTH2 authentication mechanism.
            client.AuthenticationMechanisms.Remove("XOAUTH2");

            if (!string.IsNullOrWhiteSpace(forwardRule.SmtpPassword)
                && !string.IsNullOrWhiteSpace(forwardRule.SmtpUsername))
            {
                // Note: only needed if the SMTP server requires authentication
                client.Authenticate(forwardRule.SmtpUsername, forwardRule.SmtpPassword);
            }

            return client;
        }
    }
}