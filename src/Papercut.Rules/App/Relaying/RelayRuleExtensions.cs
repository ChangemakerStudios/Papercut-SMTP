// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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


using MailKit.Net.Smtp;
using MailKit.Security;
using Papercut.Rules.Domain.Relaying;

namespace Papercut.Rules.App.Relaying;

public static class RelayRuleExtensions
{
    public static void PopulateServerFromUri(this RelayRule rule, string smtpServer)
    {
        if (rule == null) throw new ArgumentNullException(nameof(rule));

        var uri = new Uri("smtp://" + smtpServer);

        rule.SmtpServer = uri.DnsSafeHost;
        rule.SmtpPort = uri.IsDefaultPort ? 25 : uri.Port;
    }

    public static async Task<SmtpClient> CreateConnectedSmtpClientAsync(this RelayRule forwardRule, CancellationToken token)
    {
        if (forwardRule == null) throw new ArgumentNullException(nameof(forwardRule));

        var client = new SmtpClient();

        await client.ConnectAsync(
            forwardRule.SmtpServer,
            forwardRule.SmtpPort,
            forwardRule.SmtpUseSSL ? SecureSocketOptions.Auto : SecureSocketOptions.None,
            token);

        // Note: since we don't have an OAuth2 token, disable
        // the XOAUTH2 authentication mechanism.
        client.AuthenticationMechanisms.Remove("XOAUTH2");

        if (!string.IsNullOrWhiteSpace(forwardRule.SmtpPassword)
            && !string.IsNullOrWhiteSpace(forwardRule.SmtpUsername))
        {
            // Note: only needed if the SMTP server requires authentication
            await client.AuthenticateAsync(forwardRule.SmtpUsername, forwardRule.SmtpPassword, token);
        }

        return client;
    }
}